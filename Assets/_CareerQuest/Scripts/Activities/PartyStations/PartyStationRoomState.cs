using System.Collections.Generic;

namespace CareerQuest
{
    /// <summary>
    /// Room-side session glue for the generic station surface (U4): which
    /// stations completed this app session, which seed each station last
    /// played, and the shared-attempt/2P begin-or-join rules over
    /// <see cref="StationProgressNetworkState"/>.
    ///
    /// Plain C# riding the persistent PartyStationController component, so the
    /// memory is session-scoped (it survives route changes, dies with the app)
    /// without touching GameSession — the session-side reward event log is U6.
    ///
    /// Seed selection contract (design doc Replay options rule): first play
    /// enters the default seed directly; replay of a COMPLETED station (this
    /// session or via an existing best result) offers default or alternate.
    /// An abandoned attempt is not a completion, so re-entry after walking out
    /// goes straight back into the default seed.
    /// </summary>
    public sealed class PartyStationRoomState
    {
        private readonly HashSet<string> _completedStations = new();
        private readonly Dictionary<string, string> _selectedSeedByStation = new();

        /// <summary>Last shared attempt number this surface was synced against (2P).</summary>
        public int SyncedAttemptNumber { get; set; } = 1;

        public bool HasCompleted(string stationId)
        {
            return stationId != null && _completedStations.Contains(stationId);
        }

        /// <summary>Replay = completed this session OR a best result already exists.</summary>
        public bool IsReplay(string stationId, GameSession session)
        {
            return HasCompleted(stationId) || session?.GetBestResult(stationId) != null;
        }

        /// <summary>True when this entry should open the seed choice (replay + an alternate exists).</summary>
        public bool ShouldOfferSeedChoice(PartyStationDefinition definition, GameSession session)
        {
            return definition != null
                && definition.AlternateSeeds.Count > 0
                && IsReplay(definition.Id, session);
        }

        /// <summary>The seed id chosen on the most recent entry of this station, or null.</summary>
        public string SelectedSeedId(string stationId)
        {
            return stationId != null && _selectedSeedByStation.TryGetValue(stationId, out var seedId)
                ? seedId
                : null;
        }

        public void RecordSeedChoice(string stationId, string seedId)
        {
            if (string.IsNullOrEmpty(stationId) || string.IsNullOrEmpty(seedId))
            {
                return;
            }

            _selectedSeedByStation[stationId] = seedId;
        }

        public void MarkCompleted(string stationId)
        {
            if (!string.IsNullOrEmpty(stationId))
            {
                _completedStations.Add(stationId);
            }
        }

        /// <summary>
        /// 2P client seed adoption: when the host already validated a seed for
        /// this station, every joining surface plays THAT seed (R16 — the host
        /// validates the selected seed; clients never pick their own).
        /// Returns null when there is no matching active station to adopt.
        /// </summary>
        public static PartyStationSeedDefinition AdoptNetworkSeed(
            PartyStationDefinition definition,
            StationProgressNetworkState network)
        {
            if (definition == null
                || network == null
                || !network.IsSpawned
                || !network.HasActiveStation
                || network.StationId != definition.Id)
            {
                return null;
            }

            return definition.TryGetSeed(network.SeedId, out var seed) ? seed : null;
        }

        /// <summary>
        /// Host-side station begin-or-join: re-entering the same station/seed
        /// joins the in-progress attempt (BeginAttempt only resets after a
        /// completed one — partner progress is never wiped); a different
        /// station or seed begins fresh through the validated host path.
        /// </summary>
        public static void HostBeginOrJoin(StationProgressNetworkState network, string stationId, string seedId)
        {
            if (network == null || !network.IsSpawned || !network.IsServer)
            {
                return;
            }

            if (network.HasActiveStation && network.StationId == stationId && network.SeedId == seedId)
            {
                network.BeginAttempt();
                return;
            }

            network.ServerBeginStation(stationId, seedId);
        }
    }
}
