using System.Runtime.InteropServices;
using System.Text.Json.Nodes;

namespace CTeam.Experiments;

public sealed class SqliteReadOnlyDatabase : IDisposable
{
    const int Ok = 0;
    const int Row = 100;
    const int Done = 101;
    const int OpenReadOnly = 0x00000001;
    static readonly IntPtr Transient = new(-1);
    IntPtr database;

    public SqliteReadOnlyDatabase(string path)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Experiment 008B uses the installed Windows SQLite library.");
        var result = Native.sqlite3_open_v2(Path.GetFullPath(path), out database, OpenReadOnly, IntPtr.Zero);
        if (result != Ok)
        {
            var message = ErrorMessage();
            if (database != IntPtr.Zero)
                Native.sqlite3_close_v2(database);
            database = IntPtr.Zero;
            throw new SqliteReadException(result, message);
        }

        Execute("PRAGMA query_only = ON");
        Execute("PRAGMA busy_timeout = 100");
    }

    public IReadOnlyList<JsonObject> Query(string sql, params string?[] parameters)
    {
        ObjectDisposedException.ThrowIf(database == IntPtr.Zero, this);
        var result = Native.sqlite3_prepare_v2(database, sql, -1, out var statement, IntPtr.Zero);
        if (result != Ok)
            throw new SqliteReadException(result, ErrorMessage());

        try
        {
            for (var index = 0; index < parameters.Length; index++)
            {
                result = parameters[index] is null
                    ? Native.sqlite3_bind_null(statement, index + 1)
                    : Native.sqlite3_bind_text(statement, index + 1, parameters[index]!, -1, Transient);
                if (result != Ok)
                    throw new SqliteReadException(result, ErrorMessage());
            }

            var rows = new List<JsonObject>();
            while ((result = Native.sqlite3_step(statement)) == Row)
            {
                var row = new JsonObject();
                for (var column = 0; column < Native.sqlite3_column_count(statement); column++)
                {
                    var name = Marshal.PtrToStringUTF8(Native.sqlite3_column_name(statement, column)) ?? $"column_{column}";
                    row[name] = Native.sqlite3_column_type(statement, column) switch
                    {
                        1 => JsonValue.Create(Native.sqlite3_column_int64(statement, column)),
                        2 => JsonValue.Create(Native.sqlite3_column_double(statement, column)),
                        3 => JsonValue.Create(Marshal.PtrToStringUTF8(Native.sqlite3_column_text(statement, column))),
                        5 => null,
                        _ => JsonValue.Create("[blob]")
                    };
                }
                rows.Add(row);
            }

            if (result != Done)
                throw new SqliteReadException(result, ErrorMessage());
            return rows;
        }
        finally
        {
            Native.sqlite3_finalize(statement);
        }
    }

    void Execute(string sql) => Query(sql);

    string ErrorMessage() => database == IntPtr.Zero ? "SQLite database handle unavailable." : Marshal.PtrToStringUTF8(Native.sqlite3_errmsg(database)) ?? "SQLite error.";

    public void Dispose()
    {
        if (database == IntPtr.Zero)
            return;
        Native.sqlite3_close_v2(database);
        database = IntPtr.Zero;
    }

    static class Native
    {
        const string Library = "winsqlite3";

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_open_v2([MarshalAs(UnmanagedType.LPUTF8Str)] string filename, out IntPtr database, int flags, IntPtr vfs);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_close_v2(IntPtr database);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_errmsg(IntPtr database);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_prepare_v2(IntPtr database, [MarshalAs(UnmanagedType.LPUTF8Str)] string sql, int bytes, out IntPtr statement, IntPtr remainingSql);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_bind_text(IntPtr statement, int index, [MarshalAs(UnmanagedType.LPUTF8Str)] string value, int bytes, IntPtr destructor);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_bind_null(IntPtr statement, int index);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_step(IntPtr statement);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_finalize(IntPtr statement);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_column_count(IntPtr statement);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_column_name(IntPtr statement, int column);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_column_type(IntPtr statement, int column);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern long sqlite3_column_int64(IntPtr statement, int column);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern double sqlite3_column_double(IntPtr statement, int column);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr sqlite3_column_text(IntPtr statement, int column);
    }
}

public sealed class SqliteReadException(int resultCode, string message) : IOException($"SQLite read failed ({resultCode}): {message}")
{
    public int ResultCode { get; } = resultCode;
}

public static class CodexStateSchemaInspector
{
    public static JsonObject Inspect(string path)
    {
        using var database = new SqliteReadOnlyDatabase(path);
        return new JsonObject
        {
            ["user_version"] = database.Query("PRAGMA user_version").Single()["user_version"]?.DeepClone(),
            ["journal_mode"] = database.Query("PRAGMA journal_mode").Single()["journal_mode"]?.DeepClone(),
            ["tables"] = ToArray(database.Query("SELECT name, sql FROM sqlite_schema WHERE type = 'table' ORDER BY name")),
            ["thread_columns"] = ToArray(database.Query("PRAGMA table_info(threads)")),
            ["thread_indexes"] = ToArray(database.Query("PRAGMA index_list(threads)"))
        };
    }

    static JsonArray ToArray(IReadOnlyList<JsonObject> rows) => new(rows.Select(row => (JsonNode)row).ToArray());
}
