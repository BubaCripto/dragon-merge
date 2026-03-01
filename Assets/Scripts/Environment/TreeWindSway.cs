using UnityEngine;

namespace DragonMerge.Scripts.Environment
{
    public class TreeWindSway : MonoBehaviour
    {
        [Header("Bounce Settings")]
        [Tooltip("O quanto a árvore vai esticar/encolher em Y (ex: 0.05 significa 5% maior/menor)")]
        public float bounceAmount = 0.05f; 
        
        [Tooltip("Velocidade do balanço")]
        public float bounceSpeed = 1.5f; 

        // Variação de tempo para que nem todas as árvores balancem exatamente em sincronia
        private float timeOffset;
        private Vector3 baseScale;
        
        // Variáveis para ancorar a base
        private Vector3 basePosition;
        private float unscaledSpriteHeight = 1f;

        private void Start()
        {
            // Guarda o tamanho e posição originais que foram setados no Bootstrap
            baseScale = transform.localScale;
            basePosition = transform.position;

            // Busca a altura real do sprite para saber o quanto precisamos compensar
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                // bounds do sprite já nos dão o tamanho base sem escala
                unscaledSpriteHeight = sr.sprite.bounds.size.y;
            }

            // Cria um offset aleatório para que o seno de cada árvore seja diferente
            timeOffset = Random.Range(0f, 100f);

            // Causa uma variação de velocidade sutil para cada árvore
            bounceSpeed += Random.Range(-0.2f, 0.5f);
            
            // Causa uma variação na força do esticão também (umas mexem mais, outras menos)
            bounceAmount += Random.Range(-0.02f, 0.04f);
        }

        private void Update()
        {
            // Calcula o quanto ela vai esticar ou encolher baseado no tempo usando a função Seno.
            float scaleYOffset = Mathf.Sin((Time.time + timeOffset) * bounceSpeed) * bounceAmount;

            // Muda a escala
            Vector3 newScale = baseScale;
            newScale.y = baseScale.y + scaleYOffset;
            transform.localScale = newScale;

            // Se o centro de rotação (pivot) for o meio exato do sprite (comum no Unity = 0.5), 
            // a figura vai crescer pra cima e pra baixo ao mesmo tempo.
            // Para manter a Base sempre fincada no chão, nós subimos a posição exata da figura
            // pela METADE do quanto ela cresceu neste instante.
            float positionYShift = (scaleYOffset * unscaledSpriteHeight) / 2f;
            
            // Subimos na mesma direção em que a árvore aponta (transform.up), 
            // no caso de estarem inclinadas/rotacionadas.
            transform.position = basePosition + (transform.up * positionYShift);
        }
    }
}
