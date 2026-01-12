using System.Collections.Generic;
using Fusion;
using UnityEngine;
using Zenject;

namespace MPGame3d
{
    public class GameplayService : NetworkBehaviour
    {
        [SerializeField] private EnemyBehaviour _prefabEnemy;
        [SerializeField] private LayerMask floorLayerMask;
        [Networked] private TickTimer DelayBetweenEnemySpawn { get; set; }
        private bool _gameStarted;
        private MultiplayerService _multiplayerService;
        private GameConfigs _gameConfigs;
        private List<Transform> _playersTransforms;

        [Inject]
        public void Construct(MultiplayerService multiplayerService, GameConfigs gameConfigs)
        {
            _multiplayerService =  multiplayerService;
            _gameConfigs  = gameConfigs;
        }

        public void Awake() => _multiplayerService.OnGameStarted += GameStarted;
    
        public void OnDestroy() => _multiplayerService.OnGameStarted -= GameStarted;

        public override void Spawned()
        {
            DelayBetweenEnemySpawn = TickTimer.CreateFromSeconds(Runner, _gameConfigs.SpawnDelay);
        }

        private void GameStarted()
        {
            _gameStarted = true;
            _playersTransforms = _multiplayerService.GetPlayersTransforms();
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority || !_gameStarted) return;
        
            if (DelayBetweenEnemySpawn.Expired(Runner))
            {
                DelayBetweenEnemySpawn = TickTimer.CreateFromSeconds(Runner,_gameConfigs.SpawnDelay);

                var hostPlayer = _playersTransforms[0];
            
                if (hostPlayer != null)
                {
                    bool hasPlaceToSpawn = false;
                    Vector3 spawnPosition = Vector3.zero;

                    for (int i = 0; i < _gameConfigs.MaxSpawnAttempts; i++)
                    {
                        float angle = Random.Range(0f, Mathf.PI * 2);
                        float distance = Random.Range(_gameConfigs.MinSpawnDistance, _gameConfigs.MaxSpawnDistance);
                        Vector3 spawnOffset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * distance;
                        Vector3 targetPoint = _playersTransforms[Random.Range(0, _playersTransforms.Count)].position + spawnOffset;
                        Vector3 rayPoint = targetPoint + Vector3.up * _gameConfigs.RaycastHeight;

                        if (Physics.Raycast(rayPoint, Vector3.down, out RaycastHit hit, _gameConfigs.RaycastHeight+1f, floorLayerMask.value))
                        {
                            spawnPosition = hit.point + Vector3.up;
                            hasPlaceToSpawn = true;
                            break;
                        }
                    }

                    if (hasPlaceToSpawn)
                    {
                        Runner.Spawn(_prefabEnemy, spawnPosition, Quaternion.identity, PlayerRef.None,
                            (runner, newObject) => { newObject.GetComponent<EnemyBehaviour>().Init(_playersTransforms); });
                    }
                }
            }
        }
    }
}