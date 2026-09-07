using UnityEngine;

public sealed class OfficerProfile
{
    public string OfficerName { get; private set; }
    public float Leadership { get; private set; }
    public float Inspiration { get; private set; }
    public float TacticalSkill { get; private set; }
    public float Initiative { get; private set; }
    public float StaffCommandSkill { get; private set; }
    public float Discipline { get; private set; }
    public float Aggressiveness { get; private set; }
    public float Composure { get; private set; }
    public float Experience { get; private set; }

    public OfficerProfile(
        string officerName,
        float leadership,
        float inspiration,
        float tacticalSkill,
        float initiative,
        float staffCommandSkill,
        float discipline,
        float aggressiveness,
        float composure,
        float experience)
    {
        OfficerName = officerName;
        Leadership = Mathf.Clamp(leadership, 0f, 100f);
        Inspiration = Mathf.Clamp(inspiration, 0f, 100f);
        TacticalSkill = Mathf.Clamp(tacticalSkill, 0f, 100f);
        Initiative = Mathf.Clamp(initiative, 0f, 100f);
        StaffCommandSkill = Mathf.Clamp(staffCommandSkill, 0f, 100f);
        Discipline = Mathf.Clamp(discipline, 0f, 100f);
        Aggressiveness = Mathf.Clamp(aggressiveness, 0f, 100f);
        Composure = Mathf.Clamp(composure, 0f, 100f);
        Experience = Mathf.Clamp(experience, 0f, 100f);
    }

    public string CompactSummary
    {
        get
        {
            return string.Format(
                "L{0:0} I{1:0} T{2:0} Init{3:0} S{4:0} D{5:0} A{6:0} C{7:0}",
                Leadership,
                Inspiration,
                TacticalSkill,
                Initiative,
                StaffCommandSkill,
                Discipline,
                Aggressiveness,
                Composure);
        }
    }

    public static OfficerProfile CreatePrototype(string regimentName)
    {
        // QA-only profiles. They are deliberately different so behaviour can be
        // compared in v00.00.09. They are NOT historical ratings.
        switch (regimentName)
        {
            case "1. Regiment":
                return new OfficerProfile(
                    "QA Officer A", 74f, 80f, 63f, 57f, 66f, 78f, 42f, 82f, 62f);

            case "5. Regiment":
                return new OfficerProfile(
                    "QA Officer B", 59f, 54f, 71f, 77f, 57f, 49f, 76f, 48f, 46f);

            case "8th Regiment":
                return new OfficerProfile(
                    "QA Officer C", 72f, 65f, 82f, 74f, 76f, 69f, 64f, 84f, 73f);

            case "18th Regiment":
                return new OfficerProfile(
                    "QA Officer D", 61f, 68f, 55f, 43f, 60f, 84f, 32f, 63f, 52f);

            case "2. Regiment":
                return new OfficerProfile(
                    "QA Officer E", 68f, 61f, 76f, 66f, 70f, 72f, 48f, 75f, 64f);

            case "6. Regiment":
                return new OfficerProfile(
                    "QA Officer F", 56f, 72f, 58f, 52f, 55f, 67f, 38f, 58f, 49f);

            case "12th Regiment":
                return new OfficerProfile(
                    "QA Officer G", 66f, 58f, 73f, 69f, 64f, 61f, 70f, 71f, 60f);

            case "24th Regiment":
                return new OfficerProfile(
                    "QA Officer H", 63f, 62f, 67f, 81f, 59f, 54f, 82f, 56f, 57f);

            default:
                return new OfficerProfile(
                    "QA Officer", 60f, 60f, 60f, 60f, 60f, 60f, 50f, 60f, 50f);
        }
    }
}
