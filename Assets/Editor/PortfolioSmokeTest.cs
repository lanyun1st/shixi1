using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch-mode integration tests. Deterministic damage is not a balance playthrough.
public static class PortfolioSmokeTest
{
    static double started;
    static bool previousEnabled;
    static EnterPlayModeOptions previousOptions;
    public static void Run()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-rebuild-ui")>=0)UiPrefabBuilder.Build();
        PortfolioConfigInstaller.Ensure();
        PortfolioDefinitionInstaller.Ensure();
        SentryAssetInstaller.Ensure();
        if(!PortfolioRuleTests.RunAll(false)){EditorApplication.Exit(1);return;}
        PortfolioSetup.CreateScene();
        EditorSceneManager.OpenScene("Assets/Scenes/Ruins.unity");
        previousEnabled=EditorSettings.enterPlayModeOptionsEnabled;
        previousOptions=EditorSettings.enterPlayModeOptions;
        EditorSettings.enterPlayModeOptionsEnabled=true;
        EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
        started=EditorApplication.timeSinceStartup;
        EditorApplication.update+=Tick;
        EditorApplication.isPlaying=true;
    }
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup-started>40){End(1,"Timed out entering Play Mode");return;}
        if(!EditorApplication.isPlaying || EditorApplication.timeSinceStartup-started<3)return;
        var game=UnityEngine.Object.FindObjectOfType<RuinsGame>();if(game==null)return;
        EditorApplication.update-=Tick;
        try
        {
            var catalog=JsonUtility.FromJson<SpriteCatalog>("{\"entries\":"+System.IO.File.ReadAllText("Assets/Editor/SpriteCatalog.json")+"}");
            foreach(var entry in catalog.entries)
                if(Resources.Load<Sprite>("Art/"+entry.group+"/"+entry.name)==null)throw new Exception("Missing sprite: "+entry.group+"/"+entry.name);
            foreach(var d in new[]{"down","up","left","right"})foreach(var pose in new[]{"idle","windup","charge","stunned"})
                if(Resources.Load<Sprite>("Art/elite/"+d+"_"+pose)==null)throw new Exception("Missing guard sprite: "+d+pose);
            PortfolioUiChecks.Run(game);
            PortfolioProgressionChecks.Run(game);
            PortfolioSentryChecks.Run(game);
            PortfolioShieldChecks.Run(game);
            PortfolioHordeChecks.Run(game);
            PortfolioMapChecks.Run(game);
            PortfolioReactionChecks.Run(game);
            PortfolioPresentationChecks.Run(game);
            PortfolioSkillVisualChecks.Run(game);
            PortfolioPresentationPolishChecks.Run(game);
            PortfolioOptimizationChecks.Run(game);
            PortfolioInteractionPolishChecks.Run(game);
            PortfolioAimUiChecks.Run(game);
            PortfolioSkillIconChecks.Run(game);
            PortfolioBossMotionChecks.Run(game);
            PortfolioDefinitionChecks.Run(game);
            PortfolioReticleChecks.Run(game);
            PortfolioHurtboxChecks.Run(game);
            End(0,"PORTFOLIO_SMOKE_OK: sprites, UI, three-room progression, skills, input, collision, pool regression.");
        }
        catch(Exception e){End(1,e.ToString());}
    }
    static void End(int code,string message)
    {
        EditorApplication.update-=Tick;
        EditorSettings.enterPlayModeOptionsEnabled=previousEnabled;
        EditorSettings.enterPlayModeOptions=previousOptions;
        if(code==0)Debug.Log(message);else Debug.LogError(message);
        EditorApplication.Exit(code);
    }
}
