using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIManager : MonoBehaviour
{
    [SerializeField] private GameObject authenticateGameObject;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button authenticateButton;

    [SerializeField] private GameObject lobbySearchGameObject;
    [SerializeField] private GameObject lobbyItemPrefab;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TMP_InputField lobbyNameInput;
    [SerializeField] private Button createButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private GameObject lobbyListScrollView;

    [SerializeField] private GameObject lobbyGameObject;
    [SerializeField] private GameObject playerItemPrefab;
    [SerializeField] private TextMeshProUGUI playerNameText2;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private GameObject playerListScrollView;


    private void Start() {
        authenticateGameObject.SetActive(true);
        lobbySearchGameObject.SetActive(false);
        lobbyGameObject.SetActive(false);

        authenticateButton.onClick.AddListener(() => {
            LobbyManager.Instance.Authenticate(playerNameInput.text);
        });

        createButton.onClick.AddListener(() => {
            if (lobbyNameInput.text.Length == 0) return;
            LobbyManager.Instance.CreateLobby(lobbyNameInput.text, 2);
        });

        refreshButton.onClick.AddListener(async () => {
            List<Lobby> lobbies = await LobbyManager.Instance.RefreshLobbies();

            ClearLobbyListView();
            foreach (Lobby lobby in lobbies) {
                InstantiateLobbyItem(lobby);
            }

        });

        startGameButton.onClick.AddListener(() => {
            LobbyManager.Instance.StartGameHost();
        });

        readyButton.onClick.AddListener(() => {
            LobbyManager.Instance.UpdatePlayerReady(true);
        });

        leaveButton.onClick.AddListener(() => {
            LobbyManager.Instance.LeaveLobby();
        });

        LobbyManager.Instance.OnPlayerAuthenticated += LobbyManager_OnPlayerAuthenticated;
        LobbyManager.Instance.OnPlayerJoined += LobbyManager_OnPlayerJoined;
        LobbyManager.Instance.OnLobbyUpdate += LobbyManager_OnLobbyUpdate;
        LobbyManager.Instance.OnPlayerLeft += LobbyManager_OnPlayerLeft;
        LobbyManager.Instance.OnHostMigrated += LobbyManager_OnHostMigrated;
    }

    private void LobbyManager_OnHostMigrated(object sender, System.EventArgs e) {
        UpdateStartGameButton();
    }

    private void LobbyManager_OnPlayerLeft(object sender, System.EventArgs e) {
        this.lobbySearchGameObject.SetActive(true);
        this.lobbyGameObject.SetActive(false);
    }

    private void LobbyManager_OnLobbyUpdate(object sender, LobbyManager.OnLobbyUpdateEventArgs e) {
        UpdatePlayerList(e.lobby.Players);
    }

    private void LobbyManager_OnPlayerJoined(object sender, System.EventArgs e) {
        this.lobbySearchGameObject.SetActive(false);
        this.lobbyGameObject.SetActive(true);
        UpdateStartGameButton();
        this.playerNameText2.text = playerNameText.text; // TODO to naj bo en text...
    }

    private void LobbyManager_OnPlayerAuthenticated(object sender, System.EventArgs e) {
        authenticateGameObject.SetActive(false);
        lobbySearchGameObject.SetActive(true);
        playerNameText.text = playerNameInput.text;
    }

    private void UpdateStartGameButton() {
        this.startGameButton.gameObject.SetActive(LobbyManager.Instance.IsHost());
    }

    private void ClearLobbyListView() {
        foreach (Transform child in lobbyListScrollView.transform) {
            Destroy(child.gameObject);
        }
    }

    private void InstantiateLobbyItem(Lobby lobby) {
        GameObject lobbyItem = Instantiate(lobbyItemPrefab);
        lobbyItem.transform.SetParent(lobbyListScrollView.transform, false);

        lobbyItem.GetComponent<LobbyItemUI>().UpdateElement(lobby.Id, lobby.Name, lobby.Players.Count, lobby.MaxPlayers);
    }

    private void UpdatePlayerList(List<Player> players) {
        ClearPlayerListView();
        foreach (Player player in players) {
            InstantiatePlayerItem(player);
        }
    }

    private void ClearPlayerListView() {
        foreach (Transform child in playerListScrollView.transform) {
            Destroy(child.gameObject);
        }
    }

    private void InstantiatePlayerItem(Player player) {
        GameObject playerItem = Instantiate(playerItemPrefab);
        playerItem.transform.SetParent(playerListScrollView.transform, false);

        // TODO
        playerItem.GetComponent<PlayerItemUI>().UpdateElement(player.Data["PlayerName"].Value, player.Data["Ready"].Value != "0");
    }
}
