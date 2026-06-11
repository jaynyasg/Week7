namespace CareerQuest
{
    public static class ActivityPhaseRules
    {
        public static bool CanTransition(ActivityPhase from, ActivityPhase to)
        {
            if (from == to)
            {
                return true;
            }

            return (from, to) switch
            {
                (ActivityPhase.Intro, ActivityPhase.Explore) => true,
                (ActivityPhase.Explore, ActivityPhase.Interact) => true,
                (ActivityPhase.Interact, ActivityPhase.Review) => true,
                (ActivityPhase.Review, ActivityPhase.Complete) => true,
                (ActivityPhase.Complete, ActivityPhase.Ceremony) => true,
                (ActivityPhase.Ceremony, ActivityPhase.ResultRecorded) => true,
                (ActivityPhase.Intro, ActivityPhase.Exit) => true,
                (ActivityPhase.Explore, ActivityPhase.Exit) => true,
                (ActivityPhase.Interact, ActivityPhase.Exit) => true,
                (ActivityPhase.Review, ActivityPhase.Exit) => true,
                _ => false
            };
        }

        public static bool CanCompleteFrom(ActivityPhase phase)
        {
            return phase == ActivityPhase.Interact || phase == ActivityPhase.Review;
        }

        public static bool CanExitFrom(ActivityPhase phase)
        {
            return phase == ActivityPhase.Intro
                   || phase == ActivityPhase.Explore
                   || phase == ActivityPhase.Interact
                   || phase == ActivityPhase.Review;
        }
    }
}
