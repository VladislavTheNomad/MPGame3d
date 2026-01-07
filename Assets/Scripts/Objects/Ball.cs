using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Ball : NetworkBehaviour
{
    [Networked] private TickTimer Life { get; set; }
    private int _damage;

    public override void FixedUpdateNetwork()
    {
        if (Life.Expired(Runner))
        {
            Runner.Despawn(Object);
        }
        else
        {
            transform.position += 10 * transform.forward * Runner.DeltaTime;
        }
    }

    public void Init(int  damage)
    {
        _damage = damage;
        Life = TickTimer.CreateFromSeconds(Runner,2f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        if (other.TryGetComponent<EnemyBehaviour>(out var enemy))
        {
            enemy.DealDamage(_damage);
            Runner.Despawn(Object);
        }
    }
}
