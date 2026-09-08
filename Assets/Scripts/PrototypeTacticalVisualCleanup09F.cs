using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

// v00.00.09f TEST visual correction.
// Removes the persistent selected/multi-selected summary panel without touching the
// bottom command bar, and restores selected-unit Close/Medium/Long range fans as
// readable translucent ghost lines. This is visual-only and never writes movement.
[DefaultExecutionOrder(30000)]
public sealed class PrototypeTacticalVisualCleanup09F : MonoBehaviour
{
    private bool selectedPanelHidden;
    private Material ghostMaterial;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeTacticalVisualCleanup09F>() != null)
            return;

        GameObject root = new GameObject("PrototypeTacticalVisualCleanup_v000009f");
        root.AddComponent<PrototypeTacticalVisualCleanup09F>();
    }

    private void Update()
    {
        HideSelectedSummaryPanel();
    }

    private void LateUpdate()
    {
        HideSelectedSummaryPanel();
        RefreshRangeFans();
    }

    private void HideSelectedSummaryPanel()
    {
        if (selectedPanelHidden || BattleManager.Instance == null)
            return;

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo selectedStyleField = typeof(BattleManager).GetField("selectedUnitStyle", flags);
        FieldInfo hoverStyleField = typeof(BattleManager).GetField("hoverStyle", flags);

        if (selectedStyleField == null || hoverStyleField == null)
            return;

        // Wait until BattleManager has initialised all of its GUI styles. Replacing
        // selectedUnitStyle before that would cause EnsureStyles() to skip the other
        // HUD styles as well.
        if (selectedStyleField.GetValue(BattleManager.Instance) == null ||
            hoverStyleField.GetValue(BattleManager.Instance) == null)
        {
            return;
        }

        GUIStyle invisible = new GUIStyle(GUIStyle.none)
        {
            fontSize = 0,
            wordWrap = false
        };
        invisible.normal.textColor = Color.clear;
        invisible.hover.textColor = Color.clear;
        invisible.active.textColor = Color.clear;
        invisible.focused.textColor = Color.clear;
        invisible.onNormal.textColor = Color.clear;
        invisible.onHover.textColor = Color.clear;
        invisible.onActive.textColor = Color.clear;
        invisible.onFocused.textColor = Color.clear;

        selectedStyleField.SetValue(BattleManager.Instance, invisible);
        selectedPanelHidden = true;
        Debug.Log("HUD-09F|SelectedSummaryPanel=Hidden|HoverInfo=Retained|BottomCommandBar=Retained");
    }

    private void RefreshRangeFans()
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return;

        EnsureGhostMaterial();

        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment == null)
                continue;

            bool visible = regiment.IsSelected && regiment.ShowRange;

            StyleFan(
                regiment.transform.Find("CloseRangeFan"),
                visible,
                0.12f,
                new Color(1.00f, 0.94f, 0.18f, 0.52f));

            StyleFan(
                regiment.transform.Find("MediumRangeFan"),
                visible,
                0.14f,
                new Color(1.00f, 0.58f, 0.06f, 0.48f));

            StyleFan(
                regiment.transform.Find("LongRangeFan"),
                visible,
                0.17f,
                new Color(1.00f, 0.20f, 0.05f, 0.44f));
        }
    }

    private void EnsureGhostMaterial()
    {
        if (ghostMaterial != null)
            return;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Unlit/Transparent");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader == null)
            return;

        ghostMaterial = new Material(shader)
        {
            name = "RangeGhost09F_White",
            color = Color.white
        };
    }

    private void StyleFan(Transform fanTransform, bool visible, float width, Color color)
    {
        if (fanTransform == null)
            return;

        LineRenderer line = fanTransform.GetComponent<LineRenderer>();
        if (line == null)
            return;

        line.enabled = visible;
        if (!visible)
            return;

        line.widthMultiplier = width;
        line.startColor = color;
        line.endColor = color;
        line.numCapVertices = 0;
        line.numCornerVertices = 1;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;

        // Keep material alpha at 1.0 and put translucency only in the LineRenderer
        // vertex colour. 09e multiplied material alpha and vertex alpha together,
        // which made the range fan almost invisible.
        if (ghostMaterial != null)
            line.sharedMaterial = ghostMaterial;
    }
}
