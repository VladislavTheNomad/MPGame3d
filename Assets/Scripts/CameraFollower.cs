using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollower : MonoBehaviour
{
    public static CameraFollower cameraFollower;
    
    [SerializeField] private Vector3 _offset = new Vector3(0, 10, -5);
    
    private Transform _target;

    private void Awake()
    {
        cameraFollower = this;
    }
    
    public void SetTarget(Transform target) =>  _target = target;

    private void LateUpdate()
    {
        if (_target == null) return;
        
        transform.position = _target.position + _offset;
        transform.LookAt(_target);
    }
}
