using UnityEngine;

namespace Stage
{
    public class RevolveAround : MonoBehaviour
    {
        [Header("回転中心オブジェクト")]
        [SerializeField] private Transform centerObject;
        [Header("1秒あたりの回転速度（度）")]
        [SerializeField] private float rotateSpeed = 90f;

        void Update()
        {
            if (centerObject == null) return;
            transform.RotateAround(centerObject.position, Vector3.forward, rotateSpeed * Time.deltaTime);
        }
    }
}