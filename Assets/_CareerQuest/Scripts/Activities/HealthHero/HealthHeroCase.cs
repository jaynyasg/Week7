namespace CareerQuest
{
    public readonly struct HealthHeroCase
    {
        public string Symptom { get; }
        public string Tool { get; }
        public string Treatment { get; }

        public HealthHeroCase(string symptom, string tool, string treatment)
        {
            Symptom = symptom;
            Tool = tool;
            Treatment = treatment;
        }
    }
}
