using CTeam.Experiments;

if (args.Length == 0 || args[0] != "init")
{
    Console.Error.WriteLine("Usage: cteam-init init --target <existing-directory> [--dry-run]");
    return 2;
}

var targetIndex = Array.IndexOf(args, "--target");
if (targetIndex < 0 || targetIndex + 1 >= args.Length)
{
    Console.Error.WriteLine("Missing required option --target.");
    return 2;
}

var report = ProjectInitializer.Initialize(new(args[targetIndex + 1], args.Contains("--dry-run", StringComparer.Ordinal)));
Console.WriteLine(report.ToJson());
return report.Status == "rejected" ? 1 : 0;
