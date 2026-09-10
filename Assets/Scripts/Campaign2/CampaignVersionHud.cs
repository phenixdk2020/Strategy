using UnityEngine;

/// <summary>
/// Disabled in v00.00.19-C2. Campaign2DioramaMap owns the single HUD.
/// </summary>
public sealed class CampaignVersionHud : MonoBehaviour
{
    private void Awake()
    {
        enabled = false;
        gameObject.SetActive(false);
    }
}
