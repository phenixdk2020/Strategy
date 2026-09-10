using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// v00.00.22-C2: the layer-strip must never disable the rendering camera.
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class Campaign2CameraGuard : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate()
    {
        if (SceneManager.GetActiveScene().name != "CampaignMap")
            return;
        if (FindAnyObjectByType<Campaign2CameraGuard>() != null)
            return;
        new GameObject("Campaign2CameraGuard").AddComponent<Campaign2CameraGuard>();
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            Camera[] all = FindObjectsByType<Camera>(FindObjectsInactive.Include);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null)
                    continue;
                all[i].gameObject.SetActive(true);
                all[i].enabled = true;
                if (cam == null)
                    cam = all[i];
            }
        }
        if (cam == null)
            return;
        cam.gameObject.SetActive(true);
        cam.enabled = true;
        if (cam.CompareTag("MainCamera") == false)
        {
            try { cam.tag = "MainCamera"; }
            catch { }
        }
    }
}
