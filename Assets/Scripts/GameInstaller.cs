using Cinemachine;
using UnityEngine;
using Zenject;

public class GameInstaller : MonoInstaller
{
    [SerializeField] private MultiplayerService _multiplayerService;
    [SerializeField] private GameConfigs _gameConfigs;

    public override void InstallBindings()
    {
        Container.Bind<GameConfigs>().FromInstance(_gameConfigs).AsSingle().NonLazy();
        Container.BindInterfacesAndSelfTo<MultiplayerService>().FromInstance(_multiplayerService).AsSingle();
        Container.BindInterfacesAndSelfTo<GameplayService>().AsSingle();
    }
}
