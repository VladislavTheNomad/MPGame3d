using System.Collections.Generic;
using Fusion;
using UnityEngine;
using Zenject;

namespace MPGame3d
{
    [RequireComponent(typeof(NetworkCharacterController))]
    public class EnemyBehaviour : NetworkBehaviour
    {
        [SerializeField] private NetworkObject _enemyBallPrefab;
        [SerializeField] private NetworkObject _potionPrefab;
        [SerializeField] private NetworkObject _expCrystalPrefab;
    
        private List<Transform> _targets;
        private NetworkCharacterController _controller;
        private float _speed;
        private Vector3 _targetPosition;
        private float _attackDelay;
        private int _damage;
        [Inject] private GameConfigs _gameConfigs;

        [Networked] private TickTimer AttackDelay { get; set; }
        [Networked] private int HP { get; set; }
    
        private void Awake() => _controller = GetComponent<NetworkCharacterController>();

        public void Init(List<Transform> targets)
        {
            _targets = targets;
        }

        public override void Spawned()
        {
            if (ZenjectAccessor.Container != null)
            {
                ZenjectAccessor.Container.Inject(this);
                
                if (_gameConfigs != null)
                {
                    _speed = _gameConfigs.EnemySpeed;
                    _attackDelay = _gameConfigs.AttackDelay;
                    _damage = _gameConfigs.EnemyDamage;
                }
            }

            if (Object.HasStateAuthority)
            {
                HP = _gameConfigs.EnemyHP;
                AttackDelay = TickTimer.CreateFromSeconds(Runner, _attackDelay);
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (!Object.HasStateAuthority) return;

            if (other.TryGetComponent<Player>(out var player))
            {
                player.DealDamage(_damage*100);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if(Object.HasStateAuthority && _targets != null)
            {
                float minDistance = 5000f;
                _targetPosition = Vector3.zero;
        
                foreach (var target in _targets)
                {
                    if (target == null) continue;
                    float sqrDistance = (target.position - transform.position).sqrMagnitude;
                
                    if (sqrDistance < minDistance)
                    {
                        _targetPosition =  target.position;
                        minDistance = sqrDistance;
                    }
                }
            
                Vector3 directionToPlayer = (_targetPosition - transform.position).normalized;
                directionToPlayer.y = 0;
                _controller.Move(_speed * directionToPlayer * Runner.DeltaTime);

                if (directionToPlayer != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(directionToPlayer);
                }

                if (AttackDelay.ExpiredOrNotRunning(Runner))
                {
                    ShootToTarget(_targetPosition, directionToPlayer);
                }
            }
        }

        private void ShootToTarget(Vector3 target, Vector3  direction)
        {
            AttackDelay = TickTimer.CreateFromSeconds(Runner, _attackDelay);
            Runner.Spawn(_enemyBallPrefab, transform.position + direction, transform.rotation, Object.InputAuthority,
                (runner, newObject) =>
                {
                    newObject.GetComponent<Ball>().Init(_damage, _gameConfigs.BallLifeSpan);
                });
        }

        public void DealDamage(int damage)
        {
            if (!Object.HasStateAuthority) return;
        
            HP -= damage;

            if (HP <= 0)
            {
                if (_gameConfigs.PotionSpawnChance > Random.Range(0f, 100f))
                {
                    SpawnPotion();
                }

                SpawnEXPCrystal();
                Runner.Despawn(Object);
            }
        }

        private void SpawnEXPCrystal()
        {
            Runner.Spawn(_expCrystalPrefab, transform.position, Quaternion.identity, Object.InputAuthority,
                (runner, newObject) =>
                {
                    newObject.GetComponent<EXPCrystal>().Init(_gameConfigs.EXPCrystalLifeSpan);
                });
        }

        private void SpawnPotion()
        {
            Runner.Spawn(_potionPrefab, transform.position, Quaternion.identity, Object.InputAuthority,
                (runner, newObject) =>
                {
                    newObject.GetComponent<Potion>().Init(_gameConfigs.PotionLifeSpan);
                });
        }
    }
}
