using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class PortfolioPlayerBuild
{
    public static void Windows()
    {
        try
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-portfolio-output");
            if (index < 0 || index + 1 >= args.Length) throw new ArgumentException("Supply -portfolio-output with a new output directory.");
            string output = Path.GetFullPath(args[index + 1]);
            if (Directory.Exists(output)) throw new IOException("Output already exists; refusing to overwrite.");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Ruins.unity" },
                locationPathName = Path.Combine(output, "RuinsPortfolio.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Player build failed: " + report.summary.result);
            Debug.Log("PORTFOLIO_PLAYER_BUILD_OK: " + report.summary.totalSize + " bytes");
            EditorApplication.Exit(0);
        }
        catch (Exception ex) { Debug.LogException(ex); EditorApplication.Exit(1); }
    }
}
