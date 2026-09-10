using System;
using UnityEngine;

namespace Project1864.Rebuild
{
    // Clean tactical rebuild bootstrap.
    // A BattleManager blocker is created before scene load only to prevent the legacy PrototypeBootstrap
    // from constructing the old Regiment runtime. The blocker is disabled before the first Update.
    public sealed class TacticalRebuildBootstrap00B : MonoBehaviour
    {
        private static GameObject legacyBootstrapBlocker;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeSceneLoad()
        {
            if (UnityEngine.Object.FindAnyObjectByType<BattleManager>() != null)
                return;

            legacyBootstrapBlocker = new GameObject("TACTICAL_REBUILD_LEGACY_BOOTSTRAP_BLOCKER_00B");
            legacyBootstrapBlocker.AddComponent<BattleManager>();
            UnityEngine.Object.DontDestroyOnLoad(legacyBootstrapBlocker);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AfterSceneLoad()
        {
            if (UnityEngine.Object.FindAnyObjectByType<TacticalRebuildBootstrap00B>() != null)
                return;

            GameObject root = new GameObject("PROJECT1864_TacticalRebuild_v000100b");
            root.AddComponent<TacticalRebuildBootstrap00B>();
        }

        private void Awake()
        {
            Application.targetFrameRate = 120;
            BuildCleanWorld();
            BuildOOBAndCompanies();
        }

        private void Start()
        {
            DisableLegacyRuntimeLayers();

            TacticalCompanySelection00B selection = UnityEngine.Object.FindAnyObjectByType<TacticalCompanySelection00B>();
            if (selection != null)
                selection.RefreshCompanies();

            Debug.Log(
                "REBUILD-00B|Installed=True|GateA=True|GateB=True|" +
                "LegacyRegiments=0|Companies=8|MovementOwner=None|CombatOwner=None|" +
                "TransformParenting=DataOnly|ReadyForSelectionQA=True");
        }

        private static void BuildCleanWorld()
        {
            RenderSettings.ambientLight = new Color(0.56f, 0.58f, 0.54f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.68f, 0.71f, 0.73f);
            RenderSettings.fogDensity = 0.0018f;

            if (GameObject.Find("REBUILD_00B_Ground") == null)
            {
                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "REBUILD_00B_Ground";
                ground.transform.position = new Vector3(0f, -0.15f, 0f);
                ground.transform.localScale = new Vector3(360f, 0.30f, 240f);
                ground.GetComponent<Renderer>().sharedMaterial = CreateMaterial(
                    new Color(0.30f, 0.40f, 0.20f), "REBUILD_00B_Grass");
            }

            if (GameObject.Find("REBUILD_00B_CentreLine") == null)
            {
                GameObject centreLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                centreLine.name = "REBUILD_00B_CentreLine";
                centreLine.transform.position = new Vector3(0f, 0.02f, 0f);
                centreLine.transform.localScale = new Vector3(2.2f, 0.04f, 220f);
                centreLine.GetComponent<Renderer>().sharedMaterial = CreateMaterial(
                    new Color(0.44f, 0.34f, 0.21f), "REBUILD_00B_CentreRoad");
                Collider c = centreLine.GetComponent<Collider>();
                if (c != null)
                    UnityEngine.Object.Destroy(c);
            }

            if (GameObject.Find("REBUILD_00B_Sun") == null)
            {
                GameObject lightObject = new GameObject("REBUILD_00B_Sun");
                Light sun = lightObject.AddComponent<Light>();
                sun.type = LightType.Directional;
                sun.intensity = 1.10f;
                sun.color = new Color(1f, 0.95f, 0.84f);
                lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            }

            if (Camera.main == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.fieldOfView = 48f;
                camera.nearClipPlane = 0.3f;
                camera.farClipPlane = 1200f;
                cameraObject.transform.position = new Vector3(0f, 130f, -165f);
                cameraObject.transform.rotation = Quaternion.Euler(42f, 0f, 0f);
                cameraObject.AddComponent<AudioListener>();
                cameraObject.AddComponent<RTSCameraController>();
            }
        }

        private static void BuildOOBAndCompanies()
        {
            RebuildOOBRegistry00B registry = UnityEngine.Object.FindAnyObjectByType<RebuildOOBRegistry00B>();
            if (registry == null)
            {
                GameObject dataRoot = new GameObject("REBUILD_00B_OOB_DATA");
                registry = dataRoot.AddComponent<RebuildOOBRegistry00B>();
            }

            GameObject companyRoot = GameObject.Find("REBUILD_00B_COMPANY_ENTITIES");
            if (companyRoot == null)
                companyRoot = new GameObject("REBUILD_00B_COMPANY_ENTITIES");

            if (companyRoot.transform.childCount == 0)
            {
                int dkIndex = 0;
                int prIndex = 0;
                System.Collections.Generic.List<RebuildUnitRecord> active = registry.GetTacticalCompanies();

                for (int i = 0; i < active.Count; i++)
                {
                    RebuildUnitRecord record = active[i];
                    bool danish = record.Nation == RebuildNation.Denmark;
                    int lane = danish ? dkIndex++ : prIndex++;

                    float z = -60f + lane * 40f;
                    Vector3 position = danish
                        ? new Vector3(-90f, 0.15f, z)
                        : new Vector3(90f, 0.15f, z);
                    Quaternion rotation = Quaternion.Euler(0f, danish ? 90f : -90f, 0f);

                    GameObject entityObject = new GameObject(record.UnitId);
                    // This generic root is only a scene container. It is NOT the OOB parent.
                    entityObject.transform.SetParent(companyRoot.transform, false);
                    TacticalCompanyEntity00B entity = entityObject.AddComponent<TacticalCompanyEntity00B>();
                    entity.Initialize(record, position, rotation);
                }
            }

            if (UnityEngine.Object.FindAnyObjectByType<TacticalCompanySelection00B>() == null)
            {
                GameObject selectionRoot = new GameObject("REBUILD_00B_SELECTION_OWNER");
                selectionRoot.AddComponent<TacticalCompanySelection00B>();
            }
        }

        private static void DisableLegacyRuntimeLayers()
        {
            MonoBehaviour[] behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            int disabled = 0;

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null)
                    continue;

                Type type = behaviour.GetType();
                string typeNamespace = type.Namespace ?? string.Empty;
                string typeName = type.Name;

                if (typeNamespace.StartsWith("Project1864.Rebuild", StringComparison.Ordinal))
                    continue;

                // Retained platform/presentation components only.
                if (typeName == "RTSCameraController" || typeName == "PrototypeBuildVersionOverlay")
                    continue;

                if (!behaviour.enabled)
                    continue;

                behaviour.enabled = false;
                disabled++;
            }

            Debug.Log(
                "REBUILD-LEGACY-GATE-00B|LegacyMonoBehavioursDisabled=" + disabled +
                "|Retained=RTSCameraController+PrototypeBuildVersionOverlay|" +
                "NoLegacyPlayerCommander=True|NoLegacyRegimentRuntime=True");
        }

        private static Material CreateMaterial(Color color, string materialName)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            Material material = new Material(shader);
            material.name = materialName;
            material.color = color;
            return material;
        }
    }
}
