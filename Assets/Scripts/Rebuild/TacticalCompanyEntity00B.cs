using UnityEngine;

namespace Project1864.Rebuild
{
    // Gate B entity retained as the clean tactical Company state/selection anchor.
    // v00.01.00c2 removes permanent world labels; hover UI now owns readable identity presentation.
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

        private Renderer footprintRenderer;
        private LineRenderer outline;
        private Material normalMaterial;
        private Material selectedMaterial;

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

            transform.position = worldPosition;
            transform.rotation = worldRotation;

            BuildFootprint();
            SetSelected(false);

            Debug.Log(
                "REBUILD-COMPANY-00B|Created=True|UnitID=" + UnitId +
                "|Parent=" + ParentUnitId +
                "|Nation=" + Nation +
                "|Strength=" + PresentStrength +
                "|TransformParentIsOOBParent=False|WorldLabel=False");
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

        private void BuildFootprint()
        {
            // The hidden QA footprint remains the single Company hit/selection footprint.
            // Rendering is handled separately by TacticalCompanyRenderer00C.
            const int ranks = 3;
            const float fileSpacing = 0.52f;
            int files = Mathf.Max(1, Mathf.CeilToInt(PresentStrength / (float)ranks));
            FootprintWidth = Mathf.Max(8f, (files - 1) * fileSpacing + 1.1f);
            FootprintDepth = 4.2f;

            GameObject footprint = GameObject.CreatePrimitive(PrimitiveType.Cube);
            footprint.name = "QA_Footprint_" + UnitId;
            footprint.transform.SetParent(transform, false);
            footprint.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            footprint.transform.localScale = new Vector3(FootprintWidth, 0.12f, FootprintDepth);

            Collider primitiveCollider = footprint.GetComponent<Collider>();
            if (primitiveCollider != null)
                Destroy(primitiveCollider);

            footprintRenderer = footprint.GetComponent<Renderer>();
            normalMaterial = CreateMaterial(
                Nation == RebuildNation.Denmark
                    ? new Color(0.56f, 0.16f, 0.16f)
                    : new Color(0.19f, 0.24f, 0.31f),
                "00B_Normal_" + UnitId);
            selectedMaterial = CreateMaterial(new Color(0.86f, 0.72f, 0.16f), "00B_Selected_" + UnitId);
            footprintRenderer.sharedMaterial = normalMaterial;

            BoxCollider hitBox = gameObject.AddComponent<BoxCollider>();
            hitBox.center = new Vector3(0f, 0.75f, 0f);
            hitBox.size = new Vector3(FootprintWidth, 1.5f, FootprintDepth + 1.5f);

            outline = gameObject.AddComponent<LineRenderer>();
            outline.useWorldSpace = false;
            outline.loop = true;
            outline.positionCount = 4;
            outline.material = CreateMaterial(new Color(1f, 0.82f, 0.18f), "00B_SelectionOutline_" + UnitId);
            outline.startWidth = 0.22f;
            outline.endWidth = 0.22f;

            float halfW = FootprintWidth * 0.5f + 0.7f;
            float halfD = FootprintDepth * 0.5f + 0.7f;
            outline.SetPosition(0, new Vector3(-halfW, 0.18f, -halfD));
            outline.SetPosition(1, new Vector3(-halfW, 0.18f, halfD));
            outline.SetPosition(2, new Vector3(halfW, 0.18f, halfD));
            outline.SetPosition(3, new Vector3(halfW, 0.18f, -halfD));
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
