using System.Runtime.InteropServices;

namespace CTeam.Experiments.Tests;

public sealed class SqliteFixture : IDisposable
{
    const int Ok = 0;
    const int OpenReadWrite = 0x00000002;
    const int OpenCreate = 0x00000004;
    IntPtr database;

    public SqliteFixture(string path)
    {
        var result = Native.sqlite3_open_v2(path, out database, OpenReadWrite | OpenCreate, IntPtr.Zero);
        if (result != Ok)
            throw new IOException($"Could not create SQLite fixture ({result}).");
    }

    public void Execute(string sql)
    {
        var result = Native.sqlite3_exec(database, sql, IntPtr.Zero, IntPtr.Zero, out var error);
        if (result == Ok)
            return;
        var message = error == IntPtr.Zero ? "SQLite fixture error." : Marshal.PtrToStringUTF8(error)!;
        if (error != IntPtr.Zero)
            Native.sqlite3_free(error);
        throw new IOException($"SQLite fixture failed ({result}): {message}");
    }

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
        internal static extern int sqlite3_exec(IntPtr database, [MarshalAs(UnmanagedType.LPUTF8Str)] string sql, IntPtr callback, IntPtr argument, out IntPtr error);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void sqlite3_free(IntPtr pointer);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int sqlite3_close_v2(IntPtr database);
    }
}
