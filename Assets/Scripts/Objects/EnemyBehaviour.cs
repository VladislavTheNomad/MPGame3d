using System.Collections.Generic;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkCharacterController))]
public class EnemyBehaviour : NetworkBehaviour
{
    [SerializeField] private NetworkObject _enemyBallPrefab;
    [SerializeField] private NetworkObject _potionPrefab;
    
    private List<Transform> _targets;
    private NetworkCharacterController _controller;
    private float _speed;
    private Vector3 _targetPosition;
    private float _attackDelay;
    private GameConfigs _gameConfigs;
    private int _damage;

    [Networked] private TickTimer AttackDelay { get; set; }
    [Networked] private int HP { get; set; }
    
    private void Awake() => _controller = GetComponent<NetworkCharacterController>();

    public void Init(List<Transform> targets, GameConfigs gameConfigs)
    {
        _targets = targets;
        _gameConfigs = gameConfigs;
    }

    public override void Spawned()
    {
        _speed = _gameConfigs.EnemySpeed;
        _attackDelay = _gameConfigs.AttackDelay;
        HP = _gameConfigs.EnemyHP;
        _damage = _gameConfigs.EnemyDamage;
        AttackDelay = TickTimer.CreateFromSeconds(Runner, _attackDelay);
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
            if (_gameConfigs.PotionLifeSpan > Random.Range(0f, 100f))
            {
                SpawnPotion();
            }

            Runner.Despawn(Object);
        }
    }

    private void SpawnPotion()
    {
        Debug.Log("Attempting to spawn potion...");
        var potionObj = Runner.Spawn(_potionPrefab, transform.position, Quaternion.identity, Object.InputAuthority,
            (runner, newObject) =>
            {
                Debug.Log("Initializing Potion...");
                newObject.GetComponent<Potion>().Init(_gameConfigs.PotionLifeSpan);
            });

        if (potionObj != null)
        {
            Debug.Log($"Potion spawned successfully! Name: {potionObj.name}, ID: {potionObj.Id}");
        }
        else
        {
            Debug.LogError("Runner.Spawn returned NULL! Check Network Project Config -> Prefabs.");
        }
    }
}
