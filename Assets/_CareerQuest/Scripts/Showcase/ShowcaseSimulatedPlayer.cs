using UnityEngine;

namespace CareerQuest
{
    public class ShowcaseSimulatedPlayer : MonoBehaviour
    {
        [SerializeField] private Vector2 orbitCenter;
        [SerializeField] private float orbitRadius = 1f;
        [SerializeField] private float speed = 1.5f;
        [SerializeField] private float phase;

        public void Configure(Vector2 center, float radius, float phaseOffset)
        {
            orbitCenter = center;
            orbitRadius = radius;
            phase = phaseOffset;
        }

        private void Update()
        {
            var t = Time.time * speed + phase;
            transform.position = new Vector3(
                orbitCenter.x + Mathf.Cos(t) * orbitRadius,
                orbitCenter.y + Mathf.Sin(t) * orbitRadius,
                0f);
        }
    }
}
