using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public const byte MOUSE_BUTTON = 1;

    public NetworkButtons buttons;
    public Vector3 direction;
}
