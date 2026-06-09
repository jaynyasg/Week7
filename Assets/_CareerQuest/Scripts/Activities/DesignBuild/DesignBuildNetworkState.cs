using System;
using Unity.Netcode;

namespace CareerQuest
{
    public class DesignBuildNetworkState : NetworkBehaviour
    {
        private readonly NetworkList<int> _acceptedPieceIndexes = new();

        public event Action Changed;

        public int AcceptedCount => _acceptedPieceIndexes.Count;
        public bool Complete => AcceptedCount >= FutureCityBlueprint.CreateDefault().Pieces.Count;

        public override void OnNetworkSpawn()
        {
            _acceptedPieceIndexes.OnListChanged += HandleAcceptedPiecesChanged;
        }

        public override void OnNetworkDespawn()
        {
            _acceptedPieceIndexes.OnListChanged -= HandleAcceptedPiecesChanged;
        }

        public bool IsAccepted(string pieceId)
        {
            var pieceIndex = PieceIndexFor(pieceId);
            return pieceIndex >= 0 && _acceptedPieceIndexes.Contains(pieceIndex);
        }

        public void SubmitPlacement(string pieceId)
        {
            var pieceIndex = PieceIndexFor(pieceId);
            if (pieceIndex < 0)
            {
                return;
            }

            SubmitPlacementRpc(pieceIndex);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SubmitPlacementRpc(int pieceIndex)
        {
            if (pieceIndex < 0 || pieceIndex >= FutureCityBlueprint.CreateDefault().Pieces.Count)
            {
                return;
            }

            if (_acceptedPieceIndexes.Contains(pieceIndex))
            {
                return;
            }

            _acceptedPieceIndexes.Add(pieceIndex);
            Changed?.Invoke();
        }

        private static int PieceIndexFor(string pieceId)
        {
            if (string.IsNullOrWhiteSpace(pieceId))
            {
                return -1;
            }

            var pieces = FutureCityBlueprint.CreateDefault().Pieces;
            for (var i = 0; i < pieces.Count; i++)
            {
                if (pieces[i].Id == pieceId)
                {
                    return i;
                }
            }

            return -1;
        }

        private void HandleAcceptedPiecesChanged(NetworkListEvent<int> change)
        {
            Changed?.Invoke();
        }
    }
}
