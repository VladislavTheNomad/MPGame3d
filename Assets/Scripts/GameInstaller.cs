using UnityEngine;
using Zenject;

namespace MPGame3d
{
    public class GameInstaller : MonoInstaller
    {
        [SerializeField] private MultiplayerService _multiplayerService;
        [SerializeField] private GameConfigs _gameConfigs;
        [SerializeField] private UIService _uiService;
        
        public override void InstallBindings()
        {
            Container.Bind<UIService>().FromInstance(_uiService).AsSingle();
            Container.Bind<GameConfigs>().FromInstance(_gameConfigs).AsSingle().NonLazy();
            Container.BindInterfacesAndSelfTo<MultiplayerService>().FromInstance(_multiplayerService).AsSingle();
            Container.BindInterfacesAndSelfTo<GameplayService>().AsSingle();
            
            ZenjectAccessor.SetContainer(Container);
        }
    }
}
