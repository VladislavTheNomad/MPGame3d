using Fusion;
using UnityEngine;

namespace MPGame3d
{
    public enum WallType
    {
        Top,
        Bottom,
        Left,
        Right
    }
    public class Wall : NetworkBehaviour
    {
        [SerializeField] private WallType _wallType;
        
        private void OnTriggerEnter(Collider other)
        {
            if (!Object.HasStateAuthority) return;
            
            if (other.TryGetComponent<Player>(out var player))
            {
                Debug.Log("!!!");
                if (player != null)
                {
                    switch (_wallType)
                    {
                        case WallType.Top:
                            player.Teleport(new Vector3(0, 0, -0.3f));
                            break;
                        case WallType.Bottom:
                            player.Teleport(new Vector3(0, 0, 0.3f));
                            break;
                        case WallType.Left:
                            player.Teleport(new Vector3(0.3f, 0, 0));
                            break;
                        case WallType.Right:
                            player.Teleport(new Vector3(-0.3f, 0, 0));
                            break;
                        default:
                            break;
                    }
                }
            }
        }
    }
}
