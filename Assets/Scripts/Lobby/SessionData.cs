using Unity.Services.Relay.Models;
using UnityEngine;

public class SessionData : MonoBehaviour
{
    public static SessionData Instance;

    public bool isHost;
    public string gameCode;
    public string playerName;
    public Allocation hostAllocation;

    private void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }
}
