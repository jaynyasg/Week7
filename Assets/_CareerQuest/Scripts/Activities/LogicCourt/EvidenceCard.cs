namespace CareerQuest
{
    public readonly struct EvidenceCard
    {
        public string Text { get; }
        public bool Helpful { get; }

        public EvidenceCard(string text, bool helpful)
        {
            Text = text;
            Helpful = helpful;
        }
    }
}
