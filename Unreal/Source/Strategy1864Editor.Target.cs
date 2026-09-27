using UnrealBuildTool;
using System.Collections.Generic;

public class Strategy1864EditorTarget : TargetRules
{
    public Strategy1864EditorTarget(TargetInfo Target) : base(Target)
    {
        Type = TargetType.Editor;
        DefaultBuildSettings = BuildSettingsVersion.V5;
        IncludeOrderVersion = EngineIncludeOrderVersion.Latest;
        ExtraModuleNames.Add("Strategy1864");
    }
}
