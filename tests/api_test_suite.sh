#!/usr/bin/env bash
# ==============================================================================
# Sensei - Automated API Regression & QA Test Suite
# ==============================================================================
# Tests core Minimal API endpoints on .NET 8 backend:
#   - GET  /health & /api/health
#   - POST /api/auth/login & /api/auth/logout & GET /api/auth/me
#   - GET  /api/questions (with query filters)
#   - POST, PATCH, DELETE /api/tasks (Lifecycle: Create -> Toggle -> Delete)
#   - POST /api/mock-tests/start & submit & GET /api/mock-tests/{sessionId}
#   - GET  /api/modules/formula-vault & categories
#   - GET  /api/stats/overview, advanced, heatmap, weak-topics
#
# Validates HTTP status codes, JSON schema assertions, and latency benchmarks.
# ==============================================================================

BASE_URL="${SENSEI_BASE_URL:-http://localhost:5000}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COOKIE_JAR="$(mktemp /tmp/sensei_cookie_XXXXXX.txt)"

# Clean up cookie jar on exit
trap 'rm -f "$COOKIE_JAR"' EXIT

# Colors
CLR_RESET="\033[0m"
CLR_BOLD="\033[1m"
CLR_DIM="\033[2m"
CLR_RED="\033[0;31m"
CLR_GREEN="\033[0;32m"
CLR_YELLOW="\033[0;33m"
CLR_BLUE="\033[0;34m"
CLR_MAGENTA="\033[0;35m"
CLR_CYAN="\033[0;36m"
CLR_WHITE="\033[1;37m"

# Metrics & Benchmark Tracking
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0
declare -a LATENCIES=()
declare -a TEST_NAMES=()
FASTEST_LATENCY=999999
FASTEST_TEST=""
SLOWEST_LATENCY=0
SLOWEST_TEST=""

# Ensure backend runtime environment if running locally
if [ -d "$SCRIPT_DIR/dotnet/usr/lib/dotnet" ]; then
    export DOTNET_ROOT="$SCRIPT_DIR/dotnet/usr/lib/dotnet"
    export PATH="$DOTNET_ROOT:$PATH"
fi

echo -e "${CLR_CYAN}${CLR_BOLD}========================================================================${CLR_RESET}"
echo -e "${CLR_WHITE}${CLR_BOLD}       🥋 Sensei - Automated API Test & QA Suite (${BASE_URL})        ${CLR_RESET}"
echo -e "${CLR_CYAN}${CLR_BOLD}========================================================================${CLR_RESET}"
echo -e "${CLR_DIM}Timestamp: $(date -u '+%Y-%m-%d %H:%M:%SZ')${CLR_RESET}\n"

# ------------------------------------------------------------------------------
# 1. Backend Health & Liveness Verification
# ------------------------------------------------------------------------------
echo -e "${CLR_BOLD}🔍 Checking Backend Service Status...${CLR_RESET}"
if ! curl -s -f "$BASE_URL/health" > /dev/null 2>&1; then
    echo -e "${CLR_YELLOW}Backend is not responding at $BASE_URL. Attempting to start...${CLR_RESET}"
    if [ -f "$SCRIPT_DIR/backend/Program.cs" ]; then
        (cd "$SCRIPT_DIR/backend" && setsid dotnet run > "$SCRIPT_DIR/backend.log" 2>&1 &)
        echo -n "Waiting for backend to spin up"
        for i in {1..25}; do
            if curl -s -f "$BASE_URL/health" > /dev/null 2>&1; then
                echo -e " ${CLR_GREEN}✅ Ready!${CLR_RESET}"
                break
            fi
            echo -n "."
            sleep 1
        done
        if ! curl -s -f "$BASE_URL/health" > /dev/null 2>&1; then
            echo -e "\n${CLR_RED}❌ Failed to start backend service. Check $SCRIPT_DIR/backend.log${CLR_RESET}"
            exit 1
        fi
    else
        echo -e "${CLR_RED}❌ Backend directory not found at $SCRIPT_DIR/backend.${CLR_RESET}"
        exit 1
    fi
else
    echo -e "${CLR_GREEN}✅ Backend is live and healthy.${CLR_RESET}\n"
fi

# ------------------------------------------------------------------------------
# Test Runner Utility Function
# ------------------------------------------------------------------------------
# Args:
#   1: Test Name
#   2: HTTP Method (GET, POST, PATCH, DELETE)
#   3: Endpoint Path
#   4: JSON Payload (or empty string)
#   5: Expected HTTP Code Regex (e.g., "200" or "200|201")
#   6: JQ Validation Expression (must return boolean 'true')
#   7: Extra Curl flags (e.g. cookie flags)
# Output Variable:
#   LAST_RESPONSE_BODY (global)
# ------------------------------------------------------------------------------
LAST_RESPONSE_BODY=""

run_test() {
    local test_name="$1"
    local method="$2"
    local path="$3"
    local payload="$4"
    local expected_code_regex="$5"
    local jq_expr="$6"
    local extra_curl_args="$7"

    TOTAL_TESTS=$((TOTAL_TESTS + 1))
    local url="${BASE_URL}${path}"

    # Build curl command
    local curl_cmd=(curl -s -S -w "\n%{http_code}\n%{time_total}" -X "$method" "$url")
    if [ -n "$payload" ]; then
        curl_cmd+=(-H "Content-Type: application/json" -d "$payload")
    fi
    if [ -n "$extra_curl_args" ]; then
        # shellcheck disable=SC2206
        curl_cmd+=($extra_curl_args)
    fi

    local response
    response=$("${curl_cmd[@]}" 2>&1)
    local curl_exit=$?

    if [ $curl_exit -ne 0 ]; then
        FAILED_TESTS=$((FAILED_TESTS + 1))
        echo -e "${CLR_RED}[FAIL]${CLR_RESET} ${CLR_BOLD}${test_name}${CLR_RESET}"
        echo -e "       ${CLR_RED}Curl execution failed with exit code $curl_exit: $response${CLR_RESET}"
        return 1
    fi

    # Extract time_total and http_code from last two lines
    local latency_sec
    latency_sec=$(echo "$response" | tail -n 1)
    local http_code
    http_code=$(echo "$response" | tail -n 2 | head -n 1)
    local body
    body=$(echo "$response" | sed '$d' | sed '$d')
    LAST_RESPONSE_BODY="$body"

    # Latency calculation in ms
    local latency_ms
    latency_ms=$(awk -v t="$latency_sec" 'BEGIN { printf "%.2f", t * 1000 }')
    LATENCIES+=("$latency_ms")
    TEST_NAMES+=("$test_name")

    # Benchmarks comparison
    if awk -v a="$latency_ms" -v b="$FASTEST_LATENCY" 'BEGIN { exit (a < b ? 0 : 1) }'; then
        FASTEST_LATENCY="$latency_ms"
        FASTEST_TEST="$test_name"
    fi
    if awk -v a="$latency_ms" -v b="$SLOWEST_LATENCY" 'BEGIN { exit (a > b ? 0 : 1) }'; then
        SLOWEST_LATENCY="$latency_ms"
        SLOWEST_TEST="$test_name"
    fi

    # Latency styling
    local latency_color="$CLR_GREEN"
    if awk -v t="$latency_ms" 'BEGIN { exit (t > 200 ? 0 : 1) }'; then
        latency_color="$CLR_RED"
    elif awk -v t="$latency_ms" 'BEGIN { exit (t > 50 ? 0 : 1) }'; then
        latency_color="$CLR_YELLOW"
    fi

    # Validation 1: HTTP Status Code
    local code_pass=0
    if [[ "$http_code" =~ ^($expected_code_regex)$ ]]; then
        code_pass=1
    fi

    # Validation 2: JSON Structure & Assertions
    local json_valid=0
    local jq_pass=0
    local jq_err=""
    if echo "$body" | jq empty > /dev/null 2>&1; then
        json_valid=1
        if [ -n "$jq_expr" ]; then
            local jq_result
            jq_result=$(echo "$body" | jq -e "$jq_expr" 2>&1)
            if [ $? -eq 0 ] && [ "$jq_result" != "false" ] && [ "$jq_result" != "null" ]; then
                jq_pass=1
            else
                jq_err="$jq_result"
            fi
        else
            jq_pass=1
        fi
    else
        # If response body is empty and expected (e.g., 204 or certain DELETEs), handle gracefully
        if [ -z "$body" ] && [ -z "$jq_expr" ]; then
            json_valid=1
            jq_pass=1
        fi
    fi

    # Result Evaluation
    if [ $code_pass -eq 1 ] && [ $json_valid -eq 1 ] && [ $jq_pass -eq 1 ]; then
        PASSED_TESTS=$((PASSED_TESTS + 1))
        echo -e "${CLR_GREEN}[PASS]${CLR_RESET} ${CLR_BOLD}${test_name}${CLR_RESET} ${CLR_DIM}(${method} ${path})${CLR_RESET}"
        echo -e "       ${CLR_DIM}Status:${CLR_RESET} ${CLR_GREEN}${http_code}${CLR_RESET} | ${CLR_DIM}Latency:${CLR_RESET} ${latency_color}${latency_ms} ms${CLR_RESET} | ${CLR_DIM}JSON Validated:${CLR_RESET} ${CLR_GREEN}✓${CLR_RESET}"
    else
        FAILED_TESTS=$((FAILED_TESTS + 1))
        echo -e "${CLR_RED}[FAIL]${CLR_RESET} ${CLR_BOLD}${test_name}${CLR_RESET} ${CLR_DIM}(${method} ${path})${CLR_RESET}"
        echo -e "       ${CLR_DIM}Expected Status:${CLR_RESET} ${expected_code_regex} | ${CLR_DIM}Got:${CLR_RESET} ${CLR_RED}${http_code}${CLR_RESET} | ${CLR_DIM}Latency:${CLR_RESET} ${latency_color}${latency_ms} ms${CLR_RESET}"
        if [ $json_valid -ne 1 ]; then
            echo -e "       ${CLR_RED}Invalid JSON received:${CLR_RESET} $(echo "$body" | head -n 3 | cut -c 1-120)"
        elif [ $jq_pass -ne 1 ]; then
            echo -e "       ${CLR_RED}JSON Assertion Failed:${CLR_RESET} ${jq_expr}"
            [ -n "$jq_err" ] && echo -e "       ${CLR_DIM}Evaluated / Error: ${jq_err}${CLR_RESET}"
            echo -e "       ${CLR_DIM}Response snippet: $(echo "$body" | head -n 2 | cut -c 1-120)...${CLR_RESET}"
        fi
        return 1
    fi
}

echo -e "${CLR_BOLD}════════════════════════════════════════════════════════════════════════${CLR_RESET}"
echo -e "${CLR_WHITE}${CLR_BOLD} 1. SYSTEM HEALTH & DISCOVERY ENDPOINTS${CLR_RESET}"
echo -e "${CLR_BOLD}════════════════════════════════════════════════════════════════════════${CLR_RESET}"

run_test \
    "Health Probe - /health" \
    "GET" "/health" "" "200" \
    '.status == "ok" and (.runtime | contains("NET"))'

run_test \
    "Health Probe - /api/health" \
    "GET" "/api/health" "" "200" \
    '.status == "ok" and .version == "1.0.0"'

echo -e "\n${CLR_BOLD}════════════════════════════════════════════════════════════════════════${CLR_RESET}"
echo -e "${CLR_WHITE}${CLR_BOLD} 2. AUTHENTICATION & SESSION MANAGEMENT${CLR_RESET}"
echo -e "${CLR_BOLD}════════════════════════════════════════════════════════════════════════${CLR_RESET}"

# Test valid login and capture session cookie
run_test \
    "Auth Login (Valid Passphrase)" \
    "POST" "/api/auth/login" '{"passphrase":"your-secure-passphrase-here"}' "200" \
    '.authenticated == true and .message == "Logged in"' \
    "-c $COOKIE_JAR"

# Verify authenticated session using cookie
run_test \
    "Auth Me (Session Verification)" \
    "GET" "/api/auth/me" "" "200" \
    '.authenticated == true' \
    "-b $COOKIE_JAR"

# Test invalid login rejection
run_test \
    "Auth Login (Invalid Passphrase Rejection)" \
    "POST" "/api/auth/login" '{"passphrase":"wrong-password-xyz"}' "401" \
    '.authenticated == false'

# Logout session
run_test \
    "Auth Logout" \
    "POST" "/api/auth/logout" "" "200" \
    '.authenticated == false and .message == "Logged out"' \
    "-b $COOKIE_JAR -c $COOKIE_JAR"

echo -e "\n${CLR_BOLD}════════════════════════════════════════════════════════════════════════${CLR_RESET}"
echo -e "${CLR_WHITE}${CLR_BOLD} 3. QUESTION BANK & PRACTICE REPOSITORY${CLR_RESET}"
echo -e "${CLR_BOLD}════════════════════════════════════════════════════════════════════════${CLR_RESET}"

run_test \
    "Questions List (Complete Question Bank)" \
    "GET" "/api/questions" "" "200" \
    'type == "array" and length >= 10 and (.[0] | has("id") and has("topic") and has("stem"))'

run_test \
    "Questions Filter (Topic Filter & Limit)" \
    "GET" "/api/questions?topic=Operating%20Systems&limit=3" "" "200" \
    'type == "array" and length > 0 and (.[0].topic | contains("Operating Systems"))'

echo -e "\n${CLR_BOLD}════════════════════════════════════════════════════════════════════════${CLR_RESET}"
echo -e "${CLR_WHITE}${CLR_BOLD} 4. TASK LIFECYCLE (CREATE -> TOGGLE -> DELETE)${CLR_RESET}"
echo -e "${CLR_BOLD}════════════════════════════════════════════════════════════════════════${CLR_RESET}"

# 1. Create task
run_test \
    "Task Create (POST /api/tasks)" \
    "POST" "/api/tasks" \
    '{"title":"Regression Suite Study Task","topic":"QA Verification","priority":"high","estMin":30}' \
    "200|201" \
    '.id > 0 and .title == "Regression Suite Study Task" and .status == "pending"'

CREATED_TASK_ID=$(echo "$LAST_RESPONSE_BODY" | jq -r '.id')

if [ -n "$CREATED_TASK_ID" ] && [ "$CREATED_TASK_ID" != "null" ]; then
    # 2. Toggle status to done
    run_test \
        "Task Toggle Status (PATCH /api/tasks/${CREATED_TASK_ID} -> done)" \
        "PATCH" "/api/tasks/${CREATED_TASK_ID}" \
        '{"status":"done"}' \
        "200" \
        '.id == '$CREATED_TASK_ID' and .status == "done" and .doneAt != null'

    # 3. Toggle status back to pending
    run_test \
        "Task Toggle Status (PATCH /api/tasks/${CREATED_TASK_ID} -> pending)" \
        "PATCH" "/api/tasks/${CREATED_TASK_ID}" \
        '{"status":"pending"}' \
        "200" \
        '.id == '$CREATED_TASK_ID' and .status == "pending" and .doneAt == null'

    # 4. Delete task
    run_test \
        "Task Delete (DELETE /api/tasks/${CREATED_TASK_ID})" \
        "DELETE" "/api/tasks/${CREATED_TASK_ID}" \
        "" \
        "200" \
        '.deleted == true'

    # 5. Verify task no longer exists
    run_test \
        "Task Verify Deleted (GET /api/tasks/${CREATED_TASK_ID} -> 404)" \
        "GET" "/api/tasks/${CREATED_TASK_ID}" \
        "" \
        "404" \
        ""
else
    echo -e "${CLR_RED}⚠️  Skipping task toggle/delete tests due to task creation failure.${CLR_RESET}"
fi

echo -e "\n${CLR_BOLD}════════════════════════════════════════════════════════════════════════${CLR_RESET}"
echo -e "${CLR_WHITE}${CLR_BOLD} 5. MOCK EXAM SIMULATION (START -> SUBMIT -> SCORE)${CLR_RESET}"
echo -e "${CLR_BOLD}════════════════════════════════════════════════════════════════════════${CLR_RESET}"

# Start Mock Test Session
run_test \
    "Mock Test Start (POST /api/mock-tests/start)" \
    "POST" "/api/mock-tests/start" \
    '{"count":5,"mode":"quick"}' \
    "200" \
    '.sessionId != null and (.questions | length > 0) and .totalQuestions > 0'

MOCK_SESSION_ID=$(echo "$LAST_RESPONSE_BODY" | jq -r '.sessionId')
FIRST_Q_ID=$(echo "$LAST_RESPONSE_BODY" | jq -r '.questions[0].id // empty')
FIRST_Q_OPT=$(echo "$LAST_RESPONSE_BODY" | jq -r '.questions[0].options[0] // empty')

if [ -n "$MOCK_SESSION_ID" ] && [ "$MOCK_SESSION_ID" != "null" ]; then
    # Submit Answers
    SUBMIT_PAYLOAD=$(jq -n \
        --arg qid "$FIRST_Q_ID" \
        --arg ans "$FIRST_Q_OPT" \
        '{
            totalTimeSpentSec: 45,
            answers: [
                {
                    questionId: ($qid | tonumber),
                    userAnswer: $ans,
                    timeSpentSec: 20,
                    isMarkedForReview: false
                }
            ]
        }')

    run_test \
        "Mock Test Submit (POST /api/mock-tests/${MOCK_SESSION_ID}/submit)" \
        "POST" "/api/mock-tests/${MOCK_SESSION_ID}/submit" \
        "$SUBMIT_PAYLOAD" \
        "200" \
        '.sessionId == "'"$MOCK_SESSION_ID"'" and .attemptedCount >= 1 and has("totalScore") and (.subjectBreakdown | length > 0)'

    # Get Single Session Result
    run_test \
        "Mock Test Scorecard (GET /api/mock-tests/${MOCK_SESSION_ID})" \
        "GET" "/api/mock-tests/${MOCK_SESSION_ID}" \
        "" \
        "200" \
        '.sessionId == "'"$MOCK_SESSION_ID"'" and has("percentage") and has("rankEstimate")'

    # Get Mock Test History
    run_test \
        "Mock Test History (GET /api/mock-tests/history)" \
        "GET" "/api/mock-tests/history?limit=5" \
        "" \
        "200" \
        'type == "array" and length > 0 and (.[0] | has("sessionId") and has("totalScore"))'
else
    echo -e "${CLR_RED}⚠️  Skipping mock test submission tests due to session start failure.${CLR_RESET}"
fi

echo -e "\n${CLR_BOLD}════════════════════════════════════════════════════════════════════════${CLR_RESET}"
echo -e "${CLR_WHITE}${CLR_BOLD} 6. PLUGGABLE MODULES & FORMULA VAULT${CLR_RESET}"
echo -e "${CLR_BOLD}════════════════════════════════════════════════════════════════════════${CLR_RESET}"

run_test \
    "Formula Vault List (GET /api/modules/formula-vault)" \
    "GET" "/api/modules/formula-vault" \
    "" \
    "200" \
    'type == "array" and length > 0 and (.[0] | has("title") and has("formula") and has("category"))'

run_test \
    "Formula Vault Categories (GET /api/modules/formula-vault/categories)" \
    "GET" "/api/modules/formula-vault/categories" \
    "" \
    "200" \
    'type == "array" and length >= 3'

run_test \
    "Modules Hub Registry (GET /api/modules)" \
    "GET" "/api/modules" \
    "" \
    "200" \
    'type == "array" and length > 0 and (.[0] | has("moduleKey") and has("enabled"))'

echo -e "\n${CLR_BOLD}════════════════════════════════════════════════════════════════════════${CLR_RESET}"
echo -e "${CLR_WHITE}${CLR_BOLD} 7. ANALYTICS, ACTIVE RECALL & RETENTION METRICS${CLR_RESET}"
echo -e "${CLR_BOLD}════════════════════════════════════════════════════════════════════════${CLR_RESET}"

run_test \
    "Stats Overview (GET /api/stats/overview)" \
    "GET" "/api/stats/overview" \
    "" \
    "200" \
    'has("streak") and has("examCountdownDays") and has("accuracy")'

run_test \
    "Advanced Analytics (GET /api/stats/advanced)" \
    "GET" "/api/stats/advanced" \
    "" \
    "200" \
    'has("overallAccuracy") and has("retentionRateIndex") and (.subjectMastery | length > 0) and (.timeSpent | length > 0)'

run_test \
    "Activity Heatmap (GET /api/stats/heatmap)" \
    "GET" "/api/stats/heatmap?days=30" \
    "" \
    "200" \
    'has("data") and (.data | type == "array")'

run_test \
    "Weak Topics Analysis (GET /api/stats/weak-topics)" \
    "GET" "/api/stats/weak-topics?limit=5" \
    "" \
    "200" \
    'has("weakTopics") and (.weakTopics | type == "array")'

echo -e "\n${CLR_BOLD}════════════════════════════════════════════════════════════════════════${CLR_RESET}"
echo -e "${CLR_WHITE}${CLR_BOLD} 8. SENSEI AI STUDY COACH & CHAT MEMORY${CLR_RESET}"
echo -e "${CLR_BOLD}════════════════════════════════════════════════════════════════════════${CLR_RESET}"

run_test \
    "AI Chat History (GET /api/ai/messages)" \
    "GET" "/api/ai/messages?limit=5" \
    "" \
    "200" \
    'type == "array" and length > 0 and (.[0] | has("role") and has("content"))'

run_test \
    "AI Study Coach Chat (POST /api/ai/chat)" \
    "POST" "/api/ai/chat" \
    '{"messages":[{"role":"user","content":"Quick revision tip for GATE Operating Systems"}]}' \
    "200" \
    'has("role") and has("content") and (.content | length > 10)'

echo -e "\n${CLR_BOLD}════════════════════════════════════════════════════════════════════════${CLR_RESET}"
echo -e "${CLR_WHITE}${CLR_BOLD} 9. GATE SYLLABUS WEIGHTAGE & HIGH-YIELD ANALYSIS${CLR_RESET}"
echo -e "${CLR_BOLD}════════════════════════════════════════════════════════════════════════${CLR_RESET}"

run_test \
    "Weightage Overview (GET /api/modules/weightage-analysis/overview)" \
    "GET" "/api/modules/weightage-analysis/overview" \
    "" \
    "200" \
    'has("subjects") and (.subjects | length >= 8) and has("totalTargetHours")'

run_test \
    "High-Yield Topics Checklist (GET /api/modules/weightage-analysis/high-yield-topics)" \
    "GET" "/api/modules/weightage-analysis/high-yield-topics" \
    "" \
    "200" \
    'type == "array" and length >= 15 and (.[0] | has("topicName") and has("avgMarks"))'

echo -e "\n${CLR_BOLD}════════════════════════════════════════════════════════════════════════${CLR_RESET}"
echo -e "${CLR_WHITE}${CLR_BOLD} 10. LEITNER BOX SPACED REPETITION FLASHCARDS${CLR_RESET}"
echo -e "${CLR_BOLD}════════════════════════════════════════════════════════════════════════${CLR_RESET}"

run_test \
    "Flashcard Decks List (GET /api/modules/flashcards/decks)" \
    "GET" "/api/modules/flashcards/decks" \
    "" \
    "200" \
    'type == "array" and length >= 3 and (.[0] | has("title") and has("cardCount"))'

run_test \
    "Flashcards in Deck 1 (GET /api/modules/flashcards/decks/1/cards)" \
    "GET" "/api/modules/flashcards/decks/1/cards" \
    "" \
    "200" \
    'type == "array" and length > 0 and (.[0] | has("front") and has("back") and has("box"))'

run_test \
    "Flashcard Deck 1 Stats (GET /api/modules/flashcards/decks/1/stats)" \
    "GET" "/api/modules/flashcards/decks/1/stats" \
    "" \
    "200" \
    'has("totalCards") and has("dueCards") and has("box1Count")'

# ------------------------------------------------------------------------------
# Latency Benchmarks & Summary
# ------------------------------------------------------------------------------
AVG_LATENCY="0.00"
if [ ${#LATENCIES[@]} -gt 0 ]; then
    SUM_LATENCY=0
    for l in "${LATENCIES[@]}"; do
        SUM_LATENCY=$(awk -v s="$SUM_LATENCY" -v val="$l" 'BEGIN { printf "%.2f", s + val }')
    done
    AVG_LATENCY=$(awk -v s="$SUM_LATENCY" -v n="${#LATENCIES[@]}" 'BEGIN { printf "%.2f", s / n }')
fi

PASS_PERCENTAGE=$(awk -v p="$PASSED_TESTS" -v t="$TOTAL_TESTS" 'BEGIN { if (t > 0) printf "%.1f", (p / t) * 100; else print "0" }')

echo -e "\n${CLR_CYAN}${CLR_BOLD}========================================================================${CLR_RESET}"
echo -e "${CLR_WHITE}${CLR_BOLD}                         QA TEST SUITE SUMMARY                          ${CLR_RESET}"
echo -e "${CLR_CYAN}${CLR_BOLD}========================================================================${CLR_RESET}"

echo -e "  ${CLR_BOLD}Total Test Executions:${CLR_RESET}   ${CLR_WHITE}${TOTAL_TESTS}${CLR_RESET}"
echo -e "  ${CLR_BOLD}Passed:${CLR_RESET}                  ${CLR_GREEN}${PASSED_TESTS}${CLR_RESET}"
echo -e "  ${CLR_BOLD}Failed:${CLR_RESET}                  $([ $FAILED_TESTS -gt 0 ] && echo -e "${CLR_RED}${FAILED_TESTS}${CLR_RESET}" || echo -e "${CLR_GREEN}0${CLR_RESET}")"
echo -e "  ${CLR_BOLD}Pass Rate:${CLR_RESET}               $([ "$PASSED_TESTS" -eq "$TOTAL_TESTS" ] && echo -e "${CLR_GREEN}${PASS_PERCENTAGE}%${CLR_RESET}" || echo -e "${CLR_YELLOW}${PASS_PERCENTAGE}%${CLR_RESET}")"
echo -e "  ${CLR_BOLD}Test Score:${CLR_RESET}              ${CLR_WHITE}${PASSED_TESTS} / ${TOTAL_TESTS}${CLR_RESET}"

echo -e "\n${CLR_BOLD}⚡ Latency Benchmarks:${CLR_RESET}"
echo -e "  ${CLR_DIM}Average Latency:${CLR_RESET}         ${CLR_CYAN}${AVG_LATENCY} ms${CLR_RESET}"
echo -e "  ${CLR_DIM}Fastest Endpoint:${CLR_RESET}        ${CLR_GREEN}${FASTEST_LATENCY} ms${CLR_RESET} (${FASTEST_TEST})"
echo -e "  ${CLR_DIM}Slowest Endpoint:${CLR_RESET}        ${CLR_YELLOW}${SLOWEST_LATENCY} ms${CLR_RESET} (${SLOWEST_TEST})"

echo -e "${CLR_CYAN}${CLR_BOLD}========================================================================${CLR_RESET}"

if [ "$FAILED_TESTS" -eq 0 ]; then
    echo -e "${CLR_GREEN}${CLR_BOLD}🎉  VERIFICATION SUCCESS: All ${TOTAL_TESTS} automated tests passed!${CLR_RESET}\n"
    exit 0
else
    echo -e "${CLR_RED}${CLR_BOLD}⚠️   VERIFICATION FAILURE: ${FAILED_TESTS} of ${TOTAL_TESTS} tests failed.${CLR_RESET}\n"
    exit 1
fi
