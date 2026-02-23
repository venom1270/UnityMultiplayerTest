using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyItemUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI lobbyPlayersText;
    [SerializeField] private Button joinLobbyButton;

    private string lobbyId;
    private string lobbyName;
    private int currentPlayers;
    private int maxPlayers;

    private void Start() {
        joinLobbyButton.onClick.AddListener(() => {
            Debug.Log($"Lobby ID: {lobbyId}");
            LobbyManager.Instance.JoinLobbyById(lobbyId);
        });
    }

    public void UpdateElement(string lobbyId, string lobbyName, int currentPlayers, int maxPlayers) {
        Debug.Log("Updating lobby id: " + lobbyId);
        this.lobbyId = lobbyId;
        this.lobbyName = lobbyName;
        this.currentPlayers = currentPlayers;
        this.maxPlayers = maxPlayers;

        lobbyNameText.text = lobbyName;
        lobbyPlayersText.text = currentPlayers + "/" + maxPlayers;
    }
}
