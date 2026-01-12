using Fusion;
using UnityEngine;
using Zenject;

namespace MPGame3d
{
    public class EXPCrystal : NetworkBehaviour
    {
        [Networked] private TickTimer LifeTimer { get; set; }

        [Inject] private GameConfigs _gameConfigs;
        
        private float _lifeSpan;

        public void Init(float lifeSpan)
        {
            _lifeSpan = lifeSpan;
        }
        
        public override void Spawned()
        {
            if (ZenjectAccessor.Container != null)
                ZenjectAccessor.Container.Inject(this);

            if (Object.HasStateAuthority)
            {
                LifeTimer = TickTimer.CreateFromSeconds(Runner, _lifeSpan);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (LifeTimer.Expired(Runner) && LifeTimer.IsRunning)
            {
                Runner.Despawn(Object);
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            if (!Object.HasStateAuthority) return;

            if (other.TryGetComponent<Player>(out Player player))
            {
                player.GainExp();
                Runner.Despawn(Object);
            }
        }
    }
}