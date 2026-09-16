using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

// v00.00.09f29s + v00.00.09f29u compatibility hotfix
// TEST-only replacement for the old full-width "DEBUG · ENEMY AI SPAWNER" bar.
//
// UX rules:
// - compact OOB-style panel in the upper-right, below the permanent time/view controls,
// - collapsed by default,
// - expanded state exposes Start Fjende 1 / Start Fjende 2 only,
// - this entire panel is temporary QA UI and is intended to disappear before release.
//
// Compatibility:
// Older prototype generations may still contain a legacy MonoBehaviour that owns the
// original enemy-spawner OnGUI. F29S identifies that UI by its embedded GUI strings,
// disables the legacy component, and keeps the instance as an optional action target.
// If no callable legacy start method can be resolved, the two known Prussian QA
// companies are activated through the current OfficerAIController instead.
[DefaultExecutionOrder(126000)]
public sealed class PrototypeEnemyTestPanel09F29S : MonoBehaviour
{
    public static PrototypeEnemyTestPanel09F29S Instance { get; private set; }

    private const float PanelWidth = 188f;
    private const float PanelXMargin = 8f;
    private const float PanelY = 64f;
    private const float HeaderHeight = 25f;
    private const float RowHeight = 25f;
    private const float Gap = 3f;

    private static readonly Color PanelColor = new Color(0.030f, 0.040f, 0.032f, 0.96f);
    private static readonly Color HeaderColor = new Color(0.075f, 0.090f, 0.065f, 0.97f);
    private static readonly Color ButtonColor = new Color(0.085f, 0.105f, 0.075f, 0.98f);
    private static readonly Color ActiveColor = new Color(0.13f, 0.34f, 0.13f, 0.98f);
    private static readonly Color TestColor = new Color(0.95f, 0.72f, 0.20f, 1f);
    private static readonly Color TextColor = new Color(0.94f, 0.94f, 0.87f, 1f);

    private bool open; // Deliberately false: test panel starts collapsed.
    private bool legacyScanComplete;
    private float legacyScanStartedAt;
    private MonoBehaviour legacySpawner;
    private MethodInfo legacyStartEnemy1;
    private MethodInfo legacyStartEnemy2;
    private MethodInfo legacyIndexedStart;

    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle tinyButtonStyle;
    private GUIStyle actionButtonStyle;
    private GUIStyle activeButtonStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeEnemyTestPanel09F29S>() == null)
        {
            new GameObject("PrototypeEnemyTestPanel_v000009f29s")
                .AddComponent<PrototypeEnemyTestPanel09F29S>();
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        open = false;
        legacyScanStartedAt = Time.unscaledTime;

        Debug.Log(
            "ENEMY-TEST-09F29S|Installed=True|Default=Collapsed|Position=UpperRight|" +
            "LegacyWideSpawner=Suppress|ReleaseUI=False|TimerApi=UnscaledTime");
    }

    private void Start()
    {
        SuppressLegacySpawnerUi();
    }

    private void Update()
    {
        // Some old runtime-created QA components appear after this component's Start.
        // Retry until the scene has settled; once found, keep the legacy instance only
        // as an optional action target while its large OnGUI remains disabled.
        if (!legacyScanComplete)
            SuppressLegacySpawnerUi();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static bool IsPointerOverPanel(Vector3 mousePosition)
    {
        PrototypeEnemyTestPanel09F29S instance = Instance;
        if (instance == null)
            return false;

        Vector2 gui = new Vector2(mousePosition.x, Screen.height - mousePosition.y);
        return instance.GetPanelRect().Contains(gui);
    }

    private Rect GetPanelRect()
    {
        float height = HeaderHeight;
        if (open)
            height += Gap + RowHeight * 2f + Gap * 2f;

        return new Rect(
            Mathf.Max(8f, Screen.width - PanelWidth - PanelXMargin),
            PanelY,
            PanelWidth,
            height);
    }

    private void OnGUI()
    {
        EnsureStyles();
        GUI.depth = -126000;

        Rect panel = GetPanelRect();
        GUI.Box(panel, GUIContent.none, panelStyle);

        Rect header = new Rect(panel.x, panel.y, panel.width, HeaderHeight);
        GUI.Box(header, GUIContent.none, headerStyle);
        GUI.Label(new Rect(header.x + 7f, header.y + 1f, header.width - 42f, header.height - 2f),
            "TEST FJENDE", headerStyle);

        if (GUI.Button(new Rect(header.xMax - 30f, header.y + 2f, 24f, 21f),
            open ? "−" : "+", tinyButtonStyle))
        {
            open = !open;
            ConsumePointer();
        }

        if (!open)
            return;

        float y = header.yMax + Gap;
        DrawEnemyButton(new Rect(panel.x + 5f, y, panel.width - 10f, RowHeight), 1);
        y += RowHeight + Gap;
        DrawEnemyButton(new Rect(panel.x + 5f, y, panel.width - 10f, RowHeight), 2);
    }

    private void DrawEnemyButton(Rect rect, int enemyNumber)
    {
        bool active = IsEnemyAiActive(enemyNumber);
        GUIStyle style = active ? activeButtonStyle : actionButtonStyle;
        string label = "Start Fjende " + enemyNumber + (active ? "  [AI ON]" : string.Empty);

        if (GUI.Button(rect, label, style))
        {
            StartEnemy(enemyNumber);
            ConsumePointer();
        }
    }

    private void StartEnemy(int enemyNumber)
    {
        bool legacyInvoked = TryInvokeLegacyStart(enemyNumber);
        bool currentAiActivated = ActivateCurrentEnemyAi(enemyNumber);

        Debug.Log(
            "ENEMY-TEST-09F29S|Action=StartEnemy|Enemy=" + enemyNumber +
            "|LegacyInvoked=" + legacyInvoked +
            "|OfficerAIActivated=" + currentAiActivated);
    }

    private bool TryInvokeLegacyStart(int enemyNumber)
    {
        if (legacySpawner == null)
            return false;

        MethodInfo exact = enemyNumber == 1 ? legacyStartEnemy1 : legacyStartEnemy2;
        if (exact != null)
        {
            try
            {
                exact.Invoke(legacySpawner, null);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    "ENEMY-TEST-09F29S|LegacyInvoke=False|Enemy=" + enemyNumber +
                    "|Method=" + exact.Name + "|Error=" + Unwrap(ex));
            }
        }

        if (legacyIndexedStart != null)
        {
            try
            {
                ParameterInfo[] parameters = legacyIndexedStart.GetParameters();
                object value = parameters.Length == 1 && parameters[0].ParameterType == typeof(int)
                    ? (object)enemyNumber
                    : (object)(enemyNumber - 1);
                legacyIndexedStart.Invoke(legacySpawner, new[] { value });
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    "ENEMY-TEST-09F29S|LegacyInvoke=False|Enemy=" + enemyNumber +
                    "|Method=" + legacyIndexedStart.Name + "|Error=" + Unwrap(ex));
            }
        }

        return false;
    }

    private static bool ActivateCurrentEnemyAi(int enemyNumber)
    {
        Regiment enemy = ResolveEnemy(enemyNumber);
        if (enemy == null || enemy.IsRouted || enemy.CurrentStrength <= 0)
            return false;

        OfficerAIController controller = enemy.GetComponent<OfficerAIController>();
        if (controller == null)
            controller = enemy.gameObject.AddComponent<OfficerAIController>();

        // OfficerAIPrototypeManager normally configures this before the player can
        // use the test panel. If a controller was only just created, wait for the
        // normal manager rather than inventing a second OfficerProfile authority.
        if (controller.Officer == null)
        {
            Debug.LogWarning(
                "ENEMY-TEST-09F29S|Enemy=" + enemyNumber +
                "|OfficerAI=NotConfiguredYet|Unit=" + enemy.RegimentName);
            return false;
        }

        controller.SetDoctrine(OfficerAIDoctrine.Offensive);
        controller.SetAIEnabled(true);
        return true;
    }

    private static bool IsEnemyAiActive(int enemyNumber)
    {
        Regiment enemy = ResolveEnemy(enemyNumber);
        if (enemy == null)
            return false;
        OfficerAIController controller = enemy.GetComponent<OfficerAIController>();
        return controller != null && controller.AIEnabled;
    }

    private static Regiment ResolveEnemy(int enemyNumber)
    {
        BattleManager battle = BattleManager.Instance;
        if (battle == null || battle.Regiments == null)
            return null;

        // Stable F29B QA identities. Fall back to deterministic Prussian order if a
        // later test scenario changes names while still exposing two enemies.
        string preferred = enemyNumber == 1 ? "8th Regiment" : "18th Regiment";
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment != null && regiment.Team == BattleTeam.Prussia && regiment.RegimentName == preferred)
                return regiment;
        }

        List<Regiment> enemies = new List<Regiment>();
        foreach (Regiment regiment in battle.Regiments)
        {
            if (regiment != null && regiment.Team == BattleTeam.Prussia)
                enemies.Add(regiment);
        }

        enemies.Sort((a, b) => string.CompareOrdinal(a.RegimentName, b.RegimentName));
        int index = enemyNumber - 1;
        return index >= 0 && index < enemies.Count ? enemies[index] : null;
    }

    private void SuppressLegacySpawnerUi()
    {
        MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
        bool sceneReady = BattleManager.Instance != null;

        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour == null || behaviour == this)
                continue;

            Type type = behaviour.GetType();
            MethodInfo onGui = type.GetMethod(
                "OnGUI",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (onGui == null)
                continue;

            if (!MethodContainsAnyGuiMarker(onGui))
                continue;

            legacySpawner = behaviour;
            ResolveLegacyActions(type);

            if (behaviour.enabled)
                behaviour.enabled = false;

            legacyScanComplete = true;
            Debug.Log(
                "ENEMY-TEST-09F29S|LegacyWideSpawner=Disabled|Type=" + type.FullName +
                "|Method1=" + MethodName(legacyStartEnemy1) +
                "|Method2=" + MethodName(legacyStartEnemy2) +
                "|Indexed=" + MethodName(legacyIndexedStart));
            return;
        }

        // Once BattleManager exists and the compact panel has had two unscaled seconds
        // to discover late-created QA components, no match means the old overlay is absent.
        if (sceneReady && Time.unscaledTime - legacyScanStartedAt > 2.0f)
        {
            legacyScanComplete = true;
            Debug.Log("ENEMY-TEST-09F29S|LegacyWideSpawner=NotFound|CompactPanelOnly=True");
        }
    }

    private void ResolveLegacyActions(Type type)
    {
        MethodInfo[] methods = type.GetMethods(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        legacyStartEnemy1 = null;
        legacyStartEnemy2 = null;
        legacyIndexedStart = null;

        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];
            if (method == null || method.ReturnType != typeof(void))
                continue;

            string compact = Normalize(method.Name);
            ParameterInfo[] parameters = method.GetParameters();

            if (parameters.Length == 0)
            {
                if (legacyStartEnemy1 == null && IsEnemyStartName(compact, 1))
                    legacyStartEnemy1 = method;
                if (legacyStartEnemy2 == null && IsEnemyStartName(compact, 2))
                    legacyStartEnemy2 = method;
                continue;
            }

            if (legacyIndexedStart == null && parameters.Length == 1 &&
                parameters[0].ParameterType == typeof(int) &&
                (compact.Contains("start") || compact.Contains("spawn")) &&
                (compact.Contains("enemy") || compact.Contains("fjende")))
            {
                legacyIndexedStart = method;
            }
        }
    }

    private static bool IsEnemyStartName(string compactName, int number)
    {
        bool action = compactName.Contains("start") || compactName.Contains("spawn");
        bool enemy = compactName.Contains("enemy") || compactName.Contains("fjende");
        return action && enemy && compactName.Contains(number.ToString());
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrEmpty(value)
            ? string.Empty
            : value.Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
    }

    private static bool MethodContainsAnyGuiMarker(MethodInfo method)
    {
        return MethodContainsString(method, "ENEMY AI SPAWNER") ||
               MethodContainsString(method, "Start fjende") ||
               MethodContainsString(method, "Start Fjende");
    }

    private static bool MethodContainsString(MethodInfo method, string marker)
    {
        if (method == null || string.IsNullOrEmpty(marker))
            return false;

        MethodBody body;
        try
        {
            body = method.GetMethodBody();
        }
        catch
        {
            return false;
        }

        if (body == null)
            return false;

        byte[] il = body.GetILAsByteArray();
        if (il == null || il.Length == 0)
            return false;

        int position = 0;
        while (position < il.Length)
        {
            OpCode opCode;
            byte first = il[position++];
            if (first == 0xFE)
            {
                if (position >= il.Length)
                    break;
                opCode = TwoByteOpCodes[il[position++]];
            }
            else
            {
                opCode = OneByteOpCodes[first];
            }

            if (opCode.Equals(OpCodes.Ldstr))
            {
                if (position + 4 > il.Length)
                    break;
                int token = BitConverter.ToInt32(il, position);
                try
                {
                    string text = method.Module.ResolveString(token);
                    if (!string.IsNullOrEmpty(text) &&
                        text.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                }
                catch
                {
                    // Ignore invalid string resolution from a foreign/dynamic module.
                }
            }

            position += OperandSize(opCode.OperandType, il, position);
        }

        return false;
    }

    private static int OperandSize(OperandType operandType, byte[] il, int position)
    {
        switch (operandType)
        {
            case OperandType.InlineNone:
                return 0;
            case OperandType.ShortInlineBrTarget:
            case OperandType.ShortInlineI:
            case OperandType.ShortInlineVar:
                return 1;
            case OperandType.InlineVar:
                return 2;
            case OperandType.InlineI:
            case OperandType.InlineBrTarget:
            case OperandType.InlineField:
            case OperandType.InlineMethod:
            case OperandType.InlineSig:
            case OperandType.InlineString:
            case OperandType.InlineTok:
            case OperandType.InlineType:
            case OperandType.ShortInlineR:
                return 4;
            case OperandType.InlineI8:
            case OperandType.InlineR:
                return 8;
            case OperandType.InlineSwitch:
                if (position + 4 > il.Length)
                    return 0;
                int count = BitConverter.ToInt32(il, position);
                return 4 + Mathf.Max(0, count) * 4;
            default:
                return 0;
        }
    }

    private static readonly OpCode[] OneByteOpCodes = BuildOneByteOpCodes();
    private static readonly OpCode[] TwoByteOpCodes = BuildTwoByteOpCodes();

    private static OpCode[] BuildOneByteOpCodes()
    {
        OpCode[] table = new OpCode[256];
        FieldInfo[] fields = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static);
        for (int i = 0; i < fields.Length; i++)
        {
            if (!(fields[i].GetValue(null) is OpCode opCode))
                continue;
            ushort value = unchecked((ushort)opCode.Value);
            if (value < 0x100)
                table[value] = opCode;
        }
        return table;
    }

    private static OpCode[] BuildTwoByteOpCodes()
    {
        OpCode[] table = new OpCode[256];
        FieldInfo[] fields = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static);
        for (int i = 0; i < fields.Length; i++)
        {
            if (!(fields[i].GetValue(null) is OpCode opCode))
                continue;
            ushort value = unchecked((ushort)opCode.Value);
            if ((value & 0xFF00) == 0xFE00)
                table[value & 0xFF] = opCode;
        }
        return table;
    }

    private static string MethodName(MethodInfo method)
    {
        return method != null ? method.Name : "None";
    }

    private static string Unwrap(Exception ex)
    {
        if (ex is TargetInvocationException invocation && invocation.InnerException != null)
            return invocation.InnerException.Message;
        return ex != null ? ex.Message : "Unknown";
    }

    private void EnsureStyles()
    {
        if (panelStyle != null)
            return;

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = MakeTexture(PanelColor);
        panelStyle.padding = new RectOffset(0, 0, 0, 0);

        headerStyle = new GUIStyle(GUI.skin.box);
        headerStyle.normal.background = MakeTexture(HeaderColor);
        headerStyle.normal.textColor = TestColor;
        headerStyle.fontSize = 10;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleLeft;
        headerStyle.padding = new RectOffset(7, 4, 0, 0);

        tinyButtonStyle = new GUIStyle(GUI.skin.button);
        tinyButtonStyle.fontSize = 10;
        tinyButtonStyle.fontStyle = FontStyle.Bold;
        tinyButtonStyle.padding = new RectOffset(1, 1, 1, 1);

        actionButtonStyle = new GUIStyle(GUI.skin.button);
        actionButtonStyle.normal.background = MakeTexture(ButtonColor);
        actionButtonStyle.normal.textColor = TextColor;
        actionButtonStyle.fontSize = 9;
        actionButtonStyle.fontStyle = FontStyle.Bold;
        actionButtonStyle.alignment = TextAnchor.MiddleCenter;

        activeButtonStyle = new GUIStyle(actionButtonStyle);
        activeButtonStyle.normal.background = MakeTexture(ActiveColor);
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color);
        texture.Apply(false, true);
        return texture;
    }

    private static void ConsumePointer()
    {
        Input.ResetInputAxes();
    }
}
