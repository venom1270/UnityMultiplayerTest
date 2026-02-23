using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI readyText;

    private string playerName;
    private bool ready;

    private void Start() {

    }

    public void UpdateElement(string playerName, bool ready) {
        this.playerName = playerName;
        this.ready = ready;

        playerNameText.text = playerName;
        readyText.text = ready ? "READY" : "";
    }
}
