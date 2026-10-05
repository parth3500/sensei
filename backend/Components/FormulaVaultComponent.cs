using System;
using System.Collections.Generic;
using System.Text;
using Sensei.Core.Interfaces;
using Sensei.Core.Models;

namespace Sensei.Components;

public class FormulaVaultComponent : IFormulaVaultComponent
{
    private readonly IDatabaseComponent _db;

    public FormulaVaultComponent(IDatabaseComponent db)
    {
        _db = db;
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        _db.ExecuteScript(@"
            CREATE TABLE IF NOT EXISTS formula_vault (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                category TEXT NOT NULL,
                title TEXT NOT NULL,
                formula TEXT NOT NULL,
                description TEXT,
                key_variables TEXT,
                example TEXT,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );
            CREATE VIEW IF NOT EXISTS formula_items AS SELECT * FROM formula_vault;
        ");

        SeedFormulas();
    }

    private void SeedFormulas()
    {
        var existingTitles = new HashSet<string>(
            _db.Query("SELECT title FROM formula_vault;").ConvertAll(r => r["title"]?.ToString() ?? "")
        );

        var initialFormulas = new[]
        {
            (
                Category: "Algorithms & Complexity",
                Title: "Master Theorem for Divide & Conquer",
                Formula: "T(n) = a T(n/b) + Θ(n^k log^p n)",
                Description: "Solves recurrences where a problem of size n is divided into a subproblems of size n/b.",
                KeyVariables: "a ≥ 1 (subproblems), b > 1 (divisor), k ≥ 0, p ≥ 0. Compare log_b(a) with k.",
                Example: "Merge Sort: a=2, b=2, k=1, p=0 => log_2(2) = 1 = k => Case 2 => T(n) = Θ(n log n)"
            ),
            (
                Category: "Algorithms & Complexity",
                Title: "Dijkstra Shortest Path Complexity",
                Formula: "O((V + E) log V) with Binary Min-Heap; O(E + V log V) with Fibonacci Heap",
                Description: "Finds single-source shortest paths in weighted graphs with non-negative edge weights.",
                KeyVariables: "V = Number of vertices, E = Number of edges",
                Example: "Dense graph where E ≈ V²: Binary heap gives O(V² log V), Fibonacci gives O(V²)"
            ),
            (
                Category: "Discrete Mathematics",
                Title: "Euler's Formula for Planar Graphs",
                Formula: "V - E + F = 2  (for connected planar graphs)",
                Description: "Relates vertices (V), edges (E), and faces/regions (F including infinite external face).",
                KeyVariables: "V = Vertices, E = Edges, F = Faces. Corollary: E ≤ 3V - 6 for V ≥ 3.",
                Example: "Planar graph with V=6, E=9: F = 2 - V + E = 2 - 6 + 9 = 5 faces."
            ),
            (
                Category: "Discrete Mathematics",
                Title: "Inclusion-Exclusion Principle (3 Sets)",
                Formula: "|A ∪ B ∪ C| = |A| + |B| + |C| - (|A ∩ B| + |B ∩ C| + |A ∩ C|) + |A ∩ B ∩ C|",
                Description: "Computes the cardinality of the union of three finite sets without double-counting overlaps.",
                KeyVariables: "A, B, C = Finite sets; |·| = set cardinality",
                Example: "In class of 100: 50 take Math, 40 take CS, 30 take Stats. Apply formula to find unique students."
            ),
            (
                Category: "Operating Systems",
                Title: "Banker's Algorithm Safety Condition",
                Formula: "Need[i][j] ≤ Available[j], where Need[i][j] = Max[i][j] - Allocation[i][j]",
                Description: "Deadlock avoidance test ensuring at least one safe sequence of process execution exists.",
                KeyVariables: "Need[i][j] = Remaining required resource j; Available[j] = Free unallocated resource j",
                Example: "If Work ≥ Need[i], then P_i can finish, release its allocation, and Work = Work + Allocation[i]."
            ),
            (
                Category: "Operating Systems",
                Title: "Effective Memory Access Time (EMAT with TLB)",
                Formula: "EMAT = h × (t_tlb + t_mem) + (1 - h) × (t_tlb + 2 × t_mem)",
                Description: "Average time needed to read a memory address with a single-level page table and TLB cache.",
                KeyVariables: "h = TLB hit ratio (0 ≤ h ≤ 1), t_tlb = TLB search time, t_mem = Main memory access time",
                Example: "If h = 0.90, t_tlb = 20ns, t_mem = 100ns: EMAT = 0.9(120) + 0.1(220) = 108 + 22 = 130 ns."
            ),
            (
                Category: "Computer Networks",
                Title: "Shannon Channel Capacity",
                Formula: "C = B × log₂(1 + SNR)",
                Description: "Theoretical upper bound on maximum error-free data transfer rate over a noisy channel.",
                KeyVariables: "C = Capacity (bps), B = Bandwidth (Hz), SNR = Signal-to-noise ratio in linear power scale (P_signal / P_noise)",
                Example: "For B = 4000 Hz, SNR = 1023: C = 4000 × log₂(1024) = 4000 × 10 = 40,000 bps = 40 kbps."
            ),
            (
                Category: "Computer Networks",
                Title: "Sliding Window Protocol Efficiency",
                Formula: "η = W / (1 + 2a), where a = T_prop / T_trans",
                Description: "Channel utilization efficiency for ARQ protocols (Go-Back-N & Selective Repeat).",
                KeyVariables: "W = Window size (W=1 for Stop-and-Wait), T_prop = Propagation delay, T_trans = Transmission delay",
                Example: "If T_prop = 40ms, T_trans = 10ms => a = 4. For Stop-and-Wait (W=1), η = 1 / (1 + 8) = 1/9 ≈ 11.1%."
            ),
            (
                Category: "Theory of Computation",
                Title: "Pumping Lemma for Regular Languages",
                Formula: "∀w ∈ L (|w| ≥ p) ∃ x,y,z: w = xyz, |y| ≥ 1, |xy| ≤ p, ∀i ≥ 0 : x y^i z ∈ L",
                Description: "Fundamental property of all regular languages used in proofs by contradiction for non-regularity.",
                KeyVariables: "p = Pumping length (number of DFA states), w = string chosen with length ≥ p",
                Example: "To show L = {0^n 1^n | n ≥ 0} is non-regular, pick w = 0^p 1^p. Pumping y changes count of 0s only."
            ),
            (
                Category: "Database Management",
                Title: "B+ Tree Order & Max Capacity",
                Formula: "Order m: Max keys = m - 1; Min keys (Internal) = ⌈m/2⌉ - 1; Min keys (Leaf) = ⌊m/2⌋",
                Description: "Node capacity equations for balanced multiway B+ tree index structures.",
                KeyVariables: "m = Tree order (maximum child pointers per internal node)",
                Example: "Block = 1024B, Pointer = 8B, Key = 16B: m × 8 + (m - 1) × 16 ≤ 1024 => 24m ≤ 1040 => m = 43."
            ),
            (
                Category: "Java",
                Title: "Java Collections Framework Hierarchy & Time Complexities",
                Formula: "List | Set | Map | Queue Big-O Performance Cheat Sheet",
                Description: "### Java Collections Framework Complexity & Characteristics Matrix\n\n| Interface | Concrete Class | Add | Get / Contains | Remove | Iteration Order | Thread-Safe | Notes |\n|:---|:---|:---|:---|:---|:---|:---|:---|\n| **List** | `ArrayList` | O(1) amortized | O(1) index / O(n) contains | O(n) | Insertion order | No | Resizes by 50% (1.5x). Contiguous array, excellent cache locality. |\n| **List** | `LinkedList` | O(1) | O(n) (O(1) head/tail) | O(n) (O(1) via Iterator) | Insertion order | No | Doubly-linked list. 24-32 bytes pointer overhead per node. |\n| **List** | `CopyOnWriteArrayList` | O(n) | O(1) | O(n) | Snapshot order | Yes | Mutative operations clone entire underlying array. Fast, lock-free reads. |\n| **Queue / Deque** | `ArrayDeque` | O(1) amortized | O(n) contains | O(1) head/tail | FIFO / LIFO order | No | Circular array. Faster than Stack and LinkedList for stacks/queues. |\n| **Queue** | `PriorityQueue` | O(log n) | O(1) peek / O(n) contains | O(log n) poll | Min-heap order | No | Unbounded priority heap backed by array. Null not permitted. |\n| **Set** | `HashSet` | O(1) | O(1) | O(1) | No guarantee | No | Backed by HashMap. Elements stored as keys with dummy PRESENT value. |\n| **Set** | `LinkedHashSet` | O(1) | O(1) | O(1) | Insertion order | No | Backed by LinkedHashMap. Maintains doubly-linked list across hash buckets. |\n| **Set** | `TreeSet` | O(log n) | O(log n) | O(log n) | Natural / Comparator | No | Backed by NavigableMap (Red-Black tree). Keys must be Comparable. |\n| **Map** | `HashMap` | O(1) avg / O(log n) worst | O(1) avg / O(log n) worst | O(1) avg / O(log n) worst | Unspecified | No | Default load factor 0.75, capacity 16. Treeifies bucket at 8 nodes if capacity >= 64. |\n| **Map** | `LinkedHashMap` | O(1) | O(1) | O(1) | Insertion or Access order | No | Ideal for LRU cache implementations (removeEldestEntry). |\n| **Map** | `TreeMap` | O(log n) | O(log n) | O(log n) | Key-sorted order | No | Red-Black Tree implementation of NavigableMap. |\n| **Map** | `ConcurrentHashMap` | O(1) avg | O(1) lock-free read | O(1) CAS / Synchronized | Unspecified | Yes | CAS on empty bins + bin-level synchronized. Highly scalable. |",
                KeyVariables: "loadFactor = 0.75, initialCapacity = 16, TREEIFY_THRESHOLD = 8, UNTREEIFY_THRESHOLD = 6, MIN_TREEIFY_CAPACITY = 64",
                Example: "// LRU Cache using LinkedHashMap:\nMap<String, String> lru = new LinkedHashMap<>(16, 0.75f, true) {\n    @Override\n    protected boolean removeEldestEntry(Map.Entry<String, String> eldest) {\n        return size() > 100;\n    }\n};\n// Concurrent atomic counter:\nConcurrentMap<String, LongAdder> counter = new ConcurrentHashMap<>();\ncounter.computeIfAbsent(\"hits\", k -> new LongAdder()).increment();"
            ),
            (
                Category: "Java",
                Title: "JVM Memory Architecture & Garbage Collectors",
                Formula: "Heap (Young: Eden + S0 + S1, Old/Tenured) + Metaspace (Native Off-Heap)",
                Description: "### JVM Runtime Data Areas & Generation Layout\n\n```\n+-------------------------------------------------------------------------------+\n|                                  JVM HEAP                                     |\n|  +-----------------------------+  +----------------------------------------+  |\n|  |       Young Generation      |  |             Old Generation             |  |\n|  |  +--------+ +----+ +----+   |  |               (Tenured)                |  |\n|  |  |  Eden  | | S0 | | S1 |   |  |  Long-lived objects promoted after     |  |\n|  |  | Space  | |From| | To |   |  |  surviving MaxTenuringThreshold (15)  |  |\n|  |  +--------+ +----+ +----+   |  |                                        |  |\n|  +-----------------------------+  +----------------------------------------+  |\n+-------------------------------------------------------------------------------+\n|                             NATIVE MEMORY (Off-Heap)                          |\n|  +-------------------------------+  +-------------------+  +---------------+  |\n|  |           Metaspace           |  | Thread Stacks     |  | Direct Byte   |  |\n|  | Class metadata, methods,      |  | Frames, locals,   |  | Buffers       |  |\n|  | bytecode (replaced PermGen)   |  | operands, PC      |  | (NIO channels)|  |\n|  +-------------------------------+  +-------------------+  +---------------+  |\n+-------------------------------------------------------------------------------+\n```\n\n### Generational Lifecycles\n- **Eden Space:** All newly allocated objects start here (often via Thread-Local Allocation Buffers - TLABs).\n- **Minor GC:** When Eden fills, Minor GC pauses threads (STW), clears dead objects, and copies live objects to Survivor space (S0 or S1).\n- **Object Aging:** Objects swap between Survivor spaces during Minor GCs, incrementing age until `-XX:MaxTenuringThreshold=15`. Survivors reaching the threshold are promoted to Tenured (Old Gen).\n- **Major / Full GC:** Cleans the Old Generation (and Metaspace if needed). Usually longer pause times than Minor GC.\n\n### Garbage Collector Comparison Matrix\n| Collector | JVM Flag | Target Workload | Algorithms Used | STW Pause Characteristics |\n|:---|:---|:---|:---|:---|\n| **Serial GC** | `-XX:+UseSerialGC` | Single-core, small heaps (< 100MB), embedded | Young: Mark-Copy; Old: Mark-Sweep-Compact | High pause per GC; single-threaded. |\n| **Parallel GC** | `-XX:+UseParallelGC` | High-throughput batch processing | Young: Multi-threaded Mark-Copy; Old: Multi-threaded Mark-Compact | High throughput, non-deterministic pause times. |\n| **G1 GC** (Default) | `-XX:+UseG1GC` | Multi-core servers with heaps 4GB to 64GB+ | Region-based (1-32MB blocks); Incremental evacuation | Predictable pauses (-XX:MaxGCPauseMillis=200). |\n| **ZGC** | `-XX:+UseZGC` | Ultra-low latency, heaps 16GB to 16TB | Colored Pointers & Load Barriers; Concurrent evacuation | Sub-millisecond pauses (< 1ms) independent of heap size. |\n| **Shenandoah** | `-XX:+UseShenandoahGC` | Low latency, concurrent compaction | Brooks Pointers / Load-Reference Barriers | Millisecond pauses; concurrent evacuation. |",
                KeyVariables: "-Xms (initial heap), -Xmx (max heap), -Xss (thread stack), -XX:MaxMetaspaceSize, -XX:MaxGCPauseMillis, -XX:+UseG1GC, -XX:+UseZGC",
                Example: "# Recommended production flags for low-latency microservices:\njava -Xms4g -Xmx4g \\\n     -XX:+UseG1GC \\\n     -XX:MaxGCPauseMillis=100 \\\n     -XX:InitiatingHeapOccupancyPercent=45 \\\n     -XX:+ExplicitGCInvokesConcurrent \\\n     -XX:+PrintGCDetails \\\n     -jar sensei-service.jar"
            ),
            (
                Category: "Java",
                Title: "Java Concurrency & Thread Synchronization Primitives",
                Formula: "Happens-Before Order + CAS (Lock-Free) vs Monitor Locks vs AQS Primitives",
                Description: "### Java Concurrency Primitives & Synchronization Mechanisms\n\n```\n+---------------------------------------------------------------------------+\n|                          SYNCHRONIZATION SPECTRUM                         |\n|                                                                           |\n|  [Lock-Free / Atomic]   --->   [Explicit Locks]   --->   [Coordinators]   |\n|  - volatile                     - ReentrantLock           - CountDownLatch|\n|  - AtomicInteger / CAS          - ReentrantReadWriteLock  - CyclicBarrier |\n|  - LongAdder                    - StampedLock             - Semaphore     |\n|                                 - synchronized monitor    - CompletableFuture\n+---------------------------------------------------------------------------+\n```\n\n### Core Primitives Summary\n1. **`volatile`:**\n   - Guarantees **visibility** (reads/writes bypass CPU L1/L2 caches directly to main memory).\n   - Establishes **happens-before ordering** (inhibits compiler and CPU instruction reordering across memory barriers).\n   - **No atomicity** for compound actions like `count++`.\n2. **`synchronized` (Intrinsic Monitor):**\n   - Built into Java language. Mutual exclusion per object monitor.\n   - Reentrant. Lock escalation: Biased -> Lightweight (CAS spin) -> Heavyweight (OS mutex).\n   - Releases lock automatically on normal exit or exception.\n3. **`ReentrantLock` (AQS-based):**\n   - Explicit lock from `java.util.concurrent.locks`.\n   - Supports `tryLock()`, timed acquisition `tryLock(5, TimeUnit.SECONDS)`, and interruptible locking `lockInterruptibly()`.\n   - Supports multiple conditions via `lock.newCondition()` (`await()`, `signal()`).\n4. **`CountDownLatch` vs `CyclicBarrier`:**\n   - `CountDownLatch`: One-shot latch. Initialized to N. Call `countDown()` to decrement; `await()` blocks until 0. Cannot be reset.\n   - `CyclicBarrier`: Reusable barrier for cyclic algorithms. N threads call `barrier.await()`; trips when all arrive and optionally runs a barrier action.\n5. **`Semaphore`:**\n   - Maintains a set of permits. `acquire()` takes a permit; `release()` returns it. Used for rate-limiting and connection pooling.",
                KeyVariables: "AQS (AbstractQueuedSynchronizer), CAS (VarHandle / Unsafe), Condition, ReentrantLock(fair), ThreadPoolExecutor",
                Example: "// Bounded buffer with ReentrantLock and multiple conditions:\nclass BoundedQueue<T> {\n    private final ReentrantLock lock = new ReentrantLock();\n    private final Condition notFull  = lock.newCondition();\n    private final Condition notEmpty = lock.newCondition();\n    private final Object[] items = new Object[100];\n    private int putPtr, takePtr, count;\n\n    public void put(T x) throws InterruptedException {\n        lock.lock();\n        try {\n            while (count == items.length) notFull.await();\n            items[putPtr] = x;\n            if (++putPtr == items.length) putPtr = 0;\n            count++;\n            notEmpty.signal();\n        } finally { lock.unlock(); }\n    }\n}"
            ),
            (
                Category: "Java",
                Title: "Java 8+ Streams & Functional Programming Cheatsheet",
                Formula: "Stream = Source -> [Intermediate Operations (Lazy)] -> Terminal Operation (Eager)",
                Description: "### Java Stream API Architecture & Operations Reference\n\n```\n[ Collection / Array / Generator ]  <-- Source\n               |\n               v\n      [ filter(Predicate) ]         <-- Intermediate (Lazy)\n      [ map(Function) ]             <-- Intermediate (Lazy)\n      [ flatMap(Function) ]         <-- Intermediate (Lazy)\n      [ sorted(Comparator) ]        <-- Intermediate (Stateful, Lazy)\n               |\n               v\n      [ collect(Collector) ]        <-- Terminal (Eager, Consumes Stream)\n```\n\n### Core Functional Interfaces (`java.util.function`)\n- `Predicate<T>`: `boolean test(T t)` — Conditional testing (`p1.and(p2)`, `p1.negate()`).\n- `Function<T, R>`: `R apply(T t)` — Transformation (`f1.andThen(f2)`, `f1.compose(f2)`).\n- `Consumer<T>`: `void accept(T t)` — Side effects (`c1.andThen(c2)`).\n- `Supplier<T>`: `T get()` — Factory / deferred evaluation.\n- `BiFunction<T, U, R>`, `UnaryOperator<T>`, `BinaryOperator<T>`.\n\n### Common Collector Idioms\n| Target | Collector Expression |\n|:---|:---|\n| List | `Collectors.toList()` (mutable) or `Stream.toList()` (Java 16+ immutable) |\n| Set | `Collectors.toSet()` |\n| Map | `Collectors.toMap(User::getId, User::getName, (existing, replacement) -> existing)` |\n| Grouping | `Collectors.groupingBy(Employee::getDepartment)` |\n| Downstream Aggregation | `Collectors.groupingBy(Employee::getDepartment, Collectors.counting())` |\n| Partitioning | `Collectors.partitioningBy(e -> e.getSalary() > 100000)` |\n| String Join | `Collectors.joining(\", \", \"[\", \"]\")` |\n\n### `parallelStream()` Rules of Thumb\n- Powered by `ForkJoinPool.commonPool()`.\n- **Beneficial when:** N * Q > 10,000 (where N = element count, Q = compute cost per element), non-blocking CPU-bound tasks, Spliterators with cheap splitting (e.g. ArrayList vs LinkedList).\n- **Harmful when:** Blocking I/O operations (saturates shared common pool), shared mutable state, small datasets.",
                KeyVariables: "map, filter, flatMap, reduce, collect, groupingBy, Optional.ofNullable, orElseGet, ForkJoinPool.commonPool()",
                Example: "// Group transactions by currency and calculate total sum:\nMap<Currency, BigDecimal> totalByCurrency = transactions.stream()\n    .filter(Transaction::isCleared)\n    .collect(Collectors.groupingBy(\n        Transaction::getCurrency,\n        Collectors.reducing(BigDecimal.ZERO, Transaction::getAmount, BigDecimal::add)\n    ));\n\n// FlatMap flatten tags from a list of articles:\nSet<String> distinctTags = articles.stream()\n    .flatMap(a -> a.getTags().stream())\n    .map(String::toLowerCase)\n    .collect(Collectors.toSet());"
            ),
            (
                Category: "Java",
                Title: "Java OOP & SOLID Principles Architecture Guide",
                Formula: "SOLID: Single Responsibility | Open-Closed | Liskov Substitution | Interface Segregation | Dependency Inversion",
                Description: "### The SOLID Principles for Object-Oriented Software Design\n\n```\n+-------------------------------------------------------------------------------+\n|  S  | Single Responsibility  | A class should have one, and only one, reason  |\n|     | Principle              | to change. High cohesion, low coupling.        |\n+-----+------------------------+------------------------------------------------+\n|  O  | Open/Closed Principle  | Open for extension, closed for modification.   |\n|     |                        | Use abstraction, polymorphism & strategies.    |\n+-----+------------------------+------------------------------------------------+\n|  L  | Liskov Substitution    | Subtypes must be substitutable for base types  |\n|     | Principle              | without breaking system correctness.           |\n+-----+------------------------+------------------------------------------------+\n|  I  | Interface Segregation  | Clients should not depend on interfaces they  |\n|     | Principle              | do not use. Prefer small, focused interfaces.  |\n+-----+------------------------+------------------------------------------------+\n|  D  | Dependency Inversion   | Depend on abstractions, not concretions.       |\n|     | Principle              | Inversion of Control & Dependency Injection.   |\n+-----+------------------------+------------------------------------------------+\n```\n\n### Core OOP Principles & Java Specifics\n1. **Encapsulation:** Protect internal object state via `private` fields and validate mutations through accessor/mutator methods or immutable value objects (Java `record`).\n2. **Abstraction:** Expose essential contracts via `interface` and `abstract class` while hiding implementation complexities.\n3. **Inheritance:** Express \"is-a\" taxonomy. Prefer **Composition over Inheritance** to prevent tight coupling and fragile base class bugs.\n4. **Polymorphism:**\n   - *Compile-Time (Overloading):* Static dispatch determined at compile time by argument signatures.\n   - *Runtime (Overriding):* Dynamic dispatch via vtable (`invokevirtual`) determined by actual object type on heap.\n\n### LSP Rules Checklist\n- Subclasses cannot strengthen preconditions (cannot require more restrictive inputs than parent).\n- Subclasses cannot weaken postconditions (must provide at least what parent promised).\n- Subclasses cannot throw broader checked exceptions than superclass methods (can only throw covariant exceptions or subsets).",
                KeyVariables: "SRP, OCP, LSP, ISP, DIP, Composition over Inheritance, Covariant Return Types, final immutability, Record types",
                Example: "// SOLID in action: OCP & DIP via Dependency Injection & Strategy Pattern\npublic interface NotificationSender {\n    void send(String recipient, String message);\n}\n\n@Service\npublic class EmailNotificationSender implements NotificationSender {\n    public void send(String to, String msg) { /* SMTP logic */ }\n}\n\npublic class AlertService {\n    private final NotificationSender sender; // Injected abstraction (DIP)\n\n    public AlertService(NotificationSender sender) {\n        this.sender = Objects.requireNonNull(sender);\n    }\n\n    public void triggerAlert(String userId, String alert) {\n        sender.send(userId, alert); // Open for new channels without editing AlertService (OCP)\n    }\n}"
            )
        };

        foreach (var f in initialFormulas)
        {
            if (!existingTitles.Contains(f.Title))
            {
                _db.ExecuteNonQuery(@"
                    INSERT INTO formula_vault (category, title, formula, description, key_variables, example, created_at)
                    VALUES (@category, @title, @formula, @description, @keyVariables, @example, datetime('now'))
                ",
                    ("@category", f.Category),
                    ("@title", f.Title),
                    ("@formula", f.Formula),
                    ("@description", f.Description),
                    ("@keyVariables", f.KeyVariables),
                    ("@example", f.Example)
                );
            }
        }
    }

    private FormulaItem MapFormula(Dictionary<string, object?> row)
    {
        return new FormulaItem
        {
            Id = Convert.ToInt32(row["id"]),
            Category = row["category"]?.ToString() ?? "General",
            Title = row["title"]?.ToString() ?? "",
            Formula = row["formula"]?.ToString() ?? "",
            Description = row["description"]?.ToString(),
            KeyVariables = row["key_variables"]?.ToString(),
            Example = row["example"]?.ToString(),
            CreatedAt = DateTime.TryParse(row["created_at"]?.ToString(), out var dt) ? dt : DateTime.UtcNow
        };
    }

    public List<FormulaItem> GetFormulas(string? category = null, string? search = null)
    {
        var sb = new StringBuilder("SELECT * FROM formula_vault WHERE 1=1");
        var parameters = new List<(string Name, object? Value)>();

        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            sb.Append(" AND category = @category");
            parameters.Add(("@category", category));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            sb.Append(" AND (title LIKE @search OR formula LIKE @search OR description LIKE @search OR key_variables LIKE @search OR category LIKE @search)");
            parameters.Add(("@search", $"%{search.Trim()}%"));
        }

        sb.Append(" ORDER BY category ASC, title ASC");

        var rows = _db.Query(sb.ToString(), parameters.ToArray());
        var list = new List<FormulaItem>();
        foreach (var r in rows)
        {
            list.Add(MapFormula(r));
        }
        return list;
    }

    public FormulaItem? GetFormulaById(int id)
    {
        var row = _db.QuerySingle("SELECT * FROM formula_vault WHERE id = @id", ("@id", id));
        return row != null ? MapFormula(row) : null;
    }

    public FormulaItem CreateFormula(CreateFormulaRequest request)
    {
        long newId = _db.InsertAndGetId(@"
            INSERT INTO formula_vault (category, title, formula, description, key_variables, example, created_at)
            VALUES (@category, @title, @formula, @description, @keyVariables, @example, datetime('now'))
        ",
            ("@category", string.IsNullOrWhiteSpace(request.Category) ? "General" : request.Category.Trim()),
            ("@title", request.Title.Trim()),
            ("@formula", request.Formula.Trim()),
            ("@description", request.Description?.Trim()),
            ("@keyVariables", request.KeyVariables?.Trim()),
            ("@example", request.Example?.Trim())
        );

        return GetFormulaById((int)newId)!;
    }

    public bool DeleteFormula(int id)
    {
        int affected = _db.ExecuteNonQuery("DELETE FROM formula_vault WHERE id = @id", ("@id", id));
        return affected > 0;
    }

    public List<string> GetCategories()
    {
        var rows = _db.Query("SELECT DISTINCT category FROM formula_vault ORDER BY category ASC");
        var list = new List<string>();
        foreach (var r in rows)
        {
            var cat = r["category"]?.ToString();
            if (!string.IsNullOrEmpty(cat))
            {
                list.Add(cat);
            }
        }
        return list;
    }
}
