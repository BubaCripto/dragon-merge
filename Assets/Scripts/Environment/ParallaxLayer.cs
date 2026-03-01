using UnityEngine;

namespace DragonMerge.Environment
{
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private float speed = 0.1f;

        private Vector3 _start;

        private void Start()
        {
            _start = transform.position;
        }

        private void Update()
        {
            transform.position = _start + new Vector3(Mathf.Sin(Time.time * speed) * 0.1f, 0f, 0f);
        }
    }
}
