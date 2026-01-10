using Cinemachine;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkCharacterController))]
public class Player : NetworkBehaviour
{
    [SerializeField] private Ball _prefabBall;
    [SerializeField] private LayerMask _enemyLayerMask;

    [Networked] private TickTimer Delay { get; set; }
    [Networked] private int CurrentHP { get; set; }
    [Networked] private int MaxHP { get; set; }
    [Networked] private int HPRestoreFromPotion { get; set; }

    private NetworkCharacterController _controller;
    private float _speed;
    private float _attackRadius;
    private Collider[] _hitColliders = new Collider[10];
    private int _damage;
    private MultiplayerService _multiplayerService;
    private GameConfigs _gameConfigs;
    private ChangeDetector _changeDetector;
    private Material _material;
    private Color _targetColor = Color.white;

    private void Awake()
    {
        _controller = GetComponent<NetworkCharacterController>();
        _material = GetComponentInChildren<MeshRenderer>().material;
    }
    
    private void Update()
    {
        if (Object.HasInputAuthority && Input.GetKeyDown(KeyCode.R))
        {
            RPC_SendMessage("Hey Mate!");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SendMessage(string message, RpcInfo info = default)
    {
        RPC_RelayMessage(message,  info.Source);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_RelayMessage(string message, PlayerRef source)
    {
        if (_multiplayerService != null)
        {
            _multiplayerService.UpdateChatUI(message, source);
        }
    }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }
    
    public void Init(GameConfigs gameConfigs, MultiplayerService multiplayerService)
    {
        _gameConfigs = gameConfigs;
        _multiplayerService = multiplayerService;
        
        if (Object.HasStateAuthority)
        {
            MaxHP = _gameConfigs.PlayerHP;
            CurrentHP = MaxHP;
            HPRestoreFromPotion = gameConfigs.PotionHPRestore;
        }
        _damage = _gameConfigs.PlayerAttackDamage;
        _speed = _gameConfigs.PlayerSpeed;
        _attackRadius = _gameConfigs.PlayerAttackRadius;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasInputAuthority) return;
        
        if (((1 << other.gameObject.layer) & _enemyLayerMask) != 0)
        {
            RPC_RequestBan(Object.InputAuthority);
        }
    }
    
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestBan(PlayerRef player)
    {
        _multiplayerService.ProposeBan(player);
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            Vector3 moveDirection = Vector3.ClampMagnitude(data.direction, 1f);
            _controller.Move(_speed * moveDirection * Runner.DeltaTime);

            if (HasStateAuthority && Delay.ExpiredOrNotRunning(Runner))
            {
                SpawnBullet();
            }
        }
    }

    private void SpawnBullet()
    {
        Delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
        int hits = Physics.OverlapSphereNonAlloc(transform.position, _attackRadius, _hitColliders, _enemyLayerMask.value);

        if (hits > 0)
        {
            Vector3 targetPosition = _hitColliders[0].transform.position;
            Vector3 directionToShoot = (targetPosition - transform.position).normalized;
            directionToShoot.y = 0;
            Quaternion spawnRotation = Quaternion.LookRotation(directionToShoot);
            Vector3 spawnPosition = transform.position + directionToShoot * 1.5f;
                    
            Runner.Spawn(_prefabBall, spawnPosition, spawnRotation,
                Object.InputAuthority, (runner, newObject) =>
                {
                    newObject.GetComponent<Ball>().Init(_damage, _gameConfigs.BallLifeSpan);
                });
        }
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            if (change == nameof(CurrentHP))
            {
                RecalculateTargetColor();
            }
        }
    }

    private void RecalculateTargetColor()
    {
        float hp01 = Mathf.Clamp01((float)CurrentHP / MaxHP);
        _targetColor = Color.Lerp(Color.red, Color.white, hp01);
        _material.color = _targetColor;
    }

    public void DealDamage(int damage)
    {
        if (!Object.HasStateAuthority) return;
        
        CurrentHP -= damage;

        if (CurrentHP <= 0)
        {
            _multiplayerService.ProposeBan(Object.InputAuthority);
        }
    }

    public void Heal()
    {
        if (!Object.HasStateAuthority) return;
        
        CurrentHP += HPRestoreFromPotion;
        if (CurrentHP > MaxHP)
        {
            CurrentHP = MaxHP;
        }
    }

    public void Teleport(Vector3 offset)
    {
        if (!Object.HasStateAuthority) return;
        
        transform.position += offset;
        _controller.Teleport(transform.position);
    }
}