using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using UnityEngine;
using System.Threading.Tasks;
using static GameManager;
using UnityEngine.SceneManagement;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    private Lobby hostLobby;
    private Lobby joinedLobby;
    private float heartbeatTimer;
    private float lobbyUpdateTimer;

    public event EventHandler OnPlayerAuthenticated;
    public event EventHandler OnPlayerJoined;
    public event EventHandler<OnLobbyUpdateEventArgs> OnLobbyUpdate;
    public event EventHandler OnPlayerLeft;
    public event EventHandler OnHostMigrated;

    public class OnLobbyUpdateEventArgs : EventArgs {
        public Lobby lobby;
    }

    private string playerName;

    private void Awake() {
        if (Instance != null) {
            Debug.LogError("More than one LobbyManager instance!");
        }
        Instance = this;
    }

    public async void Authenticate(string playerName) {

        this.playerName = playerName;
        Debug.Log($"Authenticating {playerName}");

        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(playerName);
        await UnityServices.InitializeAsync(initializationOptions);


        AuthenticationService.Instance.SignedIn += () => {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
            OnPlayerAuthenticated.Invoke(this, EventArgs.Empty);
            //RefreshLobbyList();
        };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void Update() {
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();
    }

    private async void HandleLobbyHeartbeat() {
        if (hostLobby != null) {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f) {
                float heartbeatTimerMax = 15;
                heartbeatTimer = heartbeatTimerMax;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }

    private async void HandleLobbyPollForUpdates() {
        if (joinedLobby != null) {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0f) {
                float lobbyUpdateTimerMax = 1.1f;
                lobbyUpdateTimer = lobbyUpdateTimerMax;

                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = lobby;

                if (hostLobby == null && lobby.HostId == AuthenticationService.Instance.PlayerId) {
                    // Host migrated
                    hostLobby = lobby;
                    OnHostMigrated.Invoke(this, EventArgs.Empty);
                }

                if (lobby.Data.ContainsKey("GameCode")) {
                    StartGameClient(lobby.Data["GameCode"].Value);
                    return;
                }

                OnLobbyUpdate.Invoke(this, new OnLobbyUpdateEventArgs {
                    lobby = lobby,
                });
            }
        }
    }

    public async void JoinLobbyByCode(string lobbyCode) {
        try {
            JoinLobbyByCodeOptions joinLobbyByCodeOptions = new JoinLobbyByCodeOptions {
                Player = GetPlayer()
            };

            Debug.Log($"Trying to join lobby '{lobbyCode}'");
            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinLobbyByCodeOptions);

            Debug.Log("Joined lobby with code " + lobbyCode);

            OnPlayerJoined.Invoke(this, EventArgs.Empty);
            //PrintPlayers(joinedLobby);
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void JoinLobbyById(string lobbyId) {
        try {
            JoinLobbyByIdOptions joinLobbyByidOptions = new JoinLobbyByIdOptions {
                Player = GetPlayer()
            };

            Debug.Log($"Trying to join lobby '{lobbyId}'");
            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinLobbyByidOptions);
            this.joinedLobby = joinedLobby;

            Debug.Log("Joined lobby with ID " + lobbyId);

            OnPlayerJoined.Invoke(this, EventArgs.Empty);
            //PrintPlayers(joinedLobby);
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void CreateLobby(string lobbyName, int maxPlayers) {
        try {
            // OPTIONAL OPTIONS - private to join by code
            CreateLobbyOptions createLobbyOptions = new CreateLobbyOptions {
                IsPrivate = false,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject> {
                    { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, "TicTacToe") }
                }
            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, createLobbyOptions);

            hostLobby = lobby;
            joinedLobby = hostLobby;

            //PrintPlayers(lobby);

            Debug.Log("Created Lobby! " + lobby.Name + " " + lobby.MaxPlayers + " " + lobby.Id + " " + lobby.LobbyCode);

            OnPlayerJoined.Invoke(this, EventArgs.Empty);
            //ListLobbies();
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void UpdatePlayerReady(bool ready) {
        try {
            await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId, new UpdatePlayerOptions {
                Data = new Dictionary<string, PlayerDataObject> {
                    { "Ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, ready ? "1" : "0") },
                    //{ "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) }
                }
            });
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    private async Task UpdateLobbyGameCode(string gameCode) {
        try {
            await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions {
                Data = new Dictionary<string, DataObject> {
                    { "GameCode", new DataObject(DataObject.VisibilityOptions.Member, gameCode) },
                }
            });
            Debug.Log("Lobby game code updated! " + gameCode);
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void LeaveLobby() {
        try {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);

            joinedLobby = null;
            hostLobby = null;

            OnPlayerLeft.Invoke(this, EventArgs.Empty);
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }


    public async Task<List<Lobby>> RefreshLobbies() {
        List<Lobby> lobbies = new List<Lobby>();

        try {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions {
                Count = 25,
                Filters = new List<QueryFilter> {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                },
                Order = new List<QueryOrder> {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created),
                }
            };
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            Debug.Log("Lobbies found: " + queryResponse.Results.Count);
            foreach (Lobby lobby in queryResponse.Results) {
                Debug.Log($"{lobby.Name} {lobby.MaxPlayers} {lobby.Data["GameMode"].Value} | {lobby.LobbyCode}");
            }

            lobbies = queryResponse.Results;
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }

        return lobbies;
    }


    private Player GetPlayer() {
        return new Player {
            Data = new Dictionary<string, PlayerDataObject> {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
                { "Ready", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, "0") }
            }
        };
    }



    public async void StartGameHost() {
        // Check if enough players
        if (joinedLobby.Players.Count != joinedLobby.MaxPlayers) {
            Debug.LogWarning($"Not enough players! ({joinedLobby.Players.Count}/{joinedLobby.MaxPlayers})");
            return;
        }

        // Check all players are ready
        bool allReady = true;
        foreach (Player player in joinedLobby.Players) {
            if (player.Data["Ready"].Value == "0") {
                allReady = false;
                Debug.LogWarning($"Player {player.Data["PlayerName"].Value} not ready!");
            }
        }

        if (!allReady) {
            return;
        }


        // Create relay session and save game code
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(joinedLobby.MaxPlayers);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        await UpdateLobbyGameCode(joinCode);

        SessionData.Instance.isHost = hostLobby != null;
        if (SessionData.Instance.isHost) {
            SessionData.Instance.hostAllocation = allocation;
        }
        SessionData.Instance.gameCode = joinCode;
        SessionData.Instance.playerName = playerName;

        SceneManager.LoadScene("GameScene");
    }

    public void StartGameClient(string gameCode) {
        SessionData.Instance.gameCode = gameCode;
        SessionData.Instance.playerName = playerName;
        SessionData.Instance.isHost = false;
        SceneManager.LoadScene("GameScene");
    }

    public bool IsHost() {
        return hostLobby != null;
    }
}
