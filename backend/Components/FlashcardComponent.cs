using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sensei.Core.Interfaces;
using Sensei.Core.Models;

namespace Sensei.Components;

public class FlashcardComponent : IFlashcardComponent
{
    private readonly IDatabaseComponent _db;

    public FlashcardComponent(IDatabaseComponent db)
    {
        _db = db;
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        _db.ExecuteScript(@"
            CREATE TABLE IF NOT EXISTS flashcard_decks (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL,
                description TEXT,
                subject TEXT,
                color TEXT DEFAULT '#7c5cff',
                icon TEXT DEFAULT 'Layers',
                card_count INTEGER DEFAULT 0,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            );

            CREATE TABLE IF NOT EXISTS flashcards (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                deck_id INTEGER NOT NULL,
                front TEXT NOT NULL,
                back TEXT NOT NULL,
                notes TEXT,
                box INTEGER DEFAULT 1,
                next_review_date TEXT NOT NULL,
                reps INTEGER DEFAULT 0,
                ease REAL DEFAULT 2.5,
                interval_days INTEGER DEFAULT 1,
                last_reviewed_at TIMESTAMP,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (deck_id) REFERENCES flashcard_decks(id) ON DELETE CASCADE
            );
        ");

        var deckCount = _db.QueryScalar<long>("SELECT COUNT(*) FROM flashcard_decks;");
        if (deckCount == 0)
        {
            SeedFlashcards();
        }
    }

    private void SeedFlashcards()
    {
        var seedDecks = new[]
        {
            new
            {
                Title = "GATE CS Core Definitions",
                Description = "Essential definitions, theorems, and concepts across OS, DBMS, Theory of Computation, and Compiler Design.",
                Subject = "Core CS Foundations",
                Color = "#8b5cf6",
                Icon = "BookOpen",
                Cards = new[]
                {
                    (
                        Front: "What is the Church-Turing Thesis?",
                        Back: "The hypothesis that any real-world algorithmic computation that can be carried out by an effective procedure or algorithm can be simulated by a Turing Machine.\n\nKey Takeaway: It defines the theoretical limit of what is computable; cannot be mathematically proven, but accepted as axiom.",
                        Notes: "TOC / Computability"
                    ),
                    (
                        Front: "Define Belady's Anomaly and identify which page replacement algorithms can or cannot exhibit it.",
                        Back: "Belady's Anomaly is the phenomenon where allocating more page frames results in an increase in page faults.\n\n- Can exhibit: FIFO, Random.\n- Never exhibit: Optimal (MIN) and LRU because they are stack algorithms (pages in a smaller frame set are always a subset of a larger frame set).",
                        Notes: "Operating Systems / Virtual Memory"
                    ),
                    (
                        Front: "What is Conflict Serializability and how is it tested in DBMS transactions?",
                        Back: "A schedule is conflict serializable if it is conflict-equivalent to a serial schedule.\n\nTest: Build a Precedence (Conflict) Graph where directed edge Ti -> Tj exists if Ti performs an operation conflicting with Tj (Read-Write, Write-Read, Write-Write on same item) and Ti executes first.\n\nSchedule is conflict serializable if and only if the precedence graph has NO cycles.",
                        Notes: "DBMS / Concurrency Control"
                    ),
                    (
                        Front: "What are the 4 Coffman conditions required for a Deadlock to occur?",
                        Back: "1. Mutual Exclusion: At least one resource is held in non-shareable mode.\n2. Hold and Wait: A process holds resources while waiting for additional ones.\n3. No Preemption: Resources cannot be forcibly taken away.\n4. Circular Wait: A closed chain of processes exists where each waits for a resource held by the next.\n\nAll 4 must hold simultaneously.",
                        Notes: "Operating Systems / Deadlock"
                    ),
                    (
                        Front: "Define Chomsky Hierarchy Level 2 (Context-Free Grammar & Languages).",
                        Back: "Production Form: A -> α, where A is a single non-terminal (A ∈ V_N) and α ∈ (V_N ∪ Σ)*.\n\n- Automaton: Non-deterministic Pushdown Automata (NPDA).\n- Expressive power: Strictly more expressive than regular languages (can handle balanced parentheses, nested syntax).\n- Closed under: Union, Concatenation, Kleene Star.\n- NOT closed under: Intersection, Complement.",
                        Notes: "Theory of Computation"
                    ),
                    (
                        Front: "Compare 3NF and BCNF requirements for functional dependency X -> Y.",
                        Back: "For any non-trivial FD X -> Y:\n\n- 3NF Requirement: X must be a superkey OR Y must be a prime attribute (part of any candidate key).\n- BCNF Requirement: X must strictly be a superkey.\n\nKey Rule: Every BCNF decomposition is loss-less join, but BCNF is NOT always dependency preserving. 3NF always guarantees dependency preservation.",
                        Notes: "DBMS / Normalization"
                    ),
                    (
                        Front: "What is the difference between an LL(1) and LR(1) parser?",
                        Back: "LL(1): Top-down parser, Left-to-right scan, Leftmost derivation, 1 lookahead token. Cannot handle left recursive or common prefix grammars directly.\n\nLR(1): Bottom-up shift-reduce parser, Left-to-right scan, Rightmost derivation in reverse, 1 lookahead token. Recognizes all deterministic context-free languages (DCFLs).",
                        Notes: "Compiler Design / Parsing"
                    )
                }
            },
            new
            {
                Title = "Time & Space Complexities",
                Description = "High-yield asymptotic time and space bounds for sorting, graph algorithms, and data structure operations.",
                Subject = "Algorithms & Data Structures",
                Color = "#3b82f6",
                Icon = "Zap",
                Cards = new[]
                {
                    (
                        Front: "What are the Best, Average, and Worst case time & space complexities of QuickSort?",
                        Back: "Time Complexity:\n- Best Case: O(n log n) (balanced partitions)\n- Average Case: O(n log n)\n- Worst Case: O(n²) (unbalanced partitions, e.g. already sorted array with first element as pivot)\n\nSpace Complexity:\n- Auxiliary stack space: O(log n) best/average, O(n) worst case.",
                        Notes: "Algorithms / Sorting"
                    ),
                    (
                        Front: "What are the Time and Space complexities of Bellman-Ford Single-Source Shortest Path?",
                        Back: "Time: O(V × E)\nSpace: O(V)\n\nKey Property: Works on graphs with negative edge weights and can detect reachable negative-weight cycles by relaxing all edges |V| - 1 times and checking if a further relaxation occurs.",
                        Notes: "Algorithms / Graph Algorithms"
                    ),
                    (
                        Front: "What is the time complexity to build a Binary Heap of n elements (Bottom-Up Heapify)?",
                        Back: "Time: O(n) (Linear time!)\n\nExplanation: Nodes at level h take O(h) work to sift down. Sum of (n/2^(h+1)) × h over h=0..log n converges to O(n).\nNote: Building a heap by n successive insertions takes O(n log n).",
                        Notes: "Data Structures / Binary Heap"
                    ),
                    (
                        Front: "What is the time complexity of Floyd-Warshall All-Pairs Shortest Path?",
                        Back: "Time: Θ(V³)\nSpace: Θ(V²) (can be updated in-place)\n\nDynamic Programming Recurrence:\ndist[i][j] = min(dist[i][j], dist[i][k] + dist[k][j]) for intermediate vertex k from 1 to V.",
                        Notes: "Algorithms / Dynamic Programming"
                    ),
                    (
                        Front: "What are the time complexities of Topological Sort on a DAG?",
                        Back: "Time: O(V + E)\nSpace: O(V) for visited array or in-degree tracking.\n\nTechniques:\n1. DFS-based: Push finished vertices to stack, pop in reverse post-order.\n2. Kahn's Algorithm: Maintain in-degree count; add 0-in-degree vertices to a BFS queue.",
                        Notes: "Algorithms / Graph Traversals"
                    ),
                    (
                        Front: "What are the worst-case complexities of Search, Insert, and Delete in an AVL Tree?",
                        Back: "Search: O(log n)\nInsert: O(log n) (at most 2 rotations or 1 double rotation)\nDelete: O(log n) (may require O(log n) rotations up to the root)\n\nBalance Factor: Height(Left) - Height(Right) ∈ {-1, 0, +1}.\nMaximum height: h < 1.44 log₂(n + 2) - 0.328.",
                        Notes: "Data Structures / Balanced Search Trees"
                    )
                }
            },
            new
            {
                Title = "CN Protocols & Port Numbers",
                Description = "Essential OSI/TCP-IP networking protocols, standard port numbers, and transport layer mechanisms.",
                Subject = "Computer Networks",
                Color = "#10b981",
                Icon = "Network",
                Cards = new[]
                {
                    (
                        Front: "List standard port numbers for: DNS, DHCP, HTTP, HTTPS, SSH, and Telnet.",
                        Back: "- DNS: Port 53 (UDP for queries <512B, TCP for zone transfers)\n- DHCP: Port 67 (Server), Port 68 (Client) (UDP)\n- HTTP: Port 80 (TCP)\n- HTTPS: Port 443 (TCP/TLS)\n- SSH: Port 22 (TCP)\n- Telnet: Port 23 (TCP)",
                        Notes: "Computer Networks / Application Layer"
                    ),
                    (
                        Front: "List standard port numbers for: FTP (data/control), SMTP, POP3, IMAP, and BGP.",
                        Back: "- FTP Data: Port 20 (TCP)\n- FTP Control: Port 21 (TCP)\n- SMTP: Port 25 (TCP)\n- POP3: Port 110 (TCP)\n- IMAP: Port 143 (TCP)\n- BGP: Port 179 (TCP)",
                        Notes: "Computer Networks / Protocols"
                    ),
                    (
                        Front: "What are the header sizes and checksum rules for TCP vs UDP?",
                        Back: "TCP Header:\n- Size: 20 to 60 bytes (minimum 20 bytes when options = 0).\n- Checksum: Mandatory in both IPv4 and IPv6.\n\nUDP Header:\n- Size: Fixed 8 bytes (Source Port, Dest Port, Length, Checksum).\n- Checksum: Optional in IPv4 (all 0s if unused), Mandatory in IPv6.",
                        Notes: "Computer Networks / Transport Layer"
                    ),
                    (
                        Front: "What is the TCP 3-Way Handshake connection establishment sequence?",
                        Back: "1. Client -> Server: SYN (seq = client_isn)\n2. Server -> Client: SYN + ACK (seq = server_isn, ack = client_isn + 1)\n3. Client -> Server: ACK (seq = client_isn + 1, ack = server_isn + 1)\n\nData transmission begins after step 3 (can carry piggybacked data in step 3).",
                        Notes: "Computer Networks / TCP"
                    ),
                    (
                        Front: "State the minimum frame size equation in CSMA/CD (Ethernet) to detect collisions.",
                        Back: "Formula:\nLength_min ≥ 2 × T_prop × Bandwidth\n\nExplanation: Round-trip time (2 × T_prop) is the slot time. The sender must still be transmitting when a collision from the furthest end of the cable propagates back to it.",
                        Notes: "Computer Networks / Data Link Layer"
                    ),
                    (
                        Front: "Compare Routing Protocols: RIP, OSPF, and BGP (Type, Algorithm, Transport).",
                        Back: "- RIP: Interior Gateway (IGP), Distance Vector (Bellman-Ford), Max 15 hops, runs over UDP Port 520.\n- OSPF: Interior Gateway (IGP), Link State (Dijkstra), Dijkstra shortest path tree, runs directly over IP (Protocol 89).\n- BGP: Exterior Gateway (EGP), Path Vector, loop-free inter-AS routing, runs over TCP Port 179.",
                        Notes: "Computer Networks / Network Layer"
                    )
                }
            }
        };

        foreach (var deck in seedDecks)
        {
            long deckId = _db.InsertAndGetId(@"
                INSERT INTO flashcard_decks (title, description, subject, color, icon, card_count, created_at)
                VALUES (@title, @description, @subject, @color, @icon, @count, datetime('now'))
            ",
                ("@title", deck.Title),
                ("@description", deck.Description),
                ("@subject", deck.Subject),
                ("@color", deck.Color),
                ("@icon", deck.Icon),
                ("@count", deck.Cards.Length)
            );

            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");

            // Distribute seed cards across Box 1 to Box 3 for realistic initial dashboard
            int idx = 0;
            foreach (var card in deck.Cards)
            {
                int box = (idx % 3) + 1; // Boxes 1, 2, 3
                int interval = box switch
                {
                    1 => 1,
                    2 => 3,
                    3 => 7,
                    _ => 1
                };

                // Half due today, half due in upcoming days
                string nextReview = (idx % 2 == 0)
                    ? today
                    : DateTime.UtcNow.AddDays(interval).ToString("yyyy-MM-dd");

                _db.ExecuteNonQuery(@"
                    INSERT INTO flashcards (deck_id, front, back, notes, box, next_review_date, reps, ease, interval_days, created_at)
                    VALUES (@deckId, @front, @back, @notes, @box, @nextReview, @reps, @ease, @interval, datetime('now'))
                ",
                    ("@deckId", deckId),
                    ("@front", card.Front),
                    ("@back", card.Back),
                    ("@notes", card.Notes),
                    ("@box", box),
                    ("@nextReview", nextReview),
                    ("@reps", box - 1),
                    ("@ease", 2.5),
                    ("@interval", interval)
                );

                idx++;
            }
        }
    }

    private FlashcardItem MapFlashcard(Dictionary<string, object?> row)
    {
        return new FlashcardItem
        {
            Id = Convert.ToInt32(row["id"]),
            DeckId = Convert.ToInt32(row["deck_id"]),
            Front = row["front"]?.ToString() ?? "",
            Back = row["back"]?.ToString() ?? "",
            Notes = row["notes"]?.ToString(),
            Box = row["box"] != null ? Convert.ToInt32(row["box"]) : 1,
            NextReviewDate = row["next_review_date"]?.ToString() ?? DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Reps = row["reps"] != null ? Convert.ToInt32(row["reps"]) : 0,
            Ease = row["ease"] != null ? Convert.ToDouble(row["ease"]) : 2.5,
            IntervalDays = row["interval_days"] != null ? Convert.ToInt32(row["interval_days"]) : 1,
            LastReviewedAt = DateTime.TryParse(row["last_reviewed_at"]?.ToString(), out var lr) ? lr : null,
            CreatedAt = DateTime.TryParse(row["created_at"]?.ToString(), out var cr) ? cr : DateTime.UtcNow
        };
    }

    private FlashcardDeck MapDeck(Dictionary<string, object?> row)
    {
        int deckId = Convert.ToInt32(row["id"]);
        var deck = new FlashcardDeck
        {
            Id = deckId,
            Title = row["title"]?.ToString() ?? "",
            Description = row["description"]?.ToString(),
            Subject = row["subject"]?.ToString(),
            Color = row["color"]?.ToString() ?? "#7c5cff",
            Icon = row["icon"]?.ToString() ?? "Layers",
            CardCount = row["card_count"] != null ? Convert.ToInt32(row["card_count"]) : 0,
            CreatedAt = DateTime.TryParse(row["created_at"]?.ToString(), out var cr) ? cr : DateTime.UtcNow
        };

        // Query Box breakdown & due count
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var boxRows = _db.Query(@"
            SELECT box, COUNT(*) as cnt 
            FROM flashcards 
            WHERE deck_id = @deckId 
            GROUP BY box
        ", ("@deckId", deckId));

        for (int i = 1; i <= 5; i++)
        {
            deck.BoxCounts[i] = 0;
        }

        int totalCount = 0;
        foreach (var br in boxRows)
        {
            int b = Convert.ToInt32(br["box"]);
            int c = Convert.ToInt32(br["cnt"]);
            if (b >= 1 && b <= 5)
            {
                deck.BoxCounts[b] = c;
            }
            totalCount += c;
        }
        deck.CardCount = totalCount;

        var dueCount = _db.QueryScalar<long>(@"
            SELECT COUNT(*) 
            FROM flashcards 
            WHERE deck_id = @deckId AND next_review_date <= @today
        ", ("@deckId", deckId), ("@today", today));
        deck.DueCount = (int)dueCount;

        return deck;
    }

    public List<FlashcardDeck> GetDecks()
    {
        var rows = _db.Query("SELECT * FROM flashcard_decks ORDER BY id ASC");
        var list = new List<FlashcardDeck>();
        foreach (var r in rows)
        {
            list.Add(MapDeck(r));
        }
        return list;
    }

    public FlashcardDeck? GetDeckById(int id)
    {
        var row = _db.QuerySingle("SELECT * FROM flashcard_decks WHERE id = @id", ("@id", id));
        return row != null ? MapDeck(row) : null;
    }

    public FlashcardDeck CreateDeck(CreateDeckRequest request)
    {
        long newId = _db.InsertAndGetId(@"
            INSERT INTO flashcard_decks (title, description, subject, color, icon, card_count, created_at)
            VALUES (@title, @description, @subject, @color, @icon, 0, datetime('now'))
        ",
            ("@title", request.Title.Trim()),
            ("@description", request.Description?.Trim()),
            ("@subject", request.Subject?.Trim()),
            ("@color", request.Color ?? "#7c5cff"),
            ("@icon", request.Icon ?? "Layers")
        );

        return GetDeckById((int)newId)!;
    }

    public bool DeleteDeck(int id)
    {
        _db.ExecuteNonQuery("DELETE FROM flashcards WHERE deck_id = @id", ("@id", id));
        int affected = _db.ExecuteNonQuery("DELETE FROM flashcard_decks WHERE id = @id", ("@id", id));
        return affected > 0;
    }

    public List<FlashcardItem> GetCardsInDeck(int deckId, bool dueOnly = false)
    {
        string sql;
        List<Dictionary<string, object?>> rows;
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");

        if (dueOnly)
        {
            sql = "SELECT * FROM flashcards WHERE deck_id = @deckId AND next_review_date <= @today ORDER BY box ASC, next_review_date ASC";
            rows = _db.Query(sql, ("@deckId", deckId), ("@today", today));
        }
        else
        {
            sql = "SELECT * FROM flashcards WHERE deck_id = @deckId ORDER BY box ASC, next_review_date ASC, id ASC";
            rows = _db.Query(sql, ("@deckId", deckId));
        }

        var list = new List<FlashcardItem>();
        foreach (var r in rows)
        {
            list.Add(MapFlashcard(r));
        }
        return list;
    }

    public FlashcardItem? GetCardById(int id)
    {
        var row = _db.QuerySingle("SELECT * FROM flashcards WHERE id = @id", ("@id", id));
        return row != null ? MapFlashcard(row) : null;
    }

    public FlashcardItem AddCard(CreateFlashcardRequest request)
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        long newId = _db.InsertAndGetId(@"
            INSERT INTO flashcards (deck_id, front, back, notes, box, next_review_date, reps, ease, interval_days, created_at)
            VALUES (@deckId, @front, @back, @notes, 1, @today, 0, 2.5, 1, datetime('now'))
        ",
            ("@deckId", request.DeckId),
            ("@front", request.Front.Trim()),
            ("@back", request.Back.Trim()),
            ("@notes", request.Notes?.Trim()),
            ("@today", today)
        );

        // Update deck count
        _db.ExecuteNonQuery("UPDATE flashcard_decks SET card_count = (SELECT COUNT(*) FROM flashcards WHERE deck_id = @deckId) WHERE id = @deckId",
            ("@deckId", request.DeckId));

        return GetCardById((int)newId)!;
    }

    public FlashcardItem? ReviewCard(int id, ReviewFlashcardRequest request)
    {
        var card = GetCardById(id);
        if (card == null) return null;

        int newBox = card.Box;
        int newInterval = card.IntervalDays;
        double newEase = card.Ease;
        int newReps = card.Reps + 1;
        DateTime today = DateTime.UtcNow.Date;
        DateTime nextReview;

        string rating = request.Rating?.Trim().ToLowerInvariant() ?? "good";

        switch (rating)
        {
            case "again":
            case "1":
                // Failed recall: reset to Box 1, review tomorrow
                newBox = 1;
                newInterval = 1;
                newEase = Math.Max(1.3, card.Ease - 0.2);
                nextReview = today.AddDays(1);
                break;

            case "hard":
            case "2":
                // Stays in current box, repeat soon
                newBox = Math.Max(1, card.Box);
                newInterval = Math.Max(1, (int)(GetBoxInterval(newBox) * 0.7));
                newEase = Math.Max(1.3, card.Ease - 0.1);
                nextReview = today.AddDays(newInterval);
                break;

            case "good":
            case "3":
                // Successful recall: advance to next box (up to Box 5)
                newBox = Math.Min(5, card.Box + 1);
                newInterval = GetBoxInterval(newBox);
                nextReview = today.AddDays(newInterval);
                break;

            case "easy":
            case "4":
                // Effortless recall: advance to next box with ease boost
                newBox = Math.Min(5, card.Box + 1);
                newInterval = (int)(GetBoxInterval(newBox) * 1.35);
                newEase = Math.Min(3.0, card.Ease + 0.15);
                nextReview = today.AddDays(newInterval);
                break;

            default:
                newBox = Math.Min(5, card.Box + 1);
                newInterval = GetBoxInterval(newBox);
                nextReview = today.AddDays(newInterval);
                break;
        }

        string nextReviewDateStr = nextReview.ToString("yyyy-MM-dd");

        _db.ExecuteNonQuery(@"
            UPDATE flashcards 
            SET box = @box,
                next_review_date = @nextReview,
                reps = @reps,
                ease = @ease,
                interval_days = @interval,
                last_reviewed_at = datetime('now')
            WHERE id = @id
        ",
            ("@box", newBox),
            ("@nextReview", nextReviewDateStr),
            ("@reps", newReps),
            ("@ease", newEase),
            ("@interval", newInterval),
            ("@id", id)
        );

        return GetCardById(id);
    }

    private static int GetBoxInterval(int box) => box switch
    {
        1 => 1,    // Box 1: 1 day
        2 => 3,    // Box 2: 3 days
        3 => 7,    // Box 3: 7 days
        4 => 14,   // Box 4: 14 days
        5 => 30,   // Box 5: 30 days (Mastered)
        _ => 1
    };

    public bool DeleteCard(int id)
    {
        var card = GetCardById(id);
        if (card == null) return false;

        int affected = _db.ExecuteNonQuery("DELETE FROM flashcards WHERE id = @id", ("@id", id));
        if (affected > 0)
        {
            _db.ExecuteNonQuery("UPDATE flashcard_decks SET card_count = (SELECT COUNT(*) FROM flashcards WHERE deck_id = @deckId) WHERE id = @deckId",
                ("@deckId", card.DeckId));
            return true;
        }
        return false;
    }

    public FlashcardDeckStats GetDeckStats(int deckId)
    {
        string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var rows = _db.Query(@"
            SELECT box, COUNT(*) as cnt 
            FROM flashcards 
            WHERE deck_id = @deckId 
            GROUP BY box
        ", ("@deckId", deckId));

        int b1 = 0, b2 = 0, b3 = 0, b4 = 0, b5 = 0, total = 0;
        foreach (var r in rows)
        {
            int b = Convert.ToInt32(r["box"]);
            int c = Convert.ToInt32(r["cnt"]);
            total += c;
            switch (b)
            {
                case 1: b1 = c; break;
                case 2: b2 = c; break;
                case 3: b3 = c; break;
                case 4: b4 = c; break;
                case 5: b5 = c; break;
            }
        }

        var due = _db.QueryScalar<long>(@"
            SELECT COUNT(*) 
            FROM flashcards 
            WHERE deck_id = @deckId AND next_review_date <= @today
        ", ("@deckId", deckId), ("@today", today));

        return new FlashcardDeckStats(total, (int)due, b1, b2, b3, b4, b5);
    }
}
