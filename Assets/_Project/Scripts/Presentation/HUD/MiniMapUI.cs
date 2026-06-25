using System.Collections.Generic;
using System.Linq;
using ApexShift.Runtime.Creatures;
using ApexShift.Runtime.Resources;
using UnityEngine;
using UnityEngine.UI;

namespace ApexShift.Presentation.HUD
{
    [DisallowMultipleComponent]
    public sealed class MiniMapUI : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private float worldRadius = 120f;
        [SerializeField] private float refreshInterval = 0.25f;

        private RectTransform root;
        private RectTransform dotLayer;
        private RectTransform playerMarker;
        private RectTransform playerHeading;
        private readonly List<GameObject> markerPool = new List<GameObject>();
        private float refreshTimer;

        public void Configure(Transform playerTransform, float radius)
        {
            player = playerTransform;
            worldRadius = Mathf.Max(10f, radius);
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
                return;
            }

            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer > 0f)
            {
                return;
            }

            refreshTimer = Mathf.Max(0.05f, refreshInterval);
            RefreshMarkers();
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

        private void RefreshMarkers()
        {
            if (dotLayer == null)
            {
                return;
            }

            List<Vector3> positions = new List<Vector3>();
            List<Color> colors = new List<Color>();
            List<Vector2> markerSizes = new List<Vector2>();

            ResourceNodeView[] resources = Object.FindObjectsByType<ResourceNodeView>(FindObjectsInactive.Exclude);
            foreach (ResourceNodeView resource in resources.Where(r => r != null).Take(18))
            {
                positions.Add(resource.transform.position);
                colors.Add(GetResourceColor(resource));
                markerSizes.Add(GetResourceSize(resource));
            }

            CreatureAgentView[] creatures = Object.FindObjectsByType<CreatureAgentView>(FindObjectsInactive.Exclude);
            foreach (CreatureAgentView creature in creatures.Where(c => c != null).Take(18))
            {
                positions.Add(creature.transform.position);
                colors.Add(GetCreatureColor(creature));
                markerSizes.Add(new Vector2(7f, 7f));
            }

            EnsureMarkerCount(positions.Count);

            Vector3 center = player.position;
            if (playerMarker != null)
            {
                playerMarker.anchoredPosition = Vector2.zero;
                playerMarker.localRotation = Quaternion.identity;
            }

            if (playerHeading != null)
            {
                playerHeading.localRotation = Quaternion.Euler(0f, 0f, -player.eulerAngles.y);
            }

            int visibleIndex = 0;
            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 delta = positions[i] - center;
                Vector2 normalized = new Vector2(delta.x, delta.z) / worldRadius;
                normalized = Vector2.ClampMagnitude(normalized, 1f);

                GameObject markerGo = markerPool[visibleIndex++];
                markerGo.SetActive(true);
                RectTransform rt = markerGo.GetComponent<RectTransform>();
                if (i < markerSizes.Count)
                {
                    rt.sizeDelta = markerSizes[i];
                }
                rt.anchoredPosition = new Vector2(normalized.x * 0.5f * dotLayer.rect.width, normalized.y * 0.5f * dotLayer.rect.height);
                Image img = markerGo.GetComponent<Image>();
                if (img != null)
                {
                    img.color = colors[i];
                }
            }

            for (int i = visibleIndex; i < markerPool.Count; i++)
            {
                markerPool[i].SetActive(false);
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

        private void EnsureMarkerCount(int count)
        {
            while (markerPool.Count < count)
            {
                GameObject markerGo = new GameObject("Marker");
                markerGo.transform.SetParent(dotLayer, false);
                Image img = markerGo.AddComponent<Image>();
                img.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                RectTransform rt = markerGo.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(7f, 7f);
                markerPool.Add(markerGo);
            }
        }
    }
}
