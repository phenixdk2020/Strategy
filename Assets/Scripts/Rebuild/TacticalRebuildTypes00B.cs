using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project1864.Rebuild
{
    public enum RebuildNation
    {
        Denmark,
        Prussia
    }

    public enum RebuildUnitType
    {
        Infantry,
        Cavalry,
        Artillery,
        Headquarters,
        Support
    }

    public enum RebuildEchelon
    {
        Regiment,
        Battalion,
        Company
    }

    public enum RebuildFormation
    {
        Line,
        Column
    }

    [Serializable]
    public sealed class RebuildUnitRecord
    {
        public string UnitId;
        public string ParentUnitId;
        public string OfficialName1864;
        public string TraditionalName;
        public RebuildNation Nation;
        public RebuildUnitType UnitType;
        public RebuildEchelon Echelon;
        public int AuthorizedStrength;
        public int PresentStrength;
        public bool TacticalActive;

        public RebuildUnitRecord(
            string unitId,
            string parentUnitId,
            string officialName1864,
            string traditionalName,
            RebuildNation nation,
            RebuildUnitType unitType,
            RebuildEchelon echelon,
            int authorizedStrength,
            int presentStrength,
            bool tacticalActive)
        {
            UnitId = unitId;
            ParentUnitId = parentUnitId;
            OfficialName1864 = officialName1864;
            TraditionalName = traditionalName;
            Nation = nation;
            UnitType = unitType;
            Echelon = echelon;
            AuthorizedStrength = authorizedStrength;
            PresentStrength = presentStrength;
            TacticalActive = tacticalActive;
        }
    }

    public sealed class RebuildOOBRegistry00B : MonoBehaviour
    {
        public static RebuildOOBRegistry00B Instance { get; private set; }

        private readonly List<RebuildUnitRecord> records = new List<RebuildUnitRecord>();
        private readonly Dictionary<string, RebuildUnitRecord> byId = new Dictionary<string, RebuildUnitRecord>();

        public IReadOnlyList<RebuildUnitRecord> Records => records;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                enabled = false;
                return;
            }

            Instance = this;
            BuildReferenceOOB();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public RebuildUnitRecord Get(string unitId)
        {
            if (string.IsNullOrEmpty(unitId))
                return null;

            byId.TryGetValue(unitId, out RebuildUnitRecord record);
            return record;
        }

        public List<RebuildUnitRecord> GetChildren(string parentUnitId, bool tacticalOnly)
        {
            List<RebuildUnitRecord> result = new List<RebuildUnitRecord>();
            for (int i = 0; i < records.Count; i++)
            {
                RebuildUnitRecord record = records[i];
                if (record.ParentUnitId != parentUnitId)
                    continue;
                if (tacticalOnly && !record.TacticalActive)
                    continue;
                result.Add(record);
            }
            return result;
        }

        public List<RebuildUnitRecord> GetTacticalCompanies()
        {
            List<RebuildUnitRecord> result = new List<RebuildUnitRecord>();
            for (int i = 0; i < records.Count; i++)
            {
                RebuildUnitRecord record = records[i];
                if (record.Echelon == RebuildEchelon.Company && record.TacticalActive)
                    result.Add(record);
            }
            return result;
        }

        private void Add(RebuildUnitRecord record)
        {
            if (record == null || string.IsNullOrEmpty(record.UnitId))
                throw new InvalidOperationException("OOB record requires stable UnitID.");
            if (byId.ContainsKey(record.UnitId))
                throw new InvalidOperationException("Duplicate UnitID: " + record.UnitId);

            records.Add(record);
            byId.Add(record.UnitId, record);
        }

        private void BuildReferenceOOB()
        {
            records.Clear();
            byId.Clear();

            // Full Danish regiment data retained, but only I Battalion is tactical-active in 00.01.00b.
            Add(new RebuildUnitRecord(
                "DK-INF-001", "", "1. Infanteri-Regiment", "Danske Livregiment",
                RebuildNation.Denmark, RebuildUnitType.Infantry, RebuildEchelon.Regiment,
                1540, 1540, false));

            Add(new RebuildUnitRecord(
                "DK-INF-001-B01", "DK-INF-001", "I. Bataljon", "",
                RebuildNation.Denmark, RebuildUnitType.Infantry, RebuildEchelon.Battalion,
                759, 759, true));
            Add(new RebuildUnitRecord(
                "DK-INF-001-B02", "DK-INF-001", "II. Bataljon", "",
                RebuildNation.Denmark, RebuildUnitType.Infantry, RebuildEchelon.Battalion,
                756, 756, false));

            int[] dkStrength = { 190, 190, 190, 189, 189, 189, 189, 189 };
            for (int i = 0; i < 8; i++)
            {
                bool firstBattalion = i < 4;
                string battalionId = firstBattalion ? "DK-INF-001-B01" : "DK-INF-001-B02";
                string companyId = "DK-INF-001-C" + (i + 1).ToString("00");
                Add(new RebuildUnitRecord(
                    companyId,
                    battalionId,
                    (i + 1) + ". Kompagni",
                    "",
                    RebuildNation.Denmark,
                    RebuildUnitType.Infantry,
                    RebuildEchelon.Company,
                    dkStrength[i],
                    dkStrength[i],
                    firstBattalion));
            }

            // Full Prussian regiment data retained, but only I Battalion is tactical-active in 00.01.00b.
            Add(new RebuildUnitRecord(
                "PR-INF-008", "", "8th Regiment (Prussia QA)", "",
                RebuildNation.Prussia, RebuildUnitType.Infantry, RebuildEchelon.Regiment,
                2460, 2460, false));

            Add(new RebuildUnitRecord(
                "PR-INF-008-B01", "PR-INF-008", "I. Musketier-Bataillon", "",
                RebuildNation.Prussia, RebuildUnitType.Infantry, RebuildEchelon.Battalion,
                812, 812, true));
            Add(new RebuildUnitRecord(
                "PR-INF-008-B02", "PR-INF-008", "II. Musketier-Bataillon", "",
                RebuildNation.Prussia, RebuildUnitType.Infantry, RebuildEchelon.Battalion,
                812, 812, false));
            Add(new RebuildUnitRecord(
                "PR-INF-008-B03", "PR-INF-008", "Füsilier-Bataillon", "",
                RebuildNation.Prussia, RebuildUnitType.Infantry, RebuildEchelon.Battalion,
                811, 811, false));

            for (int i = 0; i < 12; i++)
            {
                int strength = i < 11 ? 203 : 202;
                int battalionIndex = i / 4;
                string battalionId = "PR-INF-008-B0" + (battalionIndex + 1);
                bool firstBattalion = battalionIndex == 0;
                string companyId = "PR-INF-008-C" + (i + 1).ToString("00");
                Add(new RebuildUnitRecord(
                    companyId,
                    battalionId,
                    (i + 1) + ". Kompanie",
                    "",
                    RebuildNation.Prussia,
                    RebuildUnitType.Infantry,
                    RebuildEchelon.Company,
                    strength,
                    strength,
                    firstBattalion));
            }

            Debug.Log(
                "REBUILD-OOB-00B|Installed=True|StableUnitID=True|" +
                "FullRegiments=2|TacticalBattalions=2|TacticalCompanies=8|" +
                "DKActive=759|PRActive=812|ActiveManpower=1571");
        }
    }
}
