using System.Collections.Generic;
using UnityEngine;

// v00.00.09f16: fixes the first Major-HQ usability issues from 09f15.
// Major is a first-class selectable/hoverable HQ, AI can be toggled, and DEFEND HERE
// places both subordinate companies on one common battalion front.
[DefaultExecutionOrder(240)]
public sealed class PrototypeMajorHQ09F16 : MonoBehaviour
{
    public static PrototypeMajorHQ09F16 Instance { get; private set; }

    public bool MajorAIEnabled { get; private set; }
    public bool IsSelected { get; private set; }
    public string LastOrderText { get; private set; } = "Ingen bataljonsordre";

    private PrototypeMajorHQ09F15 legacy;
    private float nextAiDecisionAt;
    private GUIStyle tooltipStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeMajorHQ09F16>() != null)
            return;

        GameObject root = new GameObject("PrototypeMajorHQ_v000009f16");
        root.AddComponent<PrototypeMajorHQ09F16>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (legacy == null)
            legacy = PrototypeMajorHQ09F15.Instance;

        if (legacy == null)
            return;

        IsSelected = legacy.DebugIsSelected;
        LastOrderText = legacy.DebugLastOrderText;

        if (MajorAIEnabled && Time.time >= nextAiDecisionAt)
        {
            nextAiDecisionAt = Time.time + 5.0f;
            RunMajorAIDecision();
        }
    }

    public void SetMajorAIEnabled(bool enabled)
    {
        MajorAIEnabled = enabled;
        nextAiDecisionAt = Time.time + 1.0f;
        Debug.Log("HQ-AI-09F16|MajorAI=" + (MajorAIEnabled ? "ON" : "OFF"));
    }

    public bool IsMouseOverHQ(Vector3 mousePosition)
    {
        return legacy != null && legacy.DebugRayHitsHQ(mousePosition);
    }

    public string GetHoverText()
    {
        if (legacy == null)
            return "Major | Bataljons HQ";

        int count = legacy.DebugSubordinateCount;
        return "MAJOR | BATALJONS HQ\n" +
               "HQ AI: " + (MajorAIEnabled ? "ON" : "OFF") + "   |   3 heste\n" +
               "Under kommando: " + count + " kompagnier\n" +
               "Aktuel ordre: " + legacy.DebugLastOrderText;
    }

    private void RunMajorAIDecision()
    {
        if (legacy == null || legacy.DebugSubordinateCount == 0)
            return;

        Vector3 center = legacy.DebugGetSubordinateCenter();
        Regiment enemy = legacy.DebugFindNearestEnemyToPoint(center, 900f);
        if (enemy == null)
        {
            legacy.DebugIssueHoldOrder();
            return;
        }

        float distance = PlanarDistance(center, enemy.transform.position);
        float strengthRatio = legacy.DebugAverageStrengthRatio();
        float cohesion = legacy.DebugAverageCohesion();

        // First battalion-AI MVP only. Personality/stat-driven interpretation comes later.
        if (strengthRatio < 0.55f || cohesion < 45f)
        {
            Vector3 fallback = center - FlatDirection(center, enemy.transform.position) * 55f;
            legacy.DebugIssueTargetedOrder("DefendHere", fallback);
            Debug.Log("HQ-AI-09F16|Decision=DEFEND|Reason=WEAK_OR_DISORDERED|Distance=" + distance.ToString("0"));
            return;
        }

        if (distance <= 125f)
        {
            legacy.DebugIssueTargetedOrder("AttackHere", enemy.transform.position);
            Debug.Log("HQ-AI-09F16|Decision=ATTACK|Distance=" + distance.ToString("0"));
            return;
        }

        Vector3 advance = center + FlatDirection(center, enemy.transform.position) * Mathf.Min(70f, Mathf.Max(25f, distance - 95f));
        legacy.DebugIssueTargetedOrder("AdvanceHere", advance);
        Debug.Log("HQ-AI-09F16|Decision=ADVANCE|Distance=" + distance.ToString("0"));
    }

    private static Vector3 FlatDirection(Vector3 from, Vector3 to)
    {
        Vector3 d = to - from;
        d.y = 0f;
        if (d.sqrMagnitude < 0.01f)
            return Vector3.forward;
        return d.normalized;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void EnsureStyle()
    {
        if (tooltipStyle != null)
            return;
        tooltipStyle = PrototypeUiTheme09F15.Panel(10);
        tooltipStyle.alignment = TextAnchor.UpperLeft;
        tooltipStyle.wordWrap = true;
    }

    private void OnGUI()
    {
        if (legacy == null || IsSelected || !IsMouseOverHQ(Input.mousePosition))
            return;

        EnsureStyle();
        GUI.depth = -900;

        Vector2 p = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        const float w = 250f;
        const float h = 76f;
        float x = Mathf.Clamp(p.x + 16f, 8f, Screen.width - w - 8f);
        float y = Mathf.Clamp(p.y - 24f, 38f, Screen.height - h - 8f);
        GUI.Box(new Rect(x, y, w, h), GetHoverText(), tooltipStyle);
    }
}
