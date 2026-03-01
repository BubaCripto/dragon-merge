using DragonMerge.Board;
using DragonMerge.Items;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DragonMerge.InputSystem
{
    public class InputManager : MonoBehaviour
    {
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private Camera cam;

        private MergeItem _selected;
        private Vector2 _startPointer;
        private bool _isDragging;

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
        }

        private void Update()
        {
            if (boardManager == null || boardManager.IsBusy) 
            {
                if (_selected != null)
                {
                    _selected.ResetHighlight();
                    _selected = null;
                    _isDragging = false;
                }
                return;
            }

            bool pressedThisFrame;
            bool releasedThisFrame;
            Vector2 pointerScreenPos;

#if ENABLE_INPUT_SYSTEM
            var pointer = Pointer.current;
            if (pointer == null) return;
            pressedThisFrame = pointer.press.wasPressedThisFrame;
            releasedThisFrame = pointer.press.wasReleasedThisFrame;
            pointerScreenPos = pointer.position.ReadValue();
#else
            pressedThisFrame = Input.GetMouseButtonDown(0);
            releasedThisFrame = Input.GetMouseButtonUp(0);
            pointerScreenPos = Input.mousePosition;
#endif

            if (pressedThisFrame)
            {
                _startPointer = cam.ScreenToWorldPoint(pointerScreenPos);
                MergeItem hitItem = PickItem(_startPointer);

                if (hitItem != null)
                {
                    if (_selected == null)
                    {
                        // 1. Não tinha outro marcado... marca este!
                        _selected = hitItem;
                        _selected.Highlight();
                        _isDragging = true;
                    }
                    else
                    {
                        // 2. Já tinha um marcado. Vamos checar nossa relação com ele:
                        if (hitItem == _selected)
                        {
                            // Clicou duas vezes na mesma peça -> Desmarcar
                            _selected.ResetHighlight();
                            _selected = null;
                            _isDragging = false;
                        }
                        else if (boardManager.AreNeighbors(_selected, hitItem))
                        {
                            // Vizinhos -> Inicia troca e desmarca!
                            _selected.ResetHighlight();
                            StartCoroutine(boardManager.TrySwapAndResolve(_selected, hitItem));
                            _selected = null;
                            _isDragging = false;
                        }
                        else
                        {
                            // Longe um do outro -> Tira o highlight do velho e põe no novo!
                            _selected.ResetHighlight();
                            _selected = hitItem;
                            _selected.Highlight();
                            _isDragging = true;
                        }
                    }
                }
                else
                {
                    // Clicou no nada -> Desmarca qualquer ovo!
                    if (_selected != null)
                    {
                        _selected.ResetHighlight();
                        _selected = null;
                        _isDragging = false;
                    }
                }
            }

            if (releasedThisFrame && _isDragging && _selected != null)
            {
                Vector2 end = cam.ScreenToWorldPoint(pointerScreenPos);
                Vector2 delta = end - _startPointer;
                
                // Só conta como "Arrastar" (Swipe) se mexeu o mouse pelo menos uma pequena distância
                if (delta.magnitude > 0.15f)
                {
                    Vector2 dir = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                        ? new Vector2(Mathf.Sign(delta.x), 0)
                        : new Vector2(0, Mathf.Sign(delta.y));

                    int tx = _selected.X + (int)dir.x;
                    int ty = _selected.Y + (int)dir.y;

                    if (tx >= 0 && tx < boardManager.Width && ty >= 0 && ty < boardManager.Height)
                    {
                        var target = boardManager.Grid[tx, ty];
                        if (target != null && boardManager.AreNeighbors(_selected, target))
                        {
                            _selected.ResetHighlight();
                            StartCoroutine(boardManager.TrySwapAndResolve(_selected, target));
                            _selected = null;
                        }
                    }

                    // Ao soltar de um Swype (independente de ter dado certo a direção), limpa!
                    if (_selected != null)
                    {
                        _selected.ResetHighlight();
                        _selected = null;
                    }
                }
                
                _isDragging = false;
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
