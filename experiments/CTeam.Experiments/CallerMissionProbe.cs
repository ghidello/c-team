namespace CTeam.Experiments;

public static class CallerMissionProbe
{
    public static MissionSnapshot Probe(CallerContext caller, string? projectHint, string? missionId, string? codexHome = null)
    {
        if (!string.IsNullOrWhiteSpace(caller.ThreadId)) return Snapshot(PersistedMissionResolver.ResolveExactCaller(caller, codexHome));
        if (!string.IsNullOrWhiteSpace(missionId)) return Snapshot(PersistedMissionResolver.ResolveExactId(missionId, "explicit-mission-id", codexHome));
        if (!string.IsNullOrWhiteSpace(projectHint))
        {
            var hint = MissionProbe.Probe(projectHint, null, codexHome);
            return hint with { CorrelationOutcome = "context-assisted", CorrelationSelection = "explicit-project-hint" };
        }
        return new(null, 0, "ambiguous", "missing-caller-context", null, 0, null, null, null, 0, false,
            CorrelationOutcome: "unresolved", CorrelationSelection: "missing-caller-context");
    }

    static MissionSnapshot Snapshot(CorrelationResolution resolution) => new(resolution.MissionKey, resolution.CandidateCount,
        resolution.Outcome == "exact" ? "certain" : "ambiguous", resolution.SelectionSignal, null, 0, null, null, null,
        resolution.ScannedFiles, resolution.ScanTruncated, resolution.Outcome, resolution.SelectionSignal, resolution.RootMissionKey,
        resolution.CallerKind, resolution.ExaminedDirectories, resolution.DirectoryEntriesExamined, resolution.BytesRead, resolution.ScanTruncated);
}
