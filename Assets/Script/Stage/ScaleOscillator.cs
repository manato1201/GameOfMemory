using UnityEngine;

namespace Stage
{
    public class ScaleOscillator : MonoBehaviour
    {
        public enum Axis { X, Y }
        [Header("伸縮設定")]
        [SerializeField] private Axis scaleAxis = Axis.X;
        [SerializeField] private float min = 0.5f;
        [SerializeField] private float max = 2f;
        [SerializeField] private float speed = 1f;

        [Header("反転時に切り替えるオブジェクト")]
        [SerializeField] private GameObject showOnMin;
        [SerializeField] private GameObject showOnMax;

        private float direction = 1f;

        void Update()
        {
            float value = scaleAxis == Axis.X ? transform.localScale.x : transform.localScale.y;
            value += speed * direction * Time.deltaTime;

            // 反転チェック
            if (direction > 0 && value >= max)
            {
                value = max;
                direction *= -1f;
                if (showOnMin) showOnMin.SetActive(false);
                if (showOnMax) showOnMax.SetActive(true);
            }
            else if (direction < 0 && value <= min)
            {
                value = min;
                direction *= -1f;
                if (showOnMin) showOnMin.SetActive(true);
                if (showOnMax) showOnMax.SetActive(false);
            }

            var scale = transform.localScale;
            if (scaleAxis == Axis.X) scale.x = value;
            else scale.y = value;
            transform.localScale = scale;
        }
    }
}