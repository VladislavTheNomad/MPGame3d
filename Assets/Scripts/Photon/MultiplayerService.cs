using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;
using Random = UnityEngine.Random;

public class MultiplayerService : MonoBehaviour, INetworkRunnerCallbacks
{
    public event Action OnGameStarted;
    public event Action OnNewPlayerComing;
    
    [SerializeField] private GameObject _menuUI;
    [SerializeField] private TMP_InputField _roomIDInputField;
    [SerializeField] private Button _generateButton;
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _joinButton;
    [SerializeField] private TMP_Text _idRoomText;
    [SerializeField] private TMP_Text _chatText;
    [SerializeField] private NetworkPrefabRef _playerPrefab;
    
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    private List<string> _blackList = new List<string>(1);
    private NetworkRunner _runner;
    private GameConfigs _gameConfigs;

    [Inject]
    public void Construct(GameConfigs gameConfigs)
    {
        _gameConfigs = gameConfigs;
    }

    private void Awake()
    {
        _hostButton.onClick.AddListener(OnClickHost);
        _joinButton.onClick.AddListener(OnClickJoin);
        _generateButton.onClick.AddListener(GenerateRandomID);
    }

    private void OnDestroy()
    {
        _hostButton.onClick.RemoveListener(OnClickHost);
        _joinButton.onClick.RemoveListener(OnClickJoin);
        _generateButton.onClick.RemoveListener(GenerateRandomID);
    }
    
    public List<Transform> GetPlayersTransforms()
    {
        List<Transform> transforms = new List<Transform>(_spawnedCharacters.Count);

        foreach (var item in _spawnedCharacters)
        {
            transforms.Add(item.Value.transform);
        }
        
        return transforms;
    }

    private async UniTaskVoid InitializeNetwork(GameMode mode)
    {
        _hostButton.interactable = false;
        _joinButton.interactable = false;
        
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;
        _runner.AddCallbacks(this);

        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
        
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = _roomIDInputField.text,
            Scene = scene,
            SceneManager = sceneManager
        });

        if (result.Ok)
        {
            _menuUI.SetActive(false);
            _idRoomText.text = _roomIDInputField.text;
            Debug.Log("Success connection to: " + _roomIDInputField.text);
        }
        else
        {
            Debug.LogError($"Error: {result.ShutdownReason}");
            RestartScene();
        }
    }
    
    private void GenerateRandomID()
    {
        string newID = Random.Range(1000, 9999).ToString();
        _roomIDInputField.text = newID;
    }

    private void RestartScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }
    
    public void ProposeBan(PlayerRef player)
    {
        if (!_runner.IsServer) return;
        
        _blackList.Add(player.ToString());

        if (player == _runner.LocalPlayer)
        {
            LeaveGame();
        }
        else
        {
            _runner.Disconnect(player);
        }
    }
    
    public void LeaveGame()
    {
        if (_runner != null)
        {
            _runner.Shutdown();
        }
    }
    
    public void UpdateChatUI(string message, PlayerRef source)
    {
        if (source == _runner.LocalPlayer)
        {
            _chatText.text = "You Said: " + message;
        }
        else
        {
            _chatText.text = $"Player {source.PlayerId + 2} said: {message}\n";
        }
    }

    private void OnClickHost()
    {
        if (_roomIDInputField.text.Length == 4)
        {
            InitializeNetwork(GameMode.Host);
        }
        else
        {
            _roomIDInputField.text = "PLZ Generate ID!!!";
        }
    }

    private void OnClickJoin()
    {
        InitializeNetwork(GameMode.Client);
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Vector3 spawnPos = new Vector3(player.RawEncoded, 5, 0);

        NetworkObject networkObject = runner.Spawn(_playerPrefab, spawnPos, Quaternion.identity, player,
            (runner, newPlayer) =>
            {
                newPlayer.GetComponent<Player>().Init(_gameConfigs, this);
                _spawnedCharacters[player] = newPlayer;
                if (runner.IsServer)
                {
                    OnGameStarted?.Invoke();
                }
                else
                {
                    OnNewPlayerComing?.Invoke();
                }
            });
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        float horizontalInput = SimpleInput.GetAxis("Horizontal");
        float verticalInput = SimpleInput.GetAxis("Vertical");
        
        data.direction = new Vector3(horizontalInput, 0, verticalInput);
        
        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

    public void OnConnectedToServer(NetworkRunner runner) { }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }

    public void OnSceneLoadDone(NetworkRunner runner) { }

    public void OnSceneLoadStart(NetworkRunner runner) { }
}
