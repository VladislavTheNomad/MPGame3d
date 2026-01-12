using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Configs/GameConfig")]
public class GameConfigs : ScriptableObject
{
    [field: Header("Enemy Spawn Settings")]
    [field: SerializeField] public float SpawnDelay { get; private set; } = 2f;
    [field: SerializeField] public float MinSpawnDistance { get; private set; } = 40f;
    [field: SerializeField] public float MaxSpawnDistance { get; private set; } = 50f;
    [field: SerializeField] public int MaxSpawnAttempts { get; private set; } = 10;
    [field: SerializeField] public float RaycastHeight { get; private set; } = 5f;

    [field: Header("Enemy Balance")]
    [field: SerializeField] public float EnemySpeed { get; private set; } = 2f;
    [field: SerializeField] public float EnemyBraking { get; private set; } = 30f;
    [field: SerializeField] public int EnemyHP { get; private set; } = 100;
    [field: SerializeField] public int EnemyDamage { get; private set; } = 5;
    [field: SerializeField] public float AttackDelay { get; private set; } = 1f;
    
    [field: Header("Ball Balance")]
    [field: SerializeField] public float BallLifeSpan { get; private set; } = 5f;
    
    [field: Header("Potion Balance")]
    [field: SerializeField] public float PotionLifeSpan { get; private set; } = 3f;
    [field: SerializeField] public float PotionSpawnChance { get; private set; } = 10f;
    
    [field: Header("EXP Crystal Balance")]
    [field: SerializeField] public float EXPCrystalLifeSpan { get; private set; } = 5f;
    

    [field: Header("Player Balance")]
    [field: SerializeField] public float PlayerSpeed { get; private set; } = 8f;
    [field: SerializeField] public float PlayerBraking { get; private set; } = 60f;
    [field: SerializeField] public int PlayerMaxHP { get; private set; } = 100;
    [field: SerializeField] public float PlayerAttackRadius { get; private set; } = 10f;
    [field: SerializeField] public int PlayerAttackDamage { get; private set; } = 10;
    [field: SerializeField] public int PotionHPRestore { get; private set; } = 20;
    [field: SerializeField] public int EXPGetFromCrystal { get; private set; } = 1;
    [field: SerializeField] public int PlayerExpToLevelUp { get; private set; } = 10;
}
