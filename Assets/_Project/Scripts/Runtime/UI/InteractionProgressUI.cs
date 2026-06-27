using UnityEngine;
using UnityEngine.UI;

namespace ApexShift.Runtime.UI
{
    public sealed class InteractionProgressUI : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image progressCircle;
        [SerializeField] private Text progressText;
        [SerializeField] private Vector3 offset = new Vector3(0, 2.2f, 0);

        private Transform target;

        public void Show(Transform character, string prompt = "Gathering...")
        {
            target = character;
            if (canvas != null) canvas.enabled = true;
            if (progressText != null) progressText.text = prompt;
            UpdateProgress(0f);
            UpdatePosition();
        }

        public void Hide()
        {
            if (canvas != null) canvas.enabled = false;
            target = null;
        }

        public void UpdateProgress(float progress)
        {
            if (progressCircle != null)
            {
                progressCircle.fillAmount = Mathf.Clamp01(progress);
            }
        }

        private void Awake()
        {
            if (canvas == null) canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.enabled = false;
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 1000;
            }
        }

        private void LateUpdate()
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (target == null) return;
            transform.position = target.position + offset;
            
            if (UnityEngine.Camera.main != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - UnityEngine.Camera.main.transform.position);
            }
        }
        
        public static InteractionProgressUI Create(Transform parent)
        {
            GameObject go = new GameObject("InteractionProgressUI");
            go.transform.SetParent(parent, false);
            go.layer = LayerMask.NameToLayer("UI");
            
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(2, 2);
            
            // Generate circular sprite
            Sprite circleSprite = CreateCircleSprite();
            
            GameObject bgGo = new GameObject("Background");
            bgGo.transform.SetParent(go.transform, false);
            Image bg = bgGo.AddComponent<Image>();
            bg.sprite = circleSprite;
            bg.color = new Color(0, 0, 0, 0.6f);
            RectTransform bgRt = bg.GetComponent<RectTransform>();
            bgRt.sizeDelta = new Vector2(0.6f, 0.6f);
            
            GameObject fillGo = new GameObject("ProgressFill");
            fillGo.transform.SetParent(go.transform, false);
            Image fill = fillGo.AddComponent<Image>();
            fill.sprite = circleSprite;
            fill.color = new Color(0.2f, 1f, 0.3f, 0.9f);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillAmount = 0;
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.sizeDelta = new Vector2(0.55f, 0.55f);
            
            GameObject textGo = new GameObject("PromptText");
            textGo.transform.SetParent(go.transform, false);
            Text text = textGo.AddComponent<Text>();
            text.font = (Font)UnityEngine.Resources.GetBuiltinResource(typeof(Font), "LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = "Gathering...";
            Outline outline = textGo.AddComponent<Outline>();
            outline.effectColor = Color.black;
            
            RectTransform textRt = text.GetComponent<RectTransform>();
            textRt.sizeDelta = new Vector2(4, 1);
            textRt.anchoredPosition = new Vector2(0, 0.6f);
            textRt.localScale = Vector3.one * 0.015f; // Scale down for world space
            
            InteractionProgressUI ui = go.AddComponent<InteractionProgressUI>();
            ui.canvas = canvas;
            ui.progressCircle = fill;
            ui.progressText = text;
            
            return ui;
        }

        private static Sprite CreateCircleSprite()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] colors = new Color[size * size];
            float center = (size - 1) / 2f;
            float radius = size / 2f - 1f;
            float innerRadius = radius * 0.7f; // For a ring look if we wanted, but solid circle is fine
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist <= radius)
                    {
                        // Anti-aliasing
                        float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                        colors[y * size + x] = new Color(1, 1, 1, alpha);
                    }
                    else
                    {
                        colors[y * size + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
}
}
