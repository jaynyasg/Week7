using UnityEngine;

namespace CareerQuest
{
    public class CampusWorldController : MonoBehaviour
    {
        private Transform _root;
        private CameraDirector _cameraDirector;
        private CampusWorldBuilder _builder;
        private HubBootController _hubBoot;
        private RoomVeilController _roomVeil;

        public bool IsHubBootComplete => _hubBoot != null && _hubBoot.IsBootComplete;
        public bool IsHubDecorLoaded => _hubBoot != null && _hubBoot.IsDecorLoaded;
        public bool IsRoomVeilActive => _roomVeil != null && _roomVeil.IsVeilActive;

        public CameraDirector CameraDirector
        {
            get
            {
                EnsureSetup();
                return _cameraDirector;
            }
        }

        /// <summary>World content root — room playfields (drag pieces/zones) mount here.</summary>
        public Transform WorldRoot
        {
            get
            {
                EnsureSetup();
                return _root;
            }
        }

        public static CampusWorldController Ensure()
        {
            var existing = FindFirstObjectByType<CampusWorldController>();
            if (existing != null)
            {
                existing.EnsureSetup();
                return existing;
            }

            var world = new GameObject("CampusWorld", typeof(CampusWorldController));
            var controller = world.GetComponent<CampusWorldController>();
            controller.EnsureSetup();
            return controller;
        }

        public void ShowEntry(GameSession session)
        {
            BeginHub("Entry");
            _builder.AddHeroCharacters(session, session?.PlayerCount > 1);
        }

        public void ShowConnection(GameSession session)
        {
            BeginHub("Connection");
            _builder.AddNetworkProof(-2.2f, 0.1f, "Host", CampusWorldPalette.PlayerBlue);
            _builder.AddNetworkProof(2.2f, 0.1f, "Join", CampusWorldPalette.PlayerGold);
        }

        public void ShowCampus(GameSession session)
        {
            BeginHub("Campus");
            _builder.AddHeroCharacters(session, session != null && (session.PlayerCount > 1 || session.Mode == AppMode.Showcase));
        }

        public void ShowProof(GameSession session)
        {
            // Hub-style world (authored diorama) with the proof characters on top.
            BeginHub("Proof");
            _builder.AddNetworkProof(-2.6f, -0.85f, "P1 Builder", CampusWorldPalette.PlayerBlue);
            _builder.AddNetworkProof(2.6f, -0.85f, "P2 Designer", CampusWorldPalette.PlayerGold);
        }

        public void ShowDesignBuild(GameSession session)
        {
            BeginRoom(() => CampusRoomScenes.ShowDesignBuild(_builder, session), AudioCueIds.AmbientDesignBuild);
        }

        public void ShowClinic(GameSession session)
        {
            BeginRoom(() => CampusRoomScenes.ShowClinic(_builder, session), AudioCueIds.AmbientHealthHero);
        }

        public void ShowCourt(GameSession session)
        {
            BeginRoom(() => CampusRoomScenes.ShowCourt(_builder, session), AudioCueIds.AmbientLogicCourt);
        }

        public void ShowGallery(GameSession session)
        {
            BeginRoom(() => CampusRoomScenes.ShowGallery(_builder, session), AudioCueIds.AmbientGallery);
        }

        /// <summary>
        /// U4 station rooms: the themed station interior plus the
        /// definition-driven station set (same room-scene pattern; the play
        /// surface itself mounts through PartyStationController). U5 retired
        /// the separate optional-room route — every Party Pack id lands here.
        /// </summary>
        public void ShowPartyStation(GameSession session, CatalogEntry entry)
        {
            BeginRoom(() => CampusRoomScenes.ShowPartyStation(_builder, session, entry), AudioCueIds.AmbientOptional);
        }

        public void ShowReveal(GameSession session)
        {
            BeginRoom(() => CampusRoomScenes.ShowReveal(_builder, session), AudioCueIds.AmbientReveal);
        }

        public void ClearWorld()
        {
            CancelBoot();
            _builder?.ClearWorld();
            _cameraDirector?.ResetToRouteShot();
        }

        private void BeginHub(string name)
        {
            EnsureSetup();
            // P24: starting a hub route cancels the previous route's pending
            // work (room veil reveal AND hub decor) so a cancelled room build
            // can never wipe or pollute the world this route mounts.
            CancelBoot();
            _cameraDirector.SetRouteShot(CameraShot.Default);
            // P4: every hub-style route restores campus ambience + music loop
            // (~1s crossfade; the director no-ops if it is already the target).
            AudioDirector.Ensure().SetAmbience(AudioCueIds.AmbientCampus, AudioCueIds.MusicCampus);
            _hubBoot.BuildCampus(name);
        }

        private void BeginRoom(System.Action buildRoom, string ambientCueId)
        {
            EnsureSetup();
            // P24: starting a room route cancels pending hub decor and any
            // previous room's pending reveal — no orphaned hub decor in rooms.
            CancelBoot();
            _cameraDirector.SetRouteShot(CameraShot.Default);
            // P4: per-room ambient flavor; campus music fades out so the room
            // flavor never fights the hub loop.
            AudioDirector.Ensure().SetAmbience(ambientCueId, null);
            _roomVeil.ShowRoom(buildRoom);
        }

        private void CancelBoot()
        {
            _hubBoot?.Cancel();
            _roomVeil?.Cancel();
        }

        private void EnsureSetup()
        {
            if (_root == null)
            {
                _root = new GameObject("CampusWorldRoot").transform;
                _root.SetParent(transform, false);
            }

            if (_cameraDirector == null)
            {
                _cameraDirector = CameraDirector.Ensure();
            }

            if (_builder == null)
            {
                _builder = new CampusWorldBuilder(_root);
                _hubBoot = new HubBootController(this, _builder);
                _roomVeil = new RoomVeilController(this, _builder);
            }
        }
    }
}
