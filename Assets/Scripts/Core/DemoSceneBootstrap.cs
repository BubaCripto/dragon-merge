using DragonMerge.Board;
using DragonMerge.Environment;
using DragonMerge.InputSystem;
using DragonMerge.Logic;
using UnityEngine;

namespace DragonMerge.Core
{
    public class DemoSceneBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Object.FindFirstObjectByType<BoardManager>() != null) return;

            var camera = Camera.main;
            if (camera == null)
            {
                var camGo = new GameObject("Main Camera");
                camera = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
                camera.orthographic = true;
            }

            camera.orthographic = true;
            camera.orthographicSize = 6.8f;
            camera.transform.position = new Vector3(2f, 3.5f, -10f);
            
            // Adiciona o script de responsividade garantindo que o tabuleiro nunca seja cortado nas laterais em celulares
            if (camera.GetComponent<MobileCameraFitter>() == null)
                camera.gameObject.AddComponent<MobileCameraFitter>();

            CreateBackground("Assets/Sprites/Environment/ceu-cenario.png", -8, 0.0f);
            CreateBackground("Assets/Sprites/Environment/cachoeira.png", -7, 0.2f);
            CreateBackground("Assets/Sprites/Environment/mega-arvores.png", -6, 0.35f);
            var boardSr = CreateBackground("Assets/Sprites/Environment/taboleiro5x8.png", -5, 0f, new Vector3(2f, 3.5f, 0f));

            var root = new GameObject("DragonMergeRuntime");
            var gm = root.AddComponent<GameManager>();
            var mc = root.AddComponent<MergeController>();
            var gc = root.AddComponent<GravityController>();
            var bm = root.AddComponent<BoardManager>();
            var im = root.AddComponent<InputManager>();

            Link(bm, im, mc, gc, gm, boardSr);
        }

        private static void Link(BoardManager bm, InputManager im, MergeController mc, GravityController gc, GameManager gm, SpriteRenderer boardSr)
        {
            SetField(mc, "boardManager", bm);
            SetField(mc, "gameManager", gm);
            SetField(gc, "boardManager", bm);
            SetField(im, "boardManager", bm);
            SetField(im, "cam", Camera.main);
            SetField(bm, "mergeController", mc);
            SetField(bm, "gravityController", gc);
            SetField(bm, "gameManager", gm);
            SetField(bm, "boardRenderer", boardSr);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var f = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            f?.SetValue(target, value);
        }

        private static SpriteRenderer CreateBackground(string path, int sortingOrder, float parallaxSpeed, Vector3? worldPos = null)
        {
            var go = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
            go.transform.position = worldPos ?? Vector3.zero;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;
            sr.sprite = LoadSprite(path);
            if (parallaxSpeed > 0f)
            {
                var p = go.AddComponent<ParallaxLayer>();
                SetField(p, "speed", parallaxSpeed);
            }

            return sr;
        }

        private static Sprite LoadSprite(string path)
        {
#if UNITY_EDITOR
            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            
            var allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in allAssets)
            {
                if (asset is Sprite s) return s;
            }
            return null;
#else
            return null;
#endif
        }
    }

    /// <summary>
    /// Força a câmera a se afastar matematicamente caso a tela (como em celulares "em pé" no modo Portrait)
    /// seja mais fina do que o necessário para cobrir a largura do nosso tabuleiro.
    /// </summary>
    public class MobileCameraFitter : MonoBehaviour
    {
        private Camera _cam;
        private float _minOrthoSize = 6.8f;
        private float _targetSafeWidth = 9.5f; // Aumentado para dar margem e o tabuleiro inteiro caber com folga 

        private void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        private void Update()
        {
            if (_cam == null) return;
            
            // Qual seria a largura atual na tela, em metros físicos, com o Ortho base num celular?
            float currentWidth = _minOrthoSize * 2f * _cam.aspect;
            
            // Poxa, cortou o tabuleiro...
            if (currentWidth < _targetSafeWidth)
            {
                // Calcula a matemática inversa para achar a Altura nova ideal puxando a câmera pra trás até a base de Largura caber perfeitamente!
                _cam.orthographicSize = _targetSafeWidth / (2f * _cam.aspect);
            }
            else
            {
                // A tela já é de deitado (Ex: Tablet / PC Landscape) ou larga o suficiente. Ignora.
                _cam.orthographicSize = _minOrthoSize;
            }
        }
    }
}
