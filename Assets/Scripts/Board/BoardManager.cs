using System.Collections;
using System.Linq;
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

        [Header("Board Mapping")]
        [SerializeField] private SpriteRenderer boardRenderer;
        [SerializeField] private bool usePerspectiveGrid = true;
        [SerializeField, Tooltip("Canto inferior esquerdo da área jogável (normalizado 0..1)")]
        private Vector2 bottomLeftN = new(0.18f, 0.24f);
        [SerializeField] private Vector2 bottomRightN = new(0.82f, 0.24f);
        [SerializeField] private Vector2 topLeftN = new(0.27f, 0.82f);
        [SerializeField] private Vector2 topRightN = new(0.73f, 0.82f);

        [Header("Visuals")]
        [SerializeField, Range(0.35f, 1.0f)] private float itemFillRatio = 0.62f;
        [SerializeField, Range(0.5f, 1.5f)] private float bottomRowScaleBoost = 1.18f;
        [SerializeField, Range(0.3f, 1.2f)] private float topRowScaleBoost = 0.82f;
        [SerializeField] private Sprite[] eggSprites;
        [SerializeField] private Sprite[] crackedSprites;
        [SerializeField] private Sprite[] hatchingSprites;
        [SerializeField] private Sprite[] babyDragonSprites;

        [Header("Fallback (se não achar board)")]
        [SerializeField] private float fallbackCellSize = 1f;
        [SerializeField] private Vector2 fallbackOffset = Vector2.zero;

        [Header("Refs")]
        [SerializeField] private MergeController mergeController;
        [SerializeField] private GravityController gravityController;
        [SerializeField] private GameManager gameManager;

        public int Width => width;
        public int Height => height;
        public MergeItem[,] Grid { get; private set; }
        public bool IsBusy { get; private set; }

        private Transform _itemRoot;
        private bool _hasMappedBoard;
        private Vector3 _bottomLeft;
        private Vector3 _bottomRight;
        private Vector3 _topLeft;
        private Vector3 _topRight;

        private void Awake()
        {
            Grid = new MergeItem[width, height];
            _itemRoot = new GameObject("ItemsRoot").transform;
            _itemRoot.SetParent(transform, false);
        }

        private IEnumerator Start()
        {
            AutoWireEditorSprites();
            ResolveBoardLayout();
            yield return FillBoardWithoutStartingMatches();
        }

        private void AutoWireEditorSprites()
        {
#if UNITY_EDITOR
            if (eggSprites == null || eggSprites.Length == 0)
                eggSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Environment/ovos.png").OfType<Sprite>().ToArray();
            if (crackedSprites == null || crackedSprites.Length == 0)
                crackedSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Environment/ovo-rachando.png").OfType<Sprite>().ToArray();
            if (hatchingSprites == null || hatchingSprites.Length == 0)
                hatchingSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Environment/ovo-chocando.png").OfType<Sprite>().ToArray();
            if (babyDragonSprites == null || babyDragonSprites.Length == 0)
                babyDragonSprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Environment/dragao-bebe.png").OfType<Sprite>().ToArray();
#endif
        }

        private void ResolveBoardLayout()
        {
            if (boardRenderer == null)
            {
                boardRenderer = FindObjectsOfType<SpriteRenderer>()
                    .FirstOrDefault(s => s.name.ToLower().Contains("taboleiro5x8") || s.name.ToLower().Contains("tabuleiro"));
            }

            _hasMappedBoard = boardRenderer != null && boardRenderer.sprite != null;
            if (!_hasMappedBoard) return;

            _bottomLeft = NormalizedToWorld(bottomLeftN);
            _bottomRight = NormalizedToWorld(bottomRightN);
            _topLeft = NormalizedToWorld(topLeftN);
            _topRight = NormalizedToWorld(topRightN);
        }

        private Vector3 NormalizedToWorld(Vector2 n)
        {
            var size = boardRenderer.sprite.bounds.size;
            var local = new Vector3((n.x - 0.5f) * size.x, (n.y - 0.5f) * size.y, 0f);
            return boardRenderer.transform.TransformPoint(local);
        }

        public Vector3 GetWorldPosition(int x, int y)
        {
            if (!_hasMappedBoard || !usePerspectiveGrid)
                return new Vector3(fallbackOffset.x + x * fallbackCellSize, fallbackOffset.y + y * fallbackCellSize, 0f);

            float t = (y + 0.5f) / height;
            float u = (x + 0.5f) / width;

            Vector3 left = Vector3.Lerp(_bottomLeft, _topLeft, t);
            Vector3 right = Vector3.Lerp(_bottomRight, _topRight, t);
            return Vector3.Lerp(left, right, u);
        }

        private float GetCellWorldSizeAtRow(int y)
        {
            if (!_hasMappedBoard || !usePerspectiveGrid)
                return fallbackCellSize;

            float t = Mathf.Clamp01((y + 0.5f) / height);
            Vector3 left = Vector3.Lerp(_bottomLeft, _topLeft, t);
            Vector3 right = Vector3.Lerp(_bottomRight, _topRight, t);
            float rowWidthCell = Vector3.Distance(left, right) / width;

            float perspectiveBoost = Mathf.Lerp(bottomRowScaleBoost, topRowScaleBoost, t);
            return rowWidthCell * perspectiveBoost;
        }

        public MergeItem SpawnRandomEgg(int x, int y, bool fromTop = false)
        {
            int paletteSize = Mathf.Min(5, eggSprites != null ? eggSprites.Length : 5);
            var color = (ItemColor)Random.Range(0, Mathf.Max(1, paletteSize));
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
            ApplyItemVisualForGrid(item);
            Grid[x, y] = item;

            return item;
        }

        public void ApplyItemVisualForGrid(MergeItem item)
        {
            if (item == null) return;
            var sr = item.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return;

            float cellTarget = GetCellWorldSizeAtRow(item.Y) * itemFillRatio;
            Vector2 spriteSize = sr.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;

            float scale = cellTarget / Mathf.Max(spriteSize.x, spriteSize.y);
            item.transform.localScale = Vector3.one * scale;
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
            ApplyItemVisualForGrid(item);
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
            ApplyItemVisualForGrid(a);
            ApplyItemVisualForGrid(b);

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
                ApplyItemVisualForGrid(a);
                ApplyItemVisualForGrid(b);

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
