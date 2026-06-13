namespace CareerQuest
{
    /// <summary>
    /// Object roles supported by the Party Pack station spine (design doc:
    /// Station Seed Bible). CoreTask, Clue, and rule-driven Meter objects count
    /// toward the per-seed task/clue-chain minimum; every role must still react
    /// visibly (no-dead-toys rule, enforced by PartyStationValidator).
    /// </summary>
    public enum PartyStationObjectRole
    {
        CoreTask,
        Clue,
        Helper,
        Wildcard,
        Reaction,
        Bonus,
        Meter
    }

    /// <summary>
    /// One interactable toy object inside a station seed. SpriteKey may be a
    /// cataloged asset id or an intentional "prop.party." placeholder key until
    /// the U4/U5 renderer art pass lands. TargetId, when set, references
    /// another object id in the same resolved seed (clues and reactions point
    /// at the objects they illuminate).
    /// </summary>
    public sealed class PartyStationObjectDefinition
    {
        public string ObjectId { get; }
        public string DisplayName { get; }
        public PartyStationObjectRole Role { get; }
        public string SpriteKey { get; }
        public string TargetId { get; }
        public string ReactionKey { get; }
        public string TraitHint { get; }

        public PartyStationObjectDefinition(
            string objectId,
            string displayName,
            PartyStationObjectRole role,
            string spriteKey,
            string targetId,
            string reactionKey,
            string traitHint = "")
        {
            ObjectId = objectId;
            DisplayName = displayName;
            Role = role;
            SpriteKey = spriteKey;
            TargetId = targetId;
            ReactionKey = reactionKey;
            TraitHint = traitHint;
        }

        /// <summary>CoreTask, Clue, and Meter objects participate in the core task/clue chain.</summary>
        public bool IsChainRole =>
            Role == PartyStationObjectRole.CoreTask ||
            Role == PartyStationObjectRole.Clue ||
            Role == PartyStationObjectRole.Meter;
    }
}
