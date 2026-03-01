using System.Collections.Generic;
using UnityEngine;

namespace DragonMerge.Scripts.VFX
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Prefabs")]
        [Tooltip("Prefab do Sistema de Partículas de Explosão")]
        public GameObject matchParticlesPrefab;
        [Tooltip("Prefab de Fumaça de Transformação")]
        public GameObject upgradePoofPrefab;
        [Tooltip("Material ou LineRenderer para desenhar o Feixe de Luz de Match")]
        public GameObject matchLinePrefab;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        // Toca as partículas estelares de match em uma coordenada
        public void PlayMatchExplosion(Vector3 position)
        {
            if (matchParticlesPrefab != null)
            {
                // Instantiate e apaga após 2 segundos
                GameObject sparks = Instantiate(matchParticlesPrefab, position, Quaternion.identity);
                Destroy(sparks, 2f);
            }
            else
            {
                Debug.LogWarning("VFXManager: Faltando particle prefab de Match!");
            }
        }

        public void PlayUpgradePoof(Vector3 position)
        {
            if (upgradePoofPrefab != null)
            {
                GameObject poof = Instantiate(upgradePoofPrefab, position, Quaternion.identity);
                Destroy(poof, 2f);
            }
        }

        // Desenha uma linha rápida conectando itens
        public void DrawMatchLine(Vector3 start, Vector3 end)
        {
            if (matchLinePrefab != null)
            {
                GameObject lineObj = Instantiate(matchLinePrefab, Vector3.zero, Quaternion.identity);
                LineRenderer lr = lineObj.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    lr.SetPosition(0, start);
                    lr.SetPosition(1, end);
                    // Aqui precisaríamos de um fader na cor ou script que apague essa linha após 0.2s
                    Destroy(lineObj, 0.3f); 
                }
            }
        }
    }
}
