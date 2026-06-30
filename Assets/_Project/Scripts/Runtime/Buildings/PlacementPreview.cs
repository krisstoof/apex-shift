using UnityEngine;

namespace ApexShift.Runtime.Buildings
{
    [DisallowMultipleComponent]
    public sealed class PlacementPreview : MonoBehaviour
    {
        [SerializeField] private Color validColor = new Color(0.12f, 1f, 0.32f, 0.72f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.18f, 0.12f, 0.82f);
        [SerializeField] private Color validEmission = new Color(0.05f, 0.9f, 0.2f, 1f);
        [SerializeField] private Color invalidEmission = new Color(0.9f, 0.08f, 0.04f, 1f);

        private GameObject previewCube;
        private Renderer previewRenderer;
        private Material previewMaterial;

        public void UpdatePreview(Vector3 position, Quaternion rotation, Vector3 footprint, bool isValid)
        {
            EnsurePreview();
            previewCube.SetActive(true);
            previewCube.transform.position = position + Vector3.up * 0.05f;
            previewCube.transform.rotation = rotation;
            previewCube.transform.localScale = new Vector3(
                Mathf.Max(0.25f, footprint.x),
                isValid ? 0.1f : 0.14f,
                Mathf.Max(0.25f, footprint.z));
            ApplyColor(isValid ? validColor : invalidColor, isValid ? validEmission : invalidEmission);
        }

        public void Hide()
        {
            if (previewCube != null)
            {
                previewCube.SetActive(false);
            }
        }

        private void EnsurePreview()
        {
            if (previewCube != null)
            {
                return;
            }

            previewCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            previewCube.name = "PlacementPreview";
            previewCube.transform.SetParent(transform, false);
            Collider collider = previewCube.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            previewRenderer = previewCube.GetComponent<Renderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            previewMaterial = new Material(shader);
            previewMaterial.renderQueue = 3000;
            previewRenderer.sharedMaterial = previewMaterial;
            ConfigureMaterialForTransparency();
        }

        private void ConfigureMaterialForTransparency()
        {
            if (previewMaterial == null)
            {
                return;
            }

            if (previewMaterial.HasProperty("_Surface"))
            {
                previewMaterial.SetFloat("_Surface", 1f);
            }

            if (previewMaterial.HasProperty("_Blend"))
            {
                previewMaterial.SetFloat("_Blend", 0f);
            }

            if (previewMaterial.HasProperty("_ZWrite"))
            {
                previewMaterial.SetFloat("_ZWrite", 0f);
            }

            previewMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            previewMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            previewMaterial.EnableKeyword("_EMISSION");
        }

        private void ApplyColor(Color color, Color emission)
        {
            if (previewMaterial == null)
            {
                return;
            }

            if (previewMaterial.HasProperty("_BaseColor"))
            {
                previewMaterial.SetColor("_BaseColor", color);
            }

            if (previewMaterial.HasProperty("_Color"))
            {
                previewMaterial.SetColor("_Color", color);
            }

            if (previewMaterial.HasProperty("_EmissionColor"))
            {
                previewMaterial.SetColor("_EmissionColor", emission);
            }
        }
    }
}
