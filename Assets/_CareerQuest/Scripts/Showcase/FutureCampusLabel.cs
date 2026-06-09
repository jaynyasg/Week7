using UnityEngine;

namespace CareerQuest
{
    public class FutureCampusLabel : MonoBehaviour
    {
        [SerializeField] private string label;

        public string Label
        {
            get => label;
            set => label = value;
        }
    }
}
