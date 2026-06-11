namespace CareerQuest
{
    public enum ActivityPhase
    {
        Intro,
        Explore,
        Interact,
        Review,
        Complete,
        Ceremony,
        ResultRecorded,
        Exit
    }

    public class ActivitySessionState
    {
        public ActivitySessionState(string activityId)
        {
            ActivityId = activityId;
        }

        public string ActivityId { get; }
        public ActivityPhase Phase { get; private set; } = ActivityPhase.Intro;
        public int AcceptedActions { get; private set; }
        public int RejectedActions { get; private set; }
        public string LastFeedback { get; private set; } = "Ready";
        public bool ResultRecorded { get; private set; }

        public void MoveTo(ActivityPhase phase)
        {
            Phase = phase;
        }

        public void ApplyFeedback(ActivityFeedback feedback)
        {
            LastFeedback = feedback.Message;

            if (feedback.Accepted)
            {
                AcceptedActions++;
            }
            else
            {
                RejectedActions++;
            }
        }

        public void MarkResultRecorded()
        {
            ResultRecorded = true;
            Phase = ActivityPhase.ResultRecorded;
        }
    }
}
