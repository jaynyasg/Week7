using System.Collections.Generic;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// P10 first-run guided beat: on the FIRST hub entry of a session the guide
    /// greets the player by their chosen avatar name via speech bubble and
    /// points to the nearest unplayed room — that door sign pulses per
    /// DESIGN.md motion rules. Plays once per session; hub re-entry does not
    /// repeat it. The session-scoped flag resets on disconnect and on a fresh
    /// app session (System-Wide Impact note).
    ///
    /// Deterministic clock: Tick(deltaSeconds) runs the beat timeline; Update
    /// forwards Time.deltaTime only when AutoTick is on.
    /// </summary>
    public class FirstRunGuideBeat : MonoBehaviour
    {
        public const float BeatDurationSeconds = 7f;

        private static bool _playedThisSession;

        private CampusGuideController _guide;
        private float _remaining;

        /// <summary>Real-time clock toggle. Tests set false and drive Tick directly.</summary>
        public bool AutoTick { get; set; } = true;

        public static bool HasPlayedThisSession => _playedThisSession;

        /// <summary>Session reset seam — called on disconnect and fresh app sessions.</summary>
        public static void ResetSessionFlag()
        {
            _playedThisSession = false;
        }

        public bool DidPlay { get; private set; }
        public bool IsRunning => _remaining > 0f;
        public DoorSign PointedDoor { get; private set; }
        public ActivityRoute PointedRoute { get; private set; }
        public string GreetingLine { get; private set; } = string.Empty;

        /// <summary>Kid-facing greeting copy (≤ 2 speech-bubble lines).</summary>
        public static string GreetingFor(AvatarDefinition avatar, string roomLabel)
        {
            var definition = avatar ?? AvatarConfig.DefaultAvatar;
            return $"Hi {definition.DisplayName}! Try the {roomLabel} room first!";
        }

        /// <summary>
        /// Starts the beat when it has not played this session. Returns true
        /// when the beat began.
        /// </summary>
        public bool TryBegin(GameSession session, CampusGuideController guide, IReadOnlyList<BuildingEntrance> entrances, Vector2 playerPosition)
        {
            if (_playedThisSession || session == null || guide == null || entrances == null || entrances.Count == 0)
            {
                return false;
            }

            var target = NearestUnplayedEntrance(session, entrances, playerPosition);
            if (target == null)
            {
                return false;
            }

            _playedThisSession = true;
            DidPlay = true;
            _guide = guide;
            PointedRoute = target.Route;
            PointedDoor = target.GetComponent<DoorSign>();
            GreetingLine = GreetingFor(session.SelectedAvatar, target.Label);

            _guide.Say(GreetingLine, BeatDurationSeconds);
            _guide.PointToDoor(PointedDoor);
            _remaining = BeatDurationSeconds;
            return true;
        }

        public void Tick(float deltaSeconds)
        {
            if (_remaining <= 0f || deltaSeconds <= 0f)
            {
                return;
            }

            _remaining -= deltaSeconds;
            if (_remaining <= 0f)
            {
                _remaining = 0f;
                EndBeat();
            }
        }

        private void Update()
        {
            if (AutoTick)
            {
                Tick(Time.deltaTime);
            }
        }

        private void EndBeat()
        {
            if (_guide != null)
            {
                _guide.StopPointing();
                _guide.SayDefaultPrompt();
            }

            PointedDoor = null;
        }

        private void OnDestroy()
        {
            // Hub teardown mid-beat: release the pulse cleanly.
            if (_remaining > 0f && _guide != null)
            {
                _guide.StopPointing();
            }
        }

        /// <summary>
        /// Nearest entrance whose activity has no recorded result, preferring
        /// core rooms (they gate the reveal) over optional rooms.
        /// </summary>
        private static BuildingEntrance NearestUnplayedEntrance(GameSession session, IReadOnlyList<BuildingEntrance> entrances, Vector2 playerPosition)
        {
            BuildingEntrance bestCore = null;
            BuildingEntrance bestAny = null;
            var bestCoreDistance = float.MaxValue;
            var bestAnyDistance = float.MaxValue;

            foreach (var entrance in entrances)
            {
                if (entrance == null || !CareerQuestCatalog.TryGetByRoute(entrance.Route, out var entry))
                {
                    continue;
                }

                if (session.GetBestResult(entry.Id) != null)
                {
                    continue;
                }

                var distance = Vector2.Distance(entrance.transform.position, playerPosition);
                if (distance < bestAnyDistance)
                {
                    bestAnyDistance = distance;
                    bestAny = entrance;
                }

                if (entry.IsCore && distance < bestCoreDistance)
                {
                    bestCoreDistance = distance;
                    bestCore = entrance;
                }
            }

            return bestCore != null ? bestCore : bestAny;
        }
    }
}
