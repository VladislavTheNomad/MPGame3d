using System.Collections.Generic;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkCharacterController))]
public class EnemyBehaviour : NetworkBehaviour
{
    private List<Transform> _targets;
    private NetworkCharacterController _controller;
    private float _speed;
    private Vector3 _targetPosition;
    
    [Networked] private int HP { get; set; }
    
    private void Awake() => _controller = GetComponent<NetworkCharacterController>();

    public void Init(List<Transform> targets, float speed, int hp)
    {
        _targets = targets;
        _speed =  speed;
        HP = hp;
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
        }
    }

    public void DealDamage(int damage)
    {
        if (!Object.HasStateAuthority) return;
        
        HP -= damage;

        if (HP <= 0)
        {
            Runner.Despawn(Object);
        }
    }
}
