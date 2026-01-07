using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkCharacterController))]
public class Player : NetworkBehaviour
{
    [SerializeField] private Ball _prefabBall;
    [SerializeField] private LayerMask _enemyLayerMask;

    [Networked] private TickTimer Delay { get; set; }

    private NetworkCharacterController _controller;
    private float _speed;
    private float _attackRadius;
    private Collider[] _hitColliders = new Collider[10];
    private int _damage;
    private MultiplayerService _multiplayerService;

    private void Awake()
    {
        _controller = GetComponent<NetworkCharacterController>();
    }

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            if (CameraFollower.cameraFollower != null)
            {
                CameraFollower.cameraFollower.SetTarget(this.transform);
            }
        }
    }
    
    public void Init(float speed, float attackRadius, int damage,  MultiplayerService multiplayerService)
    {
        _damage = damage;
        _speed =  speed;
        _attackRadius = attackRadius;
        _multiplayerService = multiplayerService;
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
        _multiplayerService.LeaveGame();
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
                Delay = TickTimer.CreateFromSeconds(Runner,0.5f);
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
                            newObject.GetComponent<Ball>().Init(_damage);
                        });
                }
            }
        }
    }
}
