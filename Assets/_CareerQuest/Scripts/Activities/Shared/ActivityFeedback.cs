namespace CareerQuest
{
    public readonly struct ActivityFeedback
    {
        public ActivityFeedback(bool accepted, string message, string cueId = "")
        {
            Accepted = accepted;
            Message = string.IsNullOrWhiteSpace(message) ? "Keep trying." : message;
            CueId = cueId ?? string.Empty;
        }

        public bool Accepted { get; }
        public string Message { get; }
        public string CueId { get; }

        public static ActivityFeedback Accept(string message, string cueId = "accepted")
        {
            return new ActivityFeedback(true, message, cueId);
        }

        public static ActivityFeedback Reject(string message, string cueId = "rejected")
        {
            return new ActivityFeedback(false, message, cueId);
        }
    }
}
