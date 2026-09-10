using System.Collections.Generic;
using UnityEngine;

// v00.00.09m1 - readable company command card + world chips.
[DefaultExecutionOrder(20040)]
public sealed class PrototypeKampCompanyHud09M1 : MonoBehaviour
{
    private GUIStyle cardStyle;
    private GUIStyle titleStyle;
    private GUIStyle chipStyle;
    private GUIStyle hintStyle;
    private bool announced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeKampCompanyHud09M1>() != null)
            return;

        GameObject root = new GameObject("PrototypeKampCompanyHud_v000009m1");
        root.AddComponent<PrototypeKampCompanyHud09M1>();
    }

    private void Update()
    {
        if (announced)
            return;
        PrototypeCompanyTacticalControl09L2 control = PrototypeCompanyTacticalControl09L2.Instance;
        if (control == null || !control.Installed)
            return;
        announced = true;
        Debug.Log("KAMP-HUD-09M1|Installed=True|SelectedCard=True|WorldChips=Selected|Formation=LIN/KOL");
    }

    private void OnGUI()
    {
        PrototypeCompanyTacticalControl09L2 control = PrototypeCompanyTacticalControl09L2.Instance;
        if (control == null || !control.Installed)
            return;

        EnsureStyles();
        Camera cam = Camera.main;
        DrawWorldChips(control, cam);
        DrawSelectedCard(control);
    }

    private void DrawWorldChips(PrototypeCompanyTacticalControl09L2 control, Camera cam)
    {
        if (cam == null)
            return;

        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;
        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = companies[i];
            if (company == null || company.CurrentStrength <= 0 || !company.IsSelected)
                continue;

            Vector3 screen = cam.WorldToScreenPoint(company.transform.position + Vector3.up * 2.4f);
            if (screen.z <= 0f)
                continue;

            string form = company.Formation == RegimentFormation.Column ? "KOL" : "LIN";
            string status = PrototypeKampCompanyMarch09M1.Instance != null
                ? PrototypeKampCompanyMarch09M1.Instance.GetStatusLabel(company)
                : (company.IsMoving ? "MARSCH" : "HOLD");
            string text = ShortName(company) + "  " + company.CurrentStrength + "  " + form + "  " + status;

            float width = 168f;
            float x = screen.x - width * 0.5f;
            float y = Screen.height - screen.y - 22f;
            GUI.Box(new Rect(x, y, width, 20f), text, chipStyle);
        }
    }

    private void DrawSelectedCard(PrototypeCompanyTacticalControl09L2 control)
    {
        if (!control.HasCompanySelection)
            return;

        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> selected = control.SelectedCompanies;
        PrototypeCompanyTacticalEntity09L2 lead = selected[0];
        if (lead == null)
            return;

        Regiment parent = lead.ParentRegiment;
        string form = lead.Formation == RegimentFormation.Column ? "KOLONNE" : "LINIE";
        string status = PrototypeKampCompanyMarch09M1.Instance != null
            ? PrototypeKampCompanyMarch09M1.Instance.GetStatusLabel(lead)
            : (lead.IsMoving ? "MARSCH" : "HOLD");

        int men = 0;
        int initial = 0;
        for (int i = 0; i < selected.Count; i++)
        {
            if (selected[i] == null)
                continue;
            men += selected[i].CurrentStrength;
            initial += selected[i].InitialStrength;
        }

        string title = selected.Count == 1
            ? lead.DisplayName
            : selected.Count + " kompagnier valgt";

        string body =
            title + "\n" +
            "Styrke  " + men + " / " + initial +
            "    Formation  " + form +
            "    " + status + "\n";

        if (parent != null)
        {
            body +=
                "Morale " + parent.Morale.ToString("0") +
                "    Kohaesion " + parent.Cohesion.ToString("0") +
                "    " + parent.WeaponShortName + "\n";
        }

        body += selected.Count == 1
            ? "Lang march skifter selv til kolonne. F = linie. C = kolonne."
            : "Gruppe bevarer indbyrdes afstand. Lang march = kolonne pr. kompagni.";

        float width = 420f;
        float height = 78f;
        Rect card = new Rect(12f, Screen.height - height - 78f, width, height);
        GUI.Box(card, body, cardStyle);
        GUI.Label(new Rect(card.x + 8f, card.y + 4f, card.width - 16f, 16f), "KOMPAGNI", titleStyle);
        GUI.Label(
            new Rect(12f, Screen.height - 26f, 520f, 18f),
            "RMB flyt  |  F linie  |  C kolonne  |  H hold  |  lang march = kolonne til kontakt/destination",
            hintStyle);
    }

    private static string ShortName(PrototypeCompanyTacticalEntity09L2 company)
    {
        string regiment = company.ParentRegiment != null ? company.ParentRegiment.RegimentName : "Reg";
        if (regiment.StartsWith("1."))
            regiment = "1.R";
        else if (regiment.StartsWith("8"))
            regiment = "8.R";
        else if (regiment.StartsWith("5"))
            regiment = "5.R";
        else if (regiment.StartsWith("18"))
            regiment = "18.R";
        return regiment + " K" + (company.CompanyIndex + 1);
    }

    private void EnsureStyles()
    {
        if (cardStyle != null)
            return;

        cardStyle = new GUIStyle(GUI.skin.box);
        cardStyle.fontSize = 11;
        cardStyle.alignment = TextAnchor.UpperLeft;
        cardStyle.padding = new RectOffset(10, 10, 20, 8);
        cardStyle.normal.textColor = new Color(0.95f, 0.93f, 0.86f);
        cardStyle.wordWrap = true;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 10;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = new Color(0.98f, 0.82f, 0.28f);

        chipStyle = new GUIStyle(GUI.skin.box);
        chipStyle.fontSize = 10;
        chipStyle.fontStyle = FontStyle.Bold;
        chipStyle.alignment = TextAnchor.MiddleCenter;
        chipStyle.normal.textColor = Color.white;

        hintStyle = new GUIStyle(GUI.skin.label);
        hintStyle.fontSize = 10;
        hintStyle.normal.textColor = new Color(0.90f, 0.88f, 0.80f);
    }
}
