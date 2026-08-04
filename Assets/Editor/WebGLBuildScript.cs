using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLBuildScript
{
    // Invoked from the command line:
    // Unity -batchmode -quit -buildTarget WebGL -executeMethod WebGLBuildScript.Build
    public static void Build()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "Build",
            target = BuildTarget.WebGL,
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log($"WebGL build finished: {report.summary.result}, size {report.summary.totalSize} bytes");

        if (report.summary.result != BuildResult.Succeeded)
        {
            EditorApplication.Exit(1);
        }
    }
}
