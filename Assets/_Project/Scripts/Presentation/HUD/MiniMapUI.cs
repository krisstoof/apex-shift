using System.Collections.Generic;
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
    [DisallowMultipleComponent]
    public sealed class MiniMapUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;

        [Header("Scale / Refresh")]
        [SerializeField] private float worldRadius = 120f;
        [SerializeField] private float refreshInterval = 0.25f;
        [SerializeField] private float terrainRefreshInterval = 0.6f;

        [Header("Marker limits")]
        [SerializeField] private int maxResourceMarkers = 18;
        [SerializeField] private int maxCreatureMarkers = 18;
        [SerializeField] private int maxBuildingMarkers = 12;
        [SerializeField] private int maxFireMarkers = 8;

        [Header("Terrain overlay")]
        [SerializeField] private bool showTopographyOverlay = true;
        [SerializeField] private int terrainSamplesPerAxis = 28;
        [SerializeField] private int maxTerrainCells = 784;

        private RectTransform root;
        private RectTransform terrainLayer;
        private RectTransform dotLayer;
        private RectTransform playerMarker;
        private RectTransform playerHeading;

        private readonly List<GameObject> markerPool = new List<GameObject>(64);
        private readonly List<GameObject> terrainPool = new List<GameObject>(784);
        private readonly List<MiniMapMarker> markers = new List<MiniMapMarker>(64);

        private float refreshTimer;
        private float terrainRefreshTimer;
        private IslandTopographyRuntime cachedTopography;
        private int cachedGridSize = -1;
        private float cachedTileSize = -1f;
        private int cachedTerrainStep = 1;

        public void Configure(Transform playerTransform, float radius)
        {
            player = playerTransform;
            worldRadius = Mathf.Max(10f, radius);
            refreshTimer = 0f;
            terrainRefreshTimer = 0f;
        }

        private void Awake()
        {
            root = GetComponent<RectTransform>();
            if (root == null)
            {
                root = gameObject.AddComponent<RectTransform>();
            }

            BuildVisuals();
        }

        private void LateUpdate()
        {
            if (player == null)
            {
                HideMarkers();
                return;
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = Mathf.Max(0.05f, refreshInterval);
            RefreshMarkersAndOverlay();
        }

        private void BuildVisuals()
        {
            Image bg = GetComponent<Image>();
            if (bg == null)
            {
                bg = gameObject.AddComponent<Image>();
            }

            bg.color = new Color(0f, 0f, 0f, 0.32f);

            GameObject frameGo = new GameObject("Frame");
            frameGo.transform.SetParent(transform, false);
            Image frame = frameGo.AddComponent<Image>();
            frame.color = new Color(0.11f, 0.16f, 0.11f, 0.96f);
            RectTransform frameRt = frameGo.GetComponent<RectTransform>();
            frameRt.anchorMin = Vector2.zero;
            frameRt.anchorMax = Vector2.one;
            frameRt.offsetMin = new Vector2(10f, 10f);
            frameRt.offsetMax = new Vector2(-10f, -10f);

            GameObject gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(frameGo.transform, false);
            Image grid = gridGo.AddComponent<Image>();
            grid.color = new Color(1f, 1f, 1f, 0.04f);
            RectTransform gridRt = gridGo.GetComponent<RectTransform>();
            gridRt.anchorMin = Vector2.zero;
            gridRt.anchorMax = Vector2.one;
            gridRt.offsetMin = new Vector2(1f, 1f);
            gridRt.offsetMax = new Vector2(-1f, -1f);

            GameObject terrainGo = new GameObject("Topography");
            terrainGo.transform.SetParent(frameGo.transform, false);
            terrainLayer = terrainGo.AddComponent<RectTransform>();
            terrainLayer.anchorMin = Vector2.zero;
            terrainLayer.anchorMax = Vector2.one;
            terrainLayer.offsetMin = new Vector2(8f, 8f);
            terrainLayer.offsetMax = new Vector2(-8f, -8f);

            GameObject dotsGo = new GameObject("Dots");
            dotsGo.transform.SetParent(frameGo.transform, false);
            dotLayer = dotsGo.AddComponent<RectTransform>();
            dotLayer.anchorMin = Vector2.zero;
            dotLayer.anchorMax = Vector2.one;
            dotLayer.offsetMin = new Vector2(8f, 8f);
            dotLayer.offsetMax = new Vector2(-8f, -8f);

            GameObject playerGo = new GameObject("PlayerMarker");
            playerGo.transform.SetParent(frameGo.transform, false);
            Image playerImg = playerGo.AddComponent<Image>();
            playerImg.color = new Color(0.92f, 0.98f, 0.95f, 1f);
            RectTransform playerRt = playerGo.GetComponent<RectTransform>();
            playerRt.anchorMin = new Vector2(0.5f, 0.5f);
            playerRt.anchorMax = new Vector2(0.5f, 0.5f);
            playerRt.pivot = new Vector2(0.5f, 0.5f);
            playerRt.sizeDelta = new Vector2(12f, 12f);
            playerMarker = playerRt;

            GameObject headingGo = new GameObject("PlayerHeading");
            headingGo.transform.SetParent(playerGo.transform, false);
            Image heading = headingGo.AddComponent<Image>();
            heading.color = new Color(0.15f, 0.9f, 0.4f, 1f);
            RectTransform headingRt = headingGo.GetComponent<RectTransform>();
            headingRt.anchorMin = new Vector2(0.5f, 0.5f);
            headingRt.anchorMax = new Vector2(0.5f, 0.5f);
            headingRt.pivot = new Vector2(0.5f, 0f);
            headingRt.sizeDelta = new Vector2(3f, 14f);
            headingRt.anchoredPosition = new Vector2(0f, 6f);
            playerHeading = headingRt;

            CreateLabel("N", new Vector2(0f, 1f), new Vector2(6f, -4f));
            CreateLabel("E", new Vector2(1f, 0.5f), new Vector2(-4f, 0f));
            CreateLabel("S", new Vector2(0f, 0f), new Vector2(6f, 4f));
            CreateLabel("W", new Vector2(0f, 0.5f), new Vector2(4f, 0f));
        }

        private void CreateLabel(string text, Vector2 anchor, Vector2 offset)
        {
            GameObject labelGo = new GameObject(text + "Label");
            labelGo.transform.SetParent(transform, false);
            Text label = labelGo.AddComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 12;
            label.color = new Color(1f, 1f, 1f, 0.8f);
            RectTransform labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = anchor;
            labelRt.anchorMax = anchor;
            labelRt.pivot = anchor;
            labelRt.sizeDelta = new Vector2(16f, 16f);
            labelRt.anchoredPosition = offset;
        }

        private void RefreshMarkersAndOverlay()
        {
            if (dotLayer == null)
            {
                return;
            }

            Vector3 center = player.position;

            terrainRefreshTimer -= Mathf.Max(0.05f, refreshInterval);
            if (terrainRefreshTimer <= 0f)
            {
                terrainRefreshTimer = Mathf.Max(0.1f, terrainRefreshInterval);
                RefreshTopographyOverlay(center);
            }

            markers.Clear();
            CollectResourceMarkers();
            CollectCreatureMarkers();
            CollectBuildingMarkers();
            CollectFireMarkers();
            CollectLandmarkMarkers();

            EnsureMarkerCount(markers.Count);
            UpdatePlayerMarker();

            for (int i = 0; i < markers.Count; i++)
            {
                MiniMapMarker marker = markers[i];
                GameObject markerGo = markerPool[i];
                markerGo.SetActive(true);

                RectTransform rt = markerGo.GetComponent<RectTransform>();
                rt.sizeDelta = marker.Size;
                rt.anchoredPosition = WorldToMiniMapPosition(marker.Position, center, clampToCircle: true);

                Image img = markerGo.GetComponent<Image>();
                if (img != null)
                {
                    img.color = marker.Color;
                }
            }

            for (int i = markers.Count; i < markerPool.Count; i++)
            {
                markerPool[i].SetActive(false);
            }
        }

        private void UpdatePlayerMarker()
        {
            if (playerMarker != null)
            {
                playerMarker.anchoredPosition = Vector2.zero;
                playerMarker.localRotation = Quaternion.identity;
            }

            if (playerHeading != null && player != null)
            {
                playerHeading.localRotation = Quaternion.Euler(0f, 0f, -player.eulerAngles.y);
            }
        }

        private void CollectResourceMarkers()
        {
            IReadOnlyList<ResourceNodeView> resources = ResourceRegistry.Resources;
            int added = 0;
            int cap = Mathf.Max(0, maxResourceMarkers);

            for (int i = 0; i < resources.Count && added < cap; i++)
            {
                ResourceNodeView resource = resources[i];
                if (resource == null || resource.gameObject == null || !resource.gameObject.activeInHierarchy)
                {
                    continue;
                }

                AddMarker(resource.transform.position, GetResourceColor(resource), GetResourceSize(resource));
                added++;
            }
        }

        private void CollectCreatureMarkers()
        {
            EcosystemRuntime ecosystem = EcosystemRuntime.Instance;
            IReadOnlyList<CreatureAgentView> creatures = ecosystem != null ? ecosystem.Creatures : null;
            if (creatures == null)
            {
                return;
            }

            int cap = Mathf.Max(0, maxCreatureMarkers);
            int added = 0;

            for (int i = 0; i < creatures.Count && added < cap; i++)
            {
                CreatureAgentView creature = creatures[i];
                if (!IsVisibleCreature(creature) || !IsVarnak(creature))
                {
                    continue;
                }

                AddMarker(creature.transform.position, GetCreatureColor(creature), new Vector2(9f, 9f));
                added++;
            }

            for (int i = 0; i < creatures.Count && added < cap; i++)
            {
                CreatureAgentView creature = creatures[i];
                if (!IsVisibleCreature(creature) || IsVarnak(creature))
                {
                    continue;
                }

                AddMarker(creature.transform.position, GetCreatureColor(creature), new Vector2(7f, 7f));
                added++;
            }
        }

        private void CollectBuildingMarkers()
        {
            BuildingRegistry registry = BuildingRegistry.Active;
            IReadOnlyList<PlaceableStructureRuntime> structures = registry != null ? registry.Structures : null;
            if (structures == null)
            {
                return;
            }

            int added = 0;
            int cap = Mathf.Max(0, maxBuildingMarkers);
            for (int i = 0; i < structures.Count && added < cap; i++)
            {
                PlaceableStructureRuntime structure = structures[i];
                if (structure == null || structure.gameObject == null || !structure.gameObject.activeInHierarchy)
                {
                    continue;
                }

                AddMarker(structure.transform.position, GetBuildingColor(structure), GetBuildingSize(structure));
                added++;
            }
        }

        private void CollectFireMarkers()
        {
            IReadOnlyList<FireSourceRuntime> sources = FireSourceRegistry.Sources;
            int added = 0;
            int cap = Mathf.Max(0, maxFireMarkers);

            for (int i = 0; i < sources.Count && added < cap; i++)
            {
                FireSourceRuntime source = sources[i];
                if (source == null || !source.IsActiveFire)
                {
                    continue;
                }

                AddMarker(source.transform.position, new Color(1f, 0.46f, 0.10f, 1f), new Vector2(8f, 8f));
                added++;
            }
        }

        private void CollectLandmarkMarkers()
        {
            IReadOnlyList<LandmarkRuntime> landmarks = LandmarkRegistry.Landmarks;
            for (int i = 0; i < landmarks.Count; i++)
            {
                LandmarkRuntime landmark = landmarks[i];
                if (landmark == null || landmark.gameObject == null || !landmark.gameObject.activeInHierarchy)
                {
                    continue;
                }

                AddMarker(landmark.transform.position, GetLandmarkColor(landmark), GetLandmarkSize(landmark));
            }
        }

        private void AddMarker(Vector3 position, Color color, Vector2 size)
        {
            markers.Add(new MiniMapMarker(position, color, size));
        }

        private void RefreshTopographyOverlay(Vector3 center)
        {
            if (!showTopographyOverlay || terrainLayer == null)
            {
                HideTerrainOverlay();
                return;
            }

            IslandTopographyRuntime topography = IslandTopographyRuntime.Active;
            TopographyCell[,] grid = topography != null && topography.IsBuilt ? topography.GetGridReadOnly() : null;
            if (grid == null)
            {
                HideTerrainOverlay();
                return;
            }

            int gridSize = Mathf.Max(0, topography.GridSize);
            if (gridSize <= 0)
            {
                HideTerrainOverlay();
                return;
            }

            if (cachedTopography != topography || cachedGridSize != gridSize || !Mathf.Approximately(cachedTileSize, topography.TileSize))
            {
                cachedTopography = topography;
                cachedGridSize = gridSize;
                cachedTileSize = topography.TileSize;
                int requestedAxis = Mathf.Clamp(terrainSamplesPerAxis, 8, 64);
                cachedTerrainStep = Mathf.Max(1, Mathf.CeilToInt(gridSize / (float)requestedAxis));
            }

            int visible = 0;
            int terrainBudget = Mathf.Max(0, maxTerrainCells);
            float cellPixelSize = ResolveTerrainCellPixelSize(topography.TileSize, cachedTerrainStep);

            for (int z = 0; z < gridSize && visible < terrainBudget; z += cachedTerrainStep)
            {
                for (int x = 0; x < gridSize && visible < terrainBudget; x += cachedTerrainStep)
                {
                    TopographyCell cell = grid[x, z];
                    if (cell == null)
                    {
                        continue;
                    }

                    Vector3 delta = cell.WorldCenter - center;
                    delta.y = 0f;
                    if (Mathf.Abs(delta.x) > worldRadius || Mathf.Abs(delta.z) > worldRadius)
                    {
                        continue;
                    }

                    Vector2 anchored = WorldToMiniMapPosition(cell.WorldCenter, center, clampToCircle: false);
                    GameObject cellGo = GetOrCreateTerrainCell(visible++);
                    RectTransform rt = cellGo.GetComponent<RectTransform>();
                    rt.anchoredPosition = anchored;
                    rt.sizeDelta = new Vector2(cellPixelSize, cellPixelSize);

                    Image img = cellGo.GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = GetTerrainColor(cell);
                    }

                    cellGo.SetActive(true);
                }
            }

            for (int i = visible; i < terrainPool.Count; i++)
            {
                terrainPool[i].SetActive(false);
            }
        }

        private float ResolveTerrainCellPixelSize(float tileSize, int step)
        {
            float baseSize = Mathf.Min(dotLayer.rect.width, dotLayer.rect.height);
            float size = tileSize * Mathf.Max(1, step) / Mathf.Max(1f, worldRadius) * baseSize * 0.5f;
            return Mathf.Clamp(size, 2f, 14f);
        }

        private Vector2 WorldToMiniMapPosition(Vector3 worldPosition, Vector3 center, bool clampToCircle)
        {
            Vector3 delta = worldPosition - center;
            Vector2 normalized = new Vector2(delta.x, delta.z) / Mathf.Max(1f, worldRadius);
            if (clampToCircle)
            {
                normalized = Vector2.ClampMagnitude(normalized, 1f);
            }

            return new Vector2(
                normalized.x * 0.5f * dotLayer.rect.width,
                normalized.y * 0.5f * dotLayer.rect.height);
        }

        private static bool IsVisibleCreature(CreatureAgentView creature)
        {
            if (creature == null || creature.gameObject == null || !creature.gameObject.activeInHierarchy)
            {
                return false;
            }

            CreatureHealthRuntime health = creature.GetComponent<CreatureHealthRuntime>();
            return health == null || !health.IsDead;
        }

        private static bool IsVarnak(CreatureAgentView creature)
        {
            string id = creature != null ? creature.CreatureId : string.Empty;
            return string.Equals(id != null ? id.Trim() : string.Empty, "varnak", System.StringComparison.OrdinalIgnoreCase);
        }

        private void HideMarkers()
        {
            for (int i = 0; i < markerPool.Count; i++)
            {
                markerPool[i].SetActive(false);
            }

            HideTerrainOverlay();
        }

        private void HideTerrainOverlay()
        {
            for (int i = 0; i < terrainPool.Count; i++)
            {
                terrainPool[i].SetActive(false);
            }
        }

        private static Color GetTerrainColor(TopographyCell cell)
        {
            if (cell == null)
            {
                return new Color(0f, 0f, 0f, 0f);
            }

            switch (cell.TerrainType)
            {
                case TerrainType.Water:
                    return new Color(0.10f, 0.28f, 0.48f, 0.55f);
                case TerrainType.Beach:
                    return new Color(0.62f, 0.56f, 0.32f, 0.45f);
                case TerrainType.Forest:
                    return new Color(0.08f, 0.32f, 0.12f, 0.52f);
                case TerrainType.Hills:
                    return new Color(0.42f, 0.36f, 0.20f, 0.50f);
                case TerrainType.Ridge:
                    return new Color(0.34f, 0.30f, 0.26f, 0.58f);
                case TerrainType.Plain:
                default:
                    return new Color(0.18f, 0.44f, 0.18f, 0.46f);
            }
        }

        private static Color GetResourceColor(ResourceNodeView resource)
        {
            if (resource == null)
            {
                return new Color(0.85f, 0.85f, 0.85f, 1f);
            }

            string kind = (resource.name ?? string.Empty).ToLowerInvariant();
            if (kind.IndexOf("rock", System.StringComparison.OrdinalIgnoreCase) >= 0) return new Color(0.78f, 0.8f, 0.84f, 1f);
            if (kind.IndexOf("wood", System.StringComparison.OrdinalIgnoreCase) >= 0) return new Color(0.65f, 0.42f, 0.18f, 1f);
            if (kind.IndexOf("tree", System.StringComparison.OrdinalIgnoreCase) >= 0) return new Color(0.18f, 0.78f, 0.26f, 1f);
            if (kind.IndexOf("bush", System.StringComparison.OrdinalIgnoreCase) >= 0) return new Color(0.26f, 0.85f, 0.38f, 1f);
            if (kind.IndexOf("grass", System.StringComparison.OrdinalIgnoreCase) >= 0 || kind.IndexOf("flower", System.StringComparison.OrdinalIgnoreCase) >= 0) return new Color(0.72f, 0.95f, 0.45f, 1f);
            if (kind.IndexOf("fiber", System.StringComparison.OrdinalIgnoreCase) >= 0) return new Color(0.88f, 0.8f, 0.2f, 1f);
            return new Color(0.95f, 0.7f, 0.2f, 1f);
        }

        private static Vector2 GetResourceSize(ResourceNodeView resource)
        {
            if (resource == null)
            {
                return new Vector2(7f, 7f);
            }

            string kind = (resource.name ?? string.Empty).ToLowerInvariant();
            if (kind.IndexOf("tree", System.StringComparison.OrdinalIgnoreCase) >= 0) return new Vector2(8f, 8f);
            if (kind.IndexOf("rock", System.StringComparison.OrdinalIgnoreCase) >= 0) return new Vector2(6f, 6f);
            return new Vector2(7f, 7f);
        }

        private static Color GetCreatureColor(CreatureAgentView creature)
        {
            if (creature == null)
            {
                return new Color(0.9f, 0.35f, 0.35f, 1f);
            }

            string id = (creature.CreatureId ?? string.Empty).Trim().ToLowerInvariant();
            if (id == "varnak") return new Color(0.95f, 0.24f, 0.24f, 1f);
            if (id == "grazer") return new Color(0.95f, 0.74f, 0.18f, 1f);
            if (id == "small_prey") return new Color(0.8f, 0.92f, 0.25f, 1f);
            return new Color(0.9f, 0.35f, 0.35f, 1f);
        }

        private static Color GetBuildingColor(PlaceableStructureRuntime structure)
        {
            string id = structure != null ? structure.BuildingId : string.Empty;
            switch (id)
            {
                case "campfire":
                    return new Color(1f, 0.42f, 0.10f, 1f);
                case "storage_box":
                    return new Color(0.55f, 0.34f, 0.16f, 1f);
                case "trap":
                    return new Color(0.72f, 0.18f, 0.12f, 1f);
                case "wall":
                    return new Color(0.62f, 0.54f, 0.42f, 1f);
                default:
                    return new Color(0.70f, 0.58f, 0.34f, 1f);
            }
        }

        private static Vector2 GetBuildingSize(PlaceableStructureRuntime structure)
        {
            if (structure == null)
            {
                return new Vector2(8f, 8f);
            }

            string id = structure.BuildingId;
            if (id == "campfire") return new Vector2(9f, 9f);
            if (id == "wall") return new Vector2(10f, 5f);
            return new Vector2(8f, 8f);
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
            if (landmark == null) return new Vector2(8f, 8f);
            switch (landmark.Type)
            {
                case LandmarkType.OldTree: return new Vector2(10f, 10f);
                case LandmarkType.Ruins: return new Vector2(9f, 9f);
                case LandmarkType.Pond: return new Vector2(9f, 9f);
                case LandmarkType.Camp: return new Vector2(8f, 8f);
                case LandmarkType.CavePlaceholder: return new Vector2(7f, 7f);
                default: return new Vector2(8f, 8f);
            }
        }

        private void EnsureMarkerCount(int count)
        {
            while (markerPool.Count < count)
            {
                GameObject markerGo = CreatePooledImage("Marker", dotLayer);
                markerGo.GetComponent<RectTransform>().sizeDelta = new Vector2(7f, 7f);
                markerPool.Add(markerGo);
            }
        }

        private GameObject GetOrCreateTerrainCell(int index)
        {
            while (terrainPool.Count <= index)
            {
                GameObject cellGo = CreatePooledImage("TerrainCell", terrainLayer);
                cellGo.GetComponent<RectTransform>().sizeDelta = new Vector2(4f, 4f);
                terrainPool.Add(cellGo);
            }

            return terrainPool[index];
        }

        private static GameObject CreatePooledImage(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            Image img = go.AddComponent<Image>();
            img.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            img.raycastTarget = false;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            return go;
        }

        private readonly struct MiniMapMarker
        {
            public readonly Vector3 Position;
            public readonly Color Color;
            public readonly Vector2 Size;

            public MiniMapMarker(Vector3 position, Color color, Vector2 size)
            {
                Position = position;
                Color = color;
                Size = size;
            }
        }
    }
}
