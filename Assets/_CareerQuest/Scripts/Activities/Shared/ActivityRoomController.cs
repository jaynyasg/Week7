using UnityEngine;

namespace CareerQuest
{
    public abstract class ActivityRoomController : MonoBehaviour
    {
        protected ActivityLifecycle Lifecycle { get; private set; }
        protected ActivityResultEmitter ResultEmitter { get; } = new();

        protected void BeginActivity(string activityId)
        {
            Lifecycle = new ActivityLifecycle(activityId);
            Lifecycle.BeginExplore();
        }

        protected bool TryRecordResult(GameSession session, MiniGameResult result)
        {
            return Lifecycle != null && ResultEmitter.TryRecord(session, Lifecycle.State, result);
        }
    }
}
