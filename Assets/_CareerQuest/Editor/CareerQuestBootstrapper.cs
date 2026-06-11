using System.IO;
using CareerQuest;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CareerQuest.Editor
{
    public static class CareerQuestBootstrapper
    {
        private const string ScenePath = "Assets/_CareerQuest/Scenes/CareerQuestCampus.unity";
        private const string PlayerPrefabPath = "Assets/_CareerQuest/Prefabs/PlayerAvatar.prefab";
        private const string NetworkPrefabsPath = "Assets/DefaultNetworkPrefabs.asset";
        private const uint PlayerAvatarHash = 0xC0171001;
        private const uint DesignBuildStateHash = 0xC0171002;
        private const uint CampusSessionStateHash = 0xC0171003;
        private const uint HealthHeroStateHash = 0xC0171004;
        private const uint LogicCourtStateHash = 0xC0171005;

        [MenuItem("Career Quest/Bootstrap Project")]
        public static void BootstrapProject()
        {
            EnsureDirectories();
            var playerPrefab = CreatePlayerPrefab();
            UpdateDefaultNetworkPrefabs(playerPrefab);
            CreateScene(playerPrefab);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureDirectories()
        {
            Directory.CreateDirectory("Assets/_CareerQuest/Scenes");
            Directory.CreateDirectory("Assets/_CareerQuest/Prefabs");
        }

        private static GameObject CreatePlayerPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (existing != null)
            {
                RefreshNetworkObjectHash(existing.GetComponent<NetworkObject>(), PlayerAvatarHash);
                return existing;
            }

            var player = new GameObject("PlayerAvatar");
            player.AddComponent<NetworkObject>();
            player.AddComponent<PlayerInputRouter>();
            player.AddComponent<PlayerAvatarNetwork>();
            var renderer = player.AddComponent<SpriteRenderer>();
            renderer.color = new Color(0.2f, 0.65f, 1f);

            var prefab = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            Object.DestroyImmediate(player);
            RefreshNetworkObjectHash(prefab.GetComponent<NetworkObject>(), PlayerAvatarHash);
            return prefab;
        }

        private static void UpdateDefaultNetworkPrefabs(GameObject playerPrefab)
        {
            var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(NetworkPrefabsPath);
            if (list == null)
            {
                list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
                AssetDatabase.CreateAsset(list, NetworkPrefabsPath);
            }

            var serialized = new SerializedObject(list);
            var prefabs = serialized.FindProperty("List");
            prefabs.ClearArray();
            prefabs.InsertArrayElementAtIndex(0);
            var item = prefabs.GetArrayElementAtIndex(0);
            item.FindPropertyRelative("Override").enumValueIndex = (int)NetworkPrefabOverride.None;
            item.FindPropertyRelative("Prefab").objectReferenceValue = playerPrefab;
            item.FindPropertyRelative("SourcePrefabToOverride").objectReferenceValue = null;
            item.FindPropertyRelative("SourceHashToOverride").uintValue = 0;
            item.FindPropertyRelative("OverridingTargetPrefab").objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(list);
        }

        private static void CreateScene(GameObject playerPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraObject = new GameObject("Main Camera", typeof(Camera));
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.78f, 0.92f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.transform.position = new Vector3(0f, 0f, -10f);

            var networkObject = new GameObject("NetworkManager", typeof(NetworkManager), typeof(UnityTransport));
            var manager = networkObject.GetComponent<NetworkManager>();
            var transport = networkObject.GetComponent<UnityTransport>();
            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.PlayerPrefab = playerPrefab;
            manager.NetworkConfig.ConnectionApproval = true;

            var designBuildState = new GameObject("DesignBuildNetworkState", typeof(NetworkObject), typeof(DesignBuildNetworkState));
            var campusSessionState = new GameObject("CampusSessionState", typeof(NetworkObject), typeof(CampusSessionState));
            var healthHeroState = new GameObject("HealthHeroNetworkState", typeof(NetworkObject), typeof(HealthHeroNetworkState));
            var logicCourtState = new GameObject("LogicCourtNetworkState", typeof(NetworkObject), typeof(LogicCourtNetworkState));

            var appObject = new GameObject("CareerQuestApp", typeof(CareerQuestApp));
            appObject.GetComponent<NetworkBootstrap>().Bind(manager, transport);

            EditorSceneManager.SaveScene(scene, ScenePath);
            RefreshNetworkObjectHash(designBuildState.GetComponent<NetworkObject>(), DesignBuildStateHash);
            RefreshNetworkObjectHash(campusSessionState.GetComponent<NetworkObject>(), CampusSessionStateHash);
            RefreshNetworkObjectHash(healthHeroState.GetComponent<NetworkObject>(), HealthHeroStateHash);
            RefreshNetworkObjectHash(logicCourtState.GetComponent<NetworkObject>(), LogicCourtStateHash);
            EditorUtility.SetDirty(designBuildState);
            EditorUtility.SetDirty(campusSessionState);
            EditorUtility.SetDirty(healthHeroState);
            EditorUtility.SetDirty(logicCourtState);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
        }

        private static void RefreshNetworkObjectHash(NetworkObject networkObject, uint hash)
        {
            if (networkObject == null)
            {
                return;
            }

            var serialized = new SerializedObject(networkObject);
            serialized.FindProperty("GlobalObjectIdHash").uintValue = hash;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(networkObject);
        }
    }
}
