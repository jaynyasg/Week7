namespace CareerQuest
{
    public class ActivityResultEmitter
    {
        private bool _emitted;

        public bool Emitted => _emitted;

        public bool TryRecord(GameSession session, ActivitySessionState state, MiniGameResult result)
        {
            if (_emitted || session == null || state == null || result == null)
            {
                return false;
            }

            if (state.ActivityId != result.ActivityId)
            {
                return false;
            }

            _emitted = true;
            state.MarkResultRecorded();
            return session.RecordResult(result);
        }
    }
}
