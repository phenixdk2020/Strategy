using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Legacy v00.00.10e compatibility shell.
// v00.00.11+ uses CampaignMapBootstrap/CampaignSession as the campaign runtime.
// Keeping this type avoids breaking old serialized/code references.
//
// IMPORTANT: CampaignModeEnabled is scene-aware. The old constant false value let
// PrototypeBootstrap auto-build the complete tactical battlefield on top of CampaignMap,
// which caused the duplicate tactical time UI, tactical helper banner, battlefield ground,
// farm/road/trees and tactical regiments to leak into the strategic map.
public sealed class GrandCampaignBootstrap : MonoBehaviour
{
    public enum CampaignNation
    {
        Denmark,
        SwedenNorway,
        Prussia,
        Austria,
        France,
        UnitedKingdom,
        Russia,
        Netherlands,
        Hanover,
        Mecklenburg,
        GermanConfederationOther
    }

    public static bool CampaignModeEnabled
    {
        get
        {
            Scene scene = SceneManager.GetActiveScene();
            return string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal);
        }
    }
}
