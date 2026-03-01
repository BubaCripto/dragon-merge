using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DragonMerge.InputSystem;
using DragonMerge.Items;
using DragonMerge.Logic;
using UnityEngine;

namespace DragonMerge.Board
{
    public class BoardManager : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private int width = 5;
        [SerializeField] private int height = 8;
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector2 boardOffset = Vector2.zero;

        [Header("Visuals")]
        [SerializeField] private Sprite[] eggSprites;
        [SerializeField] private Sprite[] crackedSprites;
        [SerializeField] private Sprite[] hatchingSprites;
        [SerializeField] private Sprite[] babyDragonSprites;

        [Header("Refs")]
        [SerializeField] private MergeController mergeController;
        [SerializeField] private GravityController gravityController;
        [SerializeField] private GameManager gameManager;

        public int Width => width;
        public int Height => height;
        public MergeItem[,] Grid { get; private set; }
        public bool IsBusy { get; private set; }

        private Transform _itemRoot;

        private void Awake()
        {
            Grid = new MergeItem[width, height];
            _itemRoot = new GameObject("ItemsRoot").transform;
            _itemRoot.SetParent(transform, false);
        }

        private IEnumerator Start()
        {
            AutoWireEditorSprites();
            yield return FillBoardWithoutStartingMatches();
        }

        private void AutoWireEditorSprites()
        {
#if UNITY_EDITOR
            if (eggSprites != null && eggSprites.Length > 0) return;
            eggSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Environment/ovos.png").OfType<Sprite>().ToArray();
            crackedSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Environment/ovo-rachando.png").OfType<Sprite>().ToArray();
            hatchingSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Environment/ovo-chocando.png").OfType<Sprite>().ToArray();
            babyDragonSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Environment/dragao-bebe.png").OfType<Sprite>().ToArray();
#endif
        }

        public Vector3 GetWorldPosition(int x, int y) => new Vector3(boardOffset.x + x * cellSize, boardOffset.y + y * cellSize, 0f);

        public MergeItem SpawnRandomEgg(int x, int y, bool fromTop = false)
        {
            var color = (ItemColor)Random.Range(0, Mathf.Min(5, eggSprites.Length));
            return SpawnItem(x, y, ItemTier.Egg, color, fromTop);
        }

        public MergeItem SpawnItem(int x, int y, ItemTier tier, ItemColor color, bool fromTop = false)
        {
            GameObject go = new GameObject($"{tier}_{color}_{x}_{y}");
            go.transform.SetParent(_itemRoot);
            go.transform.position = GetWorldPosition(x, y);
            go.layer = LayerMask.NameToLayer("Default");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            var item = go.AddComponent<MergeItem>();

            item.SetGridPosition(x, y);
            item.SetData(tier, color, GetSprite(tier, color));
            Grid[x, y] = item;

            return item;
        }

        public Sprite GetSprite(ItemTier tier, ItemColor color)
        {
            int idx = (int)color;
            Sprite[] source = tier switch
            {
                ItemTier.Egg => eggSprites,
                ItemTier.CrackedEgg => crackedSprites,
                ItemTier.HatchingEgg => hatchingSprites,
                ItemTier.BabyDragon => babyDragonSprites,
                _ => eggSprites
            };

            if (source == null || source.Length == 0)
                return null;

            return source[Mathf.Clamp(idx, 0, source.Length - 1)];
        }

        public void UpgradeItem(MergeItem item, ItemTier newTier)
        {
            item.SetData(newTier, item.Color, GetSprite(newTier, item.Color));
        }

        public void ClearCell(int x, int y, bool destroyObject)
        {
            var item = Grid[x, y];
            Grid[x, y] = null;
            if (destroyObject && item != null)
                Destroy(item.gameObject);
        }

        private IEnumerator FillBoardWithoutStartingMatches()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int safe = 0;
                    do
                    {
                        if (Grid[x, y] != null) Destroy(Grid[x, y].gameObject);
                        SpawnRandomEgg(x, y);
                        safe++;
                    }
                    while (CreatesInitialMatch(x, y) && safe < 50);
                }
            }

            yield return null;
        }

        private bool CreatesInitialMatch(int x, int y)
        {
            MergeItem current = Grid[x, y];
            if (current == null) return false;

            if (x >= 2 && IsSame(Grid[x - 1, y], current) && IsSame(Grid[x - 2, y], current)) return true;
            if (y >= 2 && IsSame(Grid[x, y - 1], current) && IsSame(Grid[x, y - 2], current)) return true;
            return false;
        }

        private bool IsSame(MergeItem a, MergeItem b) => a != null && b != null && a.Tier == b.Tier && a.Color == b.Color;

        public IEnumerator TrySwapAndResolve(MergeItem a, MergeItem b)
        {
            if (IsBusy || gameManager.IsGameOver) yield break;
            IsBusy = true;

            gameManager.SpendMove();

            int ax = a.X; int ay = a.Y;
            int bx = b.X; int by = b.Y;

            Grid[ax, ay] = b;
            Grid[bx, by] = a;
            a.SetGridPosition(bx, by);
            b.SetGridPosition(ax, ay);

            a.MoveTo(this, GetWorldPosition(bx, by));
            b.MoveTo(this, GetWorldPosition(ax, ay));
            yield return new WaitForSeconds(0.14f);

            var matches = MatchDetector.FindMatches(Grid, width, height);
            if (matches.Count == 0)
            {
                Grid[ax, ay] = a;
                Grid[bx, by] = b;
                a.SetGridPosition(ax, ay);
                b.SetGridPosition(bx, by);

                a.MoveTo(this, GetWorldPosition(ax, ay));
                b.MoveTo(this, GetWorldPosition(bx, by));
                yield return new WaitForSeconds(0.14f);
                IsBusy = false;
                yield break;
            }

            while (matches.Count > 0)
            {
                mergeController.ResolveMatches(matches, a);
                yield return gravityController.CollapseAndRefill();
                matches = MatchDetector.FindMatches(Grid, width, height);
            }

            IsBusy = false;
        }

        public bool AreNeighbors(MergeItem a, MergeItem b)
        {
            int dx = Mathf.Abs(a.X - b.X);
            int dy = Mathf.Abs(a.Y - b.Y);
            return dx + dy == 1;
        }
    }
}
