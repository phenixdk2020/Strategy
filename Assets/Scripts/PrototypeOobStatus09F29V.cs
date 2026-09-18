using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09f29v
// Adds real tactical company status to the existing OOB right column without changing
// the proven OOB selection/double-click navigation code. Status is derived from combat
// state plus the active Major mission: KLAR, RYKKER, FORSVAR, RESERVE, FLANKE,
// SQUARE, KAMP, UNDER ILD, CHARGE, MELEE or ROUT.
[DefaultExecutionOrder(40300)]
public sealed class PrototypeOobStatus09F29V : MonoBehaviour
{
    private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private const float PanelX = 8f;
    private const float PanelY = 39f;
    private const float PanelWidth = 292f;
    private const float HeaderHeight = 27f;
    private const float RegimentRowHeight = 25f;
    private const float MajorRowHeight = 23f;
    private const float CompanyRowHeight = 21f;

    private static readonly Color RowColor = new Color(0.075f, 0.090f, 0.065f, 0.95f);
    private static readonly Color RowAlternate = new Color(0.055f, 0.070f, 0.052f, 0.95f);
    private static readonly Color SelectedColor = new Color(0.44f, 0.34f, 0.075f, 0.98f);
    private static readonly Color MutedColor = new Color(0.75f, 0.78f, 0.69f, 1f);
    private static readonly Color AlertColor = new Color(1.00f, 0.56f, 0.24f, 1f);
    private static readonly Color ReadyColor = new Color(0.62f, 0.86f, 0.58f, 1f);

    private FieldInfo oobOpenField;
    private FieldInfo oobBattalionOpenField;
    private FieldInfo hierarchyBattalionsField;
    private FieldInfo regimentHasDestinationField;
    private GUIStyle rightStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeOobStatus09F29V>() == null)
            new GameObject("PrototypeOobStatus_v000009f29v").AddComponent<PrototypeOobStatus09F29V>();
    }

    private void Awake()
    {
        oobOpenField = typeof(PrototypeOobNavigator09F29Q).GetField("open", AnyInstance);
        oobBattalionOpenField = typeof(PrototypeOobNavigator09F29Q).GetField("battalionOpen", AnyInstance);
        hierarchyBattalionsField = typeof(PrototypeRegimentHierarchy09F27).GetField("battalions", AnyInstance);
        regimentHasDestinationField = typeof(Regiment).GetField("hasDestination", BindingFlags.Instance | BindingFlags.NonPublic);
        Debug.Log("OOB-STATUS-09F29V|Installed=True|CompanyStatus=KLAR-RYKKER-FORSVAR-RESERVE-FLANKE-SQUARE-KAMP-ILD-CHARGE-MELEE-ROUT");
    }

    private void OnGUI()
    {
        // F30D+ owns the complete OOB columns. Never draw the F29V right-column patch
        // on top of the unified OOB; this was the source of the repeated black "190 KLAR" layer.
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeHigherCommandOob09F30D>() != null)
            return;

        PrototypeOobNavigator09F29Q oob = PrototypeOobNavigator09F29Q.Instance;
        PrototypeRegimentHierarchy09F27 hierarchy = PrototypeRegimentHierarchy09F27.Instance;
        if (oob == null || hierarchy == null || !hierarchy.Installed || !IsOobOpen(oob))
            return;

        EnsureStyle();
        GUI.depth = -126000;

        bool[] expanded = GetBattalionOpen(oob);
        float y = PanelY + HeaderHeight + 2f;
        y += RegimentRowHeight + 2f;

        for (int b = 0; b < Mathf.Min(2, hierarchy.BattalionCount); b++)
        {
            y += MajorRowHeight;
            if (expanded == null || b >= expanded.Length || !expanded[b])
                continue;

            IReadOnlyList<Regiment> companies = hierarchy.GetCompanies(b);
            if (companies == null)
                continue;

            for (int c = 0; c < companies.Count; c++)
            {
                Regiment unit = companies[c];
                Rect patch = new Rect(PanelX + PanelWidth - 84f, y, 77f, CompanyRowHeight);
                Color background = unit != null && unit.IsSelected ? SelectedColor : ((c & 1) == 0 ? RowColor : RowAlternate);
                Color old = GUI.color;
                GUI.color = background;
                GUI.DrawTexture(patch, Texture2D.whiteTexture);
                GUI.color = old;

                string status = GetStatus(hierarchy, unit);
                int strength = unit != null ? unit.CurrentStrength : 0;
                rightStyle.normal.textColor = StatusColor(status);
                GUI.Label(new Rect(patch.x + 1f, patch.y, patch.width - 3f, patch.height),
                    unit != null ? strength + "  " + status : "—", rightStyle);
                y += CompanyRowHeight;
            }
        }
    }

    private string GetStatus(PrototypeRegimentHierarchy09F27 hierarchy, Regiment unit)
    {
        if (unit == null) return "—";
        if (unit.IsRouted) return "ROUT";

        PrototypeInfantryCharge09F25 charge = PrototypeInfantryCharge09F25.Instance;
        if (charge != null && charge.IsChargeMeleeParticipant(unit)) return "MELEE";
        if (charge != null && charge.IsCharging(unit)) return "CHARGE";
        if (PrototypeInfantrySquare09F29.IsInSquare(unit)) return "SQUARE";
        if (PrototypeAttackContact09F29G.IsLocalContact(unit)) return "KAMP";
        if (PrototypeUnderFireReaction09F26.IsReacting(unit) || unit.HasHitFeedback) return "UNDER ILD";

        if (TryGetMission(hierarchy, unit, out string order, out bool reserve, out bool flank, out bool arrived))
        {
            if (reserve) return "RESERVE";
            if (flank) return "FLANKE";
            switch (order)
            {
                case "DefendHere": return arrived ? "FORSVAR" : "RYKKER";
                case "AttackHere": return arrived ? "ANGRIB" : "RYKKER";
                case "AdvanceHere": return arrived ? "KLAR" : "RYKKER";
                case "WithdrawHere": return "TILBAGE";
                case "AssembleHere": return arrived ? "KLAR" : "SAML";
                case "HoldPosition": return "KLAR";
            }
        }

        if (HasDestination(unit)) return "RYKKER";
        return "KLAR";
    }

    private bool TryGetMission(PrototypeRegimentHierarchy09F27 hierarchy, Regiment unit, out string order, out bool reserve, out bool flank, out bool arrived)
    {
        order = string.Empty; reserve = false; flank = false; arrived = false;
        if (hierarchy == null || hierarchyBattalionsField == null || unit == null)
            return false;

        IList battalions = hierarchyBattalionsField.GetValue(hierarchy) as IList;
        if (battalions == null)
            return false;

        foreach (object battalion in battalions)
        {
            if (battalion == null) continue;
            FieldInfo missionsField = battalion.GetType().GetField("Missions", AnyInstance);
            IDictionary missions = missionsField != null ? missionsField.GetValue(battalion) as IDictionary : null;
            if (missions == null || !missions.Contains(unit)) continue;

            object mission = missions[unit];
            if (mission == null) return false;
            System.Type type = mission.GetType();
            FieldInfo orderField = type.GetField("Order", AnyInstance);
            FieldInfo reserveField = type.GetField("Reserve", AnyInstance);
            FieldInfo flankField = type.GetField("Flank", AnyInstance);
            FieldInfo arrivedField = type.GetField("Arrived", AnyInstance);
            object orderValue = orderField != null ? orderField.GetValue(mission) : null;
            order = orderValue != null ? orderValue.ToString() : string.Empty;
            reserve = reserveField != null && reserveField.GetValue(mission) is bool && (bool)reserveField.GetValue(mission);
            flank = flankField != null && flankField.GetValue(mission) is bool && (bool)flankField.GetValue(mission);
            arrived = arrivedField != null && arrivedField.GetValue(mission) is bool && (bool)arrivedField.GetValue(mission);
            return true;
        }
        return false;
    }

    private bool HasDestination(Regiment unit)
    {
        if (unit == null || regimentHasDestinationField == null) return false;
        object value = regimentHasDestinationField.GetValue(unit);
        return value is bool && (bool)value;
    }

    private bool IsOobOpen(PrototypeOobNavigator09F29Q oob)
    {
        if (oobOpenField == null) return true;
        object value = oobOpenField.GetValue(oob);
        return !(value is bool) || (bool)value;
    }

    private bool[] GetBattalionOpen(PrototypeOobNavigator09F29Q oob)
    {
        return oobBattalionOpenField != null ? oobBattalionOpenField.GetValue(oob) as bool[] : null;
    }

    private static Color StatusColor(string status)
    {
        switch (status)
        {
            case "KLAR":
            case "FORSVAR": return ReadyColor;
            case "KAMP":
            case "UNDER ILD":
            case "CHARGE":
            case "MELEE":
            case "ROUT": return AlertColor;
            default: return MutedColor;
        }
    }

    private void EnsureStyle()
    {
        if (rightStyle != null) return;
        rightStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 8,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight
        };
        rightStyle.normal.textColor = MutedColor;
    }
}