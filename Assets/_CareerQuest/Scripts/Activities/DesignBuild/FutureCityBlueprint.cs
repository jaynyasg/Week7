using System.Collections.Generic;
using System.Linq;

namespace CareerQuest
{
    public class FutureCityBlueprint
    {
        public IReadOnlyList<BuildPiece> Pieces { get; }
        public IReadOnlyList<BuildSlot> Slots { get; }

        public bool Complete => Slots.All(slot => slot.Filled);

        public FutureCityBlueprint(IReadOnlyList<BuildPiece> pieces, IReadOnlyList<BuildSlot> slots)
        {
            Pieces = pieces;
            Slots = slots;
        }

        public bool TryPlace(string pieceId)
        {
            var piece = Pieces.FirstOrDefault(candidate => candidate.Id == pieceId);
            if (string.IsNullOrEmpty(piece.Id))
            {
                return false;
            }

            var slot = Slots.FirstOrDefault(candidate => candidate.RequiredPieceId == pieceId);
            return slot != null && slot.TryFill(piece);
        }

        public static FutureCityBlueprint CreateDefault()
        {
            var pieces = new[]
            {
                new BuildPiece("clinic", "Clinic"),
                new BuildPiece("court", "Court"),
                new BuildPiece("studio", "Studio"),
                new BuildPiece("lab", "Lab"),
                new BuildPiece("art_tower", "Art Tower")
            };

            var slots = pieces.Select(piece => new BuildSlot(piece.Id, $"{piece.DisplayName} lot")).ToList();
            return new FutureCityBlueprint(pieces, slots);
        }
    }
}
