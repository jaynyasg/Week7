using System;

namespace CareerQuest
{
    public class SceneFlowRouter
    {
        public ActivityRoute CurrentRoute { get; private set; } = ActivityRoute.Entry;
        public AppMode AvatarSelectionTarget { get; private set; } = AppMode.Play;

        public ActivityRoute ShowEntry(GameSession session)
        {
            AvatarSelectionTarget = AppMode.Play;
            session.StartMode(AppMode.Entry);
            return SetRoute(session, ActivityRoute.Entry);
        }

        public ActivityRoute ShowAvatarSelection(GameSession session, AppMode target)
        {
            AvatarSelectionTarget = ValidateAvatarTarget(target);
            return SetRoute(session, ActivityRoute.AvatarSelection);
        }

        public ActivityRoute ChooseAvatar(GameSession session, string avatarId)
        {
            session.SelectAvatar(avatarId);
            return AvatarSelectionTarget == AppMode.Showcase ? BeginShowcase(session) : BeginPlay(session);
        }

        public ActivityRoute BeginPlay(GameSession session)
        {
            AvatarSelectionTarget = AppMode.Play;
            session.StartMode(AppMode.Play);
            return ShowConnection(session);
        }

        public ActivityRoute ShowConnection(GameSession session)
        {
            return SetRoute(session, ActivityRoute.Connection);
        }

        public ActivityRoute ShowShowcaseDisclaimer(GameSession session)
        {
            AvatarSelectionTarget = AppMode.Showcase;
            return SetRoute(session, ActivityRoute.ShowcaseDisclaimer);
        }

        public ActivityRoute BeginShowcase(GameSession session)
        {
            session.SeedShowcase();
            session.PlayerCount = 2;
            return SetRoute(session, ActivityRoute.ShowcaseProof);
        }

        public ActivityRoute UseConnectionMode(GameSession session, ConnectionMode mode, int playerCount)
        {
            session.SetConnectionMode(mode);
            session.PlayerCount = playerCount;
            return ShowCampus(session);
        }

        public ActivityRoute ShowCampus(GameSession session)
        {
            return SetRoute(session, ActivityRoute.Campus);
        }

        public ActivityRoute ShowShowcaseProof(GameSession session)
        {
            return SetRoute(session, ActivityRoute.ShowcaseProof);
        }

        public ActivityRoute ShowActivity(GameSession session, ActivityRoute route)
        {
            if (!IsMiniGameRoute(route))
            {
                throw new ArgumentException($"{route} is not a mini-game route.", nameof(route));
            }

            return SetRoute(session, route);
        }

        public ActivityRoute ShowGallery(GameSession session)
        {
            return SetRoute(session, ActivityRoute.Gallery);
        }

        public ActivityRoute ShowReveal(GameSession session)
        {
            return SetRoute(session, ActivityRoute.Reveal);
        }

        public ActivityRoute Quit(GameSession session)
        {
            return SetRoute(session, ActivityRoute.Quit);
        }

        public static bool IsMiniGameRoute(ActivityRoute route)
        {
            return route == ActivityRoute.DesignBuild ||
                   route == ActivityRoute.HealthHero ||
                   route == ActivityRoute.LogicCourt;
        }

        private ActivityRoute SetRoute(GameSession session, ActivityRoute route)
        {
            CurrentRoute = route;
            session.SetRoute(route);
            return route;
        }

        private static AppMode ValidateAvatarTarget(AppMode target)
        {
            if (target == AppMode.Play || target == AppMode.Showcase)
            {
                return target;
            }

            throw new ArgumentException($"{target} cannot be used as an avatar selection target.", nameof(target));
        }
    }
}
