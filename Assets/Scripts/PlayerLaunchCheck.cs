using System;
using System.Collections;
using System.IO;
using Portfolio.UI;
using UnityEngine;

// Opt-in standalone smoke check. Normal launches create no component or subscriptions.
public sealed class PlayerLaunchCheck : MonoBehaviour
{
    bool finished;
    string output;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        if (Application.isEditor || Array.IndexOf(Environment.GetCommandLineArgs(), "-portfolio-smoke") < 0) return;
        DontDestroyOnLoad(new GameObject("Standalone launch check", typeof(PlayerLaunchCheck)));
    }
    void Awake() { Application.runInBackground = true; Application.logMessageReceived += OnLog; }
    void OnDestroy() { Application.logMessageReceived -= OnLog; }
    void OnLog(string text, string trace, LogType kind)
    {
        if (!finished && (kind == LogType.Error || kind == LogType.Exception || kind == LogType.Assert))
        { finished = true; Application.Quit(2); }
    }
    void Require(bool condition, string message)
    {
        if (!condition) { Debug.LogError("PLAYER_SMOKE_FAILED: " + message); throw new InvalidOperationException(message); }
    }
    IEnumerator Start()
    {
        var args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-portfolio-captures");
        output = index >= 0 && index + 1 < args.Length ? args[index + 1] : Application.persistentDataPath;
        Directory.CreateDirectory(output);
        bool shield = Array.IndexOf(args, "-portfolio-shield") >= 0;
        yield return new WaitForSecondsRealtime(2);
        var game = FindObjectOfType<RuinsGame>();
        Require(game != null && game.enabled, "Game initialization");
        Require(FindObjectOfType<PortfolioCanvasUI>() != null, "Canvas");
        Require(game.CaptureUiState().mode == UiMode.Title, "Title state");
        Capture(shield ? "shield-title.bmp" : "ranged-title.bmp");
        yield return new WaitForSecondsRealtime(.5f);
        ((ICharacterUiCommands)game).RequestCharacter(shield);
        var commands = (IGameUiCommands)game;
        commands.RequestStart();
        Require(game.CaptureUiState().mode == UiMode.Upgrade, "Starting skill choice");
        for (int id = 0; id < 9 && game.CaptureUiState().mode == UiMode.Upgrade; id++) commands.RequestUpgrade(id);
        Require(game.CaptureUiState().mode == UiMode.Playing, "Enter gameplay");
        yield return new WaitForSecondsRealtime(3);
        var state = game.CaptureUiState();
        Require(state.shieldHero == shield && state.room == 1 && state.remaining > 0 && state.elapsed > 0, "Role, room, enemies and simulation");
        commands.RequestPause(true);
        Require(game.CaptureUiState().mode == UiMode.Paused, "Pause");
        commands.RequestPause(false);
        Require(game.CaptureUiState().mode == UiMode.Playing, "Resume");
        Capture(shield ? "shield-playing.bmp" : "ranged-playing.bmp");
        yield return new WaitForSecondsRealtime(1);
        finished = true;
        Debug.Log("PORTFOLIO_PLAYER_SMOKE_OK: " + (shield ? "shield" : "ranged"));
        Application.Quit(0);
    }
    void Capture(string name)
    {
        int width = Screen.width, height = Screen.height;
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        var camera = FindObjectOfType<RuinsGame>().GetComponentInChildren<Camera>();
        var canvas = FindObjectOfType<PortfolioCanvasUI>().GetComponent<Canvas>();
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var previousMode = canvas.renderMode;
        var previousCamera = canvas.worldCamera;
        float previousDistance = canvas.planeDistance;
        var target = new RenderTexture(width, height, 24);
        target.Create();
        try
        {
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
        }
        finally
        {
            camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
            canvas.renderMode = previousMode; canvas.worldCamera = previousCamera;
            canvas.planeDistance = previousDistance; target.Release(); Destroy(target);
        }
        var pixels = texture.GetPixels32();
        int stride = (width * 3 + 3) & ~3;
        using (var writer = new BinaryWriter(File.Create(Path.Combine(output, name))))
        {
            writer.Write((ushort)0x4d42); writer.Write(54 + stride * height);
            writer.Write(0); writer.Write(54); writer.Write(40);
            writer.Write(width); writer.Write(height); writer.Write((ushort)1);
            writer.Write((ushort)24); writer.Write(0); writer.Write(stride * height);
            writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                { var c = pixels[y * width + x]; writer.Write(c.b); writer.Write(c.g); writer.Write(c.r); }
                for (int pad = width * 3; pad < stride; pad++) writer.Write((byte)0);
            }
        }
        Destroy(texture);
    }
}
