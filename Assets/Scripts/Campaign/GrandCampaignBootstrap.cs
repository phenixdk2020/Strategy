using UnityEngine;

// Legacy v00.00.10e compatibility shell.
// v00.00.11+ uses CampaignMapBootstrap/CampaignSession as the campaign runtime.
// Keeping this type avoids breaking any old serialized/code references, but it must
// never auto-create a second campaign world on top of CampaignMap.
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

    public const bool CampaignModeEnabled = false;
}
