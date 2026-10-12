using UnityEngine;

public sealed partial class RuinsGame
{
    bool bufferedDash,bufferedPress,bufferedRelease;
    Vector2 bufferedMove,bufferedAim;
    void ClearBufferedInput(){bufferedDash=bufferedPress=bufferedRelease=false;}
    void CaptureBufferedInput()
    {
        // Capture edges before hit-stop; consume once when simulation resumes.
        if(Input.GetKeyDown(KeyCode.Space))
        {
            bufferedDash=true;
            bufferedMove=new Vector2((Input.GetKey(KeyCode.D)?1:0)-(Input.GetKey(KeyCode.A)?1:0),
                (Input.GetKey(KeyCode.S)?1:0)-(Input.GetKey(KeyCode.W)?1:0)).normalized;
        }
        if(Input.GetMouseButtonDown(1) && (canvasUI==null || !canvasUI.BlocksPointer))bufferedPress=true;
        if(Input.GetMouseButtonUp(1))
        {
            bufferedRelease=true;var p=gameCamera.ScreenToWorldPoint(Input.mousePosition);
            bufferedAim=new Vector2(p.x,720-p.y);
        }
    }
}
