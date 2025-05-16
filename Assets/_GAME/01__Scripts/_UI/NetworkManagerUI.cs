using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class NetworkManagerUI : MonoBehaviour
{
    public int index;
    public GameObject playerPrefab;
    public PlayerController pc;
    [SerializeField] Button serverBtn;
    [SerializeField] Button hostBtn;
    [SerializeField] Button clientBtn;

    // private IEnumerator Start()
    // {
    //     yield return new WaitForSeconds(0.1f);
    //     if (PlayerColorsGlobal.Instance != null)
    //         playerPrefab = PlayerColorsGlobal.Instance.playerPrefab;
    //     // NetworkManager.Singleton.StartHost();
    //     pc = playerPrefab.GetComponent<PlayerController>();
    //     pc.transform.position = GameManager.Instance.playerSpawnPoint.position;
    //     // MP_InitHostPlayer();


    // }
    // private void Awake()
    // {
    //     serverBtn.onClick.AddListener(() =>
    //     {
    //         NetworkManager.Singleton.StartServer();
    //     });
    //     hostBtn.onClick.AddListener(() =>
    //     {
    //         NetworkManager.Singleton.StartHost();
    //     });
    //     hostBtn.onClick.AddListener(() =>
    //     {
    //         GameManager.Instance.GetComponent<FPS>().enabled = true;
    //     });

    //     clientBtn.onClick.AddListener(() =>
    //     {
    //         NetworkManager.Singleton.StartClient();
    //     });
    // }
    // public void MP_InitHostPlayer()
    // {
    //     MP_CreatePlayerServerRpc(NetworkManager.Singleton.LocalClientId, 0);
    // }
    // public GameObject tempGO;
    // [ServerRpc(RequireOwnership = false)] //server owns this object but client can request a spawn
    // public void MP_CreatePlayerServerRpc(ulong clientId, int prefabId)
    // {
    //     if (prefabId == 0)
    //         tempGO = (GameObject)Instantiate(playerPrefab);
    //     else
    //         tempGO = (GameObject)Instantiate(playerPrefab);
    //     NetworkObject netObj = tempGO.GetComponent<NetworkObject>();
    //     tempGO.SetActive(true);
    //     netObj.SpawnAsPlayerObject(clientId, true);
    // }

}
