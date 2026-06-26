using System.Collections.Generic;
using System.IO;
using Match3d.Game;
using Match3d.Input;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Match3d.EditorTools
{
    public static class Match3dSceneSetup
    {
        private const string ItemLayer = "Item";
        private const string GroundLayer = "Ground";
        private const string CollectorLayer = "Collector";
        private const string ItemTag = "Item";
        private const string CollectorTag = "Collector";
        private const string ItemPrefabFolder = "Assets/Prefabs/Items";
        private const int GeneratedPrefabLimit = 12;
        private const float BoundaryWallThickness = 0.35f;
        private const float BoundaryWallHeight = 4f;

        [MenuItem("Match3D/Setup/Build Scene From Plan")]
        public static void BuildSceneFromPlan()
        {
            OpenDefaultScene();
            EnsureTagsAndLayers();
            EnsureFolders();

            GameObject gameRoot = EnsureRoot("Game");
            GameObject environmentRoot = EnsureRoot("Environment");

            GameObject itemsRoot = EnsureChild(gameRoot.transform, "ItemsRoot");
            GameObject effectsRoot = EnsureChild(gameRoot.transform, "EffectsRoot");

            GameSession gameSession = EnsureChild(gameRoot.transform, "GameSession").EnsureComponent<GameSession>();
            SpawnManager spawnManager = EnsureChild(gameRoot.transform, "SpawnManager").EnsureComponent<SpawnManager>();
            EnsureChild(gameRoot.transform, "GameBootstrap");
            EnsureChild(gameRoot.transform, "Timer");
            EnsureChild(gameRoot.transform, "CameraController");

            DragInputController dragInput = EnsureChild(gameRoot.transform, "Input").EnsureComponent<DragInputController>();

            GameObject ground = EnsureGround(environmentRoot.transform);
            SpawnArea spawnArea = EnsureChild(environmentRoot.transform, "SpawnArea").EnsureComponent<SpawnArea>();
            spawnArea.transform.position = new Vector3(0f, 2.5f, 0f);
            EnsureBoundaryWalls(environmentRoot.transform, spawnArea);

            ShaftCollector shaftCollector = EnsureShaft(environmentRoot.transform, effectsRoot.transform);
            Camera camera = EnsureCamera();
            EnsureLight();

            Canvas canvas = EnsureCanvas();
            TextMeshProUGUI counter = EnsureText(canvas.transform, "Counter", "Items: 0", new Vector2(24f, -24f));
            TextMeshProUGUI timer = EnsureText(canvas.transform, "Timer", "02:00", new Vector2(-24f, -24f), TextAnchor.UpperRight);
            GameObject gameOverPanel = EnsureGameOverPanel(canvas.transform);
            EnsureEventSystem();

            List<CollectableItem> prefabs = CreateItemPrefabs();

            AssignGameSession(gameSession, spawnManager, counter, timer, gameOverPanel);
            AssignSpawnManager(spawnManager, prefabs, spawnArea, itemsRoot.transform, gameSession);
            AssignDragInput(dragInput, camera, spawnArea);
            AssignShaftCollector(shaftCollector, effectsRoot.transform);

            Selection.activeGameObject = gameRoot;
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log("Match3D scene setup is up to date.", gameRoot);
        }

        private static void OpenDefaultScene()
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";

            if (File.Exists(scenePath) && SceneManager.GetActiveScene().path != scenePath)
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets/Prefabs", "Items");
        }

        private static void EnsureFolder(string parent, string folder)
        {
            string path = $"{parent}/{folder}";

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static GameObject EnsureRoot(string name)
        {
            GameObject existing = GameObject.Find(name);

            if (existing != null && existing.transform.parent == null)
            {
                return existing;
            }

            return new GameObject(name);
        }

        private static GameObject EnsureChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);

            if (child != null)
            {
                return child.gameObject;
            }

            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = Vector3.one;
            return gameObject;
        }

        private static GameObject EnsureGround(Transform parent)
        {
            GameObject ground = EnsureChild(parent, "Ground");

            if (ground.GetComponent<MeshFilter>() == null)
            {
                Object.DestroyImmediate(ground);
                ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.SetParent(parent);
            }

            ground.layer = LayerMask.NameToLayer(GroundLayer);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
            return ground;
        }

        private static void EnsureBoundaryWalls(Transform parent, SpawnArea spawnArea)
        {
            GameObject wallsRoot = EnsureChild(parent, "BoundaryWalls");
            Vector3 areaSize = spawnArea.Size;
            Vector3 center = spawnArea.transform.position;
            float halfWidth = areaSize.x * 0.5f;
            float halfDepth = areaSize.z * 0.5f;
            float wallY = BoundaryWallHeight * 0.5f;

            EnsureBoundaryWall(
                wallsRoot.transform,
                "Wall_North",
                new Vector3(center.x, wallY, center.z + halfDepth + BoundaryWallThickness * 0.5f),
                new Vector3(areaSize.x + BoundaryWallThickness * 2f, BoundaryWallHeight, BoundaryWallThickness));

            EnsureBoundaryWall(
                wallsRoot.transform,
                "Wall_South",
                new Vector3(center.x, wallY, center.z - halfDepth - BoundaryWallThickness * 0.5f),
                new Vector3(areaSize.x + BoundaryWallThickness * 2f, BoundaryWallHeight, BoundaryWallThickness));

            EnsureBoundaryWall(
                wallsRoot.transform,
                "Wall_East",
                new Vector3(center.x + halfWidth + BoundaryWallThickness * 0.5f, wallY, center.z),
                new Vector3(BoundaryWallThickness, BoundaryWallHeight, areaSize.z + BoundaryWallThickness * 2f));

            EnsureBoundaryWall(
                wallsRoot.transform,
                "Wall_West",
                new Vector3(center.x - halfWidth - BoundaryWallThickness * 0.5f, wallY, center.z),
                new Vector3(BoundaryWallThickness, BoundaryWallHeight, areaSize.z + BoundaryWallThickness * 2f));
        }

        private static void EnsureBoundaryWall(Transform parent, string name, Vector3 position, Vector3 size)
        {
            GameObject wall = EnsureChild(parent, name);
            wall.layer = LayerMask.NameToLayer(GroundLayer);
            wall.transform.position = position;
            wall.transform.localRotation = Quaternion.identity;
            wall.transform.localScale = Vector3.one;

            BoxCollider collider = wall.EnsureComponent<BoxCollider>();
            collider.isTrigger = false;
            collider.size = size;
            collider.center = Vector3.zero;
        }

        private static ShaftCollector EnsureShaft(Transform parent, Transform effectsRoot)
        {
            GameObject shaft = EnsureChild(parent, "Shaft");

            if (shaft.GetComponent<MeshFilter>() == null)
            {
                Object.DestroyImmediate(shaft);
                shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                shaft.name = "Shaft";
                shaft.transform.SetParent(parent);
            }

            shaft.tag = CollectorTag;
            shaft.layer = LayerMask.NameToLayer(CollectorLayer);
            shaft.transform.position = new Vector3(0f, 0.15f, -3.25f);
            shaft.transform.localScale = new Vector3(1.6f, 0.08f, 1.6f);

            Collider collider = shaft.GetComponent<Collider>();
            collider.isTrigger = true;

            ShaftCollector collector = shaft.EnsureComponent<ShaftCollector>();
            AssignShaftCollector(collector, effectsRoot);
            return collector;
        }

        private static Camera EnsureCamera()
        {
            Camera camera = Camera.main;

            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }

            camera.transform.position = new Vector3(0f, 8f, -8f);
            camera.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
            camera.fieldOfView = 55f;
            return camera;
        }

        private static void EnsureLight()
        {
            Light light = Object.FindObjectOfType<Light>();

            if (light == null)
            {
                GameObject lightObject = new GameObject("Directional Light");
                light = lightObject.AddComponent<Light>();
            }

            light.name = "Directional Light";
            light.type = LightType.Directional;
            light.intensity = 1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static Canvas EnsureCanvas()
        {
            Canvas canvas = Object.FindObjectOfType<Canvas>();

            if (canvas != null)
            {
                return canvas;
            }

            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform));
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static TextMeshProUGUI EnsureText(
            Transform parent,
            string name,
            string text,
            Vector2 anchoredPosition,
            TextAnchor alignment = TextAnchor.UpperLeft)
        {
            GameObject label = EnsureUiChild(parent, name);
            TextMeshProUGUI tmp = label.EnsureComponent<TextMeshProUGUI>();
            RectTransform rectTransform = tmp.rectTransform;

            rectTransform.anchorMin = alignment == TextAnchor.UpperRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rectTransform.anchorMax = rectTransform.anchorMin;
            rectTransform.pivot = alignment == TextAnchor.UpperRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = new Vector2(320f, 72f);

            tmp.text = text;
            tmp.fontSize = 38f;
            tmp.alignment = alignment == TextAnchor.UpperRight ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
            tmp.color = Color.white;
            return tmp;
        }

        private static GameObject EnsureGameOverPanel(Transform parent)
        {
            GameObject panel = EnsureUiChild(parent, "GameOverPanel");
            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            Image image = panel.EnsureComponent<Image>();

            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            image.color = new Color(0f, 0f, 0f, 0.55f);

            TextMeshProUGUI label = EnsureText(panel.transform, "Title", "Game Over", Vector2.zero, TextAnchor.MiddleCenter);
            label.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.rectTransform.sizeDelta = new Vector2(520f, 120f);
            label.fontSize = 64f;
            label.alignment = TextAlignmentOptions.Center;

            panel.SetActive(false);
            return panel;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static GameObject EnsureUiChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);

            if (child != null)
            {
                if (child.GetComponent<RectTransform>() != null)
                {
                    return child.gameObject;
                }

                Object.DestroyImmediate(child.gameObject);
            }

            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent);
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localRotation = Quaternion.identity;
            gameObject.transform.localScale = Vector3.one;
            return gameObject;
        }

        private static List<CollectableItem> CreateItemPrefabs()
        {
            List<CollectableItem> prefabs = new List<CollectableItem>();
            string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/fruitsandvegetables/gltf" });
            int count = Mathf.Min(GeneratedPrefabLimit, guids.Length);

            for (int i = 0; i < count; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (model == null)
                {
                    continue;
                }

                string prefabPath = $"{ItemPrefabFolder}/Item_{Path.GetFileNameWithoutExtension(assetPath)}.prefab";
                CollectableItem item = CreateItemPrefab(prefabPath, model);

                if (item != null)
                {
                    prefabs.Add(item);
                }
            }

            if (prefabs.Count == 0)
            {
                CollectableItem placeholder = CreateItemPrefab($"{ItemPrefabFolder}/Item_Placeholder.prefab", null);

                if (placeholder != null)
                {
                    prefabs.Add(placeholder);
                }
            }

            return prefabs;
        }

        private static CollectableItem CreateItemPrefab(string prefabPath, GameObject model)
        {
            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));
            root.tag = ItemTag;
            root.layer = LayerMask.NameToLayer(ItemLayer);
            root.transform.localScale = Vector3.one * 10f;

            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.useGravity = true;
            rigidbody.isKinematic = false;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = Vector3.one * 0.1f;

            root.AddComponent<AudioSource>();
            CollectableItem item = root.AddComponent<CollectableItem>();

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            if (model != null)
            {
                GameObject modelInstance = PrefabUtility.InstantiatePrefab(model, visual.transform) as GameObject;

                if (modelInstance != null)
                {
                    modelInstance.transform.localPosition = Vector3.zero;
                    modelInstance.transform.localRotation = Quaternion.identity;
                    modelInstance.transform.localScale = Vector3.one;
                    item.SetType(GuessItemType(model.name));
                }
            }
            else
            {
                GameObject placeholder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                placeholder.name = "PlaceholderMesh";
                Object.DestroyImmediate(placeholder.GetComponent<Collider>());
                placeholder.transform.SetParent(visual.transform);
                placeholder.transform.localPosition = Vector3.zero;
                placeholder.transform.localRotation = Quaternion.identity;
                placeholder.transform.localScale = Vector3.one;
            }

            SerializedObject itemObject = new SerializedObject(item);
            itemObject.FindProperty("visualRoot").objectReferenceValue = visual.transform;
            itemObject.FindProperty("audioSource").objectReferenceValue = root.GetComponent<AudioSource>();
            itemObject.ApplyModifiedPropertiesWithoutUndo();

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return savedPrefab != null ? savedPrefab.GetComponent<CollectableItem>() : null;
        }

        private static ItemType GuessItemType(string assetName)
        {
            string lowerName = assetName.ToLowerInvariant();

            if (lowerName.Contains("apple")) return ItemType.Apple;
            if (lowerName.Contains("banana")) return ItemType.Banana;
            if (lowerName.Contains("carrot")) return ItemType.Carrot;
            if (lowerName.Contains("cherry")) return ItemType.Cherry;
            if (lowerName.Contains("corn")) return ItemType.Corn;
            if (lowerName.Contains("cucumber")) return ItemType.Cucumber;
            if (lowerName.Contains("grape")) return ItemType.Grapes;
            if (lowerName.Contains("lemon")) return ItemType.Lemon;
            if (lowerName.Contains("mango")) return ItemType.Mango;
            if (lowerName.Contains("mushroom")) return ItemType.Mushroom;
            if (lowerName.Contains("onion")) return ItemType.Onion;
            if (lowerName.Contains("orange")) return ItemType.Orange;
            if (lowerName.Contains("pear")) return ItemType.Pear;
            if (lowerName.Contains("pineapple")) return ItemType.Pineapple;
            if (lowerName.Contains("potato")) return ItemType.Potato;
            if (lowerName.Contains("strawberry")) return ItemType.Strawberry;
            if (lowerName.Contains("tomato")) return ItemType.Tomato;
            if (lowerName.Contains("watermelon")) return ItemType.Watermelon;

            return ItemType.Unknown;
        }

        private static void AssignGameSession(
            GameSession gameSession,
            SpawnManager spawnManager,
            TextMeshProUGUI counter,
            TextMeshProUGUI timer,
            GameObject gameOverPanel)
        {
            SerializedObject serializedObject = new SerializedObject(gameSession);
            serializedObject.FindProperty("spawnManager").objectReferenceValue = spawnManager;
            serializedObject.FindProperty("counterLabel").objectReferenceValue = counter;
            serializedObject.FindProperty("timerLabel").objectReferenceValue = timer;
            serializedObject.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignSpawnManager(
            SpawnManager spawnManager,
            IReadOnlyList<CollectableItem> prefabs,
            SpawnArea spawnArea,
            Transform itemsRoot,
            GameSession gameSession)
        {
            SerializedObject serializedObject = new SerializedObject(spawnManager);
            SerializedProperty prefabsProperty = serializedObject.FindProperty("itemPrefabs");
            prefabsProperty.arraySize = prefabs.Count;

            for (int i = 0; i < prefabs.Count; i++)
            {
                prefabsProperty.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
            }

            serializedObject.FindProperty("spawnArea").objectReferenceValue = spawnArea;
            serializedObject.FindProperty("itemsRoot").objectReferenceValue = itemsRoot;
            serializedObject.FindProperty("gameSession").objectReferenceValue = gameSession;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignDragInput(DragInputController dragInput, Camera camera, SpawnArea spawnArea)
        {
            SerializedObject serializedObject = new SerializedObject(dragInput);
            serializedObject.FindProperty("targetCamera").objectReferenceValue = camera;
            serializedObject.FindProperty("itemLayerMask").intValue = LayerMask.GetMask(ItemLayer);
            serializedObject.FindProperty("dragBounds").objectReferenceValue = spawnArea;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignShaftCollector(ShaftCollector collector, Transform effectsRoot)
        {
            SerializedObject serializedObject = new SerializedObject(collector);
            serializedObject.FindProperty("collectedItemsRoot").objectReferenceValue = effectsRoot;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureTagsAndLayers()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            EnsureTag(tagManager, ItemTag);
            EnsureTag(tagManager, CollectorTag);
            EnsureLayer(tagManager, 6, GroundLayer);
            EnsureLayer(tagManager, 7, ItemLayer);
            EnsureLayer(tagManager, 8, CollectorLayer);
            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureTag(SerializedObject tagManager, string tag)
        {
            SerializedProperty tags = tagManager.FindProperty("tags");

            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag)
                {
                    return;
                }
            }

            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        }

        private static void EnsureLayer(SerializedObject tagManager, int index, string layer)
        {
            SerializedProperty layers = tagManager.FindProperty("layers");
            SerializedProperty layerProperty = layers.GetArrayElementAtIndex(index);

            if (string.IsNullOrEmpty(layerProperty.stringValue))
            {
                layerProperty.stringValue = layer;
            }
        }

        private static T EnsureComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }
    }
}
