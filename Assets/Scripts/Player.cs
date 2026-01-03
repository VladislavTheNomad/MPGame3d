using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkCharacterController))]
public class Player : NetworkBehaviour
{
    [SerializeField] private Ball _prefabBall;

    [Networked] private TickTimer Delay { get; set; }

    private NetworkCharacterController _controller;
    private Vector3 _forward =  Vector3.forward;

    private void Awake()
    {
        _controller = GetComponent<NetworkCharacterController>();
        _forward = transform.forward;
    }
    
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            data.direction.Normalize();
            _controller.Move(5 * data.direction * Runner.DeltaTime);
            
            if (data.direction.sqrMagnitude > 0)
            {
                _forward = data.direction;
            }

            if (HasStateAuthority && Delay.ExpiredOrNotRunning(Runner))
            {
                if (data.buttons.IsSet(NetworkInputData.MOUSE_BUTTON))
                {
                    Delay = TickTimer.CreateFromSeconds(Runner,0.5f);
                    Runner.Spawn(_prefabBall, transform.position + _forward, Quaternion.LookRotation(_forward),
                        Object.InputAuthority, (runner, newObject) =>
                        {
                            newObject.GetComponent<Ball>().Init();
                        });
                }
            }
        }
    }
}
