using System;
using System.IO;
using System.Reflection;
using Portfolio.UI;
using Portfolio.Domain;
using UnityEngine;
using UnityEngine.UI;

public static class PortfolioUiChecks
{
    static void Check(bool ok,string reason){if(!ok)throw new Exception("UI: "+reason);}
    public static void Run(RuinsGame game)
    {
        var ui=UnityEngine.Object.FindObjectOfType<PortfolioCanvasUI>();
        Check(ui!=null,"Canvas missing");
        ui.startButton.onClick.Invoke();Check(game.CaptureUiState().mode==UiMode.Upgrade,"start core choice");
        ui.cards[0].button.onClick.Invoke();Check(game.CaptureUiState().mode==UiMode.Playing,"core choice button");
        ui.pauseButton.onClick.Invoke();Check(game.CaptureUiState().mode==UiMode.Paused,"pause button");
        var frozen=game.CaptureUiState();typeof(RuinsGame).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
        Check(game.CaptureUiState().elapsed==frozen.elapsed && game.CaptureUiState().dashRemaining==frozen.dashRemaining,"pause advances gameplay clock");
        ui.resumeButton.onClick.Invoke();Check(game.CaptureUiState().mode==UiMode.Playing,"resume button");
        var model=(HealthModel)typeof(RuinsGame).GetField("vitality",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game);
        model.TakeDamage(24);Check(Mathf.Abs(ui.health.fill.fillAmount-.76f)<.001f,"health event not reflected");
        ui.pauseButton.onClick.Invoke();ui.restartPauseButton.onClick.Invoke();Check(model.Current==100 && ui.health.fill.fillAmount==1,"restart HUD");
        ((IGameUiCommands)game).RequestUpgrade(99);Check(game.CaptureUiState().upgrade==-1,"invalid upgrade accepted");
        ui.cards[0].button.onClick.Invoke();
        var state=game.CaptureUiState();state.dashRemaining=.55f;state.dashDuration=1.1f;ui.Render(state,new[]{0,1,2});
        Check(Mathf.Abs(ui.dash.mask.fillAmount-.5f)<.001f,"cooldown mask");
        state.cleared=true;state.canInteract=false;ui.Render(state,new[]{0,1,2});Check(!ui.objective.promptPanel.activeSelf,"distant portal prompt");
        state.canInteract=true;ui.Render(state,new[]{0,1,2});Check(ui.objective.promptPanel.activeSelf,"near portal prompt");
        state.mode=UiMode.Upgrade;ui.Render(state,new[]{0,1,2});Check(ui.cards[1].ChoiceId==1 && ui.cards[1].description.text.Contains("2.2"),"upgrade card data");
        state.mode=UiMode.Playing;state.room=3;state.eliteHealth=180;state.eliteMaximum=360;state.eliteStunned=true;ui.Render(state,new[]{0,1,2});
        Check(ui.elite.gameObject.activeSelf && ui.elite.status.text.Contains("破绽"),"elite weakness");
        state.mode=UiMode.Result;state.won=true;state.upgrade=2;state.buildName="穿透棱镜";ui.Render(state,new[]{0,1,2});
        Check(ui.resultTitle.text=="遗迹已稳定" && ui.resultUpgrade.text.Contains("穿透"),"result summary");
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"-ui-captures")>=0)CaptureFixtures(game,ui);
        ui.Render(game.CaptureUiState(),new[]{0,1,2});
        // Batch mode has no Game view repaint. Render once before testing graphic depth/raycast.
        var canvas=ui.GetComponent<Canvas>();var camera=game.GetComponentInChildren<Camera>();
        var savedMode=canvas.renderMode;var savedCamera=canvas.worldCamera;var savedTarget=camera.targetTexture;
        var target=new RenderTexture(1280,720,24);target.Create();camera.targetTexture=target;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        Canvas.ForceUpdateCanvases();
        camera.Render();
        var buttonRect=(RectTransform)ui.pauseButton.transform;
        var point=RectTransformUtility.WorldToScreenPoint(camera,buttonRect.TransformPoint(buttonRect.rect.center));
        var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=point};
        var raycasts=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer,raycasts);
        bool buttonHit=raycasts.Exists(r=>r.gameObject==ui.pauseButton.gameObject);
        canvas.renderMode=savedMode;canvas.worldCamera=savedCamera;camera.targetTexture=savedTarget;
        target.Release();UnityEngine.Object.DestroyImmediate(target);
        Check(buttonHit,"pause button raycast");
        Debug.Log("PORTFOLIO_UI_CHECKS_OK: buttons, health events, reset, cooldown mask, proximity prompt, cards, elite status, results.");
    }
    static void CaptureFixtures(RuinsGame game,PortfolioCanvasUI ui)
    {
        Directory.CreateDirectory("UiReview");
        var camera=game.GetComponentInChildren<Camera>();var canvas=ui.GetComponent<Canvas>();
        var rt=new RenderTexture(1280,720,24);rt.Create();
        var savedTarget=camera.targetTexture;var savedMode=canvas.renderMode;
        camera.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        camera.orthographicSize=Portfolio.Domain.ArenaLayout.Height/2;
        Canvas.ForceUpdateCanvases();
        typeof(RuinsGame).GetMethod("RefreshUi",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(game,null);
        var state=game.CaptureUiState();state.upgradeMask=1;state.buildName="灼烧核心";state.dashRemaining=.55f;state.dashDuration=1.1f;
        foreach(var mode in new[]{UiMode.Title,UiMode.Playing,UiMode.Upgrade,UiMode.Paused,UiMode.Result})
        {
            state.mode=mode;state.won=true;state.kills=16;state.elapsed=83.2f;
            state.upgradeMask=mode==UiMode.Upgrade?0:1;
            ui.Render(state,new[]{0,1,5});
            Canvas.ForceUpdateCanvases();camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=rt;
            var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
            var pixels=texture.GetPixels32();
            using(var file=new BinaryWriter(File.Create("UiReview/"+mode+".bmp")))
            {
                int bytes=1280*720*3;file.Write((byte)'B');file.Write((byte)'M');file.Write(54+bytes);file.Write(0);file.Write(54);file.Write(40);file.Write(1280);file.Write(720);file.Write((short)1);file.Write((short)24);file.Write(0);file.Write(bytes);file.Write(0);file.Write(0);file.Write(0);file.Write(0);
                foreach(var p in pixels){file.Write(p.b);file.Write(p.g);file.Write(p.r);}
            }
            UnityEngine.Object.DestroyImmediate(texture);RenderTexture.active=previous;
        }
        canvas.renderMode=savedMode;canvas.worldCamera=null;camera.targetTexture=savedTarget;rt.Release();UnityEngine.Object.DestroyImmediate(rt);
        Debug.Log("PORTFOLIO_UI_FIXTURES_CAPTURED: UiReview/*.bmp; display snapshots, not a live playthrough.");
    }
}
