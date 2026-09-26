using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Sensei.Core.Interfaces;

namespace Sensei.Components;

public class SqliteDatabaseComponent : IDatabaseComponent
{
    private const string LibName = "libsqlite3.so.0";
    private readonly string _dbPath;
    private readonly object _lock = new();

    private const int SQLITE_OPEN_READWRITE = 0x00000002;
    private const int SQLITE_OPEN_CREATE = 0x00000004;
    private const int SQLITE_OPEN_FULLMUTEX = 0x00010000;

    private const int SQLITE_INTEGER = 1;
    private const int SQLITE_FLOAT = 2;
    private const int SQLITE_TEXT = 3;
    private const int SQLITE_BLOB = 4;
    private const int SQLITE_NULL = 5;

    private const int SQLITE_OK = 0;
    private const int SQLITE_ROW = 100;
    private const int SQLITE_DONE = 101;

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_open_v2(byte[] filename, out IntPtr db, int flags, IntPtr zVfs);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_close_v2(IntPtr db);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_exec(IntPtr db, byte[] sql, IntPtr callback, IntPtr arg, out IntPtr errmsg);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_prepare_v2(IntPtr db, byte[] zSql, int nByte, out IntPtr ppStmt, IntPtr pzTail);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_step(IntPtr pStmt);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_finalize(IntPtr pStmt);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern long sqlite3_last_insert_rowid(IntPtr db);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_changes(IntPtr db);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_column_count(IntPtr pStmt);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr sqlite3_column_name(IntPtr pStmt, int N);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_column_type(IntPtr pStmt, int iCol);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern long sqlite3_column_int64(IntPtr pStmt, int iCol);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern double sqlite3_column_double(IntPtr pStmt, int iCol);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr sqlite3_column_text(IntPtr pStmt, int iCol);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_bind_parameter_index(IntPtr pStmt, byte[] zName);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_bind_null(IntPtr pStmt, int i);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_bind_int64(IntPtr pStmt, int i, long val);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_bind_double(IntPtr pStmt, int i, double val);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int sqlite3_bind_text(IntPtr pStmt, int i, byte[] text, int nLen, IntPtr destructor);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr sqlite3_errmsg(IntPtr db);

    public SqliteDatabaseComponent(string dbPath)
    {
        _dbPath = dbPath;
    }

    private IntPtr OpenDb()
    {
        byte[] pathBytes = Encoding.UTF8.GetBytes(_dbPath + "\0");
        int res = sqlite3_open_v2(pathBytes, out IntPtr db, SQLITE_OPEN_READWRITE | SQLITE_OPEN_CREATE | SQLITE_OPEN_FULLMUTEX, IntPtr.Zero);
        if (res != SQLITE_OK)
        {
            string err = Marshal.PtrToStringAnsi(sqlite3_errmsg(db)) ?? "Unknown error";
            sqlite3_close_v2(db);
            throw new InvalidOperationException($"Failed to open SQLite db at {_dbPath}: {err}");
        }
        return db;
    }

    public void Initialize()
    {
        lock (_lock)
        {
            string dir = System.IO.Path.GetDirectoryName(_dbPath) ?? "";
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
            }

            ExecuteScript(@"
                PRAGMA journal_mode=WAL;
                PRAGMA foreign_keys=ON;

                CREATE TABLE IF NOT EXISTS subjects (
                    id INTEGER PRIMARY KEY,
                    name TEXT UNIQUE,
                    parent_id INTEGER,
                    weight REAL DEFAULT 1.0,
                    color TEXT DEFAULT '#7c5cff',
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS tasks (
                    id INTEGER PRIMARY KEY,
                    title TEXT NOT NULL,
                    subject_id INTEGER,
                    topic TEXT,
                    due_date DATE,
                    priority TEXT DEFAULT 'medium',
                    status TEXT DEFAULT 'pending',
                    est_min INTEGER,
                    actual_min INTEGER DEFAULT 0,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    done_at TIMESTAMP,
                    FOREIGN KEY (subject_id) REFERENCES subjects(id)
                );

                CREATE TABLE IF NOT EXISTS questions (
                    id INTEGER PRIMARY KEY,
                    subject_id INTEGER,
                    topic TEXT NOT NULL,
                    difficulty TEXT DEFAULT 'medium',
                    stem TEXT NOT NULL,
                    options_json TEXT DEFAULT '[]',
                    answer TEXT NOT NULL,
                    solution_md TEXT,
                    source TEXT DEFAULT 'GATE',
                    tags_json TEXT DEFAULT '[]',
                    sr_ease REAL DEFAULT 2.5,
                    sr_interval INTEGER DEFAULT 0,
                    sr_due DATE DEFAULT CURRENT_DATE,
                    sr_reps INTEGER DEFAULT 0,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (subject_id) REFERENCES subjects(id)
                );

                CREATE TABLE IF NOT EXISTS attempts (
                    id INTEGER PRIMARY KEY,
                    question_id INTEGER NOT NULL,
                    user_answer TEXT NOT NULL,
                    correct BOOLEAN NOT NULL,
                    time_sec INTEGER NOT NULL,
                    confidence INTEGER DEFAULT 3,
                    notes TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (question_id) REFERENCES questions(id)
                );

                CREATE TABLE IF NOT EXISTS sessions (
                    id INTEGER PRIMARY KEY,
                    task_id INTEGER,
                    started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    ended_at TIMESTAMP,
                    duration_min INTEGER DEFAULT 0,
                    mood TEXT,
                    focus_score INTEGER,
                    notes TEXT,
                    FOREIGN KEY (task_id) REFERENCES tasks(id)
                );

                CREATE TABLE IF NOT EXISTS ai_messages (
                    id INTEGER PRIMARY KEY,
                    role TEXT NOT NULL,
                    content TEXT NOT NULL,
                    context_json TEXT,
                    tokens_in INTEGER DEFAULT 0,
                    tokens_out INTEGER DEFAULT 0,
                    model TEXT DEFAULT 'default',
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS reports (
                    id INTEGER PRIMARY KEY,
                    date DATE NOT NULL,
                    summary_md TEXT,
                    stats_json TEXT,
                    pdf_path TEXT,
                    drive_file_id TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS reminders (
                    id INTEGER PRIMARY KEY,
                    title TEXT NOT NULL,
                    cron TEXT NOT NULL,
                    channel TEXT DEFAULT 'webpush',
                    payload_json TEXT DEFAULT '{}',
                    enabled BOOLEAN DEFAULT 1,
                    last_fired TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS push_subs (
                    id INTEGER PRIMARY KEY,
                    endpoint TEXT UNIQUE,
                    p256dh TEXT,
                    auth TEXT,
                    ua TEXT,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS lecture_trackers (
                    id INTEGER PRIMARY KEY,
                    title TEXT NOT NULL,
                    video_url TEXT NOT NULL,
                    video_id TEXT,
                    current_time_sec INTEGER DEFAULT 0,
                    total_duration_sec INTEGER DEFAULT 0,
                    completed BOOLEAN DEFAULT 0,
                    notes TEXT,
                    subject_id INTEGER,
                    last_watched_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS drive_files (
                    id INTEGER PRIMARY KEY,
                    file_name TEXT NOT NULL,
                    file_id TEXT,
                    folder_path TEXT,
                    file_type TEXT,
                    extracted_summary TEXT,
                    revision_notes TEXT,
                    generated_questions_count INTEGER DEFAULT 0,
                    synced_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS study_modules (
                    id INTEGER PRIMARY KEY,
                    module_key TEXT UNIQUE NOT NULL,
                    title TEXT NOT NULL,
                    author TEXT,
                    description TEXT,
                    icon TEXT DEFAULT 'Layers',
                    enabled BOOLEAN DEFAULT 1,
                    config_json TEXT DEFAULT '{}',
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );

                CREATE TABLE IF NOT EXISTS mock_test_sessions (
                    id TEXT PRIMARY KEY,
                    title TEXT NOT NULL,
                    question_count INTEGER NOT NULL,
                    duration_min INTEGER NOT NULL,
                    questions_json TEXT NOT NULL,
                    status TEXT DEFAULT 'in_progress',
                    started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    submitted_at TIMESTAMP,
                    time_spent_sec INTEGER DEFAULT 0,
                    score REAL DEFAULT 0,
                    positive_marks REAL DEFAULT 0,
                    negative_marks REAL DEFAULT 0,
                    correct_count INTEGER DEFAULT 0,
                    wrong_count INTEGER DEFAULT 0,
                    unattempted_count INTEGER DEFAULT 0,
                    accuracy REAL DEFAULT 0,
                    percentile_estimate REAL DEFAULT 0,
                    submission_json TEXT
                );

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
        }
    }




    public void ExecuteScript(string sqlScript)
    {
        lock (_lock)
        {
            IntPtr db = OpenDb();
            try
            {
                byte[] bytes = Encoding.UTF8.GetBytes(sqlScript + "\0");
                int res = sqlite3_exec(db, bytes, IntPtr.Zero, IntPtr.Zero, out IntPtr errmsg);
                if (res != SQLITE_OK)
                {
                    string err = Marshal.PtrToStringAnsi(errmsg) ?? "Error executing script";
                    throw new InvalidOperationException($"SQL script execution failed: {err}");
                }
            }
            finally
            {
                sqlite3_close_v2(db);
            }
        }
    }

    private void BindParams(IntPtr stmt, (string Name, object? Value)[] parameters)
    {
        IntPtr SQLITE_TRANSIENT = new IntPtr(-1);

        for (int i = 0; i < parameters.Length; i++)
        {
            var (paramName, paramVal) = parameters[i];
            int idx;
            if (!string.IsNullOrEmpty(paramName))
            {
                string key = paramName.StartsWith("@") ? paramName : "@" + paramName;
                byte[] nameBytes = Encoding.UTF8.GetBytes(key + "\0");
                idx = sqlite3_bind_parameter_index(stmt, nameBytes);
                if (idx == 0) idx = i + 1;
            }
            else
            {
                idx = i + 1;
            }

            if (paramVal == null || paramVal is DBNull)
            {
                sqlite3_bind_null(stmt, idx);
            }
            else if (paramVal is int intVal)
            {
                sqlite3_bind_int64(stmt, idx, intVal);
            }
            else if (paramVal is long longVal)
            {
                sqlite3_bind_int64(stmt, idx, longVal);
            }
            else if (paramVal is bool boolVal)
            {
                sqlite3_bind_int64(stmt, idx, boolVal ? 1 : 0);
            }
            else if (paramVal is double dVal)
            {
                sqlite3_bind_double(stmt, idx, dVal);
            }
            else if (paramVal is float fVal)
            {
                sqlite3_bind_double(stmt, idx, fVal);
            }
            else if (paramVal is DateTime dtVal)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(dtVal.ToString("yyyy-MM-dd HH:mm:ss") + "\0");
                sqlite3_bind_text(stmt, idx, bytes, bytes.Length - 1, SQLITE_TRANSIENT);
            }
            else
            {
                string s = paramVal.ToString() ?? "";
                byte[] bytes = Encoding.UTF8.GetBytes(s + "\0");
                sqlite3_bind_text(stmt, idx, bytes, bytes.Length - 1, SQLITE_TRANSIENT);
            }
        }
    }

    public int ExecuteNonQuery(string sql, params (string Name, object? Value)[] parameters)
    {
        lock (_lock)
        {
            IntPtr db = OpenDb();
            try
            {
                byte[] sqlBytes = Encoding.UTF8.GetBytes(sql + "\0");
                int res = sqlite3_prepare_v2(db, sqlBytes, -1, out IntPtr stmt, IntPtr.Zero);
                if (res != SQLITE_OK)
                {
                    string err = Marshal.PtrToStringAnsi(sqlite3_errmsg(db)) ?? "Error";
                    throw new InvalidOperationException($"SQL prepare failed: {err}\nQuery: {sql}");
                }

                try
                {
                    BindParams(stmt, parameters);
                    sqlite3_step(stmt);
                    return sqlite3_changes(db);
                }
                finally
                {
                    sqlite3_finalize(stmt);
                }
            }
            finally
            {
                sqlite3_close_v2(db);
            }
        }
    }

    public long InsertAndGetId(string sql, params (string Name, object? Value)[] parameters)
    {
        lock (_lock)
        {
            IntPtr db = OpenDb();
            try
            {
                byte[] sqlBytes = Encoding.UTF8.GetBytes(sql + "\0");
                int res = sqlite3_prepare_v2(db, sqlBytes, -1, out IntPtr stmt, IntPtr.Zero);
                if (res != SQLITE_OK)
                {
                    string err = Marshal.PtrToStringAnsi(sqlite3_errmsg(db)) ?? "Error";
                    throw new InvalidOperationException($"SQL prepare failed: {err}\nQuery: {sql}");
                }

                try
                {
                    BindParams(stmt, parameters);
                    sqlite3_step(stmt);
                    return sqlite3_last_insert_rowid(db);
                }
                finally
                {
                    sqlite3_finalize(stmt);
                }
            }
            finally
            {
                sqlite3_close_v2(db);
            }
        }
    }

    public List<Dictionary<string, object?>> Query(string sql, params (string Name, object? Value)[] parameters)
    {
        lock (_lock)
        {
            IntPtr db = OpenDb();
            try
            {
                byte[] sqlBytes = Encoding.UTF8.GetBytes(sql + "\0");
                int res = sqlite3_prepare_v2(db, sqlBytes, -1, out IntPtr stmt, IntPtr.Zero);
                if (res != SQLITE_OK)
                {
                    string err = Marshal.PtrToStringAnsi(sqlite3_errmsg(db)) ?? "Error";
                    throw new InvalidOperationException($"SQL prepare failed: {err}\nQuery: {sql}");
                }

                var list = new List<Dictionary<string, object?>>();
                try
                {
                    BindParams(stmt, parameters);
                    int colCount = sqlite3_column_count(stmt);
                    string[] colNames = new string[colCount];
                    for (int c = 0; c < colCount; c++)
                    {
                        colNames[c] = Marshal.PtrToStringAnsi(sqlite3_column_name(stmt, c)) ?? $"col_{c}";
                    }

                    while (sqlite3_step(stmt) == SQLITE_ROW)
                    {
                        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                        for (int c = 0; c < colCount; c++)
                        {
                            int type = sqlite3_column_type(stmt, c);
                            object? val = type switch
                            {
                                SQLITE_INTEGER => sqlite3_column_int64(stmt, c),
                                SQLITE_FLOAT => sqlite3_column_double(stmt, c),
                                SQLITE_TEXT => Marshal.PtrToStringAnsi(sqlite3_column_text(stmt, c)),
                                SQLITE_NULL => null,
                                _ => Marshal.PtrToStringAnsi(sqlite3_column_text(stmt, c))
                            };
                            row[colNames[c]] = val;
                        }
                        list.Add(row);
                    }
                    return list;
                }
                finally
                {
                    sqlite3_finalize(stmt);
                }
            }
            finally
            {
                sqlite3_close_v2(db);
            }
        }
    }

    public Dictionary<string, object?>? QuerySingle(string sql, params (string Name, object? Value)[] parameters)
    {
        var list = Query(sql, parameters);
        return list.Count > 0 ? list[0] : null;
    }

    public T? QueryScalar<T>(string sql, params (string Name, object? Value)[] parameters)
    {
        var row = QuerySingle(sql, parameters);
        if (row == null || row.Count == 0) return default;
        foreach (var val in row.Values)
        {
            if (val == null) return default;
            try
            {
                return (T)Convert.ChangeType(val, typeof(T));
            }
            catch
            {
                return default;
            }
        }
        return default;
    }
}
