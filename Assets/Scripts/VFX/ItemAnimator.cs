using System.Collections;
using UnityEngine;

namespace DragonMerge.Scripts.VFX
{
    public class ItemAnimator : MonoBehaviour
    {
        private Coroutine currentAnimation;
        private Vector3 baseScale;
        
        private void Awake()
        {
            baseScale = transform.localScale;
        }

        public void UpdateBaseScale(Vector3 newScale)
        {
            baseScale = newScale;
        }

        // Animação de quando as peças surgem (pop in com bounce elástico)
        public void AnimateSpawn(float delay = 0f)
        {
            if (currentAnimation != null) StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(SpawnRoutine(delay));
        }

        // Animação clássica de queda (amassou embaixo e voltou pro lugar)
        public void AnimateDropBounce()
        {
            if (currentAnimation != null) StopCoroutine(currentAnimation);
            currentAnimation = StartCoroutine(SquashAndStretchRoutine(1.2f, 0.8f, 0.3f));
        }

        // Animação de quando vira um dragão/item superior a partir do merge
        public void AnimateUpgrade()
        {
            if (currentAnimation != null) StopCoroutine(currentAnimation);
            // Ao fazer o Upgrade, a BoardManager já calculou e setou o novo tamanho real do sprite
            baseScale = transform.localScale;
            currentAnimation = StartCoroutine(SquashAndStretchRoutine(1.4f, 0.7f, 0.5f));
        }

        private IEnumerator SpawnRoutine(float delay)
        {
            transform.localScale = Vector3.zero;
            if (delay > 0) yield return new WaitForSeconds(delay);
            
            float duration = 0.4f;
            float time = 0f;

            while (time < duration)
            {
                time += Time.deltaTime;
                float t = time / duration;
                
                // Matematica de Elastic Out pra dar aquele pop suculento de bala
                float scaleT = EaseOutElastic(t);
                
                transform.localScale = baseScale * scaleT;
                yield return null;
            }
            transform.localScale = baseScale;
        }

        private IEnumerator SquashAndStretchRoutine(float stretchX, float stretchY, float duration)
        {
            float time = 0f;
            float halfDuration = duration / 2f;

            // Primeiro achata
            while (time < halfDuration)
            {
                time += Time.deltaTime;
                float t = time / halfDuration;
                
                Vector3 targetScale = new Vector3(baseScale.x * stretchX, baseScale.y * stretchY, baseScale.z);
                transform.localScale = Vector3.Lerp(baseScale, targetScale, t);
                yield return null;
            }

            // Depois volta
            time = 0f;
            Vector3 squashedScale = transform.localScale;
            
            while (time < halfDuration)
            {
                time += Time.deltaTime;
                float t = time / halfDuration;
                
                // Usando uma curva suave (EaseOutBack manual)
                transform.localScale = Vector3.Lerp(squashedScale, baseScale, EaseOutBack(t));
                yield return null;
            }
            
            transform.localScale = baseScale;
        }
        
        // --- Funções Matemáticas de Animação (Juice Curves) ---
        private float EaseOutElastic(float x)
        {
            float c4 = (2 * Mathf.PI) / 3;
            return x == 0 ? 0 : x == 1 ? 1 : Mathf.Pow(2, -10 * x) * Mathf.Sin((x * 10 - 0.75f) * c4) + 1;
        }

        private float EaseOutBack(float x)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(x - 1, 3) + c1 * Mathf.Pow(x - 1, 2);
        }
    }
}
