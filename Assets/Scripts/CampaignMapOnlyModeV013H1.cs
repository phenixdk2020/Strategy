using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Campaign v00.00.13h1 — MAP-ONLY presentation gate.
// Temporary development mode while the Denmark strategic map is being visually rebuilt.
// Keeps campaign clock/construction progression available, but disables strategic combat,
// tactical transition, QA enemy movement, legacy map labels/panels and formation presentation.
// No persistent campaign data is deleted; the gate can be removed/re-enabled later.
[DefaultExecutionOrder(3900)]
public sealed class CampaignMapOnlyModeV013H1 : MonoBehaviour
{
    public const string BuildTag = "v00.00.13h1";

    private const float BaseHoursPerRealSecond = 0.35f;

    private bool paused;
    private float speed = 1f;
    private GUIStyle topStyle;
    private GUIStyle modeStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!string.Equals(scene.name, "CampaignMap", StringComparison.Ordinal))
            return;

        if (UnityEngine.Object.FindAnyObjectByType<CampaignMapOnlyModeV013H1>() != null)
            return;

        GameObject root = new GameObject("CampaignMapOnlyModeV013H1");
        root.AddComponent<CampaignMapOnlyModeV013H1>();
    }

    private void Start()
    {
        CampaignSession.EnsureInitialized();
        ApplyMapOnlyGate();
        Debug.Log("CAMPAIGN-V013H1|Mode=MAP_ONLY|BattleContacts=False|TacticalTransition=False|QaEnemyOrders=False|FormationMovement=False|ForeignLabels=False|LegacyMapUi=False|ConstructionClock=True");
    }

    private void Update()
    {
        ApplyMapOnlyGate();

        if (Input.GetKeyDown(KeyCode.Space))
            paused = !paused;
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetSpeed(1f);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            SetSpeed(5f);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            SetSpeed(20f);

        if (!paused)
        {
            float elapsedHours = Time.unscaledDeltaTime * BaseHoursPerRealSecond * speed;
            CampaignSession.AdvanceHours(elapsedHours);
        }
    }

    private void LateUpdate()
    {
        ApplyMapOnlyGate();
        HideFormationPresentation();
        HideForeignLegacyPresentation();
    }

    private static void ApplyMapOnlyGate()
    {
        // CampaignMapController owns QA enemy orders, formation movement, hostile contact,
        // contact/battle GUI and PrototypeBattle transition. Disable it wholesale while
        // map-only development is active; this script owns only the campaign clock.
        CampaignMapController controller = UnityEngine.Object.FindAnyObjectByType<CampaignMapController>();
        if (controller != null)
            controller.enabled = false;

        // These v11 helper layers add diagnostic/control UI that is useful for systems QA
        // but currently obscures the map and can reintroduce old presentation behavior.
        CampaignMapUsabilityV011 usability = UnityEngine.Object.FindAnyObjectByType<CampaignMapUsabilityV011>();
        if (usability != null)
            usability.enabled = false;

        CampaignMapSearchHoverV011 search = UnityEngine.Object.FindAnyObjectByType<CampaignMapSearchHoverV011>();
        if (search != null)
            search.enabled = false;
    }

    private static void HideFormationPresentation()
    {
        CampaignFormationView[] views = UnityEngine.Object.FindObjectsByType<CampaignFormationView>(FindObjectsSortMode.None);
        foreach (CampaignFormationView view in views)
        {
            if (view == null)
                continue;

            SetRenderersVisible(view.gameObject, false);
        }

        GameObject routeGhost = GameObject.Find("CampaignRouteGhost");
        SetRenderersVisible(routeGhost, false);
    }

    private static void HideForeignLegacyPresentation()
    {
        foreach (KeyValuePair<string, CampaignNodeState> pair in CampaignSession.Nodes)
        {
            CampaignNodeState node = pair.Value;
            if (node == null || node.Region == CampaignMapRegion.Denmark)
                continue;

            SetRenderersVisible(GameObject.Find("CampaignNode_" + node.Id), false);
            SetRenderersVisible(GameObject.Find("CampaignControl_" + node.Id), false);
            SetRenderersVisible(GameObject.Find("Settlement3D_" + node.Id), false);
            SetRenderersVisible(GameObject.Find("V013E_Settlement_" + node.Id), false);
            SetRenderersVisible(GameObject.Find("V013H_City_" + node.Id), false);
        }
    }

    private static void SetRenderersVisible(GameObject root, bool visible)
    {
        if (root == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }
    }

    private void SetSpeed(float newSpeed)
    {
        speed = newSpeed;
        paused = false;
    }

    private void EnsureStyles()
    {
        if (topStyle != null)
            return;

        topStyle = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 12,
            fontStyle = FontStyle.Bold
        };
        topStyle.normal.textColor = new Color(0.95f, 0.94f, 0.88f);

        modeStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 9,
            fontStyle = FontStyle.Bold
        };
        modeStyle.normal.textColor = new Color(0.80f, 0.82f, 0.74f);
    }

    private void OnGUI()
    {
        EnsureStyles();

        Rect top = new Rect(Screen.width * 0.5f - 360f, 18f, 720f, 34f);
        GUI.Box(top, string.Empty);

        string state = paused ? "PAUSE" : "x" + speed.ToString("0");
        GUI.Box(new Rect(top.x + 4f, top.y + 4f, 255f, 26f),
            CampaignSession.CurrentDateTime.ToString("d MMM yyyy HH:mm") + " | " + state,
            topStyle);

        if (GUI.Button(new Rect(top.x + 264f, top.y + 4f, 100f, 26f), paused ? "FORTSÆT" : "PAUSE"))
            paused = !paused;
        if (GUI.Button(new Rect(top.x + 369f, top.y + 4f, 80f, 26f), speed == 1f && !paused ? "[1]" : "1"))
            SetSpeed(1f);
        if (GUI.Button(new Rect(top.x + 454f, top.y + 4f, 80f, 26f), speed == 5f && !paused ? "[5]" : "5"))
            SetSpeed(5f);
        if (GUI.Button(new Rect(top.x + 539f, top.y + 4f, 80f, 26f), speed == 20f && !paused ? "[20]" : "20"))
            SetSpeed(20f);
        if (GUI.Button(new Rect(top.x + 624f, top.y + 4f, 92f, 26f), "RESET"))
        {
            CampaignSession.ResetCampaign();
            SceneManager.LoadScene("CampaignMap");
        }

        GUI.Label(new Rect(top.x + 250f, top.y + 36f, 220f, 18f), "MAP-ONLY DEV · KAMP DEAKTIVERET", modeStyle);
    }
}
