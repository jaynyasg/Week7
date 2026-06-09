using System;
using UnityEngine;

namespace CareerQuest
{
    public class BuildingEntrance : MonoBehaviour
    {
        private Action<ActivityRoute> _onEnter;

        public ActivityRoute Route { get; private set; }
        public string Label { get; private set; }
        public float Radius { get; private set; } = 0.75f;

        public void Configure(ActivityRoute route, string label, float radius, Action<ActivityRoute> onEnter)
        {
            Route = route;
            Label = label;
            Radius = Mathf.Max(0.1f, radius);
            _onEnter = onEnter;
        }

        public bool Contains(Vector2 worldPosition)
        {
            return Vector2.Distance(transform.position, worldPosition) <= Radius;
        }

        public void Enter()
        {
            _onEnter?.Invoke(Route);
        }
    }
}
