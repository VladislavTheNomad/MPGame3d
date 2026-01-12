using Fusion;
using UnityEngine;

namespace MPGame3d
{
    public struct NetworkInputData : INetworkInput
    {
        public const byte MOUSE_BUTTON = 1;

        public NetworkButtons buttons;
        public Vector3 direction;
    }
}
