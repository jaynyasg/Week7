using UnityEngine;

namespace CareerQuest
{
    public abstract class ActivityRoomController : MonoBehaviour
    {
        protected ActivityLifecycle Lifecycle { get; private set; }

        protected void BeginRoom(string activityId)
        {
            Lifecycle = new ActivityLifecycle(activityId);
            Lifecycle.BeginExplore();
            Lifecycle.BeginInteract();
        }

        protected bool CanCompleteRoom()
        {
            return Lifecycle != null && ActivityPhaseRules.CanCompleteFrom(Lifecycle.State.Phase);
        }

        protected bool TryCompleteRoom(GameSession session, CareerQuestApp app, MiniGameResult result)
        {
            if (!CanCompleteRoom() || session == null || app == null || result == null)
            {
                return false;
            }

            if (!ActivityPhaseRules.CanTransition(Lifecycle.State.Phase, ActivityPhase.Review))
            {
                return false;
            }

            Lifecycle.BeginReview();
            Lifecycle.MarkComplete();
            app.CompleteActivity(result);
            return true;
        }

        protected bool ExitToCampus(CareerQuestApp app)
        {
            if (Lifecycle == null || app == null || !ActivityPhaseRules.CanExitFrom(Lifecycle.State.Phase))
            {
                return false;
            }

            Lifecycle.Exit();
            app.ShowCampus();
            return true;
        }

        protected bool TryRecordResult(GameSession session, MiniGameResult result)
        {
            if (Lifecycle == null || session == null || result == null)
            {
                return false;
            }

            var emitter = new ActivityResultEmitter();
            return emitter.TryRecord(session, Lifecycle.State, result);
        }
    }
}
