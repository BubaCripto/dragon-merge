using UnityEngine;

namespace DragonMerge.Scripts.Environment
{
    public class CloudParallax : MonoBehaviour
    {
        [Header("Cloud Settings")]
        [Tooltip("A velocidade horizontal atual da nuvem")]
        public float speed = 1f;

        [Header("Wrap Settings")]
        [Tooltip("Quando a nuvem passa deste ponto na direita, ela reseta")]
        public float rightBound = 12f;
        [Tooltip("A nuvem reaparece neste ponto na esquerda")]
        public float resetX = -12f;

        [Header("Randomization")]
        public float minSpeed = 0.02f;
        public float maxSpeed = 0.08f;

        private void Start()
        {
            // Sorteia uma velocidade aleatória para cada nuvem quando o jogo inicia
            speed = Random.Range(minSpeed, maxSpeed);
        }

        private void Update()
        {
            // Move a nuvem para a direita
            transform.Translate(Vector3.right * speed * Time.deltaTime, Space.World);

            // Se sair da tela pela direita, reaparece do lado esquerdo com uma nova velocidade aleatória
            if (transform.position.x > rightBound)
            {
                // Reseta a posição X
                Vector3 newPos = transform.position;
                newPos.x = resetX;
                transform.position = newPos;

                // Sorteia uma nova velocidade para a próxima travessia
                speed = Random.Range(minSpeed, maxSpeed);
            }
        }
    }
}
