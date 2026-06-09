namespace CareerQuest
{
    public readonly struct ShowcaseStep
    {
        public string Id { get; }
        public string Title { get; }
        public float DurationSeconds { get; }

        public ShowcaseStep(string id, string title, float durationSeconds)
        {
            Id = id;
            Title = title;
            DurationSeconds = durationSeconds;
        }
    }
}
