using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using Unity.Services.Relay;

public class NetworkManagerInitializer : MonoBehaviour
{
    async void Start()
    {

        if (SessionData.Instance == null) {
            Debug.LogError("NO SESSION DATA!");
            return;
        }

        if (SessionData.Instance.isHost) {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetHostRelayData(
                SessionData.Instance.hostAllocation.RelayServer.IpV4,
                (ushort)SessionData.Instance.hostAllocation.RelayServer.Port,
                SessionData.Instance.hostAllocation.AllocationIdBytes,
                SessionData.Instance.hostAllocation.Key,
                SessionData.Instance.hostAllocation.ConnectionData
            );
            NetworkManager.Singleton.StartHost();
        } else {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(SessionData.Instance.gameCode);
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetClientRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData
            );
            NetworkManager.Singleton.StartClient();
        }

        
    }

}
