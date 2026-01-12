using Fusion;
using UnityEngine;

namespace MPGame3d
{
    public class Potion : NetworkBehaviour
    {
        [Networked] private TickTimer Life { get; set; }
        private float _lifeSpan;

        private void OnTriggerEnter(Collider other)
        {
            if (!Object.HasStateAuthority) return;

            if (other.TryGetComponent<Player>(out var player))
            {
                if (player != null)
                {
                    player.Heal();
                    Runner.Despawn(Object);
                }
            }
        }

        public void Init(float potionLifeSpan)
        {
            _lifeSpan =  potionLifeSpan;
            Life = TickTimer.CreateFromSeconds(Runner, _lifeSpan);
        }
    
        public override void FixedUpdateNetwork()
        {
            if (Life.IsRunning && Life.Expired(Runner))
            {
                Runner.Despawn(Object);
            }
        }
    }
}
