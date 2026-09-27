using UnrealBuildTool;
using System.Collections.Generic;

public class Strategy1864Target : TargetRules
{
    public Strategy1864Target(TargetInfo Target) : base(Target)
    {
        Type = TargetType.Game;
        DefaultBuildSettings = BuildSettingsVersion.V7;
        IncludeOrderVersion = EngineIncludeOrderVersion.Latest;
        ExtraModuleNames.Add("Strategy1864");
    }
}
