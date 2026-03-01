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

                // Dispara a Explosão de partículas no centro do alvo principal
                if (DragonMerge.Scripts.VFX.VFXManager.Instance != null)
                {
                    DragonMerge.Scripts.VFX.VFXManager.Instance.PlayMatchExplosion(upgradeTarget.transform.position);
                }

                for (int i = 0; i < group.Count; i++)
                {
                    if (group[i] == upgradeTarget) continue;
                    
                    // Desenha uma linha de luz conectando as peças combinadas ao alvo
                    if (DragonMerge.Scripts.VFX.VFXManager.Instance != null)
                    {
                        DragonMerge.Scripts.VFX.VFXManager.Instance.DrawMatchLine(group[i].transform.position, upgradeTarget.transform.position);
                    }
                    
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
                    
                    // Toca animação de Pump / Upgrade na peça que evoluiu
                    if (upgradeTarget.Animator != null) upgradeTarget.Animator.AnimateUpgrade();
                    
                    // Spawna nuvem de fumaça de transformação
                    if (DragonMerge.Scripts.VFX.VFXManager.Instance != null)
                    {
                        DragonMerge.Scripts.VFX.VFXManager.Instance.PlayUpgradePoof(upgradeTarget.transform.position);
                    }
                }
            }

            return hadAny;
        }
    }
}
