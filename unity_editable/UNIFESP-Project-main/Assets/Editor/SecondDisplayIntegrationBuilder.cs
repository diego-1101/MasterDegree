using System.Linq;
using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class SecondDisplayIntegrationBuilder
{
    public static void Build()
    {
        var output = Environment.GetEnvironmentVariable("UNIFESP_BUILD_OUTPUT");
        if (string.IsNullOrWhiteSpace(output))
            output = "SecondDisplayIntegrationBuild/UNIFESP Project.exe";

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception("Experiment 2 build failed: " + report.summary.result);
    }
}
