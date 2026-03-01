using DragonMerge.Board;
using DragonMerge.InputSystem;
using DragonMerge.Logic;
using UnityEngine;

namespace DragonMerge.Core
{
    public class DemoSceneBootstrap : MonoBehaviour
    {
        private struct EnvItem 
        {
            public string Path;
            public string Name;
            public Vector3 Position;
            public Vector3 Scale;
            public float RotationZ;
            public int Order;
            
            public EnvItem(string name, string path, float px, float py, float sx, float sy, float rotZ, int order)
            {
                Name = name; Path = path;
                Position = new Vector3(px, py, 0);
                Scale = new Vector3(sx, sy, 1);
                RotationZ = rotZ;
                Order = order;
            }
        }

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

            var boardSr = RebuildDioramaDynamically();

            var root = new GameObject("DragonMergeRuntime");
            var gm = root.AddComponent<GameManager>();
            var mc = root.AddComponent<MergeController>();
            var gc = root.AddComponent<GravityController>();
            var bm = root.AddComponent<BoardManager>();
            var im = root.AddComponent<InputManager>();
            var vfx = root.AddComponent<DragonMerge.Scripts.VFX.VFXManager>();

#if UNITY_EDITOR
            if (vfx.matchParticlesPrefab == null)
            {
                vfx.matchParticlesPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Misc/CFXR Magic Poof.prefab");
            }
            if (vfx.upgradePoofPrefab == null)
            {
                vfx.upgradePoofPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs/Explosions/CFXR Explosion Smoke 2 Solo (HDR).prefab");
            }
#endif

            Link(bm, im, mc, gc, gm, boardSr);
        }

        private static SpriteRenderer RebuildDioramaDynamically()
        {
            var items = new EnvItem[] {
                new EnvItem("cenarios_1 (1)", "Assets/Sprites/Environment/cenarios.png", -3.600f, 2.960f, 1.423f, 1.387f, -88.986f, -2),
                new EnvItem("cenarios_1 (2)", "Assets/Sprites/Environment/cenarios.png", 7.510f, 3.480f, 1.423f, 1.387f, 91.587f, -2),
                new EnvItem("cenarios_1", "Assets/Sprites/Environment/cenarios.png", 2.340f, -3.070f, 1.423f, 1.387f, 2.001f, -1),
                new EnvItem("cenarios_0", "Assets/Sprites/Environment/cenarios.png", 2.040f, 10.280f, 1.270f, 1.270f, 0.000f, -1),
                new EnvItem("mega-arvores_0 (1)", "Assets/Sprites/Environment/mega-arvores.png", -2.482f, 6.921f, 1.000f, 1.000f, 0.000f, 0),
                new EnvItem("mega-arvores_8", "Assets/Sprites/Environment/mega-arvores.png", 5.995f, 6.485f, 1.000f, 1.000f, 0.000f, 0),
                new EnvItem("taboleiro5x8_0", "Assets/Sprites/Environment/taboleiro5x8.png", 1.930f, 3.680f, 1.000f, 1.000f, 0.000f, 0),
                new EnvItem("mega-arvores_3", "Assets/Sprites/Environment/mega-arvores.png", 6.280f, -1.250f, 1.000f, 1.000f, 0.000f, 0),
                new EnvItem("mega-arvores_1", "Assets/Sprites/Environment/mega-arvores.png", -2.875f, -1.186f, 1.000f, 1.000f, 0.000f, 0),
                new EnvItem("mega-arvores_5", "Assets/Sprites/Environment/mega-arvores.png", -2.526f, 4.655f, 1.000f, 1.000f, 0.000f, 0),
                new EnvItem("mega-arvores_6", "Assets/Sprites/Environment/mega-arvores.png", 6.191f, 7.204f, 1.000f, 1.000f, 0.000f, 0),
                new EnvItem("mega-arvores_3 (1)", "Assets/Sprites/Environment/mega-arvores.png", -2.722f, 5.810f, 1.000f, 1.000f, 0.000f, 0),
                new EnvItem("mega-arvores_0", "Assets/Sprites/Environment/mega-arvores.png", 6.430f, -0.680f, 1.000f, 1.000f, 0.000f, 1),
                new EnvItem("nuvens_0", "Assets/Sprites/Environment/nuvens.png", -3.419f, 11.367f, 0.5f, 0.5f, 0.000f, 1),
                new EnvItem("mega-arvores_2", "Assets/Sprites/Environment/mega-arvores.png", 2.720f, -1.820f, 1.000f, 1.000f, 0.000f, 2),
                new EnvItem("mega-arvores_6 (1)", "Assets/Sprites/Environment/mega-arvores.png", 6.450f, 5.890f, 1.266f, 1.473f, 0.000f, 2),
                new EnvItem("nuvens_1", "Assets/Sprites/Environment/nuvens.png", 4.750f, 11.260f, 0.5f, 0.5f, 0.000f, 2),
                new EnvItem("mega-arvores_4", "Assets/Sprites/Environment/mega-arvores.png", -2.199f, 7.597f, 1.000f, 1.000f, 0.000f, 2),
                new EnvItem("nuvens_25", "Assets/Sprites/Environment/nuvens.png", -0.410f, 10.670f, 0.5f, 0.5f, 0.000f, 2),
                new EnvItem("mega-arvores_0 (3)", "Assets/Sprites/Environment/mega-arvores.png", 0.305f, -2.035f, 1.000f, 1.000f, 0.000f, 2),
                new EnvItem("nuvens_15", "Assets/Sprites/Environment/nuvens.png", 1.900f, 11.000f, 0.5f, 0.5f, 0.000f, 2),
                new EnvItem("mega-arvores_5 (1)", "Assets/Sprites/Environment/mega-arvores.png", 4.620f, -1.250f, 1.000f, 1.000f, 0.000f, 2),
                new EnvItem("mega-arvores_8 (1)", "Assets/Sprites/Environment/mega-arvores.png", 2.510f, -1.050f, 1.000f, 1.000f, 0.000f, 2),
                new EnvItem("mega-arvores_4 (1)", "Assets/Sprites/Environment/mega-arvores.png", -2.550f, 5.260f, 1.000f, 1.000f, 42.347f, 2),
                new EnvItem("mega-arvores_0 (2)", "Assets/Sprites/Environment/mega-arvores.png", 7.390f, 0.220f, 1.381f, 1.508f, 0.000f, 2),
                new EnvItem("nuvens_30", "Assets/Sprites/Environment/nuvens.png", 0.329f, 11.933f, 1.000f, 1.000f, 0.000f, 2),
                new EnvItem("mega-arvores_10", "Assets/Sprites/Environment/mega-arvores.png", -2.840f, 6.380f, -0.872f, 1.000f, 0.000f, 2),
                new EnvItem("mega-arvores_6 (2)", "Assets/Sprites/Environment/mega-arvores.png", 0.940f, -1.210f, 1.000f, 1.000f, 0.000f, 2)
            };

            var rootGo = new GameObject("DioramaRuntime");
            SpriteRenderer mapBoard = null;

            foreach (var item in items)
            {
                var go = new GameObject(item.Name);
                go.transform.SetParent(rootGo.transform);
                go.transform.position = item.Position;
                go.transform.localScale = item.Scale;
                go.transform.rotation = Quaternion.Euler(0, 0, item.RotationZ);
                
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = item.Order;
                
                var loaded = LoadSprite(item.Path, item.Name);
                if (loaded != null) {
                    sr.sprite = loaded;
                } else {
                    Debug.LogWarning("Não achou o sprite no caminho: " + item.Path);
                }

                if (item.Name.Contains("taboleiro"))
                {
                    mapBoard = sr;
                }
                
                if (item.Name.Contains("nuven"))
                {
                    go.AddComponent<DragonMerge.Scripts.Environment.CloudParallax>();
                }
                
                if (item.Name.Contains("mega-arvores"))
                {
                    go.AddComponent<DragonMerge.Scripts.Environment.TreeWindSway>();
                }
            }

            return mapBoard;
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
            return sr;
        }

        private static Sprite LoadSprite(string path, string goName = "")
        {
#if UNITY_EDITOR
            if (path.Contains("cenarios.png"))
            {
                var allAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
                if (goName.Contains("cenarios_1"))
                {
                    foreach (var asset in allAssets)
                        if (asset is Sprite s && s.name == "cenarios_1") return s;
                }
                else if (goName.Contains("cenarios_0"))
                {
                    foreach (var asset in allAssets)
                        if (asset is Sprite s && s.name == "cenarios_0") return s;
                }
            }

            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            
            var allAssets2 = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in allAssets2)
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
