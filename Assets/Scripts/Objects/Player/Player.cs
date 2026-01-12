using System;
using Fusion;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Random = UnityEngine.Random;

namespace MPGame3d
{
    public enum LevelUps
    {
        Damage,
        AttackRadius,
        MaxHp,
        Speed,
    }

    [RequireComponent(typeof(NetworkCharacterController))]
    public class Player : NetworkBehaviour
    {
        [SerializeField] private Ball _prefabBall;
        [SerializeField] private LayerMask _enemyLayerMask;

        [Networked] private TickTimer Delay { get; set; }
        [Networked] private int CurrentHP { get; set; }
        [Networked] private int MaxHP { get; set; }
        [Networked] private int HPRestoreFromPotion { get; set; }
        [Networked] private int ExpGainFromCrystal { get; set; }
        [Networked] private int CurrentEXP { get; set; }
        [Networked] private int PlayerExpToLevelUp { get; set; }
        [Networked] private int Damage { get; set; }
        [Networked] private float AttackRadius { get; set; }
        [Networked] private float Speed { get; set; }

        [Inject] private UIService _uiService;
        [Inject] private GameConfigs _gameConfigs;
        [Inject] private MultiplayerService _multiplayerService;

        private NetworkCharacterController _controller;
        private ChangeDetector _changeDetector;
        private Slider _hpBarInGame;
        private Material _material;
        private Collider[] _hitColliders = new Collider[10];
        private Color _targetColor = Color.white;
        private LevelUps[] _levelUpBonuses;

        private void Awake()
        {
            _controller = GetComponent<NetworkCharacterController>();
            _material = GetComponentInChildren<MeshRenderer>().material;
            _levelUpBonuses = (LevelUps[])Enum.GetValues(typeof(LevelUps));
        }

        public override void Spawned()
        {
            _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
            _hpBarInGame = GetComponentInChildren<Slider>();
            
            if (ZenjectAccessor.Container != null)
            {
                ZenjectAccessor.Container.Inject(this);
            }
            
            if (Object.HasStateAuthority)
            {
                Speed = _gameConfigs.PlayerSpeed;
                Damage = _gameConfigs.PlayerAttackDamage;
                AttackRadius = _gameConfigs.PlayerAttackRadius;
                MaxHP = _gameConfigs.PlayerMaxHP;
                CurrentHP = MaxHP;
                HPRestoreFromPotion = _gameConfigs.PotionHPRestore;
                ExpGainFromCrystal = _gameConfigs.EXPGetFromCrystal;
                PlayerExpToLevelUp = _gameConfigs.PlayerExpToLevelUp;
                CurrentEXP = 0;
            }

            UpdateVisuals();
        }

        public override void Render()
        {
            foreach (var change in _changeDetector.DetectChanges(this))
            {
                switch (change)
                {
                    case nameof(CurrentHP):
                    case nameof(MaxHP):
                    case nameof(CurrentEXP):
                    case nameof(PlayerExpToLevelUp):
                        UpdateVisuals();
                        break;
                }
            }
        }

        private void UpdateVisuals()
        {
            if (_hpBarInGame != null)
            {
                _hpBarInGame.maxValue = MaxHP > 0 ? MaxHP : 100;
                _hpBarInGame.value = CurrentHP;
            }

            RecalculateTargetColor();

            if (Object.HasInputAuthority && _uiService != null)
            {
                _uiService.UpdateMaxHP(MaxHP);
                _uiService.UpdateHpBar(CurrentHP);
                _uiService.UpdateMaxExpToLvl(PlayerExpToLevelUp);
                _uiService.UpdateExpBar(CurrentEXP);
            }
        }

        private void RecalculateTargetColor()
        {
            if (MaxHP <= 0) return;
            float hp01 = Mathf.Clamp01((float)CurrentHP / MaxHP);
            _targetColor = Color.Lerp(Color.red, Color.white, hp01);
            _material.color = _targetColor;
        }

        public override void FixedUpdateNetwork()
        {
            if (GetInput(out NetworkInputData data))
            {
                Vector3 moveDirection = Vector3.ClampMagnitude(data.direction, 1f);
                _controller.Move(Speed * moveDirection * Runner.DeltaTime);

                if (HasStateAuthority && Delay.ExpiredOrNotRunning(Runner))
                {
                    SpawnBullet();
                }
            }
        }

        private void SpawnBullet()
        {
            Delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
            int hits = Physics.OverlapSphereNonAlloc(transform.position, AttackRadius, _hitColliders, _enemyLayerMask.value);

            if (hits > 0)
            {
                Vector3 targetPosition = _hitColliders[0].transform.position;
                Vector3 directionToShoot = (targetPosition - transform.position).normalized;
                directionToShoot.y = 0;
                Quaternion spawnRotation = Quaternion.LookRotation(directionToShoot);
                Vector3 spawnPosition = transform.position + directionToShoot * 1.5f;
                    
                Runner.Spawn(_prefabBall, spawnPosition, spawnRotation,
                    Object.InputAuthority, (runner, newObject) =>
                    {
                        newObject.GetComponent<Ball>().Init(Damage, _gameConfigs.BallLifeSpan);
                    });
            }
        }

        public void DealDamage(int damage)
        {
            if (!Object.HasStateAuthority) return;
            CurrentHP -= damage;
            if (CurrentHP <= 0)
            {
                _multiplayerService.ProposeBan(Object.InputAuthority);
                Runner.Despawn(Object);
            }
        }

        public void Heal()
        {
            if (!Object.HasStateAuthority) return;
            CurrentHP = Mathf.Min(CurrentHP + HPRestoreFromPotion, MaxHP);
        }

        public void Teleport(Vector3 offset)
        {
            if (!Object.HasStateAuthority) return;
            _controller.Teleport(transform.position + offset);
        }

        public void GainExp()
        {
            if (!Object.HasStateAuthority) return;
            CurrentEXP += ExpGainFromCrystal;
            if (CurrentEXP >= PlayerExpToLevelUp) LevelUp();
        }

        private void LevelUp()
        {
            if (!Object.HasStateAuthority) return;
            CurrentHP = MaxHP;
            CurrentEXP = 0;
            PlayerExpToLevelUp = (int)(PlayerExpToLevelUp * 1.2f);
            int chosenBonusIndex = Random.Range(0, _levelUpBonuses.Length);
            GetBonus(_levelUpBonuses[chosenBonusIndex]);
        }

        private void GetBonus(LevelUps levelUpBonus)
        {
            switch (levelUpBonus)
            {
                case LevelUps.Damage: Damage += 2; break;
                case LevelUps.AttackRadius: AttackRadius += 5f; break;
                case LevelUps.MaxHp: MaxHP += 20; break;
                case LevelUps.Speed: Speed += 1; break;
            }
            CurrentHP = MaxHP;
        }
    }
}