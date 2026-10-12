using UnityEngine;
using UnityEngine.UI;

namespace Portfolio.UI
{
    public sealed class RoomObjectiveView : MonoBehaviour
    {
        public Text roomLabel,objective,prompt;
        public GameObject promptPanel;
        bool rendered;GameUiSnapshot previous;
        public void Render(GameUiSnapshot state)
        {
            if(!rendered || previous.room!=state.room)roomLabel.text="遗迹回廊  /  "+state.room.ToString("00")+" — 03";
            if(!rendered || previous.room!=state.room || previous.wave!=state.wave || previous.waveTotal!=state.waveTotal || previous.remaining!=state.remaining || previous.pending!=state.pending || previous.cleared!=state.cleared || previous.canInteract!=state.canInteract)objective.text=UiText.Objective(state);
            promptPanel.SetActive(state.canInteract && state.mode==UiMode.Playing);
            prompt.text=state.room==3?"E  ·  离开遗迹":"E  ·  进入传送门";
            previous=state;rendered=true;
        }
    }
}
