using UnityEngine;

/// <summary>
/// Disabled in v00.00.19-C2 on campaign2. DioramaMap owns the single HUD.
/// </summary>
public sealed class CampaignBuildVersionOverlay : MonoBehaviour
{
    public const string CampaignVersion = "v00.00.19-C2";
    public const string CampaignChannel = "campaign2 / Strategy-Campaign2";

    private void Awake()
    {
        enabled = false;
        gameObject.SetActive(false);
    }
}
