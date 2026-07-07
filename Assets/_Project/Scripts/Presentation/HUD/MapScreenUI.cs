using System.Collections.Generic;
using System.Linq;
using ApexShift.Runtime.Buildings;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Fire;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.World.Topography;
using ApexShift.Runtime.World.Landmarks;
using UnityEngine;
using UnityEngine.UI;

namespace ApexShift.Presentation.HUD
{
    /// <summary>
    /// Player-facing full map screen built from runtime registries and topography data.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapScreenUI : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private Font uiFont;
        [SerializeField] private bool pauseWhileOpen = true;
        [SerializeField] private float fallbackWorldRadius = 140f;
        [SerializeField] private int maxTerrainCellsPerAxis = 72;
        [SerializeField] private int maxResourceMarkers = 96;
        [SerializeField] private int maxCreatureMarkers = 96;
        [SerializeField] private int maxBuildingMarkers = 48;
        [SerializeField] private float markerRefreshInterval = 0.20f;

        private CanvasGroup canvasGroup;
        private RectTransform mapSurface;
        private RectTransform terrainLayer;
        private RectTransform markerLayer;
        private Text statusText;
        private Text coordinatesText;
        private Text[] filterLabels;
        private readonly List<GameObject> markerPool = new List<GameObject>();

        private bool isOpen;
        private bool terrainDirty = true;
        private float markerRefreshTimer;
        private float previousTimeScale = 1f;
        private bool timeScaleCaptured;

        private bool showResources = true;
        private bool showCreatures = true;
        private bool showBuildings = true;
        private bool showFireSources = true;
        private bool showLandmarks = true;

        private IslandTopographyRuntime cachedTopography;
        private int cachedGridSize;
        private float cachedTileSize;

        public bool IsOpen => isOpen;

        public void Configure(Transform playerTransform, Font font)
        {
            player = playerTransform;
            uiFont = font != null ? font : uiFont;
            if (uiFont == null)
            {
                uiFont = (Font)Resources.GetBuiltinResource(typeof(Font), "LegacyRuntime.ttf");
            }

            ApplyFontToExistingText();
            terrainDirty = true;
            RefreshNow();
            SetVisible(false);
        }

        private void OnEnable()
        {
            if (!isOpen)
            {
                SetVisible(false);
            }
        }

        private void Awake()
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt == null)
            {
                rt = gameObject.AddComponent<RectTransform>();
            }

            Stretch(rt);
            BuildVisuals();
            SetVisible(false);
        }

        private void OnDisable()
        {
            RestoreTimeScaleIfNeeded();
        }

        private void OnDestroy()
        {
            RestoreTimeScaleIfNeeded();
        }

        private void Update()
        {
            if (!isOpen)
            {
                return;
            }

            markerRefreshTimer -= Time.unscaledDeltaTime;
            if (markerRefreshTimer <= 0f)
            {
                markerRefreshTimer = Mathf.Max(0.05f, markerRefreshInterval);
                RefreshNow();
            }
        }

        public void Toggle()
        {
            SetVisible(!isOpen);
        }

        public void SetVisible(bool visible)
        {
            isOpen = visible;
            gameObject.SetActive(true);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1f : 0f;
                canvasGroup.interactable = visible;
                canvasGroup.blocksRaycasts = visible;
            }

            if (visible)
            {
                CaptureTimeScaleIfNeeded();
                RefreshNow(true);
                transform.SetAsLastSibling();
            }
            else
            {
                RestoreTimeScaleIfNeeded();
            }

            gameObject.SetActive(visible);
        }

        private void BuildVisuals()
        {
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            Image blocker = GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            blocker.color = new Color(0f, 0f, 0f, 0.72f);
            blocker.raycastTarget = true;

            GameObject panel = new GameObject("MapPanel");
            panel.transform.SetParent(transform, false);
            RectTransform panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(1180f, 820f);
            panelRt.anchoredPosition = Vector2.zero;
            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0.025f, 0.035f, 0.025f, 0.98f);

            CreateFrame(panel.transform);

            Text title = CreateText(panel.transform, "Title", "Island Map", 42, new Vector2(0f, 1f), new Vector2(42f, -30f), new Vector2(520f, 56f), TextAnchor.UpperLeft);
            title.color = new Color(0.98f, 0.98f, 0.90f, 1f);
            title.fontStyle = FontStyle.Bold;

            Text hint = CreateText(panel.transform, "Hint", "Press M or Esc to close. Filters use live runtime data.", 15, new Vector2(0f, 1f), new Vector2(44f, -84f), new Vector2(720f, 28f), TextAnchor.UpperLeft);
            hint.color = new Color(0.78f, 0.86f, 0.72f, 0.95f);

            coordinatesText = CreateText(panel.transform, "Coordinates", "x=0 z=0", 14, new Vector2(1f, 1f), new Vector2(-310f, -42f), new Vector2(260f, 28f), TextAnchor.UpperRight);
            coordinatesText.color = new Color(0.90f, 0.94f, 0.84f, 0.95f);

            statusText = CreateText(panel.transform, "Status", "Topography: waiting for world generation", 14, new Vector2(0f, 0f), new Vector2(44f, 34f), new Vector2(760f, 30f), TextAnchor.LowerLeft);
            statusText.color = new Color(0.82f, 0.88f, 0.78f, 0.95f);

            GameObject surface = new GameObject("MapSurface");
            surface.transform.SetParent(panel.transform, false);
            mapSurface = surface.AddComponent<RectTransform>();
            mapSurface.anchorMin = new Vector2(0.5f, 0.5f);
            mapSurface.anchorMax = new Vector2(0.5f, 0.5f);
            mapSurface.pivot = new Vector2(0.5f, 0.5f);
            mapSurface.sizeDelta = new Vector2(720f, 720f);
            mapSurface.anchoredPosition = new Vector2(-118f, -18f);
            Image surfaceBg = surface.AddComponent<Image>();
            surfaceBg.color = new Color(0.03f, 0.06f, 0.08f, 1f);

            GameObject terrain = new GameObject("TerrainLayer");
            terrain.transform.SetParent(surface.transform, false);
            terrainLayer = terrain.AddComponent<RectTransform>();
            Stretch(terrainLayer);

            GameObject markers = new GameObject("MarkerLayer");
            markers.transform.SetParent(surface.transform, false);
            markerLayer = markers.AddComponent<RectTransform>();
            Stretch(markerLayer);

            CreateCrosshair(surface.transform);
            BuildLegend(panel.transform);
            BuildFilters(panel.transform);
        }

        private void BuildLegend(Transform parent)
        {
            GameObject legend = new GameObject("Legend");
            legend.transform.SetParent(parent, false);
            RectTransform rt = legend.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.sizeDelta = new Vector2(280f, 300f);
            rt.anchoredPosition = new Vector2(-42f, -38f);
            Image bg = legend.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.07f, 0.04f, 0.82f);

            Text label = CreateText(legend.transform, "LegendTitle", "Legend", 22, new Vector2(0f, 1f), new Vector2(16f, -12f), new Vector2(240f, 32f), TextAnchor.UpperLeft);
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(0.95f, 0.98f, 0.86f, 1f);

            CreateLegendRow(legend.transform, "Water", TerrainColor(TerrainType.Water), 54f);
            CreateLegendRow(legend.transform, "Beach / shore", TerrainColor(TerrainType.Beach), 84f);
            CreateLegendRow(legend.transform, "Plain", TerrainColor(TerrainType.Plain), 114f);
            CreateLegendRow(legend.transform, "Forest", TerrainColor(TerrainType.Forest), 144f);
            CreateLegendRow(legend.transform, "Hills", TerrainColor(TerrainType.Hills), 174f);
            CreateLegendRow(legend.transform, "Ridge", TerrainColor(TerrainType.Ridge), 204f);
            CreateLegendRow(legend.transform, "Player", new Color(0.95f, 1f, 0.95f, 1f), 242f);
        }

        private void BuildFilters(Transform parent)
        {
            GameObject filters = new GameObject("Filters");
            filters.transform.SetParent(parent, false);
            RectTransform rt = filters.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.sizeDelta = new Vector2(280f, 210f);
            rt.anchoredPosition = new Vector2(-42f, -112f);
            Image bg = filters.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.07f, 0.04f, 0.76f);

            Text title = CreateText(filters.transform, "FiltersTitle", "Filters", 22, new Vector2(0f, 1f), new Vector2(16f, -12f), new Vector2(240f, 32f), TextAnchor.UpperLeft);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.95f, 0.98f, 0.86f, 1f);

            filterLabels = new Text[5];
            CreateFilterButton(filters.transform, 0, "Resources", new Vector2(16f, -54f), () => showResources = !showResources);
            CreateFilterButton(filters.transform, 1, "Creatures", new Vector2(16f, -84f), () => showCreatures = !showCreatures);
            CreateFilterButton(filters.transform, 2, "Buildings", new Vector2(16f, -114f), () => showBuildings = !showBuildings);
            CreateFilterButton(filters.transform, 3, "Fire sources", new Vector2(16f, -144f), () => showFireSources = !showFireSources);
            CreateFilterButton(filters.transform, 4, "Landmarks (pending)", new Vector2(16f, -174f), () => showLandmarks = !showLandmarks);
            UpdateFilterLabels();
        }

        private void CreateFilterButton(Transform parent, int index, string label, Vector2 anchoredPos, System.Action toggle)
        {
            GameObject go = new GameObject($"Filter_{label}");
            go.transform.SetParent(parent, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(244f, 24f);
            rt.anchoredPosition = anchoredPos;

            Image bg = go.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.14f, 0.10f, 0.86f);

            Button button = go.AddComponent<Button>();
            button.targetGraphic = bg;
            button.onClick.AddListener(() =>
            {
                toggle?.Invoke();
                UpdateFilterLabels();
                RefreshNow();
            });

            Text text = CreateText(go.transform, "Label", label, 14, new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(228f, 22f), TextAnchor.MiddleLeft);
            filterLabels[index] = text;
        }

        private void RefreshNow(bool forceTerrain = false)
        {
            RefreshTerrain(forceTerrain);
            RefreshMarkers();
            RefreshCoordinates();
        }

        private void RefreshTerrain(bool force)
        {
            IslandTopographyRuntime topography = IslandTopographyRuntime.Active;
            bool topographyChanged = topography != cachedTopography
                || (topography != null && (topography.GridSize != cachedGridSize || Mathf.Abs(topography.TileSize - cachedTileSize) > 0.001f));

            if (!force && !terrainDirty && !topographyChanged)
            {
                return;
            }

            terrainDirty = false;
            cachedTopography = topography;
            cachedGridSize = topography != null ? topography.GridSize : 0;
            cachedTileSize = topography != null ? topography.TileSize : 0f;

            ClearChildren(terrainLayer);
            if (terrainLayer == null)
            {
                return;
            }

            if (topography == null || !topography.IsBuilt || topography.GetGridReadOnly() == null)
            {
                if (statusText != null)
                {
                    statusText.text = "Topography: not built yet. Generate or load a world first.";
                }

                CreateFallbackMapBackground();
                return;
            }

            TopographyCell[,] grid = topography.GetGridReadOnly();
            int gridSize = Mathf.Max(1, topography.GridSize);
            int step = Mathf.Max(1, Mathf.CeilToInt(gridSize / (float)Mathf.Max(8, maxTerrainCellsPerAxis)));
            float mapSize = GetMapSize();
            float cellSize = Mathf.Max(2f, mapSize / gridSize * step);

            for (int z = 0; z < gridSize; z += step)
            {
                for (int x = 0; x < gridSize; x += step)
                {
                    TopographyCell cell = grid[x, z];
                    if (cell == null)
                    {
                        continue;
                    }

                    GameObject go = new GameObject($"Cell_{x}_{z}");
                    go.transform.SetParent(terrainLayer, false);
                    Image image = go.AddComponent<Image>();
                    image.color = TerrainColor(cell.TerrainType);
                    image.raycastTarget = false;

                    RectTransform rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(cellSize + 0.5f, cellSize + 0.5f);

                    float normalizedX = (x + 0.5f) / gridSize - 0.5f;
                    float normalizedY = (z + 0.5f) / gridSize - 0.5f;
                    rt.anchoredPosition = new Vector2(normalizedX * mapSize, normalizedY * mapSize);
                }
            }

            if (statusText != null)
            {
                statusText.text = $"Topography: {topography.LandCellCount} land / {topography.WaterCellCount} water / {topography.RidgeCellCount} ridge";
            }
        }

        private void RefreshMarkers()
        {
            if (markerLayer == null)
            {
                return;
            }

            int index = 0;

            if (showResources)
            {
                IReadOnlyList<ResourceNodeView> resources = ResourceRegistry.Resources;
                int count = 0;
                for (int i = 0; i < resources.Count && count < maxResourceMarkers; i++)
                {
                    ResourceNodeView resource = resources[i];
                    if (resource == null || resource.gameObject == null || !resource.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    ShowMarker(ref index, resource.transform.position, GetResourceColor(resource), new Vector2(7f, 7f), "resource");
                    count++;
                }
            }

            if (showCreatures)
            {
                EcosystemRuntime ecosystem = EcosystemRuntime.Instance;
                IReadOnlyList<CreatureAgentView> creatures = ecosystem != null ? ecosystem.Creatures : null;
                if (creatures != null)
                {
                    int count = 0;
                    for (int i = 0; i < creatures.Count && count < maxCreatureMarkers; i++)
                    {
                        CreatureAgentView creature = creatures[i];
                        if (creature == null || creature.gameObject == null || !creature.gameObject.activeInHierarchy)
                        {
                            continue;
                        }

                        bool isVarnak = Normalize(creature.CreatureId) == "varnak";
                        ShowMarker(ref index, creature.transform.position, GetCreatureColor(creature), isVarnak ? new Vector2(12f, 12f) : new Vector2(8f, 8f), isVarnak ? "varnak" : "creature");
                        count++;
                    }
                }
            }

            if (showBuildings)
            {
                BuildingRegistry registry = BuildingRegistry.Active;
                IReadOnlyList<PlaceableStructureRuntime> structures = registry != null ? registry.Structures : null;
                if (structures != null)
                {
                    int count = 0;
                    for (int i = 0; i < structures.Count && count < maxBuildingMarkers; i++)
                    {
                        PlaceableStructureRuntime structure = structures[i];
                        if (structure == null || structure.gameObject == null || !structure.gameObject.activeInHierarchy)
                        {
                            continue;
                        }

                        ShowMarker(ref index, structure.transform.position, GetBuildingColor(structure), new Vector2(10f, 10f), "building");
                        count++;
                    }
                }
            }

            if (showFireSources)
            {
                IReadOnlyList<FireSourceRuntime> sources = FireSourceRegistry.Sources;
                for (int i = 0; i < sources.Count; i++)
                {
                    FireSourceRuntime source = sources[i];
                    if (source == null || source.gameObject == null || !source.gameObject.activeInHierarchy || !source.IsActiveFire)
                    {
                        continue;
                    }

                    ShowMarker(ref index, source.transform.position, new Color(1f, 0.48f, 0.08f, 1f), new Vector2(12f, 12f), "fire");
                }
            }

            if (showLandmarks)
            {
                IReadOnlyList<LandmarkRuntime> landmarks = LandmarkRegistry.Landmarks;
                for (int i = 0; i < landmarks.Count; i++)
                {
                    LandmarkRuntime landmark = landmarks[i];
                    if (landmark == null || landmark.gameObject == null || !landmark.gameObject.activeInHierarchy)
                    {
                        continue;
                    }

                    ShowMarker(ref index, landmark.transform.position, GetLandmarkColor(landmark), GetLandmarkSize(landmark), "landmark");
                }
            }

            ShowPlayerMarker(ref index);

            for (int i = index; i < markerPool.Count; i++)
            {
                markerPool[i].SetActive(false);
            }
        }

        private void ShowPlayerMarker(ref int index)
        {
            if (player == null)
            {
                return;
            }

            GameObject marker = ShowMarker(ref index, player.position, new Color(0.95f, 1f, 0.95f, 1f), new Vector2(16f, 16f), "player");
            marker.transform.localRotation = Quaternion.Euler(0f, 0f, -player.eulerAngles.y);
        }

        private GameObject ShowMarker(ref int index, Vector3 worldPos, Color color, Vector2 size, string label)
        {
            EnsureMarkerCount(index + 1);
            GameObject marker = markerPool[index++];
            marker.name = $"Marker_{label}";
            marker.SetActive(true);

            RectTransform rt = marker.GetComponent<RectTransform>();
            rt.anchoredPosition = WorldToMap(worldPos);
            rt.sizeDelta = size;
            rt.localRotation = Quaternion.identity;

            Image img = marker.GetComponent<Image>();
            if (img != null)
            {
                img.color = color;
            }

            return marker;
        }

        private void EnsureMarkerCount(int count)
        {
            while (markerPool.Count < count)
            {
                GameObject go = new GameObject("Marker");
                go.transform.SetParent(markerLayer, false);
                Image img = go.AddComponent<Image>();
                img.raycastTarget = false;

                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(8f, 8f);

                markerPool.Add(go);
            }
        }

        private Vector2 WorldToMap(Vector3 worldPos)
        {
            float mapSize = GetMapSize();
            IslandTopographyRuntime topography = IslandTopographyRuntime.Active;
            if (topography != null && topography.IsBuilt && topography.GridSize > 0 && topography.TileSize > 0f)
            {
                float extent = topography.GridSize * topography.TileSize;
                Vector2 normalized = new Vector2(worldPos.x / extent, worldPos.z / extent);
                normalized = Vector2.ClampMagnitude(normalized, 0.5f);
                return new Vector2(normalized.x * mapSize, normalized.y * mapSize);
            }

            Vector2 fallback = new Vector2(worldPos.x, worldPos.z) / Mathf.Max(10f, fallbackWorldRadius);
            fallback = Vector2.ClampMagnitude(fallback, 1f);
            return fallback * (mapSize * 0.5f);
        }

        private void RefreshCoordinates()
        {
            if (coordinatesText == null)
            {
                return;
            }

            if (player == null)
            {
                coordinatesText.text = "Player: unavailable";
                return;
            }

            coordinatesText.text = $"x={player.position.x:0} z={player.position.z:0}";
        }

        private void CaptureTimeScaleIfNeeded()
        {
            if (!pauseWhileOpen || timeScaleCaptured)
            {
                return;
            }

            previousTimeScale = Time.timeScale;
            timeScaleCaptured = true;
            Time.timeScale = 0f;
        }

        private void RestoreTimeScaleIfNeeded()
        {
            if (!timeScaleCaptured)
            {
                return;
            }

            Time.timeScale = previousTimeScale;
            timeScaleCaptured = false;
        }

        private void CreateFallbackMapBackground()
        {
            GameObject go = new GameObject("FallbackIsland");
            go.transform.SetParent(terrainLayer, false);
            Image image = go.AddComponent<Image>();
            image.color = new Color(0.16f, 0.34f, 0.18f, 0.72f);
            image.raycastTarget = false;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.one * (GetMapSize() * 0.72f);
            rt.anchoredPosition = Vector2.zero;
        }

        private void CreateFrame(Transform parent)
        {
            GameObject border = new GameObject("Frame");
            border.transform.SetParent(parent, false);
            Image image = border.AddComponent<Image>();
            image.color = new Color(0.22f, 0.30f, 0.18f, 1f);
            image.raycastTarget = false;

            RectTransform rt = border.GetComponent<RectTransform>();
            Stretch(rt);
            rt.offsetMin = new Vector2(-3f, -3f);
            rt.offsetMax = new Vector2(3f, 3f);
            border.transform.SetAsFirstSibling();
        }

        private void CreateCrosshair(Transform parent)
        {
            CreateLine(parent, "HorizontalAxis", true, new Color(1f, 1f, 1f, 0.12f));
            CreateLine(parent, "VerticalAxis", false, new Color(1f, 1f, 1f, 0.12f));
        }

        private void CreateLine(Transform parent, string name, bool horizontal, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Image image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = horizontal
                ? new Vector2(GetMapSize(), 2f)
                : new Vector2(2f, GetMapSize());
            rt.anchoredPosition = Vector2.zero;
        }

        private void CreateLegendRow(Transform parent, string text, Color color, float y)
        {
            GameObject swatch = new GameObject(text + "Swatch");
            swatch.transform.SetParent(parent, false);
            Image swatchImg = swatch.AddComponent<Image>();
            swatchImg.color = color;
            swatchImg.raycastTarget = false;
            RectTransform swatchRt = swatch.GetComponent<RectTransform>();
            swatchRt.anchorMin = new Vector2(0f, 1f);
            swatchRt.anchorMax = new Vector2(0f, 1f);
            swatchRt.pivot = new Vector2(0f, 1f);
            swatchRt.sizeDelta = new Vector2(18f, 18f);
            swatchRt.anchoredPosition = new Vector2(18f, -y);

            Text label = CreateText(parent, text + "Label", text, 14, new Vector2(0f, 1f), new Vector2(46f, -y + 2f), new Vector2(210f, 22f), TextAnchor.UpperLeft);
            label.color = new Color(0.88f, 0.92f, 0.84f, 1f);
        }

        private Text CreateText(Transform parent, string name, string text, int size, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta, TextAnchor alignment)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text label = go.AddComponent<Text>();
            label.text = text;
            label.fontSize = size;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            label.font = uiFont != null ? uiFont : (Font)Resources.GetBuiltinResource(typeof(Font), "LegacyRuntime.ttf");

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.sizeDelta = sizeDelta;
            rt.anchoredPosition = anchoredPosition;
            return label;
        }

        private void UpdateFilterLabels()
        {
            SetFilterLabel(0, showResources, "Resources");
            SetFilterLabel(1, showCreatures, "Creatures");
            SetFilterLabel(2, showBuildings, "Buildings");
            SetFilterLabel(3, showFireSources, "Fire sources");
            SetFilterLabel(4, showLandmarks, "Landmarks (pending)");
        }

        private void SetFilterLabel(int index, bool active, string label)
        {
            if (filterLabels == null || index < 0 || index >= filterLabels.Length || filterLabels[index] == null)
            {
                return;
            }

            filterLabels[index].text = $"{(active ? "[x]" : "[ ]")} {label}";
            filterLabels[index].color = active ? new Color(0.92f, 0.98f, 0.84f, 1f) : new Color(0.55f, 0.62f, 0.52f, 1f);
        }

        private void ApplyFontToExistingText()
        {
            if (uiFont == null)
            {
                return;
            }

            Text[] texts = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null)
                {
                    texts[i].font = uiFont;
                }
            }
        }

        private static void Stretch(RectTransform rt)
        {
            if (rt == null)
            {
                return;
            }

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void ClearChildren(RectTransform parent)
        {
            if (parent == null)
            {
                return;
            }

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private float GetMapSize()
        {
            if (mapSurface != null)
            {
                float width = mapSurface.rect.width;
                float height = mapSurface.rect.height;
                if (width > 1f && height > 1f)
                {
                    return Mathf.Min(width, height);
                }
            }

            return 720f;
        }

        private static Color TerrainColor(TerrainType type)
        {
            switch (type)
            {
                case TerrainType.Water: return new Color(0.10f, 0.22f, 0.42f, 1f);
                case TerrainType.Beach: return new Color(0.76f, 0.68f, 0.38f, 1f);
                case TerrainType.Plain: return new Color(0.22f, 0.46f, 0.20f, 1f);
                case TerrainType.Forest: return new Color(0.08f, 0.30f, 0.11f, 1f);
                case TerrainType.Hills: return new Color(0.42f, 0.39f, 0.18f, 1f);
                case TerrainType.Ridge: return new Color(0.34f, 0.32f, 0.28f, 1f);
                default: return new Color(0.16f, 0.16f, 0.16f, 1f);
            }
        }

        private static Color GetResourceColor(ResourceNodeView resource)
        {
            string kind = resource != null ? (resource.name ?? string.Empty).ToLowerInvariant() : string.Empty;
            if (kind.Contains("rock")) return new Color(0.78f, 0.80f, 0.84f, 1f);
            if (kind.Contains("tree") || kind.Contains("wood")) return new Color(0.28f, 0.82f, 0.28f, 1f);
            if (kind.Contains("bush") || kind.Contains("grass")) return new Color(0.62f, 0.90f, 0.28f, 1f);
            if (kind.Contains("meat")) return new Color(0.85f, 0.24f, 0.20f, 1f);
            return new Color(0.95f, 0.68f, 0.20f, 1f);
        }

        private static Color GetCreatureColor(CreatureAgentView creature)
        {
            string id = creature != null ? Normalize(creature.CreatureId) : string.Empty;
            if (id == "varnak") return new Color(1f, 0.12f, 0.12f, 1f);
            if (id == "grazer") return new Color(0.95f, 0.72f, 0.18f, 1f);
            if (id == "small_prey") return new Color(0.82f, 0.94f, 0.26f, 1f);
            return new Color(0.92f, 0.35f, 0.35f, 1f);
        }

        private static Color GetBuildingColor(PlaceableStructureRuntime structure)
        {
            string id = structure != null ? Normalize(structure.BuildingId) : string.Empty;
            if (id == "campfire") return new Color(1f, 0.48f, 0.08f, 1f);
            if (id == "storage_box") return new Color(0.55f, 0.34f, 0.14f, 1f);
            if (id == "trap") return new Color(0.78f, 0.18f, 0.86f, 1f);
            return new Color(0.56f, 0.62f, 0.68f, 1f);
        }

        private static Color GetLandmarkColor(LandmarkRuntime landmark)
        {
            if (landmark == null) return new Color(0.75f, 0.75f, 0.75f, 1f);
            switch (landmark.Type)
            {
                case LandmarkType.OldTree: return new Color(0.18f, 0.72f, 0.18f, 1f);
                case LandmarkType.Ruins: return new Color(0.68f, 0.68f, 0.68f, 1f);
                case LandmarkType.Pond: return new Color(0.18f, 0.58f, 0.88f, 1f);
                case LandmarkType.Camp: return new Color(1f, 0.68f, 0.28f, 1f);
                case LandmarkType.CavePlaceholder: return new Color(0.38f, 0.38f, 0.38f, 1f);
                default: return new Color(0.75f, 0.75f, 0.75f, 1f);
            }
        }

        private static Vector2 GetLandmarkSize(LandmarkRuntime landmark)
        {
            if (landmark == null) return new Vector2(10f, 10f);
            switch (landmark.Type)
            {
                case LandmarkType.OldTree: return new Vector2(14f, 14f);
                case LandmarkType.Ruins: return new Vector2(12f, 12f);
                case LandmarkType.Pond: return new Vector2(13f, 13f);
                case LandmarkType.Camp: return new Vector2(11f, 11f);
                case LandmarkType.CavePlaceholder: return new Vector2(10f, 10f);
                default: return new Vector2(10f, 10f);
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }
}
