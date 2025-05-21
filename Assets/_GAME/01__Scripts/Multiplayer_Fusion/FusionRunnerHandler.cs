using Fusion;
using UnityEngine;

public class FusionRunnerHandler : MonoBehaviour
{
    [SerializeField] private NetworkRunner runnerPrefab;

    private NetworkRunner _runner;

    private async void Start()
    {
        _runner = Instantiate(runnerPrefab);
        _runner.name = "NetworkRunner (Auto)";
        _runner.ProvideInput = true;
        // _runner.OnInput += OnInput;

        var startArgs = new StartGameArgs()
        {
            GameMode = GameMode.Host, // or Client/Shared
            SessionName = "MyGameSession",
            Scene = SceneRef.FromIndex(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        var result = await _runner.StartGame(startArgs);

        if (result.Ok)
            Debug.Log("Fusion Game Started");
        else
            Debug.LogError("Fusion Failed to Start: " + result.ShutdownReason);
    }

    private void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (MobileInput.Instance != null)
            input.Set(MobileInput.Instance.CreateNetworkInput());
    }
}
