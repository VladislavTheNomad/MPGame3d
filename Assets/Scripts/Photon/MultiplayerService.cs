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
using Object = System.Object;
using Random = UnityEngine.Random;

namespace MPGame3d
{
    public class MultiplayerService : MonoBehaviour, INetworkRunnerCallbacks
    {
        public event Action OnGameStarted;
    
        [SerializeField] private GameObject _menuUI;
        [SerializeField] private TMP_InputField _roomIDInputField;
        [SerializeField] private Button _generateButton;
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private TMP_Text _idRoomText;
        [SerializeField] private TMP_Text _chatText;
        [SerializeField] private NetworkPrefabRef _playerPrefab;
    
        private Dictionary<PlayerRef, Player> _spawnedCharacters = new Dictionary<PlayerRef, Player>();
        private List<string> _blackList = new List<string>(1);
        private NetworkRunner _runner;

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
                SceneManager = sceneManager,
                ConnectionToken = GetConnectionToken(),
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
        
        private byte[] GetConnectionToken()
        {
            string tokenKey = "MyToken";
            string saved = PlayerPrefs.GetString(tokenKey, string.Empty);
            if (string.IsNullOrEmpty(saved))
            {
                byte[] newToken = Guid.NewGuid().ToByteArray();
                PlayerPrefs.SetString(tokenKey, Convert.ToBase64String(newToken));
                return newToken;
            }
            return Convert.FromBase64String(saved);
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


            byte[] token = _runner.GetPlayerConnectionToken(player);
            string tokenString = new Guid(token).ToString();
            _blackList.Add(tokenString);

            if (player == _runner.LocalPlayer)
            {
                _runner.Shutdown();
            }
            else
            {
                _runner.Disconnect(player);
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
            if (!runner.IsServer) return;
            
            if (_blackList.Contains(player.ToString()))
            {
                runner.Disconnect(player);
                return;
            }
            
            Vector3 spawnPos = new Vector3(player.RawEncoded*3, 5, 0);

            runner.Spawn(_playerPrefab, spawnPos, Quaternion.identity, player, (runner, newObject) =>
            {
                var playerScript = newObject.GetComponent<Player>();
                _spawnedCharacters.Add(player, playerScript);

                if (runner.IsServer)
                {
                    OnGameStarted?.Invoke();
                }
            });
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (_spawnedCharacters.TryGetValue(player, out Player playerComponent))
            {
                runner.Despawn(playerComponent.Object);
                _spawnedCharacters.Remove(player);
            }
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        { 
            RestartScene();
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            RestartScene();
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request,
            byte[] token)
        {
            if (token == null || token.Length == 0)
            {
                request.Refuse();
                return;
            }

            string clientGuid = new Guid(token).ToString();

            if (_blackList.Contains(clientGuid))
            {
                request.Refuse();
            }
        }

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
}