using System.Collections;
using DragonMerge.Board;
using DragonMerge.Items;
using UnityEngine;

namespace DragonMerge.Logic
{
    public class GravityController : MonoBehaviour
    {
        [SerializeField] private BoardManager boardManager;

        public IEnumerator CollapseAndRefill()
        {
            int width = boardManager.Width;
            int height = boardManager.Height;

            for (int x = 0; x < width; x++)
            {
                int targetY = 0;
                for (int y = 0; y < height; y++)
                {
                    MergeItem item = boardManager.Grid[x, y];
                    if (item == null) continue;

                    if (y != targetY)
                    {
                        boardManager.Grid[x, targetY] = item;
                        boardManager.Grid[x, y] = null;
                        item.SetGridPosition(x, targetY);
                        boardManager.ApplyItemVisualForGrid(item);
                        item.MoveTo(boardManager, boardManager.GetWorldPosition(x, targetY), 0.1f);
                    }

                    targetY++;
                }

                for (int y = targetY; y < height; y++)
                {
                    var spawned = boardManager.SpawnRandomEgg(x, y, fromTop: true);
                    spawned.transform.position = boardManager.GetWorldPosition(x, height + 1);
                    spawned.MoveTo(boardManager, boardManager.GetWorldPosition(x, y), 0.15f);
                }
            }

            yield return new WaitForSeconds(0.18f);
        }
    }
}
