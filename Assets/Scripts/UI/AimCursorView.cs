using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.UI
{
    // Screen-space presentation only. Never modifies aim, target selection or damage.
    public sealed class AimCursorView : MonoBehaviour
    {
        AimReticleGraphic image; RectTransform root,rect; Canvas canvas;
        bool playing,shield,blocked,ownsCursor,previousCursorVisible;
        float kick;
        int previousShots=-1;
        bool charging;
        float charge;
        public void Bind(Transform parent)
        {
            transform.SetParent(parent,false);rect=(RectTransform)transform;
            image=gameObject.AddComponent<AimReticleGraphic>();image.raycastTarget=false;
            root=(RectTransform)parent;canvas=parent.GetComponent<Canvas>();rect.sizeDelta=Vector2.one*32;
            rect.anchorMin=rect.anchorMax=rect.pivot=Vector2.one*.5f;image.enabled=false;
        }
        public void Present(bool inCombat,bool melee,bool pointerBlocked,int shots=-1,bool isCharging=false,float fraction=0)
        {
            if(inCombat && playing && !pointerBlocked && previousShots>=0 && shots>previousShots)kick=1;
            if(!inCombat || melee!=shield)kick=0;
            previousShots=shots;playing=inCombat;shield=melee;blocked=pointerBlocked;
            charging=isCharging && !melee;charge=fraction;
        }
        void LateUpdate()
        {RenderPointer(Input.mousePosition,Application.isFocused,false,Time.unscaledDeltaTime);}
        public void RenderPointer(Vector2 screenPoint,bool focused,bool fired,float dt)
        {
            bool inside=screenPoint.x>=0 && screenPoint.y>=0 && screenPoint.x<Screen.width && screenPoint.y<Screen.height;
            bool visible=playing && !blocked && inside && focused;
            image.enabled=visible;
            if(!visible){kick=0;Restore();return;}
            if(!ownsCursor){previousCursorVisible=Cursor.visible;ownsCursor=true;}Cursor.visible=false;
            if(fired)kick=1;kick=Mathf.Max(0,kick-dt*7);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screenPoint,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var local);
            rect.anchoredPosition=local;rect.localScale=Vector3.one;rect.localRotation=Quaternion.identity;
            image.SetState(shield,kick,charging,charge);
            transform.SetAsLastSibling();
        }
        void Restore(){if(ownsCursor){Cursor.visible=previousCursorVisible;ownsCursor=false;}}
        void OnDisable(){Restore();if(image!=null)image.enabled=false;}
        void OnDestroy(){Restore();}
        void OnApplicationFocus(bool focused){if(!focused)Restore();}
    }
}
