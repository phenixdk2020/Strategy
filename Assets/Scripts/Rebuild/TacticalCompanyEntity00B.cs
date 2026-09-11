using UnityEngine;

namespace Project1864.Rebuild
{
    // Clean Company tactical state/selection anchor.
    // v00.01.00c4 adds drill-only animated formation/facing transitions.
    // Translation, navigation, combat and AI remain intentionally absent.
    public sealed class TacticalCompanyEntity00B : MonoBehaviour
    {
        public string UnitId { get; private set; }
        public string ParentUnitId { get; private set; }
        public string DisplayName { get; private set; }
        public RebuildNation Nation { get; private set; }
        public int AuthorizedStrength { get; private set; }
        public int PresentStrength { get; private set; }
        public RebuildFormation Formation { get; private set; }
        public bool IsSelected { get; private set; }

        public float FootprintWidth { get; private set; }
        public float FootprintDepth { get; private set; }

        public bool IsReforming { get; private set; }
        public RebuildFormation ReformFromFormation { get; private set; }
        public RebuildFormation ReformTargetFormation { get; private set; }
        public float ReformProgress { get; private set; } = 1f;
        public bool IsTurning { get; private set; }

        private Renderer footprintRenderer;
        private LineRenderer outline;
        private BoxCollider hitBox;
        private Transform footprintTransform;
        private Material normalMaterial;
        private Material selectedMaterial;

        private float reformDuration;
        private float reformElapsed;
        private float reformStartWidth;
        private float reformStartDepth;
        private float reformTargetWidth;
        private float reformTargetDepth;

        private Quaternion turnStartRotation;
        private Quaternion turnTargetRotation;
        private float turnDuration;
        private float turnElapsed;

        public void Initialize(RebuildUnitRecord record, Vector3 worldPosition, Quaternion worldRotation)
        {
            if (record == null)
                throw new System.ArgumentNullException(nameof(record));
            if (record.Echelon != RebuildEchelon.Company)
                throw new System.InvalidOperationException("TacticalCompanyEntity00B requires a Company OOB record.");

            UnitId = record.UnitId;
            ParentUnitId = record.ParentUnitId;
            DisplayName = record.OfficialName1864;
            Nation = record.Nation;
            AuthorizedStrength = record.AuthorizedStrength;
            PresentStrength = record.PresentStrength;
            Formation = RebuildFormation.Line;
            ReformFromFormation = Formation;
            ReformTargetFormation = Formation;

            transform.position = worldPosition;
            transform.rotation = worldRotation;

            BuildFootprint();
            SetSelected(false);

            Debug.Log(
                "REBUILD-COMPANY-00C4|Created=True|UnitID=" + UnitId +
                "|Parent=" + ParentUnitId +
                "|Nation=" + Nation +
                "|Strength=" + PresentStrength +
                "|Formation=" + Formation +
                "|AnimatedDrill=True|TransformParentIsOOBParent=False|WorldLabel=False|Flags=False");
        }

        private void Update()
        {
            float dt = Mathf.Max(0f, Time.unscaledDeltaTime);

            if (IsReforming)
                UpdateReform(dt);

            if (IsTurning)
                UpdateTurn(dt);
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;

            if (footprintRenderer != null)
                footprintRenderer.sharedMaterial = selected ? selectedMaterial : normalMaterial;

            if (outline != null)
            {
                outline.enabled = selected;
                outline.startWidth = selected ? 0.22f : 0.10f;
                outline.endWidth = outline.startWidth;
            }
        }

        public bool BeginFormationDrill(RebuildFormation targetFormation, float durationSeconds)
        {
            RebuildFormation effectiveCurrent = IsReforming ? ReformTargetFormation : Formation;
            if (effectiveCurrent == targetFormation)
                return false;

            ReformFromFormation = Formation;
            ReformTargetFormation = targetFormation;
            ReformProgress = 0f;
            reformDuration = Mathf.Max(0.15f, durationSeconds);
            reformElapsed = 0f;
            IsReforming = true;

            reformStartWidth = FootprintWidth;
            reformStartDepth = FootprintDepth;
            GetFootprintDimensions(targetFormation, out reformTargetWidth, out reformTargetDepth);

            Debug.Log(
                "REBUILD-COMPANY-00C4|Action=FormationTransitionStart|UnitID=" + UnitId +
                "|From=" + ReformFromFormation +
                "|To=" + ReformTargetFormation +
                "|Duration=" + reformDuration.ToString("0.00"));
            return true;
        }

        // Compatibility helper retained for earlier drill code. c4 uses BeginFormationDrill.
        public void SetFormationForDrill(RebuildFormation formation)
        {
            if (Formation == formation && !IsReforming)
                return;

            IsReforming = false;
            ReformProgress = 1f;
            Formation = formation;
            ReformFromFormation = formation;
            ReformTargetFormation = formation;
            RecalculateFootprintGeometry();
        }

        public bool BeginTurnDrill(float degrees, float durationSeconds)
        {
            if (Mathf.Abs(degrees) < 0.01f)
                return false;

            turnStartRotation = transform.rotation;
            turnTargetRotation = Quaternion.AngleAxis(degrees, Vector3.up) * transform.rotation;
            turnDuration = Mathf.Max(0.10f, durationSeconds);
            turnElapsed = 0f;
            IsTurning = true;

            Debug.Log(
                "REBUILD-COMPANY-00C4|Action=TurnStart|UnitID=" + UnitId +
                "|Degrees=" + degrees.ToString("0") +
                "|Duration=" + turnDuration.ToString("0.00"));
            return true;
        }

        private void UpdateReform(float dt)
        {
            reformElapsed += dt;
            float raw = Mathf.Clamp01(reformElapsed / reformDuration);
            ReformProgress = Smooth01(raw);

            float width = Mathf.Lerp(reformStartWidth, reformTargetWidth, ReformProgress);
            float depth = Mathf.Lerp(reformStartDepth, reformTargetDepth, ReformProgress);
            ApplyFootprintGeometry(width, depth);

            if (raw < 1f)
                return;

            Formation = ReformTargetFormation;
            ReformFromFormation = Formation;
            ReformTargetFormation = Formation;
            ReformProgress = 1f;
            IsReforming = false;
            RecalculateFootprintGeometry();

            Debug.Log(
                "REBUILD-COMPANY-00C4|Action=FormationTransitionComplete|UnitID=" + UnitId +
                "|Formation=" + Formation +
                "|Width=" + FootprintWidth.ToString("0.00") +
                "|Depth=" + FootprintDepth.ToString("0.00"));
        }

        private void UpdateTurn(float dt)
        {
            turnElapsed += dt;
            float raw = Mathf.Clamp01(turnElapsed / turnDuration);
            float t = Smooth01(raw);

            Vector3 positionBefore = transform.position;
            transform.rotation = Quaternion.Slerp(turnStartRotation, turnTargetRotation, t);
            transform.position = positionBefore;

            if (raw < 1f)
                return;

            transform.rotation = turnTargetRotation;
            transform.position = positionBefore;
            IsTurning = false;

            Debug.Log(
                "REBUILD-COMPANY-00C4|Action=TurnComplete|UnitID=" + UnitId +
                "|Y=" + transform.eulerAngles.y.ToString("0.0") +
                "|Translation=False");
        }

        private void BuildFootprint()
        {
            GameObject footprint = GameObject.CreatePrimitive(PrimitiveType.Cube);
            footprint.name = "QA_Footprint_" + UnitId;
            footprint.transform.SetParent(transform, false);
            footprint.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            footprintTransform = footprint.transform;

            Collider primitiveCollider = footprint.GetComponent<Collider>();
            if (primitiveCollider != null)
                Destroy(primitiveCollider);

            footprintRenderer = footprint.GetComponent<Renderer>();
            normalMaterial = CreateMaterial(
                Nation == RebuildNation.Denmark
                    ? new Color(0.56f, 0.16f, 0.16f)
                    : new Color(0.19f, 0.24f, 0.31f),
                "00C4_Normal_" + UnitId);
            selectedMaterial = CreateMaterial(new Color(0.86f, 0.72f, 0.16f), "00C4_Selected_" + UnitId);
            footprintRenderer.sharedMaterial = normalMaterial;

            hitBox = gameObject.AddComponent<BoxCollider>();

            outline = gameObject.AddComponent<LineRenderer>();
            outline.useWorldSpace = false;
            outline.loop = true;
            outline.positionCount = 4;
            outline.material = CreateMaterial(new Color(1f, 0.82f, 0.18f), "00C4_SelectionOutline_" + UnitId);
            outline.startWidth = 0.22f;
            outline.endWidth = 0.22f;

            RecalculateFootprintGeometry();
        }

        private void RecalculateFootprintGeometry()
        {
            GetFootprintDimensions(Formation, out float width, out float depth);
            ApplyFootprintGeometry(width, depth);
        }

        private void GetFootprintDimensions(RebuildFormation formation, out float width, out float depth)
        {
            if (formation == RebuildFormation.Column)
            {
                const int filesAcross = 8;
                const float fileSpacing = 0.60f;
                const float rowSpacing = 0.72f;

                int rows = Mathf.Max(1, Mathf.CeilToInt(PresentStrength / (float)filesAcross));
                width = Mathf.Max(5.2f, (filesAcross - 1) * fileSpacing + 1.0f);
                depth = Mathf.Max(4.0f, (rows - 1) * rowSpacing + 1.0f);
            }
            else
            {
                const int ranks = 3;
                const float fileSpacing = 0.52f;
                int files = Mathf.Max(1, Mathf.CeilToInt(PresentStrength / (float)ranks));
                width = Mathf.Max(8f, (files - 1) * fileSpacing + 1.1f);
                depth = 4.2f;
            }
        }

        private void ApplyFootprintGeometry(float width, float depth)
        {
            FootprintWidth = width;
            FootprintDepth = depth;

            if (footprintTransform != null)
                footprintTransform.localScale = new Vector3(FootprintWidth, 0.12f, FootprintDepth);

            if (hitBox != null)
            {
                hitBox.center = new Vector3(0f, 0.80f, 0f);
                hitBox.size = new Vector3(FootprintWidth, 1.60f, FootprintDepth + 1.5f);
            }

            if (outline != null)
            {
                float halfW = FootprintWidth * 0.5f + 0.7f;
                float halfD = FootprintDepth * 0.5f + 0.7f;
                outline.SetPosition(0, new Vector3(-halfW, 0.18f, -halfD));
                outline.SetPosition(1, new Vector3(-halfW, 0.18f, halfD));
                outline.SetPosition(2, new Vector3(halfW, 0.18f, halfD));
                outline.SetPosition(3, new Vector3(halfW, 0.18f, -halfD));
            }
        }

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static Material CreateMaterial(Color color, string materialName)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");

            Material material = new Material(shader);
            material.name = materialName;
            material.color = color;
            return material;
        }
    }
}
