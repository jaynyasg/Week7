using Unity.Netcode;
using UnityEngine;

namespace CareerQuest
{
    public class HealthHeroNetworkState : NetworkBehaviour
    {
        public const int RequiredSteps = 3;

        private readonly NetworkList<int> _completedSteps = new();

        public int CompletedSteps => _completedSteps.Count;

        public bool Complete => CompletedSteps >= RequiredSteps;

        public bool IsStepComplete(int stepIndex)
        {
            return _completedSteps.Contains(stepIndex);
        }

        public void SubmitStep(int stepIndex)
        {
            SubmitStepRpc(stepIndex);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SubmitStepRpc(int stepIndex, RpcParams rpcParams = default)
        {
            if (stepIndex < 0 || stepIndex >= RequiredSteps || _completedSteps.Contains(stepIndex))
            {
                return;
            }

            _completedSteps.Add(stepIndex);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _completedSteps.OnListChanged += OnStepsChanged;
        }

        public override void OnNetworkDespawn()
        {
            _completedSteps.OnListChanged -= OnStepsChanged;
            base.OnNetworkDespawn();
        }

        private void OnStepsChanged(NetworkListEvent<int> changeEvent)
        {
            if (!IsServer)
            {
                return;
            }

            Debug.Log($"[HealthHeroNetworkState] steps={CompletedSteps}/{RequiredSteps}");
        }
    }
}
