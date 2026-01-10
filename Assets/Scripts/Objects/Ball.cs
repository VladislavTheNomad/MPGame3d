using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Ball : NetworkBehaviour
{
    [Networked] private TickTimer Life { get; set; }
    private int _damage;
    private float _lifeSpan;

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

    public void Init(int  damage, float lifeSpan)
    {
        _lifeSpan = lifeSpan;
        _damage = damage;
        Life = TickTimer.CreateFromSeconds(Runner, _lifeSpan);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        if (other.TryGetComponent<EnemyBehaviour>(out var enemy))
        {
            if (enemy != null)
            {
                enemy.DealDamage(_damage);
            }
        }
        else if (other.TryGetComponent<Player>(out var player))
        {
            if (player != null)
            {
                player.DealDamage(_damage);
            }
        }
        
        Runner.Despawn(Object);
    }
}
