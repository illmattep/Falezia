using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Falezia.EditorTools
{
    /// <summary>
    /// Build del client Linux. Menu: Falezia > Build Linux.
    /// Batch: -executeMethod Falezia.EditorTools.FaleziaBuild.BuildLinux
    /// </summary>
    public static class FaleziaBuild
    {
        const string Scene = "Assets/Scenes/Main.unity";
        const string Calanera = "Assets/Scenes/Calanera.unity";
        const string OutDir = "build/linux";
        const string OutExe = OutDir + "/Falezia.x86_64";

        [MenuItem("Falezia/Build Linux")]
        public static void BuildLinux()
        {
            if (!Directory.Exists(OutDir)) Directory.CreateDirectory(OutDir);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { Scene, Calanera },
                locationPathName = OutExe,
                target = BuildTarget.StandaloneLinux64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary s = report.summary;

            foreach (var step in report.steps)
                foreach (var m in step.messages)
                    if (m.type == LogType.Warning)
                        Debug.LogWarning("[FALEZIA] warning build: " + m.content);
            Debug.Log("[FALEZIA] build " + s.result + " · " +
                      (s.totalSize / (1024 * 1024)) + " MB · " +
                      s.totalErrors + " errori · " + s.totalWarnings + " warning · " +
                      s.totalTime.TotalSeconds.ToString("F0") + " s");

            if (s.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
