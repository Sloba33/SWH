using System.Collections;
using Coherence.Toolkit;
using Coherence.Connection;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiplayerLobbyHandler : MonoBehaviour
{
    private CoherenceBridge _coherenceBridge;

    private void Awake()
    {
        _coherenceBridge = FindFirstObjectByType<CoherenceBridge>();
        _coherenceBridge.onConnected.AddListener(OnBridgeConnection);
        _coherenceBridge.onDisconnected.AddListener(OnBridgeDisconnection);
        
        // Raised whenever a new connection is made (including the local one).
        _coherenceBridge.ClientConnections.OnCreated += connection =>
        {
            Debug.Log($"Connection #{connection.ClientId} " +
                      $"of type {connection.Type} created.");
        };

        // Raised whenever a connection is destroyed.
        _coherenceBridge.ClientConnections.OnDestroyed += connection =>
        {
            Debug.Log($"Connection #{connection.ClientId} " +
                      $"of type {connection.Type} destroyed.");
        };

        // Raised when all initial connections have been synced.
        _coherenceBridge.ClientConnections.OnSynced += connectionManager =>
        {
            Debug.Log($"ClientConnections are now ready to be used.");
        };
    }

    private void OnBridgeConnection(CoherenceBridge arg0)
    {
        StartCoroutine(LoadNextScene(51));
    }

    private void OnBridgeDisconnection(CoherenceBridge arg0, ConnectionCloseReason arg1)
    {
        StartCoroutine(LoadNextScene(50));
    }
    
    private IEnumerator LoadNextScene(int sceneIndex)
    {
        yield return CoherenceSceneManager.LoadScene(_coherenceBridge, sceneIndex);
    }
    
    private void OnDestroy()
    {
        _coherenceBridge.onConnected.AddListener(OnBridgeConnection);
        _coherenceBridge.onDisconnected.AddListener(OnBridgeDisconnection);
    }
}