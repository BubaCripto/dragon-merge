using DragonMerge.Board;
using DragonMerge.Items;
using UnityEngine;

namespace DragonMerge.InputSystem
{
    public class InputManager : MonoBehaviour
    {
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private Camera cam;

        private MergeItem _selected;
        private Vector2 _startPointer;

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
        }

        private void Update()
        {
            if (boardManager == null || boardManager.IsBusy) return;

            if (Input.GetMouseButtonDown(0))
            {
                _startPointer = cam.ScreenToWorldPoint(Input.mousePosition);
                _selected = PickItem(_startPointer);
            }

            if (Input.GetMouseButtonUp(0) && _selected != null)
            {
                Vector2 end = cam.ScreenToWorldPoint(Input.mousePosition);
                Vector2 delta = end - _startPointer;
                if (delta.magnitude < 0.15f)
                {
                    _selected = null;
                    return;
                }

                Vector2 dir = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                    ? new Vector2(Mathf.Sign(delta.x), 0)
                    : new Vector2(0, Mathf.Sign(delta.y));

                int tx = _selected.X + (int)dir.x;
                int ty = _selected.Y + (int)dir.y;

                if (tx >= 0 && tx < boardManager.Width && ty >= 0 && ty < boardManager.Height)
                {
                    var target = boardManager.Grid[tx, ty];
                    if (target != null && boardManager.AreNeighbors(_selected, target))
                        StartCoroutine(boardManager.TrySwapAndResolve(_selected, target));
                }

                _selected = null;
            }
        }

        private MergeItem PickItem(Vector2 worldPos)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit == null) return null;
            return hit.GetComponent<MergeItem>();
        }
    }
}
