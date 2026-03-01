using System.Collections.Generic;
using DragonMerge.Board;
using DragonMerge.Items;
using UnityEngine;

namespace DragonMerge.Logic
{
    public class MergeController : MonoBehaviour
    {
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private GameManager gameManager;

        public int BabyDragonsCollected { get; private set; }

        public bool ResolveMatches(List<List<MergeItem>> matches, MergeItem preferredUpgradeTarget)
        {
            if (matches == null || matches.Count == 0)
                return false;

            bool hadAny = false;

            foreach (var group in matches)
            {
                if (group == null || group.Count < 3) continue;
                hadAny = true;

                MergeItem upgradeTarget = preferredUpgradeTarget != null && group.Contains(preferredUpgradeTarget)
                    ? preferredUpgradeTarget
                    : group[0];

                ItemTier nextTier = (ItemTier)Mathf.Min((int)upgradeTarget.Tier + 1, (int)ItemTier.BabyDragon);

                for (int i = 0; i < group.Count; i++)
                {
                    if (group[i] == upgradeTarget) continue;
                    boardManager.ClearCell(group[i].X, group[i].Y, destroyObject: true);
                }

                if (nextTier == ItemTier.BabyDragon)
                {
                    BabyDragonsCollected++;
                    gameManager.AddScore(100);
                    boardManager.ClearCell(upgradeTarget.X, upgradeTarget.Y, destroyObject: true);
                }
                else
                {
                    boardManager.UpgradeItem(upgradeTarget, nextTier);
                    gameManager.AddScore(25);
                }
            }

            return hadAny;
        }
    }
}
