using System;
using UnityEngine;

namespace CareerQuest
{
    public class BuildingEntrance : MonoBehaviour
    {
        private Action<BuildingEntrance> _onEnter;

        public ActivityRoute Route { get; private set; }

        /// <summary>
        /// U2 station-id routing: set when this entrance enters via the generic
        /// <see cref="ActivityRoute.PartyStation"/> branch (null for legacy
        /// route-based doors).
        /// </summary>
        public string StationId { get; private set; }

        public string Label { get; private set; }
        public float Radius { get; private set; } = 0.75f;

        /// <summary>True when this entrance routes by station id, not by ActivityRoute.</summary>
        public bool IsStationEntrance => Route == ActivityRoute.PartyStation && !string.IsNullOrWhiteSpace(StationId);

        public void Configure(ActivityRoute route, string label, float radius, Action<BuildingEntrance> onEnter)
        {
            Configure(route, null, label, radius, onEnter);
        }

        public void Configure(ActivityRoute route, string stationId, string label, float radius, Action<BuildingEntrance> onEnter)
        {
            Route = route;
            StationId = stationId;
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
            _onEnter?.Invoke(this);
        }
    }
}
