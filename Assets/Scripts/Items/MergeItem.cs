using System.Collections;
using UnityEngine;

namespace DragonMerge.Items
{
    public enum ItemTier
    {
        Egg = 0,
        CrackedEgg = 1,
        HatchingEgg = 2,
        BabyDragon = 3
    }

    public enum ItemColor
    {
        Color0 = 0,
        Color1 = 1,
        Color2 = 2,
        Color3 = 3,
        Color4 = 4,
        Color5 = 5,
        Color6 = 6,
        Color7 = 7,
        Color8 = 8,
        Color9 = 9,
        Color10 = 10,
        Color11 = 11
    }

    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class MergeItem : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public int X { get; private set; }
        public int Y { get; private set; }
        public ItemTier Tier { get; private set; }
        public ItemColor Color { get; private set; }
        public DragonMerge.Scripts.VFX.ItemAnimator Animator { get; private set; }

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
                
            Animator = gameObject.AddComponent<DragonMerge.Scripts.VFX.ItemAnimator>();
        }

        public void SetGridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public void SetData(ItemTier tier, ItemColor color, Sprite sprite)
        {
            Tier = tier;
            Color = color;
            spriteRenderer.sprite = sprite;

            var box = GetComponent<BoxCollider2D>();
            if (box != null && sprite != null)
            {
                box.size = new Vector2(sprite.bounds.size.x, sprite.bounds.size.y);
            }
        }

        public void Highlight()
        {
            if (spriteRenderer != null) spriteRenderer.color = new UnityEngine.Color(0.8f, 0.8f, 0.8f, 1f);
            transform.localScale *= 1.1f;
        }

        public void ResetHighlight()
        {
            if (spriteRenderer != null) spriteRenderer.color = UnityEngine.Color.white;
            transform.localScale /= 1.1f;
        }

        public Coroutine MoveTo(MonoBehaviour runner, Vector3 target, float duration = 0.12f)
        {
            return runner.StartCoroutine(MoveRoutine(target, duration));
        }

        private IEnumerator MoveRoutine(Vector3 target, float duration)
        {
            Vector3 start = transform.position;
            float t = 0f;

            while (t < duration)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(start, target, t / duration);
                yield return null;
            }

            transform.position = target;
            
            // Toca a animação de mola / gelatina quando finaliza a queda
            if (Animator != null)
            {
                Animator.AnimateDropBounce();
            }
        }
    }
}
