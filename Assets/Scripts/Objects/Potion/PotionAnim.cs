using UnityEngine;

namespace MPGame3d
{
    public class PotionAnim : MonoBehaviour
    {
        private Vector3 _startPos;
        
        private void Start()
        {
            _startPos =  transform.position;
        }
        private void Update()
        {
            float offset = Mathf.Sin(Time.time) * 0.5f;
            transform.position = _startPos + new Vector3(0, offset, 0);
        }
    }
}
