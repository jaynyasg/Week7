namespace CareerQuest
{
    public class BuildSlot
    {
        public string RequiredPieceId { get; }
        public string Label { get; }
        public bool Filled { get; private set; }

        public BuildSlot(string requiredPieceId, string label)
        {
            RequiredPieceId = requiredPieceId;
            Label = label;
        }

        public bool TryFill(BuildPiece piece)
        {
            if (Filled || piece.Id != RequiredPieceId)
            {
                return false;
            }

            Filled = true;
            return true;
        }
    }
}
