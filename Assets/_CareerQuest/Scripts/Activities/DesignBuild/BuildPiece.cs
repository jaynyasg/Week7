namespace CareerQuest
{
    public readonly struct BuildPiece
    {
        public string Id { get; }
        public string DisplayName { get; }

        public BuildPiece(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
        }
    }
}
