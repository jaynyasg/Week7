namespace CareerQuest
{
    public enum ActivityRoute
    {
        Entry,
        AvatarSelection,
        ShowcaseDisclaimer,
        Connection,
        Campus,
        ShowcaseProof,
        DesignBuild,
        HealthHero,
        LogicCourt,
        AiLab,
        MusicStudio,
        RoboticsGarage,
        CommunityKitchen,
        Gallery,
        Reveal,
        Quit,

        /// <summary>
        /// U2 (KTD3): the single generic Party Pack station branch. The station
        /// identity travels as a station id string next to this route
        /// (SceneFlowRouter.CurrentStationId) — new stations never add enum
        /// values or switch branches here. Appended last so the serialized
        /// network ints of the legacy routes stay stable.
        /// </summary>
        PartyStation
    }
}
