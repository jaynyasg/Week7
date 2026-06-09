namespace CareerQuest
{
    public readonly struct ActivityAction
    {
        public ActivityAction(string id, string actorId = "local", string payload = "")
        {
            Id = string.IsNullOrWhiteSpace(id) ? "unknown" : id;
            ActorId = string.IsNullOrWhiteSpace(actorId) ? "local" : actorId;
            Payload = payload ?? string.Empty;
        }

        public string Id { get; }
        public string ActorId { get; }
        public string Payload { get; }
    }
}
