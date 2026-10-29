using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class SecondDisplayIntegrationBuilder
{
    public static void Build()
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            locationPathName = "SecondDisplayIntegrationBuild/UNIFESP Project.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception("Experiment 2 build failed: " + report.summary.result);
    }
}
