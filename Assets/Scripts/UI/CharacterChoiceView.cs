using UnityEngine;
using UnityEngine.UI;
namespace Portfolio.UI
{
    // Character selection is a view adapter; it issues commands, never mutates game state.
    public sealed class CharacterChoiceView
    {
        readonly Button[] buttons=new Button[4];
        readonly Text titleGuide,titleDescription,heroName;
        readonly Image portrait;
        readonly Sprite rangedPortrait,shieldPortrait;
        readonly Image heroIllustration;
        public CharacterChoiceView(PortfolioCanvasUI view,ICharacterUiCommands commands)
        {
            titleGuide=view.titleScreen.transform.Find("Guide").GetComponent<Text>();
            titleDescription=view.titleScreen.transform.Find("Description").GetComponent<Text>();
            heroName=view.health.transform.Find("Name").GetComponent<Text>();
            portrait=view.health.transform.Find("Portrait").GetComponent<Image>();
            rangedPortrait=portrait.sprite;shieldPortrait=Resources.Load<Sprite>("Art/shield/down_idle_0");
            for(int i=0;i<4;i++)
            {
                bool shield=i%2==1;bool result=i>=2;
                var parent=result?view.resultScreen.transform:view.titleScreen.transform;
                var button=Object.Instantiate(view.startButton,parent);
                button.name=shield?"Select Shield":"Select Ranged";
                var rt=(RectTransform)button.transform;
                rt.anchoredPosition=new Vector2(shield?375:65,result?-306:-236);
                rt.sizeDelta=new Vector2(280,result?32:44);
                ((RectTransform)button.transform.Find("Gold Edge")).sizeDelta=new Vector2(280,2);
                button.onClick.RemoveAllListeners();button.onClick.AddListener(()=>commands.RequestCharacter(shield));
                var text=button.GetComponentInChildren<Text>();text.fontSize=result?15:18;
                var textRect=(RectTransform)text.transform;
                textRect.sizeDelta=new Vector2(270,result?26:38);
                buttons[i]=button;
            }
            ((RectTransform)titleGuide.transform).anchoredPosition=new Vector2(50,-285);
            titleGuide.fontSize=16;
            ((RectTransform)view.toastPanel.transform).anchoredPosition=new Vector2(420,-598);
            // Created by the skin before binding when the prefab is baked; runtime fallback is styled afterwards.
            var art=view.titleScreen.transform.Find("Hero illustration");heroIllustration=art==null?null:art.GetComponent<Image>();
        }
        public void Render(GameUiSnapshot state)
        {
            if(rendered && previousShield==state.shieldHero && previousName==state.characterName)return;
            rendered=true;previousShield=state.shieldHero;previousName=state.characterName;
            for(int i=0;i<buttons.Length;i++)
            {
                bool selected=state.shieldHero==(i%2==1);
                buttons[i].GetComponentInChildren<Text>().text=(selected?"✓ ":"")+(i%2==1?"盾卫 · 近战反击":"修复师 · 远程蓄力");
                buttons[i].image.color=selected?Color.white:new Color(.6f,.7f,.75f);
            }
            heroName.text=state.characterName??"遗迹修复师";heroName.fontSize=16;
            portrait.sprite=state.shieldHero?shieldPortrait:rangedPortrait;
            var illustration=heroIllustration!=null?heroIllustration:titleDescription.transform.parent.Find("Hero illustration")?.GetComponent<Image>();
            if(illustration!=null)illustration.sprite=Resources.Load<Sprite>(state.shieldHero?"UI/Skin/hero_shield_v2":"UI/Skin/hero_ranged_v2");
            titleGuide.text=state.shieldHero?"WASD 移动 · 左键挥锤 · 右键格挡 · 空格盾冲":"WASD 移动 · 左键射击 · 右键蓄力 · 空格冲刺";
            titleDescription.text=state.shieldHero?"150 生命 · 正面减伤 / 精准反击\n4秒未受伤后有限回血 · 精英冲锋不可格挡":"净化三个房间，选择核心强化。\n利用地形，击败遗迹深处的石甲守卫。";
        }
        bool rendered,previousShield;string previousName;
    }
}
