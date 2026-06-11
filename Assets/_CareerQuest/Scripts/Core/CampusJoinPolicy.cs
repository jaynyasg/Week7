namespace CareerQuest
{
    public static class CampusJoinPolicy
    {
        public const int MaxPlayers = 2;

        public static bool CanJoin(SessionPhase hostPhase, int connectedClientCount)
        {
            if (connectedClientCount >= MaxPlayers)
            {
                return false;
            }

            return hostPhase == SessionPhase.Hub || hostPhase == SessionPhase.Gallery;
        }

        public static string GetRejectionMessage(SessionPhase hostPhase, int connectedClientCount)
        {
            if (connectedClientCount >= MaxPlayers)
            {
                return "This game already has two players. Try hosting your own game instead.";
            }

            if (hostPhase == SessionPhase.InRoom)
            {
                return "The host is inside an activity room. Wait until they return to campus, then try again.";
            }

            if (hostPhase == SessionPhase.Ceremony)
            {
                return "The host is finishing an activity. Try joining again in a moment.";
            }

            return string.Empty;
        }
    }
}
