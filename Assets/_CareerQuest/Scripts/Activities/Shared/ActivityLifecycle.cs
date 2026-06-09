using System;

namespace CareerQuest
{
    public class ActivityLifecycle
    {
        public ActivityLifecycle(string activityId)
        {
            State = new ActivitySessionState(activityId);
        }

        public ActivitySessionState State { get; }

        public event Action<ActivitySessionState> Changed;

        public void BeginExplore()
        {
            MoveTo(ActivityPhase.Explore);
        }

        public void BeginInteract()
        {
            MoveTo(ActivityPhase.Interact);
        }

        public void BeginReview()
        {
            MoveTo(ActivityPhase.Review);
        }

        public void MarkComplete()
        {
            MoveTo(ActivityPhase.Complete);
        }

        public void Exit()
        {
            MoveTo(ActivityPhase.Exit);
        }

        public ActivityFeedback ApplyAction(ActivityAction action, Func<ActivityAction, ActivityFeedback> reducer)
        {
            if (State.Phase == ActivityPhase.Intro)
            {
                BeginInteract();
            }

            var feedback = reducer != null
                ? reducer(action)
                : ActivityFeedback.Reject("That action is not available yet.");
            State.ApplyFeedback(feedback);
            Changed?.Invoke(State);
            return feedback;
        }

        private void MoveTo(ActivityPhase phase)
        {
            State.MoveTo(phase);
            Changed?.Invoke(State);
        }
    }
}
