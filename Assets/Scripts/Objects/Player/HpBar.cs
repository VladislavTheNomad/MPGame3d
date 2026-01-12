using UnityEngine;

namespace MPGame3d
{
    public class HpBar : MonoBehaviour
    {
        private Camera _mainCamera;
        
        private void Awake()
        {
            _mainCamera =  Camera.main;
        }

        private void LateUpdate()
        {
            transform.rotation = Quaternion.LookRotation(_mainCamera.transform.forward);
        }
    }
}
