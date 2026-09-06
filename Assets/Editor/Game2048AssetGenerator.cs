using System.IO;
using Mergeur.Game2048;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mergeur.Editor
{
    [InitializeOnLoad]
    public static class Game2048AssetGenerator
    {
        private const string Root = "Assets/Game/Resources/Game2048";
        private const string Models = Root + "/Models";
        private const string Materials = Root + "/Materials";
        private const string Prefabs = Root + "/Prefabs";
        private const string BoardPrefabPath = Prefabs + "/Board3D.prefab";
        private const string TilePrefabPath = Prefabs + "/Tile3D.prefab";

        static Game2048AssetGenerator()
        {
            EditorApplication.delayCall += GenerateIfMissing;
        }

        [MenuItem("Tools/Mergeur/Regenerate 2048 3D Assets")]
        public static void Generate()
        {
            EnsureFolder("Assets/Game", "Resources");
            EnsureFolder("Assets/Game/Resources", "Game2048");
            EnsureFolder(Root, "Models");
            EnsureFolder(Root, "Materials");
            EnsureFolder(Root, "Prefabs");

            Mesh cubeMesh = CreateCubeMeshAsset(Models + "/BeveledTileCube.asset", "2048 Tile Cube Mesh");
            Mesh boardMesh = CreateCubeMeshAsset(Models + "/BoardBase.asset", "2048 Board Base Mesh");
            Material boardMaterial = CreateMaterial(Materials + "/Board.mat", "#745642", 0.18f, 0.42f);
            Material slotMaterial = CreateMaterial(Materials + "/Slot.mat", "#34261F", 0.05f, 0.28f);

            int[] values = { 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096 };
            foreach (int value in values)
            {
                CreateMaterial(Materials + $"/Tile_{value}.mat", ColorFor(value), 0.48f,
                    value >= 128 ? 0.52f : 0.24f);
            }

            CreateBoardPrefab(boardMesh, cubeMesh, boardMaterial, slotMaterial);
            CreateTilePrefab(cubeMesh, AssetDatabase.LoadAssetAtPath<Material>(Materials + "/Tile_2.mat"));
            AssetDatabase.SaveAssets();
            SetupBootstrapScene();
            AssetDatabase.Refresh();
            Debug.Log("[2048] Generated 3D meshes, materials and prefabs under " + Root);
        }

        private static void GenerateIfMissing()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(BoardPrefabPath) && File.Exists(TilePrefabPath))
            {
                return;
            }

            Generate();
        }

        private static void EnsureFolder(string parent, string name)
        {
            string path = parent + "/" + name;
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, name);
        }

        private static Mesh CreateCubeMeshAsset(string path, string name)
        {
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;

            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Mesh mesh = Object.Instantiate(primitive.GetComponent<MeshFilter>().sharedMesh);
            mesh.name = name;
            Object.DestroyImmediate(primitive);
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Material CreateMaterial(string path, string hex, float metallic, float smoothness)
        {
            ColorUtility.TryParseHtmlString(hex, out Color color);
            return CreateMaterial(path, color, metallic, smoothness);
        }

        private static Material CreateMaterial(string path, Color color, float metallic, float smoothness)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = Path.GetFileNameWithoutExtension(path);
            material.color = color;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateBoardPrefab(Mesh boardMesh, Mesh slotMesh, Material boardMaterial, Material slotMaterial)
        {
            var root = new GameObject("Board3D");
            AddMeshObject("Board Base", root.transform, boardMesh, boardMaterial,
                new Vector3(0f, -0.18f, 0f), new Vector3(5.18f, 0.34f, 5.18f));

            for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++)
            {
                Vector3 cell = CellPosition(x, y);
                AddMeshObject($"Slot {x + 1},{y + 1}", root.transform, slotMesh, slotMaterial,
                    new Vector3(cell.x, 0.025f, cell.z), new Vector3(1.02f, 0.08f, 1.02f));
            }

            PrefabUtility.SaveAsPrefabAsset(root, BoardPrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void CreateTilePrefab(Mesh mesh, Material material)
        {
            var root = new GameObject("Tile3D");
            var filter = root.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            var labelObject = new GameObject("Number");
            labelObject.transform.SetParent(root.transform, false);
            labelObject.transform.localPosition = new Vector3(0f, 0.515f, -0.02f);
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var label = labelObject.AddComponent<TextMesh>();
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 96;
            label.characterSize = 0.012f;
            label.fontStyle = FontStyle.Bold;
            label.color = Hex("#3D312B");

            PrefabUtility.SaveAsPrefabAsset(root, TilePrefabPath);
            Object.DestroyImmediate(root);
        }

        private static void SetupBootstrapScene()
        {
            SetupScene("Assets/Scenes/Bootstrap.unity");
            SetupScene("Assets/Game/Scenes/Game.unity");
        }

        private static void SetupScene(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Mergeur.Game2048.Game2048 game = Object.FindFirstObjectByType<Mergeur.Game2048.Game2048>();
            if (game == null)
            {
                Debug.LogError($"[2048] Game2048 component is missing from {scenePath}.");
                return;
            }

            Transform oldWorld = game.transform.Find("2048 3D World");
            if (oldWorld != null) Object.DestroyImmediate(oldWorld.gameObject);

            var worldObject = new GameObject("2048 3D World");
            worldObject.transform.SetParent(game.transform, false);
            Transform world = worldObject.transform;

            CreateSceneCamera(world);
            CreateSceneLighting(world);

            GameObject boardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BoardPrefabPath);
            GameObject tilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TilePrefabPath);
            var board = (GameObject)PrefabUtility.InstantiatePrefab(boardPrefab, world);
            board.name = "Board3D Model";

            var sceneTiles = new GameObject[16];
            for (int y = 0; y < 4; y++) for (int x = 0; x < 4; x++)
            {
                int index = y * 4 + x;
                GameObject tile = (GameObject)PrefabUtility.InstantiatePrefab(tilePrefab, world);
                tile.name = $"Tile {x + 1},{y + 1}";
                Vector3 cell = CellPosition(x, y);
                tile.transform.localPosition = new Vector3(cell.x, 0.34f, cell.z);
                tile.transform.localScale = new Vector3(1.02f, 0.56f, 1.02f);
                sceneTiles[index] = tile;
            }

            var serializedGame = new SerializedObject(game);
            SerializedProperty tilesProperty = serializedGame.FindProperty("sceneTiles");
            tilesProperty.arraySize = sceneTiles.Length;
            for (int i = 0; i < sceneTiles.Length; i++)
                tilesProperty.GetArrayElementAtIndex(i).objectReferenceValue = sceneTiles[i];

            int[] materialValues = { 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096 };
            SerializedProperty materialsProperty = serializedGame.FindProperty("sceneTileMaterials");
            materialsProperty.arraySize = materialValues.Length;
            for (int i = 0; i < materialValues.Length; i++)
            {
                materialsProperty.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Material>(Materials + $"/Tile_{materialValues[i]}.mat");
            }
            serializedGame.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void CreateSceneCamera(Transform parent)
        {
            var cameraObject = new GameObject("Game 3D Camera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.localPosition = new Vector3(0f, 6.8f, -7.1f);
            cameraObject.transform.localRotation = Quaternion.Euler(43f, 0f, 0f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("#171321");
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 40f;
            camera.depth = -10f;
            camera.allowHDR = false;
        }

        private static void CreateSceneLighting(Transform parent)
        {
            var keyObject = new GameObject("Key Light");
            keyObject.transform.SetParent(parent, false);
            keyObject.transform.localRotation = Quaternion.Euler(48f, -32f, 0f);
            var key = keyObject.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = Hex("#FFF0D5");
            key.intensity = 1.35f;
            key.shadows = LightShadows.Soft;

            var fillObject = new GameObject("Fill Light");
            fillObject.transform.SetParent(parent, false);
            fillObject.transform.localPosition = new Vector3(-3f, 4f, -2f);
            var fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.color = Hex("#778DFF");
            fill.intensity = 5f;
            fill.range = 12f;
        }

        private static void AddMeshObject(string name, Transform parent, Mesh mesh, Material material,
            Vector3 position, Vector3 scale)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = position;
            child.transform.localScale = scale;
            child.AddComponent<MeshFilter>().sharedMesh = mesh;
            child.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Vector3 CellPosition(int x, int y)
        {
            const float half = 1.5f;
            return new Vector3((x - half) * 1.18f, 0f, (half - y) * 1.18f);
        }

        private static Color ColorFor(int value)
        {
            switch (value)
            {
                case 2: return Hex("#EEE4DA"); case 4: return Hex("#EDE0C8"); case 8: return Hex("#F2B179");
                case 16: return Hex("#F59563"); case 32: return Hex("#F67C5F"); case 64: return Hex("#F65E3B");
                case 128: return Hex("#EDCF72"); case 256: return Hex("#EDCC61"); case 512: return Hex("#EDC850");
                case 1024: return Hex("#EDC53F"); case 2048: return Hex("#EDC22E"); default: return Hex("#3C3A32");
            }
        }

        private static Color Hex(string value) => ColorUtility.TryParseHtmlString(value, out Color color) ? color : Color.white;
    }
}
