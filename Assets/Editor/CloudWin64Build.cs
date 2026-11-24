#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UnderwaterDiving.Editor
{
    /// <summary>
    /// Headless Win64 build entry: Unity -batchmode -quit -executeMethod UnderwaterDiving.Editor.CloudWin64Build.Build
    /// </summary>
    public static class CloudWin64Build
    {
        public static void Build()
        {
            const string scene = "Assets/Scenes/Ocean.unity";
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), scene)))
            {
                Debug.LogError("CloudWin64Build: missing scene " + scene);
                EditorApplication.Exit(2);
                return;
            }

            string outDir = Path.GetFullPath("Builds/Win64");
            Directory.CreateDirectory(outDir);
            string exe = Path.Combine(outDir, "UnderwaterDiving.exe");

            var opts = new BuildPlayerOptions
            {
                scenes = new[] { scene },
                locationPathName = exe,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            Debug.Log("CloudWin64Build: building " + exe);
            var report = BuildPipeline.BuildPlayer(opts);
            var summary = report.summary;
            Debug.Log($"CloudWin64Build: result={summary.result} size={summary.totalSize} errors={summary.totalErrors}");
            EditorApplication.Exit(summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }
    }
}
#endif
