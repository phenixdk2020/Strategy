using UnityEngine;

// Keeps the Major bottom panel from leaking clicks into PlayerCommander/box selection.
[DefaultExecutionOrder(-9600)]
public sealed class PrototypeMajorPointerIsolation09F18 : MonoBehaviour
{
    private bool commanderSuppressed;
    private bool boxSuppressed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Object.FindAnyObjectByType<PrototypeMajorPointerIsolation09F18>() != null)
            return;

        GameObject root = new GameObject("PrototypeMajorPointerIsolation_v000009f18");
        root.AddComponent<PrototypeMajorPointerIsolation09F18>();
    }

    private void Update()
    {
        PrototypeMajorUi09F18 ui = PrototypeMajorUi09F18.Instance;
        bool over = ui != null && ui.IsPointerOverControls(Input.mousePosition);

        if (over)
        {
            PlayerCommander commander = PlayerCommander.Instance;
            if (commander != null && commander.enabled)
            {
                commander.enabled = false;
                commanderSuppressed = true;
            }

            PrototypeBoxSelection09H box = Object.FindAnyObjectByType<PrototypeBoxSelection09H>();
            if (box != null && box.enabled)
            {
                box.enabled = false;
                boxSuppressed = true;
            }
        }
        else
        {
            Restore();
        }
    }

    private void OnDisable()
    {
        Restore();
    }

    private void Restore()
    {
        if (commanderSuppressed)
        {
            PlayerCommander commander = PlayerCommander.Instance;
            if (commander != null)
                commander.enabled = true;
            commanderSuppressed = false;
        }

        if (boxSuppressed)
        {
            PrototypeBoxSelection09H box = Object.FindAnyObjectByType<PrototypeBoxSelection09H>();
            if (box != null)
                box.enabled = true;
            boxSuppressed = false;
        }
    }
}
