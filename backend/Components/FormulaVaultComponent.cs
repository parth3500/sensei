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
        ");

        var count = _db.QueryScalar<long>("SELECT COUNT(*) FROM formula_vault;");
        if (count == 0)
        {
            SeedFormulas();
        }
    }

    private void SeedFormulas()
    {
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
            )
        };

        foreach (var f in initialFormulas)
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
