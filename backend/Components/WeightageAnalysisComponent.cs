using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sensei.Core.Interfaces;
using Sensei.Core.Models;

namespace Sensei.Components;

public class WeightageAnalysisComponent : IWeightageAnalysisComponent
{
    private readonly IDatabaseComponent _db;

    // Canonical 8 GATE CS Subject Definitions with prompt's historical weightages:
    // Engg Math 13%, Aptitude 15%, OS 10%, DBMS 8%, CN 9%, Algorithms 11%, TOC/CD 12%, COA/Digital 12%
    private static readonly SubjectDefinition[] CanonicalSubjects = new[]
    {
        new SubjectDefinition(
            Key: "aptitude",
            Name: "General Aptitude",
            Weightage: 15.0,
            TargetHours: 98.0,
            Color: "#84cc16",
            TopicKeywords: new[] { "aptitude", "quantitative", "verbal", "reasoning", "numerical", "interpretation", "permutation" },
            HighYieldHighlights: new[] { "Quantitative Reasoning", "Syllogism & Deductions", "Data Interpretation" }
        ),
        new SubjectDefinition(
            Key: "math",
            Name: "Engineering Math & Discrete Math",
            Weightage: 13.0,
            TargetHours: 85.0,
            Color: "#a855f7",
            TopicKeywords: new[] { "linear algebra", "eigen", "matrix", "calculus", "probability", "discrete mathematics", "propositional logic", "graph theory", "combinatorics" },
            HighYieldHighlights: new[] { "Eigenvalues & Rank", "Propositional Logic", "Graph Theory & Trees", "Bayes Theorem" }
        ),
        new SubjectDefinition(
            Key: "toc_cd",
            Name: "Theory of Computation & CD",
            Weightage: 12.0,
            TargetHours: 78.0,
            Color: "#06b6d4",
            TopicKeywords: new[] { "theory of computation", "automata", "dfa", "nfa", "pda", "turing", "decidab", "chomsky", "regular", "compiler", "parser", "parsing", "syntax", "grammar" },
            HighYieldHighlights: new[] { "Pumping Lemma & Regularity", "LL/LR Parsing Conflicts", "Decidability & Turing Machines" }
        ),
        new SubjectDefinition(
            Key: "coa_digital",
            Name: "COA & Digital Logic",
            Weightage: 12.0,
            TargetHours: 78.0,
            Color: "#f43f5e",
            TopicKeywords: new[] { "cache", "pipelining", "hazards", "memory hierarchy", "floating point", "digital logic", "combinational", "sequential", "counters", "multiplexers" },
            HighYieldHighlights: new[] { "Cache Mapping & Hit Ratio", "Pipeline Hazards & CPI", "Multiplexers & Modulo Counters" }
        ),
        new SubjectDefinition(
            Key: "algo_ds",
            Name: "Algorithms & Data Structures",
            Weightage: 11.0,
            TargetHours: 71.0,
            Color: "#8b5cf6",
            TopicKeywords: new[] { "algorithms", "data structures", "arrays", "sorting", "graph algorithms", "dijkstra", "spanning tree", "dynamic programming", "hashing", "trees", "complexity" },
            HighYieldHighlights: new[] { "Dijkstra & MST (Prim/Kruskal)", "Dynamic Programming (Knapsack/LCS)", "Master Theorem Recurrence" }
        ),
        new SubjectDefinition(
            Key: "os",
            Name: "Operating Systems",
            Weightage: 10.0,
            TargetHours: 65.0,
            Color: "#ec4899",
            TopicKeywords: new[] { "operating systems", "cpu scheduling", "paging", "virtual memory", "tlb", "synchronization", "semaphores", "deadlock", "disk scheduling" },
            HighYieldHighlights: new[] { "Paging, TLB & Effective Memory Access", "Round Robin & SRTF Scheduling", "Process Sync Semaphores" }
        ),
        new SubjectDefinition(
            Key: "cn",
            Name: "Computer Networks",
            Weightage: 9.0,
            TargetHours: 58.0,
            Color: "#f59e0b",
            TopicKeywords: new[] { "computer networks", "subnetting", "ipv4", "cidr", "tcp", "congestion control", "sliding window", "routing", "udp", "flow control" },
            HighYieldHighlights: new[] { "TCP Congestion Control & Window", "IPv4 Subnetting & CIDR", "Sliding Window Protocol (GBN/SR)" }
        ),
        new SubjectDefinition(
            Key: "dbms",
            Name: "Database Systems (DBMS)",
            Weightage: 8.0,
            TargetHours: 52.0,
            Color: "#10b981",
            TopicKeywords: new[] { "relational database", "database normalization", "bcnf", "3nf", "transactions", "concurrency", "serializability", "b+ trees", "indexing", "sql" },
            HighYieldHighlights: new[] { "Relational Normalization (BCNF/3NF)", "Conflict Serializability & 2PL", "B+ Tree Indexing Node Order" }
        )
    };

    public WeightageAnalysisComponent(IDatabaseComponent db)
    {
        _db = db;
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        _db.ExecuteScript(@"
            CREATE TABLE IF NOT EXISTS high_yield_topics (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                subject_key TEXT NOT NULL,
                subject_name TEXT NOT NULL,
                topic_name TEXT NOT NULL,
                priority TEXT NOT NULL,
                frequency_score INTEGER NOT NULL,
                avg_marks REAL NOT NULL,
                recurrence_pattern TEXT NOT NULL,
                key_formula_or_concept TEXT NOT NULL,
                is_completed INTEGER DEFAULT 0,
                notes TEXT,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS weighted_practice_papers (
                id TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                question_count INTEGER NOT NULL,
                duration_min INTEGER NOT NULL,
                subject_distribution_json TEXT NOT NULL,
                questions_json TEXT NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
        ");

        var hyCount = _db.QueryScalar<long>("SELECT COUNT(*) FROM high_yield_topics;");
        if (hyCount == 0)
        {
            SeedHighYieldTopics();
        }

        // Ensure General Aptitude questions exist in questions table so weighted paper can draw questions
        var aptCount = _db.QueryScalar<long>(
            "SELECT COUNT(*) FROM questions WHERE topic LIKE '%Aptitude%' OR topic LIKE '%Quantitative%' OR topic LIKE '%Reasoning%';"
        );
        if (aptCount < 5)
        {
            SeedAptitudeQuestions();
        }
    }

    private void SeedHighYieldTopics()
    {
        var topics = new[]
        {
            // COA & Digital (12%)
            ("coa_digital", "COA & Digital Logic", "Cache Mapping (Direct, Set-Associative) & EMAT", "Critical", 95, 4.0,
             "Appears in 9 out of 10 GATE CS papers as 2-mark numerical on tag/set bits and hit times",
             "Tag + Index + Offset = Address Bits; Cache Size = Sets * Ways * BlockSize; EMAT = HitTime + MissRate * MissPenalty"),
            
            ("coa_digital", "COA & Digital Logic", "Instruction Pipelining, Branch Hazards & Speedup", "Critical", 90, 3.5,
             "Tested nearly every year for pipeline stalls, CPI penalties, and structural/data hazards",
             "Speedup = NonPipelined_Time / Pipelined_Time = (k * n) / ((k + n - 1 + Stalls) * Clock)"),
            
            ("coa_digital", "COA & Digital Logic", "Combinational Circuits (Multiplexers & Decoders)", "High", 85, 2.5,
             "Implementing arbitrary Boolean logic functions with 2^n:1 MUX",
             "2^n-to-1 MUX has n select lines and 2^n inputs; Decoders generate all 2^n minterms"),

            ("coa_digital", "COA & Digital Logic", "Sequential Circuits, Counters & Flip-Flop Conversion", "High", 80, 3.0,
             "Modulo-N ripple/synchronous counters, JK to D flip-flop conversions",
             "MOD-N counter requires ceil(log2(N)) flip-flops; Characteristic: Q_next = J Q' + K' Q (JK), D (D-FF)"),

            // Operating Systems (10%)
            ("os", "Operating Systems", "Paging, TLB Hit Ratio & Multi-level Page Tables", "Critical", 95, 4.0,
             "Consistent 2-mark question on Effective Memory Access Time and Page Table size calculation",
             "EMAT = TLB_hit * (t_TLB + t_mem) + (1 - TLB_hit) * (t_TLB + (Levels + 1) * t_mem)"),

            ("os", "Operating Systems", "CPU Scheduling (Round Robin, SRTF) & Gantt Metrics", "High", 90, 3.0,
             "Turnaround time, waiting time, and preemption overhead calculations",
             "Turnaround Time = Completion - Arrival; Waiting Time = Turnaround - Burst; SRTF can suffer starvation"),

            ("os", "Operating Systems", "Process Synchronization (Counting Semaphores & Mutex)", "Critical", 90, 3.5,
             "Wait(S) and Signal(S) race conditions, producer-consumer and reader-writer invariants",
             "Semaphore S >= 0: available resources; S < 0: |S| waiting processes in queue"),

            ("os", "Operating Systems", "Deadlock Avoidance & Banker's Safety Algorithm", "High", 85, 2.5,
             "Resource Allocation Graph cycles, Need Matrix = Max - Allocation, safe sequence detection",
             "System is safe iff a valid sequence exists where Need[i] <= Available for all i"),

            // Algorithms & Data Structures (11%)
            ("algo_ds", "Algorithms & Data Structures", "Dijkstra's Algorithm & Minimum Spanning Trees (MST)", "Critical", 90, 3.5,
             "Prim's and Kruskal's greedy choice property, edge relaxation order, min-heap complexity",
             "Dijkstra: O((V + E) log V) with min-binary heap; Prim's: O((V + E) log V); Kruskal: O(E log V) with Union-Find"),

            ("algo_ds", "Algorithms & Data Structures", "Dynamic Programming: Knapsack, LCS & Matrix Chain", "Critical", 90, 4.0,
             "State transitions, optimal substructure, DAG paths, and 2D DP memoization tables",
             "0/1 Knapsack: DP[i,w] = max(DP[i-1,w], DP[i-1,w-wt[i]] + val[i]); LCS length and backtracking"),

            ("algo_ds", "Algorithms & Data Structures", "Master Theorem & Divide-and-Conquer Recurrences", "Critical", 95, 3.5,
             "Direct 1-mark and 2-mark asymptotic complexity bounds (Big-O, Theta, Omega)",
             "T(n) = a T(n/b) + Theta(n^k log^p n); Compare log_b(a) with k; Case 1 (<), Case 2 (=), Case 3 (>)"),

            ("algo_ds", "Algorithms & Data Structures", "Binary Search Trees, AVL Rotations & Hashing", "High", 85, 3.0,
             "Preorder/Inorder/Postorder tree reconstructions, AVL balance factors (-1, 0, +1), hash collisions",
             "Number of unlabeled binary trees with n nodes = C_n (nth Catalan number) = (2n)! / ((n+1)! * n!)"),

            // Theory of Computation & Compiler Design (12%)
            ("toc_cd", "Theory of Computation & CD", "Pumping Lemma & Regular Language Identification", "Critical", 95, 3.5,
             "Identifying whether a language is Regular, Context-Free, or Non-CFL",
             "Regular Pumping Lemma: |xy| <= p, |y| >= 1, x y^i z in L for all i >= 0; Closure properties under complement/intersection"),

            ("toc_cd", "Theory of Computation & CD", "Decidability, Halting Problem & Rice's Theorem", "Critical", 90, 3.5,
             "Classifying problems into Decidable, Semi-Decidable (RE), and Undecidable",
             "Rice's Theorem: Any non-trivial semantic property of RE languages is undecidable"),

            ("toc_cd", "Theory of Computation & CD", "Parsing: LL(1), LR(0), SLR(1), LALR(1) & Conflicts", "Critical", 90, 4.0,
             "First and Follow sets, Shift-Reduce (SR) and Reduce-Reduce (RR) conflicts",
             "Power: LR(0) < SLR(1) < LALR(1) < CLR(1); LL(1) requires disjoint First sets for alternatives"),

            ("toc_cd", "Theory of Computation & CD", "Syntax-Directed Translation (SDT): S vs L-Attributed", "High", 80, 2.5,
             "Synthesized vs Inherited attributes, bottom-up parsing evaluation order",
             "S-attributed: Synthesized only (evaluable during bottom-up parsing); L-attributed: Left-to-right dependencies"),

            // Database Management Systems (8%)
            ("dbms", "Database Systems (DBMS)", "Relational Normalization (BCNF, 3NF, 2NF) & Keys", "Critical", 95, 4.0,
             "Finding candidate keys, prime attributes, lossless join and dependency preservation",
             "BCNF: For every FD X -> Y, X must be superkey; 3NF: X is superkey OR Y is prime attribute"),

            ("dbms", "Database Systems (DBMS)", "Transactions: Conflict Serializability & Strict 2PL", "Critical", 90, 3.5,
             "Precedence graph cycle detection, recoverable and cascade-free schedules",
             "Schedule is conflict serializable iff Precedence Graph has no cycles; Strict 2PL prevents cascading rollbacks"),

            ("dbms", "Database Systems (DBMS)", "B+ Trees: Order, Node Fill & Index Calculations", "High", 85, 3.0,
             "Determining order, minimum/maximum keys per node, search I/O cost",
             "Order p internal node: at most p block pointers, p-1 keys; Leaf stores (record_ptr, search_key)"),

            // Computer Networks (9%)
            ("cn", "Computer Networks", "TCP Congestion Control (Slow Start, AIMD, Fast Recovery)", "Critical", 90, 3.5,
             "CWND growth curve, threshold (ssthresh) halving on 3 duplicate ACKs vs timeout",
             "Slow start: CWND doubles every RTT; Congestion Avoidance: CWND increases by 1 MSS/RTT; Timeout sets CWND = 1 MSS"),

            ("cn", "Computer Networks", "IPv4 Subnetting, Supernetting & CIDR Routing Lookups", "Critical", 95, 4.0,
             "Subnet masks, broadcast addresses, longest prefix matching in routing tables",
             "Subnet mask /n: 32-n host bits; Number of usable hosts = 2^(32-n) - 2; Longest Prefix Match breaks routing ties"),

            ("cn", "Computer Networks", "Sliding Window Protocols (Go-Back-N vs Selective Repeat)", "High", 85, 3.0,
             "Link utilization, sender/receiver window sizes, sequence numbers",
             "Efficiency = N / (1 + 2a) where a = Propagation_Delay / Transmission_Delay; Min seq numbers: N+1 (GBN), 2N (SR)"),

            // Engineering Mathematics & Discrete Math (13%)
            ("math", "Engineering Math & Discrete Math", "Linear Algebra: Eigenvalues, Eigenvectors & Rank", "Critical", 95, 3.5,
             "Characteristic equation det(A - lambda*I) = 0, Cayley-Hamilton theorem, rank-nullity",
             "Sum of eigenvalues = Trace(A); Product of eigenvalues = det(A); A and A^T have identical eigenvalues"),

            ("math", "Engineering Math & Discrete Math", "Propositional Logic & First-Order Quantifiers", "Critical", 90, 3.0,
             "Logical equivalence, tautology, negation of quantifiers",
             "P -> Q = ~P v Q; Contrapositive = ~Q -> ~P; ~(forall x P(x)) = exists x ~P(x)"),

            ("math", "Engineering Math & Discrete Math", "Graph Theory: Planar Graphs, Handshaking & Coloring", "Critical", 90, 3.5,
             "Euler formula V - E + F = 2, planarity upper bound E <= 3V - 6, chromatic numbers",
             "Sum of degrees = 2 * E; Planar connected graph: E <= 3V - 6 (for V >= 3) and no K5 or K3,3 minor"),

            ("math", "Engineering Math & Discrete Math", "Probability Distributions & Bayes Theorem", "High", 85, 2.5,
             "Conditional probability, Poisson, Binomial, expectation and variance",
             "P(A|B) = P(B|A) * P(A) / P(B); E[X + Y] = E[X] + E[Y]; Var(X) = E[X^2] - (E[X])^2"),

            // General Aptitude (15%)
            ("aptitude", "General Aptitude", "Quantitative Reasoning: Speed, Distance & Work", "Critical", 90, 4.0,
             "Time & work rate equations, relative speed of crossing trains/vehicles",
             "Combined work rate = 1/A + 1/B; Relative speed in opposite direction = s1 + s2; Same direction = |s1 - s2|"),

            ("aptitude", "General Aptitude", "Permutations, Combinations & Probability Puzzles", "High", 85, 3.0,
             "Arrangements with identical items, circular permutations, pigeonhole selections",
             "nPr = n! / (n-r)!; nCr = n! / (r! * (n-r)!); Handshakes in n people = n(n-1)/2"),

            ("aptitude", "General Aptitude", "Logical Deduction, Syllogisms & Venn Diagrams", "High", 85, 3.0,
             "Deductive syllogisms, truth/lie puzzles, Venn diagram inclusion-exclusion",
             "|A v B| = |A| + |B| - |A ^ B|; |A v B v C| = sum(|A|) - sum(|A^B|) + |A^B^C|")
        };

        foreach (var (subjKey, subjName, topName, prio, freq, avgMarks, pattern, formula) in topics)
        {
            _db.ExecuteNonQuery(@"
                INSERT INTO high_yield_topics (
                    subject_key, subject_name, topic_name, priority, frequency_score, avg_marks, recurrence_pattern, key_formula_or_concept, is_completed
                ) VALUES (
                    @subjKey, @subjName, @topName, @prio, @freq, @avgMarks, @pattern, @formula, 0
                )
            ",
                ("@subjKey", subjKey),
                ("@subjName", subjName),
                ("@topName", topName),
                ("@prio", prio),
                ("@freq", freq),
                ("@avgMarks", avgMarks),
                ("@pattern", pattern),
                ("@formula", formula)
            );
        }
    }

    private void SeedAptitudeQuestions()
    {
        var aptQuestions = new[]
        {
            (
                Topic: "Quantitative Aptitude",
                Difficulty: "easy",
                Stem: "A train running at 54 km/h crosses a 250 m long platform in 30 seconds. What is the length of the train?",
                Options: new[] { "200 m", "250 m", "150 m", "300 m" },
                Answer: "200 m",
                Solution: "Speed in m/s = 54 * (5 / 18) = 15 m/s. Total distance covered in 30 s = 15 * 30 = 450 m. Distance = Length of Train + Length of Platform => 450 = L + 250 => L = 200 m."
            ),
            (
                Topic: "Permutations & Combinations",
                Difficulty: "medium",
                Stem: "In how many distinct ways can the letters of the word 'SUCCESS' be arranged such that all the three 'S's are always kept together?",
                Options: new[] { "60", "120", "240", "720" },
                Answer: "60",
                Solution: "Treat (SSS) as a single block. The remaining distinct items to arrange are: (SSS), U, C, C, E. This gives 5 entities with 2 'C's repeating. Number of arrangements = 5! / 2! = 120 / 2 = 60 ways."
            ),
            (
                Topic: "Logical Reasoning",
                Difficulty: "medium",
                Stem: "Statements: All routers are switches. Some switches are firewalls. No firewall is a hub. Conclusions: I. Some routers are firewalls. II. No switch is a hub.",
                Options: new[] { "Neither I nor II follows", "Only I follows", "Only II follows", "Both I and II follow" },
                Answer: "Neither I nor II follows",
                Solution: "Routers are a subset of switches, but switches only partially overlap firewalls, so routers need not intersect firewalls. Switches are not disjoint from hubs (only firewalls are), so some switches could be hubs. Neither conclusion necessarily holds."
            ),
            (
                Topic: "Numerical Ability",
                Difficulty: "easy",
                Stem: "If x + 1/x = 3, what is the value of x^4 + 1/x^4?",
                Options: new[] { "47", "49", "51", "45" },
                Answer: "47",
                Solution: "Square both sides: (x + 1/x)^2 = 9 => x^2 + 1/x^2 = 9 - 2 = 7. Square again: (x^2 + 1/x^2)^2 = 49 => x^4 + 1/x^4 = 49 - 2 = 47."
            ),
            (
                Topic: "Data Interpretation",
                Difficulty: "medium",
                Stem: "The ratio of boys to girls in a computer science batch is 5:3. If 6 boys leave and 6 girls join, the ratio becomes 1:1. What is the total number of students in the batch?",
                Options: new[] { "48", "36", "40", "56" },
                Answer: "48",
                Solution: "Let boys = 5k, girls = 3k. After changes: (5k - 6) / (3k + 6) = 1 => 5k - 6 = 3k + 6 => 2k = 12 => k = 6. Total students = 5k + 3k = 8k = 48."
            ),
            (
                Topic: "Verbal Ability",
                Difficulty: "easy",
                Stem: "Choose the word that is most nearly OPPOSITE in meaning to 'TRANSIENT':",
                Options: new[] { "Permanent", "Fleeting", "Ephemeral", "Temporal" },
                Answer: "Permanent",
                Solution: "'Transient' means lasting only for a short time or impermanent; its antonym is 'Permanent'."
            )
        };

        foreach (var q in aptQuestions)
        {
            _db.ExecuteNonQuery(@"
                INSERT INTO questions (
                    topic, difficulty, stem, options_json, answer, solution_md, source, tags_json, sr_ease, sr_interval, sr_due, sr_reps, created_at
                ) VALUES (
                    @topic, @diff, @stem, @opts, @ans, @sol, 'GATE-General-Aptitude', '[""GATE"",""Aptitude""]', 2.5, 0, date('now'), 0, datetime('now')
                )
            ",
                ("@topic", q.Topic),
                ("@diff", q.Difficulty),
                ("@stem", q.Stem),
                ("@opts", JsonSerializer.Serialize(q.Options)),
                ("@ans", q.Answer),
                ("@sol", q.Solution)
            );
        }
    }

    public WeightageOverviewDto GetOverview()
    {
        // 1. Load high-yield topic stats
        var hyRows = _db.Query("SELECT id, subject_key, is_completed, priority FROM high_yield_topics;");
        var hyBySubject = hyRows.GroupBy(r => r["subject_key"]?.ToString() ?? "");
        int totalHy = hyRows.Count;
        int completedHy = hyRows.Count(r => Convert.ToInt32(r["is_completed"]) == 1);
        int criticalPending = hyRows.Count(r => Convert.ToInt32(r["is_completed"]) == 0 && string.Equals(r["priority"]?.ToString(), "Critical", StringComparison.OrdinalIgnoreCase));
        double hyCompletionPct = totalHy > 0 ? Math.Round((double)completedHy / totalHy * 100, 1) : 0;

        // 2. Load attempts joined with questions to compute subject accuracy
        var attemptRows = _db.Query(@"
            SELECT q.topic, a.correct 
            FROM attempts a 
            JOIN questions q ON a.question_id = q.id;
        ");

        // 3. Load tasks to compute actual hours spent per subject
        var taskRows = _db.Query("SELECT topic, title, actual_min FROM tasks;");

        var subjectItems = new List<SubjectWeightageItem>();
        double totalTargetHours = 0;
        double totalActualHours = 0;
        double weightedMasterySum = 0;

        foreach (var def in CanonicalSubjects)
        {
            totalTargetHours += def.TargetHours;

            // Match attempts for this subject
            var matchedAttempts = attemptRows.Where(r =>
            {
                string top = (r["topic"]?.ToString() ?? "").ToLowerInvariant();
                return def.TopicKeywords.Any(k => top.Contains(k));
            }).ToList();

            int attemptsCount = matchedAttempts.Count;
            int correctCount = matchedAttempts.Count(r => Convert.ToInt32(r["correct"]) == 1);
            double accuracy = attemptsCount > 0 ? Math.Round((double)correctCount / attemptsCount * 100, 1) : 0;

            // Match tasks for actual time
            int actualMinutes = taskRows
                .Where(r =>
                {
                    string top = (r["topic"]?.ToString() ?? "").ToLowerInvariant();
                    string tit = (r["title"]?.ToString() ?? "").ToLowerInvariant();
                    return def.TopicKeywords.Any(k => top.Contains(k) || tit.Contains(k));
                })
                .Sum(r => r["actual_min"] != null ? Convert.ToInt32(r["actual_min"]) : 0);

            // Add estimated 2.5 minutes per attempt
            actualMinutes += (int)(attemptsCount * 2.5);
            double actualHours = Math.Round(actualMinutes / 60.0, 1);
            totalActualHours += actualHours;

            // Check high yield completion for this subject
            var subHy = hyBySubject.FirstOrDefault(g => g.Key == def.Key);
            int subHyTotal = subHy?.Count() ?? 0;
            int subHyDone = subHy?.Count(r => Convert.ToInt32(r["is_completed"]) == 1) ?? 0;
            double hySubPct = subHyTotal > 0 ? (double)subHyDone / subHyTotal : 0;

            // Calculate mastery: blend of accuracy (60%) and high-yield checklist progress (40%)
            double masteryPercentage;
            if (attemptsCount == 0 && subHyDone == 0)
            {
                masteryPercentage = 0;
            }
            else if (attemptsCount == 0)
            {
                masteryPercentage = Math.Round(hySubPct * 60, 1);
            }
            else
            {
                masteryPercentage = Math.Round(accuracy * 0.6 + hySubPct * 40, 1);
            }
            masteryPercentage = Math.Clamp(masteryPercentage, 0, 100);

            // Status label
            string status = masteryPercentage >= 70 ? "Mastered"
                          : masteryPercentage >= 40 ? "On Track"
                          : attemptsCount > 0 || subHyDone > 0 ? "Needs Practice"
                          : "Untested";

            weightedMasterySum += (masteryPercentage * def.Weightage);

            subjectItems.Add(new SubjectWeightageItem(
                SubjectKey: def.Key,
                SubjectName: def.Name,
                WeightagePercent: def.Weightage,
                TargetHours: def.TargetHours,
                ActualHours: actualHours,
                AttemptedCount: attemptsCount,
                CorrectCount: correctCount,
                Accuracy: accuracy,
                MasteryPercentage: masteryPercentage,
                Color: def.Color,
                Status: status,
                HighYieldSummary: def.HighYieldHighlights.ToList()
            ));
        }

        double overallMastery = Math.Round(weightedMasterySum / 100.0, 1);

        return new WeightageOverviewDto(
            Subjects: subjectItems,
            TotalTargetHours: Math.Round(totalTargetHours, 1),
            TotalActualHours: Math.Round(totalActualHours, 1),
            OverallMastery: overallMastery,
            HighYieldSummary: new HighYieldSummaryDto(
                TotalTopics: totalHy,
                CompletedTopics: completedHy,
                CriticalPendingCount: criticalPending,
                CompletionPercentage: hyCompletionPct
            )
        );
    }

    public List<HighYieldTopicDto> GetHighYieldTopics(string? subject = null, string? priority = null)
    {
        string query = "SELECT * FROM high_yield_topics WHERE 1=1";
        var pars = new List<(string, object?)>();

        if (!string.IsNullOrWhiteSpace(subject) && !string.Equals(subject, "all", StringComparison.OrdinalIgnoreCase))
        {
            query += " AND subject_key = @subj";
            pars.Add(("@subj", subject.Trim().ToLowerInvariant()));
        }

        if (!string.IsNullOrWhiteSpace(priority) && !string.Equals(priority, "all", StringComparison.OrdinalIgnoreCase))
        {
            query += " AND priority = @prio";
            pars.Add(("@prio", priority.Trim()));
        }

        query += " ORDER BY CASE priority WHEN 'Critical' THEN 1 WHEN 'High' THEN 2 ELSE 3 END, frequency_score DESC;";

        var rows = _db.Query(query, pars.ToArray());
        return rows.Select(r => new HighYieldTopicDto(
            Id: Convert.ToInt32(r["id"]),
            SubjectKey: r["subject_key"]?.ToString() ?? "",
            SubjectName: r["subject_name"]?.ToString() ?? "",
            TopicName: r["topic_name"]?.ToString() ?? "",
            Priority: r["priority"]?.ToString() ?? "Medium",
            FrequencyScore: Convert.ToInt32(r["frequency_score"]),
            AvgMarks: Convert.ToDouble(r["avg_marks"]),
            RecurrencePattern: r["recurrence_pattern"]?.ToString() ?? "",
            KeyFormulaOrConcept: r["key_formula_or_concept"]?.ToString() ?? "",
            IsCompleted: Convert.ToInt32(r["is_completed"]) == 1,
            Notes: r["notes"]?.ToString()
        )).ToList();
    }

    public HighYieldTopicDto? ToggleChecklistItem(int id, bool? isCompleted = null)
    {
        var existing = _db.QuerySingle("SELECT * FROM high_yield_topics WHERE id = @id", ("@id", id));
        if (existing == null) return null;

        bool currentStatus = Convert.ToInt32(existing["is_completed"]) == 1;
        bool nextStatus = isCompleted.HasValue ? isCompleted.Value : !currentStatus;

        _db.ExecuteNonQuery(
            "UPDATE high_yield_topics SET is_completed = @stat WHERE id = @id",
            ("@stat", nextStatus ? 1 : 0),
            ("@id", id)
        );

        var updated = _db.QuerySingle("SELECT * FROM high_yield_topics WHERE id = @id", ("@id", id));
        if (updated == null) return null;

        return new HighYieldTopicDto(
            Id: Convert.ToInt32(updated["id"]),
            SubjectKey: updated["subject_key"]?.ToString() ?? "",
            SubjectName: updated["subject_name"]?.ToString() ?? "",
            TopicName: updated["topic_name"]?.ToString() ?? "",
            Priority: updated["priority"]?.ToString() ?? "Medium",
            FrequencyScore: Convert.ToInt32(updated["frequency_score"]),
            AvgMarks: Convert.ToDouble(updated["avg_marks"]),
            RecurrencePattern: updated["recurrence_pattern"]?.ToString() ?? "",
            KeyFormulaOrConcept: updated["key_formula_or_concept"]?.ToString() ?? "",
            IsCompleted: Convert.ToInt32(updated["is_completed"]) == 1,
            Notes: updated["notes"]?.ToString()
        );
    }

    public HighYieldTopicDto? UpdateChecklistNotes(int id, string notes)
    {
        int affected = _db.ExecuteNonQuery(
            "UPDATE high_yield_topics SET notes = @notes WHERE id = @id",
            ("@notes", notes),
            ("@id", id)
        );
        if (affected == 0) return null;

        var updated = _db.QuerySingle("SELECT * FROM high_yield_topics WHERE id = @id", ("@id", id));
        if (updated == null) return null;

        return new HighYieldTopicDto(
            Id: Convert.ToInt32(updated["id"]),
            SubjectKey: updated["subject_key"]?.ToString() ?? "",
            SubjectName: updated["subject_name"]?.ToString() ?? "",
            TopicName: updated["topic_name"]?.ToString() ?? "",
            Priority: updated["priority"]?.ToString() ?? "Medium",
            FrequencyScore: Convert.ToInt32(updated["frequency_score"]),
            AvgMarks: Convert.ToDouble(updated["avg_marks"]),
            RecurrencePattern: updated["recurrence_pattern"]?.ToString() ?? "",
            KeyFormulaOrConcept: updated["key_formula_or_concept"]?.ToString() ?? "",
            IsCompleted: Convert.ToInt32(updated["is_completed"]) == 1,
            Notes: updated["notes"]?.ToString()
        );
    }

    public WeightedPaperResponse GenerateWeightedPaper(GenerateWeightedPaperRequest request)
    {
        int totalQuestions = request.QuestionCount > 0 ? request.QuestionCount : 25;
        int durationMinutes = request.DurationMinutes.HasValue && request.DurationMinutes.Value > 0
            ? request.DurationMinutes.Value
            : totalQuestions <= 10 ? 15
            : totalQuestions <= 25 ? 45
            : totalQuestions <= 35 ? 60
            : 180;

        string title = !string.IsNullOrWhiteSpace(request.Title)
            ? request.Title.Trim()
            : $"GATE CS Weighted Paper ({totalQuestions} Qs)";

        // 1. Calculate question distribution proportional to syllabus weightage
        var allocations = new List<PaperSubjectAllocation>();
        int allocatedCount = 0;

        foreach (var def in CanonicalSubjects)
        {
            // Initial proportional count
            int qCount = (int)Math.Round((def.Weightage / 100.0) * totalQuestions);
            if (qCount < 1 && totalQuestions >= 8) qCount = 1;
            allocations.Add(new PaperSubjectAllocation(
                SubjectKey: def.Key,
                SubjectName: def.Name,
                TargetPercent: def.Weightage,
                QuestionCount: qCount,
                Color: def.Color
            ));
            allocatedCount += qCount;
        }

        // Adjust rounding discrepancy so sum(qCount) == totalQuestions
        int diff = totalQuestions - allocatedCount;
        while (diff != 0)
        {
            if (diff > 0)
            {
                // Add to subjects in order of highest weightage
                var topSubj = allocations.OrderByDescending(a => a.TargetPercent).First();
                int idx = allocations.IndexOf(topSubj);
                allocations[idx] = topSubj with { QuestionCount = topSubj.QuestionCount + 1 };
                diff--;
            }
            else
            {
                // Remove from largest count
                var maxSubj = allocations.OrderByDescending(a => a.QuestionCount).First();
                int idx = allocations.IndexOf(maxSubj);
                allocations[idx] = maxSubj with { QuestionCount = Math.Max(1, maxSubj.QuestionCount - 1) };
                diff++;
            }
        }

        // 2. Fetch all questions from database to sample per subject
        var allQuestionsRows = _db.Query("SELECT * FROM questions ORDER BY RANDOM();");
        var selectedQuestions = new List<WeightedPaperQuestionDto>();
        var selectedIds = new HashSet<int>();

        foreach (var alloc in allocations)
        {
            var def = CanonicalSubjects.First(s => s.Key == alloc.SubjectKey);

            // Filter questions matching subject keywords
            var candidateRows = allQuestionsRows.Where(r =>
            {
                int qId = Convert.ToInt32(r["id"]);
                if (selectedIds.Contains(qId)) return false;

                string top = (r["topic"]?.ToString() ?? "").ToLowerInvariant();
                return def.TopicKeywords.Any(k => top.Contains(k));
            }).ToList();

            // Take needed count
            var picked = candidateRows.Take(alloc.QuestionCount).ToList();
            foreach (var r in picked)
            {
                int qId = Convert.ToInt32(r["id"]);
                selectedIds.Add(qId);

                List<string> options;
                try
                {
                    options = JsonSerializer.Deserialize<List<string>>(r["options_json"]?.ToString() ?? "[]") ?? new List<string>();
                }
                catch
                {
                    options = new List<string>();
                }

                selectedQuestions.Add(new WeightedPaperQuestionDto(
                    Id: qId,
                    SubjectKey: def.Key,
                    SubjectName: def.Name,
                    Topic: r["topic"]?.ToString() ?? def.Name,
                    Difficulty: r["difficulty"]?.ToString() ?? "medium",
                    Stem: r["stem"]?.ToString() ?? "",
                    Options: options,
                    CorrectAnswer: r["answer"]?.ToString(),
                    SolutionMd: r["solution_md"]?.ToString()
                ));
            }
        }

        // If some subjects had fewer questions in the DB than allocated, supplement from remaining pool
        if (selectedQuestions.Count < totalQuestions)
        {
            var remainingRows = allQuestionsRows.Where(r => !selectedIds.Contains(Convert.ToInt32(r["id"]))).ToList();
            int needed = totalQuestions - selectedQuestions.Count;

            foreach (var r in remainingRows.Take(needed))
            {
                int qId = Convert.ToInt32(r["id"]);
                selectedIds.Add(qId);

                string top = (r["topic"]?.ToString() ?? "").ToLowerInvariant();
                var matchedDef = CanonicalSubjects.FirstOrDefault(s => s.TopicKeywords.Any(k => top.Contains(k))) ?? CanonicalSubjects[0];

                List<string> options;
                try
                {
                    options = JsonSerializer.Deserialize<List<string>>(r["options_json"]?.ToString() ?? "[]") ?? new List<string>();
                }
                catch
                {
                    options = new List<string>();
                }

                selectedQuestions.Add(new WeightedPaperQuestionDto(
                    Id: qId,
                    SubjectKey: matchedDef.Key,
                    SubjectName: matchedDef.Name,
                    Topic: r["topic"]?.ToString() ?? matchedDef.Name,
                    Difficulty: r["difficulty"]?.ToString() ?? "medium",
                    Stem: r["stem"]?.ToString() ?? "",
                    Options: options,
                    CorrectAnswer: r["answer"]?.ToString(),
                    SolutionMd: r["solution_md"]?.ToString()
                ));
            }
        }

        // Shuffle questions so test mimics realistic randomized GATE question order
        var rnd = new Random();
        selectedQuestions = selectedQuestions.OrderBy(_ => rnd.Next()).ToList();

        // 3. Persist generated paper
        string paperId = "wp_" + Guid.NewGuid().ToString("N")[..12];
        string allocationsJson = JsonSerializer.Serialize(allocations);
        string questionsJson = JsonSerializer.Serialize(selectedQuestions);

        _db.ExecuteNonQuery(@"
            INSERT INTO weighted_practice_papers (
                id, title, question_count, duration_min, subject_distribution_json, questions_json, created_at
            ) VALUES (
                @id, @title, @count, @dur, @allocJson, @qJson, datetime('now')
            )
        ",
            ("@id", paperId),
            ("@title", title),
            ("@count", selectedQuestions.Count),
            ("@dur", durationMinutes),
            ("@allocJson", allocationsJson),
            ("@qJson", questionsJson)
        );

        // Also register in mock_test_sessions so user can optionally take it via standard Mock Exam simulation
        _db.ExecuteNonQuery(@"
            INSERT OR REPLACE INTO mock_test_sessions (
                id, title, question_count, duration_min, questions_json, status, started_at
            ) VALUES (
                @id, @title, @qCount, @dur, @qJson, 'in_progress', datetime('now')
            )
        ",
            ("@id", paperId),
            ("@title", title),
            ("@qCount", selectedQuestions.Count),
            ("@dur", durationMinutes),
            ("@qJson", questionsJson)
        );

        return new WeightedPaperResponse(
            PaperId: paperId,
            Title: title,
            TotalQuestions: selectedQuestions.Count,
            DurationMinutes: durationMinutes,
            SubjectAllocations: allocations,
            Questions: selectedQuestions,
            CreatedAt: DateTime.UtcNow
        );
    }

    public List<WeightedPaperSummaryDto> GetGeneratedPapers(int limit = 20)
    {
        var rows = _db.Query(
            "SELECT id, title, question_count, duration_min, created_at FROM weighted_practice_papers ORDER BY created_at DESC LIMIT @limit",
            ("@limit", limit)
        );

        return rows.Select(r => new WeightedPaperSummaryDto(
            PaperId: r["id"]?.ToString() ?? "",
            Title: r["title"]?.ToString() ?? "",
            TotalQuestions: Convert.ToInt32(r["question_count"]),
            DurationMinutes: Convert.ToInt32(r["duration_min"]),
            CreatedAt: DateTime.TryParse(r["created_at"]?.ToString(), out var dt) ? dt : DateTime.UtcNow
        )).ToList();
    }

    public WeightedPaperResponse? GetPaperById(string paperId)
    {
        var row = _db.QuerySingle(
            "SELECT * FROM weighted_practice_papers WHERE id = @id",
            ("@id", paperId)
        );
        if (row == null) return null;

        var allocations = JsonSerializer.Deserialize<List<PaperSubjectAllocation>>(
            row["subject_distribution_json"]?.ToString() ?? "[]"
        ) ?? new List<PaperSubjectAllocation>();

        var questions = JsonSerializer.Deserialize<List<WeightedPaperQuestionDto>>(
            row["questions_json"]?.ToString() ?? "[]"
        ) ?? new List<WeightedPaperQuestionDto>();

        return new WeightedPaperResponse(
            PaperId: row["id"]?.ToString() ?? "",
            Title: row["title"]?.ToString() ?? "",
            TotalQuestions: Convert.ToInt32(row["question_count"]),
            DurationMinutes: Convert.ToInt32(row["duration_min"]),
            SubjectAllocations: allocations,
            Questions: questions,
            CreatedAt: DateTime.TryParse(row["created_at"]?.ToString(), out var dt) ? dt : DateTime.UtcNow
        );
    }

    private record SubjectDefinition(
        string Key,
        string Name,
        double Weightage,
        double TargetHours,
        string Color,
        string[] TopicKeywords,
        string[] HighYieldHighlights
    );
}
