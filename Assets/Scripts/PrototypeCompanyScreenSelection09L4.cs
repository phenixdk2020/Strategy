using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// v00.00.09l4 - screen-space company hit testing.
// GPU-instanced soldiers have no colliders. This compatibility layer therefore
// selects a company from the projected visible company footprint after the ordinary
// 09l2 click path has completed. Drag selection remains owned by 09l2.
[DefaultExecutionOrder(300)]
public sealed class PrototypeCompanyScreenSelection09L4 : MonoBehaviour
{
    private const float ClickDragThresholdPixels = 9f;
    private const float ScreenPaddingPixels = 7f;

    private Camera cam;
    private bool tracking;
    private Vector2 mouseDown;
    private bool shiftAtDown;
    private bool ctrlAtDown;

    private FieldInfo selectedCompaniesField;
    private FieldInfo regimentSelectedField;
    private IList selectedCompanies;
    private IList selectedRegiments;
    private bool announced;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeCompanyScreenSelection09L4>() != null)
            return;

        GameObject root = new GameObject("PrototypeCompanyScreenSelection_v000009l4");
        root.AddComponent<PrototypeCompanyScreenSelection09L4>();
    }

    private void Update()
    {
        PrototypeCompanyTacticalControl09L2 control = PrototypeCompanyTacticalControl09L2.Instance;
        if (control == null || !control.Installed)
            return;

        if (cam == null)
            cam = Camera.main;
        if (cam == null)
            return;

        ResolveLists(control);
        if (selectedCompanies == null)
            return;

        if (!announced)
        {
            announced = true;
            Debug.Log(
                "SELECT-09L4|Installed=True|Mode=ScreenSpaceCompanyFootprint|" +
                "SoldierCollidersRequired=False|DragSelectionOwner=09L2");
        }

        bool overUi = BattleManager.Instance != null &&
                      BattleManager.Instance.IsPointerOverSimulationControls(Input.mousePosition);

        if (!overUi && Input.GetMouseButtonDown(0))
        {
            tracking = true;
            mouseDown = Input.mousePosition;
            shiftAtDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            ctrlAtDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        }

        if (!tracking || !Input.GetMouseButtonUp(0))
            return;

        tracking = false;
        Vector2 mouseUp = Input.mousePosition;
        if (Vector2.Distance(mouseDown, mouseUp) >= ClickDragThresholdPixels)
            return;

        PrototypeCompanyTacticalEntity09L2 hit = FindCompanyAtScreenPoint(control, mouseUp);
        if (hit == null || hit.ParentRegiment == null || hit.ParentRegiment.Team != BattleTeam.Denmark)
            return;

        ApplySelection(hit, shiftAtDown, ctrlAtDown);
    }

    private void ResolveLists(PrototypeCompanyTacticalControl09L2 control)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;

        if (selectedCompaniesField == null)
            selectedCompaniesField = typeof(PrototypeCompanyTacticalControl09L2).GetField("selectedCompanies", flags);

        if (selectedCompanies == null && selectedCompaniesField != null)
            selectedCompanies = selectedCompaniesField.GetValue(control) as IList;

        PlayerCommander commander = PlayerCommander.Instance;
        if (commander == null)
            return;

        if (regimentSelectedField == null)
            regimentSelectedField = typeof(PlayerCommander).GetField("selected", flags);

        if (selectedRegiments == null && regimentSelectedField != null)
            selectedRegiments = regimentSelectedField.GetValue(commander) as IList;
    }

    private PrototypeCompanyTacticalEntity09L2 FindCompanyAtScreenPoint(
        PrototypeCompanyTacticalControl09L2 control,
        Vector2 screenPoint)
    {
        IReadOnlyList<PrototypeCompanyTacticalEntity09L2> companies = control.Companies;
        PrototypeCompanyTacticalEntity09L2 best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < companies.Count; i++)
        {
            PrototypeCompanyTacticalEntity09L2 company = companies[i];
            if (company == null || company.ParentRegiment == null ||
                company.ParentRegiment.Team != BattleTeam.Denmark ||
                !company.gameObject.activeInHierarchy || company.ParentRegiment.IsRouted)
                continue;

            Rect rect;
            Vector2 centre;
            if (!TryGetProjectedFootprint(company, out rect, out centre))
                continue;

            rect.xMin -= ScreenPaddingPixels;
            rect.xMax += ScreenPaddingPixels;
            rect.yMin -= ScreenPaddingPixels;
            rect.yMax += ScreenPaddingPixels;

            if (!rect.Contains(screenPoint, true))
                continue;

            float score = Vector2.SqrMagnitude(screenPoint - centre);
            if (score < bestScore)
            {
                bestScore = score;
                best = company;
            }
        }

        return best;
    }

    private bool TryGetProjectedFootprint(
        PrototypeCompanyTacticalEntity09L2 company,
        out Rect rect,
        out Vector2 centre)
    {
        rect = new Rect();
        centre = Vector2.zero;

        float halfW = company.GetFootprintWidth() * 0.5f;
        float halfD = company.GetFootprintDepth() * 0.5f;

        Vector3[] localCorners =
        {
            new Vector3(-halfW, 0.7f, -halfD),
            new Vector3(-halfW, 0.7f,  halfD),
            new Vector3( halfW, 0.7f,  halfD),
            new Vector3( halfW, 0.7f, -halfD)
        };

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < localCorners.Length; i++)
        {
            Vector3 world = company.transform.TransformPoint(localCorners[i]);
            Vector3 screen = cam.WorldToScreenPoint(world);
            if (screen.z <= 0f)
                return false;

            minX = Mathf.Min(minX, screen.x);
            maxX = Mathf.Max(maxX, screen.x);
            minY = Mathf.Min(minY, screen.y);
            maxY = Mathf.Max(maxY, screen.y);
        }

        rect = Rect.MinMaxRect(minX, minY, maxX, maxY);
        Vector3 centre3 = cam.WorldToScreenPoint(company.transform.position + Vector3.up * 0.7f);
        if (centre3.z <= 0f)
            return false;

        centre = new Vector2(centre3.x, centre3.y);
        return true;
    }

    private void ApplySelection(
        PrototypeCompanyTacticalEntity09L2 company,
        bool additive,
        bool toggle)
    {
        ClearRegimentSelection();

        if (!additive && !toggle)
            ClearCompanySelection();

        if (toggle && selectedCompanies.Contains(company))
        {
            selectedCompanies.Remove(company);
            company.SetSelected(false);
        }
        else
        {
            if (!selectedCompanies.Contains(company))
                selectedCompanies.Add(company);
            company.SetSelected(true);
        }

        Debug.Log(
            "SELECT-09L4|Company=" + company.DisplayName +
            "|Selected=True|Mode=" + (toggle ? "Toggle" : additive ? "Add" : "Replace") +
            "|Source=VisibleFootprint");
    }

    private void ClearCompanySelection()
    {
        if (selectedCompanies == null)
            return;

        for (int i = selectedCompanies.Count - 1; i >= 0; i--)
        {
            PrototypeCompanyTacticalEntity09L2 company =
                selectedCompanies[i] as PrototypeCompanyTacticalEntity09L2;
            if (company != null)
                company.SetSelected(false);
        }
        selectedCompanies.Clear();
    }

    private void ClearRegimentSelection()
    {
        if (selectedRegiments == null)
            return;

        for (int i = selectedRegiments.Count - 1; i >= 0; i--)
        {
            Regiment regiment = selectedRegiments[i] as Regiment;
            if (regiment != null)
                regiment.SetSelected(false);
        }
        selectedRegiments.Clear();
    }
}
