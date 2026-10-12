using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.UI
{
    // Shared canvas/typography setup. Layout and art have a single owner: UiArtSkin.
    public static class UiPresentationStyle
    {
        public static void Apply(PortfolioCanvasUI ui)
        {
            foreach(var rect in new[]{(RectTransform)ui.hud.transform.parent,(RectTransform)ui.hud.transform})
            {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=Vector2.zero;}
            foreach(var label in ui.GetComponentsInChildren<Text>(true))
            {
                if(UiTypography.Body!=null)label.font=label.fontSize>=24?UiTypography.Heading:UiTypography.Body;
                label.lineSpacing=1.2f;label.supportRichText=false;
                var shadow=label.GetComponent<Shadow>();if(shadow!=null)shadow.enabled=false;
            }
            UiArtSkin.Apply(ui);
        }
    }
}
