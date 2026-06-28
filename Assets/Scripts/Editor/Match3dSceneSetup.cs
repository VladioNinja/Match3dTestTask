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
        private const string ItemPhysicsMaterialPath = "Assets/Materials/ItemPhysicsMaterial.physicMaterial";
        private const int GeneratedPrefabLimit = 12;
        private const float BoundaryWallThickness = 0.35f;
        private const float BoundaryWallHeight = 4f;

        [MenuItem("Match3D/Setup/Build Scene From Plan")]
        public static void BuildSceneFromPlan()
        {
            OpenDefaultScene();
            EnsureTagsAndLayers();
            EnsureFolders();
            PhysicMaterial itemPhysicsMaterial = EnsureItemPhysicsMaterial();

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

            ShaftCollector shaftCollector = EnsureShaft(environmentRoot.transform);
            Camera camera = EnsureCamera();
            EnsureLight();

            Canvas canvas = EnsureCanvas();
            TextMeshProUGUI counter = EnsureText(canvas.transform, "Counter", "Stars: 0", new Vector2(24f, -24f));
            counter.color = new Color(1f, 0.84f, 0.2f);
            TextMeshProUGUI timer = EnsureText(canvas.transform, "Timer", "0s", new Vector2(-24f, -24f), TextAnchor.UpperRight);
            GameObject gameOverPanel = EnsureGameOverPanel(canvas.transform);
            TextMeshProUGUI gameOverLabel = gameOverPanel.transform.Find("Title").GetComponent<TextMeshProUGUI>();
            Button retryButton = gameOverPanel.transform.Find("RetryButton").GetComponent<Button>();
            CollectFeedbackController collectFeedback = EnsureChild(gameRoot.transform, "CollectFeedback").EnsureComponent<CollectFeedbackController>();
            AudioSource feedbackAudioSource = collectFeedback.gameObject.EnsureComponent<AudioSource>();
            EnsureEventSystem();

            List<CollectableItem> prefabs = CreateItemPrefabs(itemPhysicsMaterial);

            AssignGameSession(gameSession, spawnManager, counter, timer, gameOverPanel, gameOverLabel, retryButton);
            AssignSpawnManager(spawnManager, prefabs, spawnArea, itemsRoot.transform, gameSession);
            AssignDragInput(dragInput, camera, spawnArea);
            AssignCollectFeedback(collectFeedback, canvas, camera, counter.rectTransform, feedbackAudioSource);
            AssignShaftCollector(shaftCollector, effectsRoot.transform, collectFeedback);

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
            EnsureFolder("Assets", "Materials");
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

        private static ShaftCollector EnsureShaft(Transform parent)
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

            return shaft.EnsureComponent<ShaftCollector>();
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

            camera.transform.position = new Vector3(0f, 12f, 0f);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 30f;
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

            TextMeshProUGUI label = EnsureText(panel.transform, "Title", "Game Over\nTime: 0s", new Vector2(0f, 58f), TextAnchor.MiddleCenter);
            label.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            label.rectTransform.sizeDelta = new Vector2(640f, 160f);
            label.fontSize = 64f;
            label.alignment = TextAlignmentOptions.Center;

            GameObject retryObject = EnsureUiChild(panel.transform, "RetryButton");
            RectTransform retryTransform = retryObject.GetComponent<RectTransform>();
            retryTransform.anchorMin = new Vector2(0.5f, 0.5f);
            retryTransform.anchorMax = new Vector2(0.5f, 0.5f);
            retryTransform.pivot = new Vector2(0.5f, 0.5f);
            retryTransform.anchoredPosition = new Vector2(0f, -82f);
            retryTransform.sizeDelta = new Vector2(220f, 68f);

            Image retryImage = retryObject.EnsureComponent<Image>();
            retryImage.color = new Color(1f, 0.84f, 0.2f, 1f);

            Button retryButton = retryObject.EnsureComponent<Button>();
            ColorBlock colors = retryButton.colors;
            colors.normalColor = new Color(1f, 0.84f, 0.2f, 1f);
            colors.highlightedColor = new Color(1f, 0.92f, 0.45f, 1f);
            colors.pressedColor = new Color(0.86f, 0.62f, 0.08f, 1f);
            retryButton.colors = colors;

            TextMeshProUGUI retryLabel = EnsureText(retryObject.transform, "Label", "Retry", Vector2.zero, TextAnchor.MiddleCenter);
            retryLabel.rectTransform.anchorMin = Vector2.zero;
            retryLabel.rectTransform.anchorMax = Vector2.one;
            retryLabel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            retryLabel.rectTransform.offsetMin = Vector2.zero;
            retryLabel.rectTransform.offsetMax = Vector2.zero;
            retryLabel.fontSize = 34f;
            retryLabel.color = Color.black;
            retryLabel.alignment = TextAlignmentOptions.Center;

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

        private static PhysicMaterial EnsureItemPhysicsMaterial()
        {
            PhysicMaterial material = AssetDatabase.LoadAssetAtPath<PhysicMaterial>(ItemPhysicsMaterialPath);

            if (material == null)
            {
                material = new PhysicMaterial("ItemPhysicsMaterial");
                AssetDatabase.CreateAsset(material, ItemPhysicsMaterialPath);
            }

            material.dynamicFriction = 1f;
            material.staticFriction = 1f;
            material.bounciness = 0f;
            material.frictionCombine = PhysicMaterialCombine.Maximum;
            material.bounceCombine = PhysicMaterialCombine.Minimum;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static List<CollectableItem> CreateItemPrefabs(PhysicMaterial itemPhysicsMaterial)
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
                CollectableItem item = CreateItemPrefab(prefabPath, model, itemPhysicsMaterial);

                if (item != null)
                {
                    prefabs.Add(item);
                }
            }

            if (prefabs.Count == 0)
            {
                CollectableItem placeholder = CreateItemPrefab($"{ItemPrefabFolder}/Item_Placeholder.prefab", null, itemPhysicsMaterial);

                if (placeholder != null)
                {
                    prefabs.Add(placeholder);
                }
            }

            return prefabs;
        }

        private static CollectableItem CreateItemPrefab(string prefabPath, GameObject model, PhysicMaterial itemPhysicsMaterial)
        {
            GameObject root = new GameObject(Path.GetFileNameWithoutExtension(prefabPath));
            root.tag = ItemTag;
            root.layer = LayerMask.NameToLayer(ItemLayer);
            root.transform.localScale = Vector3.one * 10f;

            Rigidbody rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.mass = 1.15f;
            rigidbody.drag = 4f;
            rigidbody.angularDrag = 8f;
            rigidbody.maxAngularVelocity = 4f;
            rigidbody.maxDepenetrationVelocity = 1.6f;
            rigidbody.useGravity = true;
            rigidbody.isKinematic = false;
            rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            root.AddComponent<AudioSource>();
            CollectableItem item = root.AddComponent<CollectableItem>();

            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            GameObject collidersRoot = new GameObject("Colliders");
            collidersRoot.layer = LayerMask.NameToLayer(ItemLayer);
            collidersRoot.transform.SetParent(root.transform);
            collidersRoot.transform.localPosition = Vector3.zero;
            collidersRoot.transform.localRotation = Quaternion.identity;
            collidersRoot.transform.localScale = Vector3.one;

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

            CreatePrimitiveColliders(root.transform, collidersRoot.transform, visual.transform, itemPhysicsMaterial);

            SerializedObject itemObject = new SerializedObject(item);
            itemObject.FindProperty("visualRoot").objectReferenceValue = visual.transform;
            itemObject.FindProperty("audioSource").objectReferenceValue = root.GetComponent<AudioSource>();
            itemObject.FindProperty("physicsMaterial").objectReferenceValue = itemPhysicsMaterial;
            itemObject.FindProperty("mass").floatValue = 1.15f;
            itemObject.FindProperty("linearDrag").floatValue = 4f;
            itemObject.FindProperty("angularDrag").floatValue = 8f;
            itemObject.FindProperty("maxDropVelocity").floatValue = 1.4f;
            itemObject.FindProperty("maxDropAngularVelocity").floatValue = 4f;
            itemObject.FindProperty("maxDepenetrationVelocity").floatValue = 1.6f;
            itemObject.FindProperty("maxPushedVelocity").floatValue = 1.75f;
            itemObject.FindProperty("maxPushedAngularVelocity").floatValue = 4f;
            itemObject.FindProperty("minCollisionImpulseForSound").floatValue = 0.35f;
            itemObject.FindProperty("collisionSoundCooldown").floatValue = 0.08f;
            itemObject.FindProperty("collisionSoundVolume").floatValue = 0.28f;
            itemObject.FindProperty("disableCollisionWhileDragging").boolValue = false;
            itemObject.ApplyModifiedPropertiesWithoutUndo();

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return savedPrefab != null ? savedPrefab.GetComponent<CollectableItem>() : null;
        }

        private static void CreatePrimitiveColliders(
            Transform itemRoot,
            Transform collidersRoot,
            Transform visualRoot,
            PhysicMaterial itemPhysicsMaterial)
        {
            if (!TryGetVisualLocalBounds(itemRoot, visualRoot, out Bounds bounds))
            {
                AddBoxCollider(collidersRoot, "Collider_Core", Vector3.zero, Vector3.one * 0.1f, itemPhysicsMaterial);
                AddSphereCollider(collidersRoot, "Collider_Left", new Vector3(-0.03f, 0f, 0f), 0.035f, itemPhysicsMaterial);
                AddSphereCollider(collidersRoot, "Collider_Right", new Vector3(0.03f, 0f, 0f), 0.035f, itemPhysicsMaterial);
                return;
            }

            Vector3 size = bounds.size;
            size.x = Mathf.Max(size.x, 0.02f);
            size.y = Mathf.Max(size.y, 0.02f);
            size.z = Mathf.Max(size.z, 0.02f);

            int longestAxis = GetLongestAxis(size);
            float longestSize = GetAxis(size, longestAxis);
            float secondSize = GetSecondLongestSize(size, longestAxis);

            if (longestSize > secondSize * 1.65f)
            {
                AddSphereChain(collidersRoot, bounds, longestAxis, 3, itemPhysicsMaterial);
                return;
            }

            AddBoxCollider(collidersRoot, "Collider_Core", bounds.center, size * 0.72f, itemPhysicsMaterial);

            int supportAxis = size.x >= size.z ? 0 : 2;
            float supportOffset = GetAxis(size, supportAxis) * 0.25f;
            float supportRadius = Mathf.Max(GetAverageOtherAxes(size, supportAxis) * 0.28f, 0.025f);

            Vector3 leftCenter = bounds.center;
            Vector3 rightCenter = bounds.center;
            SetAxis(ref leftCenter, supportAxis, GetAxis(bounds.center, supportAxis) - supportOffset);
            SetAxis(ref rightCenter, supportAxis, GetAxis(bounds.center, supportAxis) + supportOffset);

            AddSphereCollider(collidersRoot, "Collider_Left", leftCenter, supportRadius, itemPhysicsMaterial);
            AddSphereCollider(collidersRoot, "Collider_Right", rightCenter, supportRadius, itemPhysicsMaterial);
        }

        private static bool TryGetVisualLocalBounds(Transform itemRoot, Transform visualRoot, out Bounds localBounds)
        {
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                localBounds = default;
                return false;
            }

            localBounds = ToLocalBounds(itemRoot, renderers[0].bounds);

            for (int i = 1; i < renderers.Length; i++)
            {
                Bounds rendererBounds = ToLocalBounds(itemRoot, renderers[i].bounds);
                localBounds.Encapsulate(rendererBounds.min);
                localBounds.Encapsulate(rendererBounds.max);
            }

            return true;
        }

        private static void AddSphereChain(Transform parent, Bounds bounds, int axis, int count, PhysicMaterial itemPhysicsMaterial)
        {
            Vector3 size = bounds.size;
            float length = GetAxis(size, axis);
            float radius = Mathf.Max(GetAverageOtherAxes(size, axis) * 0.38f, 0.02f);
            float step = length / Mathf.Max(count, 1) * 0.55f;

            for (int i = 0; i < count; i++)
            {
                float centeredIndex = i - (count - 1) * 0.5f;
                Vector3 center = bounds.center;
                SetAxis(ref center, axis, GetAxis(bounds.center, axis) + centeredIndex * step);
                AddSphereCollider(parent, $"Collider_Sphere_{i + 1}", center, radius, itemPhysicsMaterial);
            }
        }

        private static void AddBoxCollider(
            Transform parent,
            string name,
            Vector3 center,
            Vector3 size,
            PhysicMaterial itemPhysicsMaterial)
        {
            GameObject colliderObject = CreateColliderObject(parent, name);
            BoxCollider collider = colliderObject.AddComponent<BoxCollider>();
            collider.center = center;
            collider.size = size;
            collider.sharedMaterial = itemPhysicsMaterial;
        }

        private static void AddSphereCollider(
            Transform parent,
            string name,
            Vector3 center,
            float radius,
            PhysicMaterial itemPhysicsMaterial)
        {
            GameObject colliderObject = CreateColliderObject(parent, name);
            SphereCollider collider = colliderObject.AddComponent<SphereCollider>();
            collider.center = center;
            collider.radius = radius;
            collider.sharedMaterial = itemPhysicsMaterial;
        }

        private static GameObject CreateColliderObject(Transform parent, string name)
        {
            GameObject colliderObject = new GameObject(name);
            colliderObject.layer = LayerMask.NameToLayer(ItemLayer);
            colliderObject.transform.SetParent(parent);
            colliderObject.transform.localPosition = Vector3.zero;
            colliderObject.transform.localRotation = Quaternion.identity;
            colliderObject.transform.localScale = Vector3.one;
            return colliderObject;
        }

        private static int GetLongestAxis(Vector3 value)
        {
            if (value.x >= value.y && value.x >= value.z) return 0;
            if (value.y >= value.x && value.y >= value.z) return 1;
            return 2;
        }

        private static float GetSecondLongestSize(Vector3 value, int longestAxis)
        {
            if (longestAxis == 0) return Mathf.Max(value.y, value.z);
            if (longestAxis == 1) return Mathf.Max(value.x, value.z);
            return Mathf.Max(value.x, value.y);
        }

        private static float GetAverageOtherAxes(Vector3 value, int axis)
        {
            if (axis == 0) return (value.y + value.z) * 0.5f;
            if (axis == 1) return (value.x + value.z) * 0.5f;
            return (value.x + value.y) * 0.5f;
        }

        private static float GetAxis(Vector3 value, int axis)
        {
            if (axis == 0) return value.x;
            if (axis == 1) return value.y;
            return value.z;
        }

        private static void SetAxis(ref Vector3 value, int axis, float axisValue)
        {
            if (axis == 0)
            {
                value.x = axisValue;
            }
            else if (axis == 1)
            {
                value.y = axisValue;
            }
            else
            {
                value.z = axisValue;
            }
        }

        private static Bounds ToLocalBounds(Transform root, Bounds worldBounds)
        {
            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 worldPoint = worldBounds.center + Vector3.Scale(worldBounds.extents, new Vector3(x, y, z));
                        Vector3 localPoint = root.InverseTransformPoint(worldPoint);
                        min = Vector3.Min(min, localPoint);
                        max = Vector3.Max(max, localPoint);
                    }
                }
            }

            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        private static ItemType GuessItemType(string assetName)
        {
            string lowerName = assetName.ToLowerInvariant();

            if (lowerName.Contains("apple")) return ItemType.Apple;
            if (lowerName.Contains("asparagus")) return ItemType.Asparagus;
            if (lowerName.Contains("aubergine")) return ItemType.Aubergine;
            if (lowerName.Contains("avocado")) return ItemType.Avocado;
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
            GameObject gameOverPanel,
            TextMeshProUGUI gameOverLabel,
            Button restartButton)
        {
            SerializedObject serializedObject = new SerializedObject(gameSession);
            serializedObject.FindProperty("spawnManager").objectReferenceValue = spawnManager;
            serializedObject.FindProperty("counterLabel").objectReferenceValue = counter;
            serializedObject.FindProperty("timerLabel").objectReferenceValue = timer;
            serializedObject.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            serializedObject.FindProperty("gameOverLabel").objectReferenceValue = gameOverLabel;
            serializedObject.FindProperty("restartButton").objectReferenceValue = restartButton;
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

        private static void AssignCollectFeedback(
            CollectFeedbackController feedback,
            Canvas canvas,
            Camera camera,
            RectTransform counterTarget,
            AudioSource audioSource)
        {
            SerializedObject serializedObject = new SerializedObject(feedback);
            serializedObject.FindProperty("canvas").objectReferenceValue = canvas;
            serializedObject.FindProperty("worldCamera").objectReferenceValue = camera;
            serializedObject.FindProperty("starCounterTarget").objectReferenceValue = counterTarget;
            serializedObject.FindProperty("audioSource").objectReferenceValue = audioSource;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignShaftCollector(
            ShaftCollector collector,
            Transform effectsRoot,
            CollectFeedbackController collectFeedback)
        {
            SerializedObject serializedObject = new SerializedObject(collector);
            serializedObject.FindProperty("collectedItemsRoot").objectReferenceValue = effectsRoot;
            serializedObject.FindProperty("collectFeedback").objectReferenceValue = collectFeedback;
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
