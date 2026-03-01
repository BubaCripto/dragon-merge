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
        Red = 0,
        Blue = 1,
        Green = 2,
        Yellow = 3,
        Purple = 4
    }

    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class MergeItem : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        public int X { get; private set; }
        public int Y { get; private set; }
        public ItemTier Tier { get; private set; }
        public ItemColor Color { get; private set; }

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
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
        }
    }
}
