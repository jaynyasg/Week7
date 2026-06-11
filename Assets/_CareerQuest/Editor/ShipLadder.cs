using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CareerQuest.Editor
{
    public static class ShipLadder
    {
        private const string LogsDirectory = "Logs";
        private const string ShipLogPath = "Logs/ship-ladder.log";

        [MenuItem("Career Quest/Ship Ladder")]
        public static void RunInteractive()
        {
            RunInternal(interactive: true);
        }

        public static void RunHeadless()
        {
            RunInternal(interactive: false);
        }

        private static void RunInternal(bool interactive)
        {
            Directory.CreateDirectory(LogsDirectory);
            var logLines = new List<string>
            {
                $"ShipLadder started {DateTime.UtcNow:u}"
            };

            try
            {
                logLines.Add("Generating sprite kit...");
                CareerQuestSpriteKitGenerator.Generate();
                logLines.Add("Sprite kit generated.");

                RunUnityTests("EditMode", "Logs/ship-ladder-editmode.xml", logLines);
                RunUnityTests("PlayMode", "Logs/ship-ladder-playmode.xml", logLines);

                logLines.Add("Building Windows player...");
                CareerQuestBuild.BuildWindowsPlayer();
                logLines.Add("Windows build succeeded.");

                File.WriteAllLines(ShipLogPath, logLines);
                Debug.Log($"ShipLadder complete. Log: {ShipLogPath}");

                if (interactive)
                {
                    EditorUtility.DisplayDialog("Ship Ladder", "Tests and Windows build completed successfully.", "OK");
                }
            }
            catch (Exception exception)
            {
                logLines.Add($"ShipLadder failed: {exception.Message}");
                File.WriteAllLines(ShipLogPath, logLines);
                Debug.LogError($"ShipLadder failed: {exception}");
                if (interactive)
                {
                    EditorUtility.DisplayDialog("Ship Ladder", exception.Message, "OK");
                }

                throw;
            }
        }

        private static void RunUnityTests(string platform, string resultsPath, List<string> logLines)
        {
            var projectPath = Directory.GetCurrentDirectory().Replace('\\', '/');
            var unityPath = EditorApplication.applicationPath;
            var logPath = $"Logs/ship-ladder-{platform.ToLowerInvariant()}.log";
            var arguments =
                $"-batchmode -nographics -projectPath \"{projectPath}\" -runTests -testPlatform {platform} -testResults \"{resultsPath}\" -logFile \"{logPath}\"";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = unityPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"{platform} tests failed with exit code {process.ExitCode}. See {resultsPath}.");
            }

            logLines.Add($"{platform} tests passed ({resultsPath}).");
        }
    }
}
