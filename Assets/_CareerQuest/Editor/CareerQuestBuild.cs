using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace CareerQuest.Editor
{
    public static class CareerQuestBuild
    {
        private const string ScenePath = "Assets/_CareerQuest/Scenes/CareerQuestCampus.unity";
        private const string WindowsOutputPath = "Builds/Windows/CareerQuestCampus.exe";

        [MenuItem("Career Quest/Build Windows Player")]
        public static void BuildWindowsPlayer()
        {
            var outputDirectory = Path.GetDirectoryName(WindowsOutputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = WindowsOutputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new System.InvalidOperationException($"Windows build failed: {report.summary.result}");
            }
        }
    }
}
