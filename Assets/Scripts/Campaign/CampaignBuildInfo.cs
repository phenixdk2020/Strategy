public static class CampaignBuildInfo
{
    // Single source of truth for the overall Campaign3 build shown in UI/log headers.
    // Subsystem class names may retain the version in which that subsystem was introduced.
    public const string CurrentVersion = "v00.00.10n7f";
    public const string CurrentBuildName = "CAMPAIGN3 COMPILE CLEANUP 10N7F";
    public const string BranchTarget = "channel-campaign3";
    public const string LogTag = "CAMPAIGN-10N7F";

    // Runtime helper intentionally hides the const comparison from compile-time flow analysis.
    // This lets retired prototype components self-guard without CS0162 unreachable-code warnings.
    public static bool IsCurrentVersion(string version)
    {
        return System.String.Equals(CurrentVersion, version, System.StringComparison.Ordinal);
    }
}
