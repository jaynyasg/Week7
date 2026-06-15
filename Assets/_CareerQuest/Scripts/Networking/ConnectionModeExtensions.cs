namespace CareerQuest
{
    public static class ConnectionModeExtensions
    {
        public static bool IsNetworked(this ConnectionMode mode)
        {
            return mode == ConnectionMode.HostP1
                || mode == ConnectionMode.JoinLocalhostP2
                || mode == ConnectionMode.JoinLanByIp;
        }

        public static PlayerControlScheme ToPlayerControlScheme(this ConnectionMode mode)
        {
            return mode == ConnectionMode.HostP1
                ? PlayerControlScheme.SplitKeyboardP1
                : mode.IsNetworked()
                    ? PlayerControlScheme.SplitKeyboardP2
                    : PlayerControlScheme.SoloKeyboardMouse;
        }
    }
}
