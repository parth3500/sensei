using System;
using System.Text.Json;
using Sensei.Core.Interfaces;

namespace Sensei.Components;

public static class DatabaseSeeder
{
    public static void Seed(IDatabaseComponent db)
    {
        // 1. Seed Subjects
        var subCount = db.QueryScalar<long>("SELECT COUNT(*) FROM subjects;");
        if (subCount == 0)
        {
            var subjects = new (string Name, string? Parent, double Weight, string Color)[]
            {
                ("GATE CS", null, 1.0, "#7c5cff"),
                ("Data Structures", "GATE CS", 1.2, "#3b82f6"),
                ("Algorithms", "GATE CS", 1.2, "#8b5cf6"),
                ("Operating Systems", "GATE CS", 1.0, "#ec4899"),
                ("Database Systems", "GATE CS", 1.0, "#10b981"),
                ("Computer Networks", "GATE CS", 1.0, "#f59e0b"),
                ("Theory of Computation", "GATE CS", 1.0, "#06b6d4"),
                ("Compilers", "GATE CS", 0.8, "#6366f1"),
                ("Discrete Mathematics", "GATE CS", 1.0, "#14b8a6"),
                ("General Aptitude", null, 0.8, "#84cc16"),
                ("Engineering Mathematics", null, 1.0, "#a855f7"),
                ("Java & OOP", null, 1.2, "#f89820")
            };

            foreach (var (name, parent, weight, color) in subjects)
            {
                int? parentId = null;
                if (parent != null)
                {
                    var pRow = db.QuerySingle("SELECT id FROM subjects WHERE name = @name", ("@name", parent));
                    if (pRow != null && pRow["id"] != null)
                        parentId = Convert.ToInt32(pRow["id"]);
                }

                db.ExecuteNonQuery(@"
                    INSERT INTO subjects (name, parent_id, weight, color, created_at)
                    VALUES (@name, @parentId, @weight, @color, datetime('now'))
                ", ("@name", name), ("@parentId", parentId), ("@weight", weight), ("@color", color));
            }
        }

        // Ensure "Java & OOP" subject exists
        var javaSubRow = db.QuerySingle("SELECT id FROM subjects WHERE name = @name", ("@name", "Java & OOP"));
        int? javaSubjectId = null;
        if (javaSubRow == null || javaSubRow["id"] == null)
        {
            var newId = db.InsertAndGetId(@"
                INSERT INTO subjects (name, parent_id, weight, color, created_at)
                VALUES (@name, NULL, 1.2, '#f89820', datetime('now'))
            ", ("@name", "Java & OOP"));
            javaSubjectId = (int)newId;
        }
        else
        {
            javaSubjectId = Convert.ToInt32(javaSubRow["id"]);
        }

        // 2. Seed Reminders
        var remCount = db.QueryScalar<long>("SELECT COUNT(*) FROM reminders;");
        if (remCount == 0)
        {
            var reminders = new (string Title, string Cron, string Channel, string Payload)[]
            {
                ("Morning Study Plan", "0 8 * * *", "webpush", "{\"type\":\"tasks\"}"),
                ("Midday Focus Check-in", "0 14 * * *", "webpush", "{\"type\":\"checkin\"}"),
                ("Evening Review & Weak Topics", "0 21 * * *", "webpush", "{\"type\":\"pending\"}"),
                ("Nightly Performance Report", "30 23 * * *", "both", "{\"type\":\"report\"}")
            };

            foreach (var (title, cron, channel, payload) in reminders)
            {
                db.ExecuteNonQuery(@"
                    INSERT INTO reminders (title, cron, channel, payload_json, enabled)
                    VALUES (@title, @cron, @channel, @payload, 1)
                ", ("@title", title), ("@cron", cron), ("@channel", channel), ("@payload", payload));
            }
        }

        // 3. Seed Questions
        var existingStems = new HashSet<string>(
            db.Query("SELECT stem FROM questions;").ConvertAll(r => r["stem"]?.ToString() ?? "")
        );

        var seedQuestions = new (string Topic, string Difficulty, string Stem, string[] Options, string Answer, string Solution)[]
        {
            (
                "Arrays & Complexity",
                "easy",
                "What is the worst-case time complexity of accessing an element in an array by its index?",
                new[] { "O(1)", "O(n)", "O(log n)", "O(n log n)" },
                "O(1)",
                "Arrays provide contiguous memory allocation, allowing direct pointer arithmetic to compute element address in O(1) time."
            ),
            (
                "Sorting Algorithms",
                "medium",
                "What is the worst-case time complexity of QuickSort when the pivot is always chosen as the smallest element?",
                new[] { "O(n log n)", "O(n^2)", "O(n)", "O(log n)" },
                "O(n^2)",
                "If the pivot is always extreme (smallest/largest), the recursion tree becomes skewed of depth n, leading to n * (n-1)/2 comparisons = O(n^2)."
            ),
            (
                "CPU Scheduling",
                "medium",
                "Which CPU scheduling algorithm is preemptive and uses a fixed time quantum?",
                new[] { "Round Robin", "First-Come First-Served", "Shortest Job First (non-preemptive)", "Priority Scheduling (non-preemptive)" },
                "Round Robin",
                "Round Robin allocates a fixed time quantum per process and preempts the running process if it does not finish within the slice."
            ),
            (
                "Relational Database",
                "hard",
                "A relation R(A, B, C, D) with functional dependencies A -> B, B -> C, C -> D is in which normal form?",
                new[] { "1NF", "2NF", "3NF", "BCNF" },
                "2NF",
                "The candidate key is A. Transitive dependencies exist (A -> B -> C -> D), so it violates 3NF, but satisfies 2NF since there are no partial dependencies."
            ),
            (
                "Computer Networks",
                "medium",
                "In TCP protocol, what is the default header size without any optional fields?",
                new[] { "20 bytes", "32 bytes", "40 bytes", "16 bytes" },
                "20 bytes",
                "The minimum header size of a standard TCP segment is 20 bytes (5 words of 32 bits)."
            ),
            (
                "Graph Algorithms",
                "medium",
                "What is the time complexity of Dijkstra's single-source shortest path algorithm using a Min-Binary Heap?",
                new[] { "O((V + E) log V)", "O(V^2)", "O(V * E)", "O(E + V)" },
                "O((V + E) log V)",
                "Each vertex extract-min takes O(log V) and each edge relaxation takes O(log V) decrease-key, totaling O((V + E) log V)."
            ),
            (
                "Theory of Computation",
                "easy",
                "Which class of languages is accepted by a Deterministic Finite Automaton (DFA)?",
                new[] { "Regular Languages", "Context-Free Languages", "Context-Sensitive Languages", "Recursively Enumerable" },
                "Regular Languages",
                "DFAs recognize exactly the class of regular languages, equivalent to NFAs and regular expressions."
            ),
            (
                "Compiler Design",
                "medium",
                "In compiler architecture, which phase performs syntax analysis and constructs parse trees?",
                new[] { "Parser", "Lexer", "Semantic Analyzer", "Code Generator" },
                "Parser",
                "The parser takes tokens from lexical analysis and applies grammar rules to generate abstract syntax trees."
            ),
            (
                "Operating Systems",
                "hard",
                "Belady's Anomaly occurs in which page replacement algorithm?",
                new[] { "FIFO (First In First Out)", "LRU (Least Recently Used)", "Optimal Page Replacement", "LFU" },
                "FIFO (First In First Out)",
                "FIFO can suffer from Belady's anomaly, where increasing the number of page frames leads to more page faults."
            ),
            (
                "Discrete Mathematics",
                "medium",
                "How many edges are in a complete graph K_n with n vertices?",
                new[] { "n(n-1)/2", "n^2", "2n", "n(n+1)/2" },
                "n(n-1)/2",
                "In complete graph K_n, each vertex is connected to n-1 other vertices. Edges = C(n,2) = n(n-1)/2."
            ),
            (
                "Cache Memory",
                "medium",
                "A 32-bit memory address uses a 4-way set-associative cache with 64 KB cache size and 32-byte cache blocks. How many bits are used for the Index field?",
                new[] { "9 bits", "10 bits", "11 bits", "12 bits" },
                "9 bits",
                "Cache size = 64 KB = 2^16 bytes. Block size = 32 bytes = 2^5 bytes. Total blocks = 2^11 = 2048. Number of sets = 2048 / 4 = 512 = 2^9. Hence, 9 bits for Index."
            ),
            (
                "Pipelining & Hazards",
                "medium",
                "In a standard 5-stage RISC pipeline (IF, ID, EX, MEM, WB) without operand forwarding, how many stall cycles are required for a RAW dependency where instruction i2 uses the loaded value from i1?",
                new[] { "2 stall cycles", "1 stall cycle", "3 stall cycles", "0 stall cycles" },
                "2 stall cycles",
                "Without forwarding, instruction i1 completes write back in WB (stage 5), meaning i2 can only read the register in ID (stage 2) after WB completes, incurring 2 stall cycles."
            ),
            (
                "Virtual Memory",
                "medium",
                "In a 32-bit virtual address space with two-level paging, page size is 4 KB and page table entry size is 4 bytes. Virtual address is divided as (p1, p2, offset). What are the sizes of (p1, p2, offset)?",
                new[] { "(10, 10, 12)", "(12, 10, 10)", "(10, 12, 10)", "(8, 12, 12)" },
                "(10, 10, 12)",
                "Page size = 4 KB = 2^12 bytes, so offset = 12 bits. Each page table fits in 4 KB / 4 B = 1024 entries = 2^10 entries (p2 = 10 bits). p1 = 32 - 12 - 10 = 10 bits."
            ),
            (
                "Process Synchronization",
                "easy",
                "A counting semaphore S is initialized to 7. If 20 P operations (wait) and 15 V operations (signal) are executed on S, what is the resulting value of S?",
                new[] { "2", "12", "0", "-2" },
                "2",
                "Semaphore value = Initial + Signals - Waits = 7 + 15 - 20 = 22 - 20 = 2."
            ),
            (
                "Deadlock Avoidance",
                "medium",
                "A system has 4 processes and 9 resources of the same type. Each process needs at most 3 units. Can a deadlock occur?",
                new[] { "Deadlock can never occur", "Deadlock will always occur", "Deadlock occurs only if quantum expires", "Deadlock depends on process priorities" },
                "Deadlock can never occur",
                "Condition for deadlock-free system: R >= Sum(Max_i - 1) + 1 = 4 * (3 - 1) + 1 = 9. Since R = 9 >= 9, deadlock is strictly impossible."
            ),
            (
                "Database Normalization",
                "medium",
                "Which normal form strictly eliminates all non-trivial functional dependencies X -> Y where X is not a superkey?",
                new[] { "Boyce-Codd Normal Form (BCNF)", "Third Normal Form (3NF)", "Fourth Normal Form (4NF)", "Second Normal Form (2NF)" },
                "Boyce-Codd Normal Form (BCNF)",
                "BCNF requires that for every non-trivial functional dependency X -> Y, X must be a superkey."
            ),
            (
                "Transactions & Concurrency",
                "medium",
                "Which concurrency control protocol guarantees conflict serializability but does NOT prevent cascading rollbacks or deadlocks?",
                new[] { "Basic Two-Phase Locking (2PL)", "Strict 2PL", "Rigorous 2PL", "Timestamp Ordering with Thomas Write Rule" },
                "Basic Two-Phase Locking (2PL)",
                "Basic 2PL ensures conflict serializability, but releasing locks before commit permits cascading aborts, and cyclic waiting can produce deadlocks."
            ),
            (
                "B+ Trees",
                "medium",
                "In a B+ tree of order p (maximum child pointers per node), what is the minimum number of child pointers an internal node (other than root) can have?",
                new[] { "ceil(p / 2)", "floor(p / 2)", "p / 2 - 1", "ceil((p - 1) / 2)" },
                "ceil(p / 2)",
                "By standard B+ tree definition, internal nodes (except root) must contain at least ceil(p/2) child pointers."
            ),
            (
                "Sliding Window Protocol",
                "easy",
                "In a Go-Back-N ARQ protocol, if the sequence number field in the frame header is k bits wide, what is the maximum sender window size?",
                new[] { "2^k - 1", "2^k", "2^(k-1)", "2^(k-1) - 1" },
                "2^k - 1",
                "In Go-Back-N, the sender window size cannot exceed 2^k - 1 to distinguish between new frames and retransmissions when ACKs are lost."
            ),
            (
                "Congestion Control",
                "medium",
                "In TCP Reno, what happens to the congestion window (cwnd) when packet loss is detected via 3 duplicate ACKs?",
                new[] { "cwnd is halved (Fast Recovery)", "cwnd is reset to 1 MSS", "cwnd remains unchanged", "cwnd is doubled" },
                "cwnd is halved (Fast Recovery)",
                "TCP Reno invokes Fast Retransmit and Fast Recovery upon 3 duplicate ACKs, setting ssthresh = cwnd / 2 and cwnd = ssthresh (halving the window)."
            ),
            (
                "Routing Algorithms",
                "easy",
                "The 'Count to Infinity' problem is an inherent disadvantage of which routing algorithm?",
                new[] { "Distance Vector Routing (Bellman-Ford)", "Link State Routing (Dijkstra)", "Hierarchical Routing", "Flooding" },
                "Distance Vector Routing (Bellman-Ford)",
                "Distance Vector Routing suffers from count-to-infinity due to step-by-step uncoordinated propagation of path costs."
            ),
            (
                "Trees & Balancing",
                "medium",
                "What is the maximum height of an AVL tree with 12 nodes (height of single-node tree = 0)?",
                new[] { "4", "3", "5", "6" },
                "4",
                "Minimum nodes for height h: N(0)=1, N(1)=2, N(2)=4, N(3)=7, N(4)=12. Hence with 12 nodes, the maximum height is 4."
            ),
            (
                "Hashing",
                "medium",
                "In open addressing with linear probing, h(k, i) = (k mod 11 + i) mod 11. If keys [22, 33, 44] are inserted into an empty table of size 11, at which index is 44 stored?",
                new[] { "2", "0", "1", "3" },
                "2",
                "22 mod 11 = 0 (pos 0). 33 mod 11 = 0 -> probes pos 1. 44 mod 11 = 0 -> probes pos 1 (occupied) -> probes pos 2 (free)."
            ),
            (
                "Dynamic Programming",
                "medium",
                "What is the space complexity of calculating Longest Common Subsequence (LCS) of two strings of lengths m and n using row-optimized DP?",
                new[] { "O(min(m, n))", "O(m * n)", "O(m + n)", "O(log(m + n))" },
                "O(min(m, n))",
                "Only the previous row is required to compute the next row. By keeping the shorter string as columns, auxiliary space is O(min(m, n))."
            ),
            (
                "Minimum Spanning Tree",
                "easy",
                "If a connected, undirected graph G = (V, E) has all edge weights distinct, which statement is ALWAYS TRUE?",
                new[] { "G has a unique Minimum Spanning Tree (MST)", "G has multiple MSTs", "Kruskal's algorithm fails", "Shortest path tree is identical to MST" },
                "G has a unique Minimum Spanning Tree (MST)",
                "When all edge weights are distinct in an undirected connected graph, the MST is strictly unique."
            ),
            (
                "Asymptotic Complexity",
                "medium",
                "Solve the recurrence relation T(n) = 4T(n/2) + O(n^2) using the Master Theorem.",
                new[] { "Theta(n^2 log n)", "Theta(n^2)", "Theta(n^3)", "Theta(n log n)" },
                "Theta(n^2 log n)",
                "a=4, b=2, n^(log_2 4) = n^2. Since f(n) = Theta(n^2), by Master Theorem Case 2, T(n) = Theta(n^2 log n)."
            ),
            (
                "Automata Theory",
                "medium",
                "Is the language L = { a^n b^n c^n | n >= 1 } context-free?",
                new[] { "No, it is Context-Sensitive", "Yes, accepted by a Pushdown Automaton", "Yes, it is Regular", "No, it is Unrestricted only" },
                "No, it is Context-Sensitive",
                "Using the Pumping Lemma for CFLs, a single stack cannot simultaneously verify matching counts for three independent symbols."
            ),
            (
                "Turing Machines & Decidability",
                "medium",
                "Which of the following problems is DECIDABLE?",
                new[] { "Emptiness problem for Regular Languages", "Halting problem for Turing Machines", "Equivalence problem for Context-Free Grammars", "Ambiguity problem for Context-Free Grammars" },
                "Emptiness problem for Regular Languages",
                "Emptiness for DFAs is decidable via BFS/DFS reachability to any final state. CFG equivalence and ambiguity are undecidable."
            ),
            (
                "Parsing & Grammars",
                "medium",
                "Which bottom-up parser has the exact same number of states as SLR(1) for any grammar?",
                new[] { "LALR(1)", "LR(1)", "Canonical LR(1)", "LL(1)" },
                "LALR(1)",
                "LALR(1) merges states with identical LR(0) cores, yielding the exact same state count as SLR(1) and LR(0)."
            ),
            (
                "Syntax-Directed Translation",
                "easy",
                "In Syntax-Directed Definition (SDD), an attribute is INHERITED if its value at a node is computed from:",
                new[] { "Its parent or sibling nodes", "Its children nodes only", "Lexical tokens only", "Global constants only" },
                "Its parent or sibling nodes",
                "Inherited attributes are defined in terms of attribute values of the parent and/or siblings in the parse tree."
            ),
            (
                "Digital Logic",
                "medium",
                "How many 2-to-1 multiplexers are required to construct a 16-to-1 multiplexer?",
                new[] { "15", "16", "8", "31" },
                "15",
                "Tree of 2-to-1 MUXes: 8 (stage 1) + 4 (stage 2) + 2 (stage 3) + 1 (final) = 15 multiplexers."
            ),
            (
                "Combinational Circuits",
                "easy",
                "What is the dual of the Boolean expression: A + B * (C + 0)?",
                new[] { "A * (B + (C * 1))", "A' + B' * (C' + 1)", "A * B + C * 1", "(A + B) * (C + 1)" },
                "A * (B + (C * 1))",
                "The dual is formed by interchanging + and *, and 0 and 1: A * (B + (C * 1))."
            ),
            (
                "Propositional Logic",
                "easy",
                "What is the contrapositive of the conditional statement P -> Q?",
                new[] { "~Q -> ~P", "~P -> ~Q", "Q -> P", "P and ~Q" },
                "~Q -> ~P",
                "The contrapositive of P -> Q is ~Q -> ~P, and both propositions are logically equivalent."
            ),
            (
                "Probability",
                "medium",
                "Two fair six-sided dice are rolled simultaneously. What is the probability that the sum of the numbers is at least 10?",
                new[] { "1/6", "1/12", "1/4", "5/36" },
                "1/6",
                "Pairs with sum >= 10: Sum 10 (4,6; 5,5; 6,4) [3], Sum 11 (5,6; 6,5) [2], Sum 12 (6,6) [1]. Total = 6 / 36 = 1/6."
            ),
            (
                "Linear Algebra",
                "easy",
                "If the eigenvalues of a 2x2 matrix A are 3 and 5, what is the determinant of matrix A?",
                new[] { "15", "8", "2", "34" },
                "15",
                "The determinant of any square matrix is equal to the product of its eigenvalues: Det(A) = 3 * 5 = 15."
            ),
            (
                "Combinatorics",
                "medium",
                "In how many ways can 5 distinct balls be distributed into 3 identical boxes such that no box remains empty?",
                new[] { "25", "15", "50", "90" },
                "25",
                "Given by Stirling numbers of the second kind S(5, 3) = (3^5 - 3*2^5 + 3*1^5) / 3! = (243 - 96 + 3) / 6 = 150 / 6 = 25."
            ),
            (
                "Floating Point Representation",
                "medium",
                "In IEEE 754 single-precision floating point standard (32-bit), how many bits are allocated for sign, exponent, and mantissa respectively?",
                new[] { "1, 8, 23", "1, 11, 52", "1, 7, 24", "1, 8, 24" },
                "1, 8, 23",
                "IEEE 754 32-bit float uses 1 bit for sign, 8 bits for biased exponent (bias = 127), and 23 bits for fraction/mantissa."
            ),
            (
                "Sequential Circuits",
                "easy",
                "Which flip-flop architecture is specifically designed to eliminate the race-around condition present in level-triggered JK flip-flops?",
                new[] { "Master-Slave JK Flip-Flop", "SR Flip-Flop", "T Flip-Flop with delay", "D Latch" },
                "Master-Slave JK Flip-Flop",
                "A Master-Slave JK flip-flop uses two cascaded stages clocked on opposite clock levels, preventing input changes from feeding around during the clock pulse."
            ),
            (
                "Disk Scheduling",
                "medium",
                "Which disk scheduling algorithm services requests only in one direction until reaching the innermost or outermost cylinder, then reverses direction?",
                new[] { "SCAN (Elevator Algorithm)", "C-SCAN", "LOOK", "SSTF" },
                "SCAN (Elevator Algorithm)",
                "SCAN moves the disk arm across all tracks servicing requests in that direction until reaching the disk boundary, then reverses direction."
            ),
            (
                "IPv4 Addressing & Subnetting",
                "medium",
                "An organization is allocated the block 198.51.100.0/24. If the organization creates 8 equal subnets, what is the subnet mask and usable hosts per subnet?",
                new[] { "255.255.255.224 and 30 hosts", "255.255.255.240 and 14 hosts", "255.255.255.192 and 62 hosts", "255.255.255.224 and 32 hosts" },
                "255.255.255.224 and 30 hosts",
                "To form 8 = 2^3 subnets, 3 subnet bits are borrowed from host part: prefix becomes /27 -> mask 255.255.255.224. Usable hosts = 2^5 - 2 = 30."
            ),
            (
                "Shortest Paths & Negative Cycles",
                "medium",
                "Which graph algorithm can detect negative-weight cycles reachable from a source vertex in O(V * E) time?",
                new[] { "Bellman-Ford Algorithm", "Dijkstra's Algorithm", "Floyd-Warshall Algorithm", "Prim's Algorithm" },
                "Bellman-Ford Algorithm",
                "Bellman-Ford relaxes all edges V-1 times. If an edge can still be relaxed in the V-th iteration, a negative-weight cycle exists."
            ),
            (
                "Chomsky Hierarchy",
                "medium",
                "A Deterministic Pushdown Automaton (DPDA) cannot recognize which of the following languages?",
                new[] { "All palindromes { w w^R | w in {0,1}* }", "Even length strings { w | |w| mod 2 = 0 }", "Matched parentheses { a^n b^n | n >= 0 }", "Equal number of 0s and 1s" },
                "All palindromes { w w^R | w in {0,1}* }",
                "Even palindromes without a middle marker require guessing the center point nondeterministically, which cannot be done by a deterministic PDA."
            ),
            (
                "Java - Collections",
                "medium",
                "In Java 8 and later, what data structure does HashMap transition into for a bucket when the number of elements exceeds TREEIFY_THRESHOLD (8) and table capacity is at least 64?",
                new[] { "Red-Black Tree", "AVL Tree", "B+ Tree", "SkipList" },
                "Red-Black Tree",
                "In Java 8, when a bucket's collision chain exceeds TREEIFY_THRESHOLD (8) and the table capacity is >= 64 (MIN_TREEIFY_CAPACITY), HashMap replaces the linked list node chain with a balanced Red-Black Tree (TreeNode), improving worst-case search complexity from O(n) to O(log n)."
            ),
            (
                "Java - Concurrency",
                "medium",
                "In the Java Memory Model (JMM), which keyword establishes a happens-before relationship ensuring memory visibility without acquiring an exclusive monitor lock?",
                new[] { "volatile", "transient", "final", "strictfp" },
                "volatile",
                "A write to a volatile field happens-before every subsequent read of that same field. It acts as a memory barrier preventing compiler reordering and flushing changes to main memory without acquiring monitor locks."
            ),
            (
                "Java - Concurrency",
                "hard",
                "In ConcurrentHashMap (Java 8+), how is thread-safe insertion implemented for empty table buckets?",
                new[] { "Compare-And-Swap (CAS) on null node pointer", "Segment-level ReentrantLock", "Table-wide synchronized block", "ReadWriteLock" },
                "Compare-And-Swap (CAS) on null node pointer",
                "Java 8 redesigned ConcurrentHashMap removing the Java 7 Segment lock array. If a bucket bin is empty, it initializes the bin using lock-free CAS (Unsafe.compareAndSwapObject / VarHandle). If collision occurs, it synchronizes only on the head node of that bin."
            ),
            (
                "Java - JVM Internals",
                "hard",
                "Which garbage collector in modern OpenJDK aims for sub-millisecond maximum pause times regardless of heap size, utilizing colored pointers and load barriers?",
                new[] { "ZGC (Z Garbage Collector)", "G1 GC", "Parallel GC", "Serial GC" },
                "ZGC (Z Garbage Collector)",
                "ZGC is a low-latency concurrent generational garbage collector introduced in JDK 11/15. It performs all heavy phases concurrently (marking, relocation) using 64-bit colored pointers and load barriers to achieve sub-millisecond max pause times even on multi-terabyte heaps."
            ),
            (
                "Java - JVM Internals",
                "medium",
                "In Java 8 and later, what replaced PermGen (Permanent Generation) for storing class metadata, and where is it allocated?",
                new[] { "Metaspace, allocated in native OS memory", "Eden Space, allocated in young generation heap", "Code Cache, allocated in CPU registers", "Survivor Space, allocated in old generation heap" },
                "Metaspace, allocated in native OS memory",
                "Java 8 completely removed PermGen and replaced it with Metaspace. Metaspace stores class definitions, runtime constant pools, and method metadata in native process memory (unlimited by default, configurable via -XX:MaxMetaspaceSize), preventing java.lang.OutOfMemoryError: PermGen space."
            ),
            (
                "Java - Streams & Lambdas",
                "medium",
                "In Java Stream API, which of the following operations is a stateful intermediate operation?",
                new[] { "sorted()", "map()", "filter()", "flatMap()" },
                "sorted()",
                "sorted() must buffer all upstream elements to perform comparisons before emitting the first element, making it a stateful intermediate operation. In contrast, map(), filter(), and flatMap() are stateless intermediate operations that process elements lazily one-by-one."
            ),
            (
                "Java - Streams & Lambdas",
                "easy",
                "What is the result of invoking Optional.of(null) in Java 8+?",
                new[] { "NullPointerException is thrown immediately", "Returns an empty Optional (Optional.empty())", "Returns Optional containing null", "Returns null" },
                "NullPointerException is thrown immediately",
                "Optional.of(value) strictly requires a non-null argument and immediately throws NullPointerException if value is null. To safely wrap a potentially null reference, Optional.ofNullable(value) must be used."
            ),
            (
                "Java - OOP Concepts",
                "medium",
                "What happens if a class implements two interfaces that both declare identical default methods with the exact same signature?",
                new[] { "Compile-time error unless the implementing class explicitly overrides the method", "The first declared interface in the 'implements' clause takes precedence", "The JVM resolves it at runtime via dynamic dispatch", "Both default implementations execute sequentially" },
                "Compile-time error unless the implementing class explicitly overrides the method",
                "Java compiler flags this as an ambiguous diamond inheritance conflict at compile-time. The implementing class must explicitly override the conflicting method and can optionally delegate using InterfaceName.super.methodName()."
            ),
            (
                "Java - Collections",
                "easy",
                "What is the contract between equals() and hashCode() in Java?",
                new[] { "If o1.equals(o2) is true, then o1.hashCode() must equal o2.hashCode()", "If o1.hashCode() == o2.hashCode(), then o1.equals(o2) must be true", "Both hashCode() and equals() must be identical in return type", "No relationship exists" },
                "If o1.equals(o2) is true, then o1.hashCode() must equal o2.hashCode()",
                "The fundamental Java Object contract states: if two objects are equal according to equals(Object), their hashCode() MUST return the same integer. The converse is false: two unequal objects may have the same hash code (hash collision)."
            ),
            (
                "Java - Exception Handling",
                "medium",
                "In a try-with-resources statement, in what order are the AutoCloseable resources closed?",
                new[] { "Reverse order of their creation/declaration", "Same order of their creation/declaration", "Arbitrary non-deterministic order", "Simultaneously in parallel threads" },
                "Reverse order of their creation/declaration",
                "try (Resource r1 = ...; Resource r2 = ...) closes resources in reverse order (r2.close() followed by r1.close()). This mirrors stack unwinding and ensures dependent downstream resources close before their underlying dependencies."
            ),
            (
                "Java - Core Concepts",
                "easy",
                "Where are String literals stored in Java, and what is the effect of invoking s.intern()?",
                new[] { "In the String Constant Pool; returns canonical reference from pool", "On the thread stack; prevents garbage collection", "In Metaspace; converts String to char array", "In CPU cache; accelerates substring operations" },
                "In the String Constant Pool; returns canonical reference from pool",
                "String literals are stored in the String Constant Pool (allocated inside Java heap memory since Java 7). Calling s.intern() checks the pool: if an equal string exists, its reference is returned; otherwise s is added to the pool and returned."
            ),
            (
                "Java - Generics",
                "medium",
                "According to the PECS rule (Producer Extends, Consumer Super) in Java Generics, which wildcard should be used when a collection produces data to be read?",
                new[] { "<? extends T>", "<? super T>", "<?>", "<T>" },
                "<? extends T>",
                "PECS principle (Joshua Bloch): 'Producer Extends, Consumer Super'. If your method reads instances from a collection (it produces items), use <? extends T>. If your method inserts instances into a collection (it consumes items), use <? super T>."
            ),
            (
                "Java - Concurrency",
                "medium",
                "What is the key difference between ExecutorService.submit(Callable) and ExecutorService.execute(Runnable)?",
                new[] { "submit() returns a Future allowing result retrieval and exception inspection; execute() returns void", "execute() runs on background threads whereas submit() runs synchronously on calling thread", "submit() is deprecated in favor of execute()", "execute() supports timeouts whereas submit() does not" },
                "submit() returns a Future allowing result retrieval and exception inspection; execute() returns void",
                "submit() accepts Callable<T> or Runnable and returns a Future<T> which captures return values and thrown exceptions via future.get(). execute() is a void method from Executor interface that cannot return values."
            ),
            (
                "Java - OOP Concepts",
                "easy",
                "Can a static method be overridden in Java?",
                new[] { "No, static methods cannot be overridden; re-declaring them in a subclass is method hiding", "Yes, static methods participate in runtime polymorphism via dynamic dispatch", "Yes, but only if declared with public access modifier", "Yes, if the superclass method is not marked final" },
                "No, static methods cannot be overridden; re-declaring them in a subclass is method hiding",
                "Static methods are bound at compile time based on the reference type (invokestatic), not runtime object instance. If a subclass defines a static method with the same signature, it hides (shadows) the parent method rather than overriding it."
            ),
            (
                "Java - Memory Model",
                "easy",
                "In Java, what makes an object eligible for garbage collection?",
                new[] { "It is unreachable through any chain of strong references from GC roots", "Its reference count decrements to zero", "Its destructor method is called immediately", "It is moved to Metaspace" },
                "It is unreachable through any chain of strong references from GC roots",
                "HotSpot JVM uses tracing garbage collection (root set reachability), NOT reference counting. An object is eligible for GC when it cannot be reached by traversing reference chains originating from GC roots (local stack variables, active thread references, static class fields, JNI handles)."
            )
        };

        foreach (var (topic, diff, stem, opts, ans, sol) in seedQuestions)
        {
            if (!existingStems.Contains(stem))
            {
                db.ExecuteNonQuery(@"
                    INSERT INTO questions (topic, difficulty, stem, options_json, answer, solution_md, source, tags_json, sr_ease, sr_interval, sr_due, sr_reps, created_at)
                    VALUES (@topic, @diff, @stem, @opts, @ans, @sol, 'GATE-PYQ', '[""GATE"",""CS""]', 2.5, 0, date('now'), 0, datetime('now'))
                ",
                    ("@topic", topic),
                    ("@diff", diff),
                    ("@stem", stem),
                    ("@opts", JsonSerializer.Serialize(opts)),
                    ("@ans", ans),
                    ("@sol", sol)
                );
            }
        }
    }
}

