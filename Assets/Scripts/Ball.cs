using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class Ball : NetworkBehaviour
{
    [Networked] private TickTimer Life { get; set; }

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

    public void Init()
    {
        Life = TickTimer.CreateFromSeconds(Runner,2f);
    }
}
