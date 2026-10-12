using Portfolio.Domain;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Portfolio.UI
{
    // Screen coordinator: receives data and sends requests. It cannot modify gameplay state.
    public sealed class PortfolioCanvasUI : MonoBehaviour
    {
        public GameObject hud,modal,titleScreen,pauseScreen,upgradeScreen,resultScreen,toastPanel;
        public HealthBarView health;
        public DashCooldownView dash;
        public RoomObjectiveView objective;
        public EliteStatusView elite;
        public UpgradeCardView[] cards;
        public Button startButton,resumeButton,restartPauseButton,restartResultButton,pauseButton;
        public Image buffIcon;
        public Text buffName,resultTitle,resultStats,resultUpgrade,toast,chargeStatus;
        public Image[] enemyBars;
        IGameUiCommands commands;
        UiMode previous=(UiMode)(-1);
        int lastUpgrade=-1;
        int renderedMask=-1;
        float toastRemaining;
        Font runtimeFont;
        bool ownsRuntimeFont;
        CharacterChoiceView characterChoice;
        MapNavigationView navigation;
        AimCursorView aimCursor;
        Text controls;
        LoadoutStripView loadoutStrip;
        Image dashArt;
        bool styled;
        bool renderedShield,chargeRendered;
        int chargePercent=-1,chargeTenth=-1;bool chargeWasActive,chargeWasFull;
        string previousAbilityHint;
        public bool BlocksPointer=>EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject();
        public void Bind(IGameUiCommands requests,HealthModel source)
        {
            commands=requests;health.Bind(source);
            if(runtimeFont==null){runtimeFont=UiTypography.Body;if(runtimeFont==null){runtimeFont=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},24);ownsRuntimeFont=true;}}
            foreach(var label in GetComponentsInChildren<Text>(true))label.font=runtimeFont;
            if(navigation==null)
            {
                navigation=new MapNavigationView(hud.transform,runtimeFont);
                controls=hud.transform.Find("Controls").GetComponent<Text>();
                var plate=new GameObject("Control hint backing",typeof(RectTransform),typeof(Image));plate.transform.SetParent(hud.transform,false);plate.transform.SetAsFirstSibling();
                var rt=(RectTransform)plate.transform;rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(280,-646);rt.sizeDelta=new Vector2(720,65);
                plate.GetComponent<Image>().color=new Color(.02f,.055f,.065f,.85f);plate.GetComponent<Image>().raycastTarget=false;
            }
            if(characterChoice==null && requests is ICharacterUiCommands characterCommands)characterChoice=new CharacterChoiceView(this,characterCommands);
            if(!styled){UiPresentationStyle.Apply(this);styled=true;}
            loadoutStrip=hud.GetComponentInChildren<LoadoutStripView>(true);
            dashArt=dash.transform.Find("Dash Icon").GetComponent<Image>();
            if(aimCursor==null){aimCursor=new GameObject("Combat aim cursor",typeof(RectTransform)).AddComponent<AimCursorView>();aimCursor.Bind(transform);}
            startButton.onClick.RemoveAllListeners();startButton.onClick.AddListener(()=>commands.RequestStart());
            resumeButton.onClick.RemoveAllListeners();resumeButton.onClick.AddListener(()=>commands.RequestPause(false));
            restartPauseButton.onClick.RemoveAllListeners();restartPauseButton.onClick.AddListener(()=>commands.RequestStart());
            restartResultButton.onClick.RemoveAllListeners();restartResultButton.onClick.AddListener(()=>commands.RequestStart());
            pauseButton.onClick.RemoveAllListeners();pauseButton.onClick.AddListener(()=>commands.RequestPause(true));
            foreach(var card in cards){var item=card;item.button.onClick.RemoveAllListeners();item.button.onClick.AddListener(()=>commands.RequestUpgrade(item.ChoiceId));}
        }
        public void Render(GameUiSnapshot state,int[] choices)
        {
            aimCursor?.Present(state.mode==UiMode.Playing,state.shieldHero,BlocksPointer,state.shots,state.charging,state.chargeFraction);
            characterChoice?.Render(state);
            navigation?.Render(state);
            loadoutStrip?.Render(state.upgradeMask,state.shieldHero);
            if(dashArt!=null && (!chargeRendered || renderedShield!=state.shieldHero))dashArt.sprite=SkillIconCatalog.Dash(state.shieldHero);
            if(controls!=null)controls.text=state.shieldHero?"WASD 移动 · 左键挥锤 · 右键格挡 · 空格盾冲 · E 交互 · ESC 暂停":"WASD 移动 · 左键射击 · 右键蓄力 · 空格冲刺 · E 交互 · ESC 暂停";
            bool overlay=state.mode!=UiMode.Playing && state.mode!=UiMode.Dying;
            hud.SetActive(state.mode!=UiMode.Title);modal.SetActive(overlay);
            titleScreen.SetActive(state.mode==UiMode.Title);pauseScreen.SetActive(state.mode==UiMode.Paused);
            upgradeScreen.SetActive(state.mode==UiMode.Upgrade);resultScreen.SetActive(state.mode==UiMode.Result);
            dash.Render(state.dashRemaining,state.dashDuration);objective.Render(state);elite.Render(state);
            if(chargeStatus!=null)
            {
                int percent=Mathf.RoundToInt(state.chargeFraction*100),tenth=Mathf.CeilToInt(state.chargeCooldown*10);
                if(!chargeRendered || renderedShield!=state.shieldHero || chargePercent!=percent || chargeTenth!=tenth || chargeWasActive!=state.charging || chargeWasFull!=(state.chargeFraction>=1) || previousAbilityHint!=state.abilityHint)
                {
                chargeStatus.text=state.charging?(state.chargeFraction>=1?"蓄力完成 · 松开右键释放":"蓄力中 · "+Mathf.RoundToInt(state.chargeFraction*100)+"%"):
                    state.chargeCooldown>0?"蓄力射击 · 冷却 "+(tenth*.1f).ToString("F1")+"s":"按住右键蓄力 · 松开释放 · 冲刺可取消";
                chargeStatus.color=state.charging?new Color(1,.8f,.3f):new Color(.7f,.82f,.85f);
                if(state.shieldHero)chargeStatus.text=state.abilityHint;
                chargePercent=percent;chargeTenth=tenth;chargeWasActive=state.charging;chargeWasFull=state.chargeFraction>=1;previousAbilityHint=state.abilityHint;chargeRendered=true;
                }
            }
            pauseButton.interactable=state.mode==UiMode.Playing;
            buffName.text=state.routeName??UiText.UpgradeName(state.upgrade);buffName.fontSize=state.upgradeMask>0?14:20;
            if(lastUpgrade!=state.upgrade || renderedShield!=state.shieldHero)buffIcon.sprite=UiText.UpgradeIcon(state.upgrade,state.shieldHero);buffIcon.enabled=state.upgrade>=0;
            if(state.upgrade>=0 && lastUpgrade!=state.upgrade){toast.text="已获得 · "+UiText.UpgradeName(state.upgrade,state.shieldHero);toastRemaining=2.2f;}
            if(state.upgrade<0)toastRemaining=0;
            lastUpgrade=state.upgrade;
            if(state.mode==UiMode.Playing)toastRemaining-=Time.unscaledDeltaTime;
            toastPanel.SetActive(toastRemaining>0 && state.mode==UiMode.Playing);
            if(state.mode==UiMode.Upgrade)
            {
                var heading=upgradeScreen.transform.Find("Title").GetComponent<Text>();
                var hint=upgradeScreen.transform.Find("Hint").GetComponent<Text>();
                heading.text=state.rewardTitle??"选择强化";hint.text=state.rewardHint??"保留已有强化";
                for(int i=0;i<cards.Length;i++)if(previous!=state.mode || renderedMask!=state.upgradeMask || renderedShield!=state.shieldHero || cards[i].ChoiceId!=choices[i])cards[i].Configure(choices[i],state.upgradeMask,state.shieldHero);
            }
            if(state.mode==UiMode.Result)
            {
                resultTitle.text=state.won?"遗迹已稳定":"探索暂告一段落";
                resultStats.text="进度  "+state.room+" / 3      击破  "+state.kills+"\n用时  "+state.elapsed.ToString("F1")+" 秒      "+(state.shieldHero?"挥锤命中率  ":"命中率  ")+(state.shots==0?0:Mathf.RoundToInt(state.hits*100f/state.shots))+"%";
                resultUpgrade.text="本局强化  /  "+(state.buildName??UiText.UpgradeName(state.upgrade));
                resultUpgrade.resizeTextForBestFit=true;resultUpgrade.resizeTextMinSize=12;resultUpgrade.resizeTextMaxSize=20;
            }
            if(previous!=state.mode && EventSystem.current!=null)
            {
                GameObject selected=state.mode==UiMode.Title?startButton.gameObject:state.mode==UiMode.Paused?resumeButton.gameObject:state.mode==UiMode.Result?restartResultButton.gameObject:null;
                EventSystem.current.SetSelectedGameObject(selected);
            }
            previous=state.mode;renderedMask=state.upgradeMask;renderedShield=state.shieldHero;
        }
        public void AvoidPlayer(Rect screenBounds)=>navigation?.AvoidPlayer(screenBounds,Time.unscaledDeltaTime);
        public void EnemyBar(int index,Vector2 point,float fraction)
        {
            if(index>=enemyBars.Length)return;
            var image=enemyBars[index];image.transform.parent.gameObject.SetActive(true);
            var rt=(RectTransform)image.transform.parent;
            var rootCanvas=GetComponent<Canvas>();
            var eventCamera=rootCanvas.renderMode==RenderMode.ScreenSpaceOverlay?null:rootCanvas.worldCamera;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rt.parent,point,eventCamera,out var local))
                rt.localPosition=new Vector3(local.x-25,local.y,0);
            image.fillAmount=Mathf.Clamp01(fraction);
        }
        public void HideEnemyBarsFrom(int index){for(int i=index;i<enemyBars.Length;i++)enemyBars[i].transform.parent.gameObject.SetActive(false);}
        void OnDestroy(){if(health!=null)health.Unbind();if(ownsRuntimeFont && runtimeFont!=null)Destroy(runtimeFont);commands=null;}
    }
}
