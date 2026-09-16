using System.Reflection;
using UnityEngine;

// v00.00.09f29t
// Prevents a minimap click from also resolving a pending legacy F9 ground/attack order.
// The pending order is preserved; the order component is only disabled for this frame.
[DefaultExecutionOrder(-51000)]
public sealed class PrototypeTacticalMapOrderIsolation09F29T : MonoBehaviour
{
    private const float PanelWidth = 270f;
    private const float PanelHeight = 188f;
    private const float BottomGuard = 104f;
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private FieldInfo mapVisibleField;
    private PrototypeCameraNavigation09F29P mapNavigation;
    private bool restoreTacticalOrders;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (UnityEngine.Object.FindAnyObjectByType<PrototypeTacticalMapOrderIsolation09F29T>() == null)
            new GameObject("PrototypeTacticalMapOrderIsolation_v000009f29t")
                .AddComponent<PrototypeTacticalMapOrderIsolation09F29T>();
    }

    private void Awake()
    {
        mapVisibleField = typeof(PrototypeCameraNavigation09F29P)
            .GetField("mapVisible", PrivateInstance);
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (mapNavigation == null)
            mapNavigation = UnityEngine.Object.FindAnyObjectByType<PrototypeCameraNavigation09F29P>();
        if (mapNavigation == null || !mapNavigation.enabled || !IsMapVisible())
            return;

        Vector2 gui = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        if (!GetMapRect().Contains(gui))
            return;

        PrototypeTacticalOrders09F4 orders = PrototypeTacticalOrders09F4.Instance;
        if (orders != null && orders.enabled)
        {
            orders.enabled = false;
            restoreTacticalOrders = true;
        }
    }

    private void LateUpdate()
    {
        if (!restoreTacticalOrders)
            return;

        PrototypeTacticalOrders09F4 orders = PrototypeTacticalOrders09F4.Instance;
        if (orders != null)
            orders.enabled = true;
        restoreTacticalOrders = false;
    }

    private void OnDisable()
    {
        if (!restoreTacticalOrders)
            return;

        PrototypeTacticalOrders09F4 orders = PrototypeTacticalOrders09F4.Instance;
        if (orders != null)
            orders.enabled = true;
        restoreTacticalOrders = false;
    }

    private bool IsMapVisible()
    {
        if (mapVisibleField == null)
            return true;
        object value = mapVisibleField.GetValue(mapNavigation);
        return !(value is bool) || (bool)value;
    }

    private static Rect GetMapRect()
    {
        float panelX = Mathf.Max(8f, Screen.width - PanelWidth - 10f);
        float panelY = Mathf.Max(66f, Screen.height - BottomGuard - PanelHeight - 8f);
        return new Rect(panelX + 8f, panelY + 25f, PanelWidth - 16f, 112f);
    }
}
