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
        private Vector2 bottomLeftCoord = new(0.068f, 0.148f);
        [SerializeField] private Vector2 bottomRightCoord = new(0.932f, 0.148f);
        [SerializeField] private Vector2 topLeftCoord = new(0.175f, 0.872f);
        [SerializeField] private Vector2 topRightCoord = new(0.825f, 0.872f);
        [SerializeField, Tooltip("Curva Vertical (Ajuste fino! Ex: 0.88. Mais perto de 1 desce os debaixo)")]
        private float verticalPerspectiveCurve = 0.75f;

        [Header("Manual Y (Fileira a Fileira)")]
        [SerializeField, Tooltip("Desliga a curva matemática e usa as 8 medidas abaixo exatas")]
        private bool useManualY = true;
        [SerializeField, Tooltip("Onde 0 = Base (BottomLeft) e 1 = Topo (TopLeft). Índice 0 = primeira linha de baixo")]
        private float[] rowYPositions = new float[] {
            0.22f, 0.34f, 0.47f, 0.59f, 0.70f, 0.79f, 0.87f, 0.95f
        };

        [Header("Manual X (Coluna a Coluna)")]
        [SerializeField, Tooltip("Desliga o espaçamento igual e usa as 5 medidas de largura")]
        private bool useManualX = true;
        [SerializeField, Tooltip("Onde 0 = Esquerda e 1 = Direita. Padrão para 5 itens: 0.1, 0.3, 0.5, 0.7, 0.9")]
        private float[] colXPositions = new float[] {
            0.1f, 0.3f, 0.5f, 0.7f, 0.9f
        };

        [Header("Visuals")]
        [SerializeField, Range(0.35f, 1.2f)] private float itemFillRatio = 0.85f;
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
                boardRenderer = FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                    .FirstOrDefault(s => s.name.ToLower().Contains("taboleiro5x8") || s.name.ToLower().Contains("tabuleiro"));
            }

            _hasMappedBoard = boardRenderer != null && boardRenderer.sprite != null;
            if (!_hasMappedBoard) return;

            _bottomLeft = NormalizedToWorld(bottomLeftCoord);
            _bottomRight = NormalizedToWorld(bottomRightCoord);
            _topLeft = NormalizedToWorld(topLeftCoord);
            _topRight = NormalizedToWorld(topRightCoord);
        }

        private Vector3 NormalizedToWorld(Vector2 n)
        {
            var size = boardRenderer.sprite.bounds.size;
            var local = new Vector3((n.x - 0.5f) * size.x, (n.y - 0.5f) * size.y, 0f);
            return boardRenderer.transform.TransformPoint(local);
        }

        private float GetVerticalPerspectiveT(float linearT, int yIndex)
        {
            if (useManualY && rowYPositions != null && yIndex >= 0 && yIndex < rowYPositions.Length)
            {
                // Se o modo manual estiver ligado, basta pegar o valor exato no array de 8 fileiras
                return rowYPositions[yIndex];
            }

            if (!usePerspectiveGrid) return linearT;
            return Mathf.Pow(linearT, verticalPerspectiveCurve);
        }

        public Vector3 GetWorldPosition(int x, int y)
        {
            if (!_hasMappedBoard || !usePerspectiveGrid)
                return new Vector3(fallbackOffset.x + x * fallbackCellSize, fallbackOffset.y + y * fallbackCellSize, 0f);

            float linearT = (y + 0.5f) / height;
            float tY = GetVerticalPerspectiveT(linearT, y);
            
            float u = (x + 0.5f) / width;
            if (useManualX && colXPositions != null && x >= 0 && x < colXPositions.Length)
            {
                u = colXPositions[x];
            }

            // HORIZONTAL: Mantém a interpolação linear original para não quebrar a largura
            Vector3 linearLeft = Vector3.Lerp(_bottomLeft, _topLeft, linearT);
            Vector3 linearRight = Vector3.Lerp(_bottomRight, _topRight, linearT);
            float finalX = Mathf.Lerp(linearLeft.x, linearRight.x, u);

            // VERTICAL: Aplica a nossa curva de elevação (tY) para subir os ovos inferiores
            Vector3 perspLeft = Vector3.Lerp(_bottomLeft, _topLeft, tY);
            Vector3 perspRight = Vector3.Lerp(_bottomRight, _topRight, tY);
            float finalY = Mathf.Lerp(perspLeft.y, perspRight.y, u);

            return new Vector3(finalX, finalY, 0f);
        }

        private float GetCellWorldSizeAtRow(int y)
        {
            if (!_hasMappedBoard || !usePerspectiveGrid)
                return fallbackCellSize;

            float linearT = Mathf.Clamp01((y + 0.5f) / height);
            
            // Usa as margens originais para a escala, para manter proporções agradáveis
            Vector3 left = Vector3.Lerp(_bottomLeft, _topLeft, linearT);
            Vector3 right = Vector3.Lerp(_bottomRight, _topRight, linearT);
            float rowWidth = Vector3.Distance(left, right) / width;

            return rowWidth;
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
            Vector3 finalScale = Vector3.one * scale;
            
            item.transform.localScale = finalScale;
            
            if (item.Animator != null)
            {
                item.Animator.UpdateBaseScale(finalScale);
            }
        }

        public Sprite GetSprite(ItemTier tier, ItemColor color)
        {
            int baseIdx = (int)color;
            int finalIdx = baseIdx;

            switch (tier)
            {
                case ItemTier.CrackedEgg:
                    int[] crackedMap = { 0, 2, 4, 6, 1, 5, 3, 7, 15, 14, 13, 11 };
                    finalIdx = baseIdx < crackedMap.Length ? crackedMap[baseIdx] : baseIdx;
                    if (crackedSprites != null && crackedSprites.Length > 0)
                        return crackedSprites[Mathf.Clamp(finalIdx, 0, crackedSprites.Length - 1)];
                    break;

                case ItemTier.HatchingEgg:
                    int[] hatchingMap = { 0, 1, 2, 3, 7, 5, 6, 7, 8, 9, 10, 11 };
                    finalIdx = baseIdx < hatchingMap.Length ? hatchingMap[baseIdx] : baseIdx;
                    if (hatchingSprites != null && hatchingSprites.Length > 0)
                        return hatchingSprites[Mathf.Clamp(finalIdx, 0, hatchingSprites.Length - 1)];
                    break;

                case ItemTier.BabyDragon:
                    int[] babyMap = { 0, 1, 2, 3, 8, 0, 7, 8, 12, 11, 9, 10 };
                    finalIdx = baseIdx < babyMap.Length ? babyMap[baseIdx] : baseIdx;
                    if (babyDragonSprites != null && babyDragonSprites.Length > 0)
                        return babyDragonSprites[Mathf.Clamp(finalIdx, 0, babyDragonSprites.Length - 1)];
                    break;

                case ItemTier.Egg:
                default:
                    if (eggSprites != null && eggSprites.Length > 0)
                        return eggSprites[Mathf.Clamp(baseIdx, 0, eggSprites.Length - 1)];
                    break;
            }

            return null;
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
