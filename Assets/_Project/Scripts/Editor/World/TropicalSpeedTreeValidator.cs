using System;
using System.Collections.Generic;
using System.Linq;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Resources;
using ApexShift.Runtime.World.Vegetation;
using UnityEditor;
using UnityEngine;

namespace ApexShift.Editor.World
{
    public static class TropicalSpeedTreeValidator
    {
        public const string ShaderName = "Universal Render Pipeline/Nature/SpeedTree9_URP";

        [MenuItem("Apex Shift/Validation/Validate Tropical SpeedTree Readiness")]
        public static void ValidateFromMenu()
        {
            var errors = CollectReadinessProblems(TropicalSpeedTreeManifest.Load());
            if (errors.Count == 0) Debug.Log("Complete tropical SpeedTree set is ready to bind.");
            else Debug.LogWarning("Tropical SpeedTree set is not ready:\n" + string.Join("\n", errors));
        }

        public static List<string> CollectReadinessProblems(TropicalSpeedTreeManifest manifest)
        {
            var errors = manifest.Validate();
            if (errors.Count != 0) return errors;
            foreach (var entry in manifest.assets)
            {
                ValidateModel(entry, errors);
                ValidateWrapper(entry, AssetDatabase.LoadAssetAtPath<GameObject>(entry.WrapperPath), errors);
                if (!entry.authoringReviewed || !entry.windReviewed)
                    errors.Add(entry.modelName + ": authoring/pivot/LOD and Games wind review has not been recorded.");
                if (string.IsNullOrWhiteSpace(entry.source) || string.IsNullOrWhiteSpace(entry.vendor)
                    || string.IsNullOrWhiteSpace(entry.license) || string.IsNullOrWhiteSpace(entry.attribution))
                    errors.Add(entry.modelName + ": source/vendor/license/attribution record is incomplete.");
                if (entry.IsTree && !entry.hero && (!FinitePositive(entry.trunkRadius) || !FinitePositive(entry.trunkHeight)
                    || entry.trunkHeight < entry.trunkRadius * 2 || !FinitePositive(entry.trunkCenterY)))
                    errors.Add(entry.modelName + ": measured trunk radius/height/center are required (zero is unmeasured).");
            }
            return errors;
        }

        public static void ValidateModel(TropicalSpeedTreeEntry entry, List<string> errors)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(entry.ModelPath);
            if (model == null) { errors.Add("Missing native SpeedTree export: " + entry.ModelPath); return; }
            if (!IsNativeSt9Importer(AssetImporter.GetAtPath(entry.ModelPath)))
                errors.Add("Model is not imported by the native SpeedTree importer: " + entry.ModelPath);
            ValidateGeometry(entry, model, errors);
        }

        public static void ValidateWrapper(TropicalSpeedTreeEntry entry, GameObject wrapper, List<string> errors)
        {
            if (wrapper == null) { errors.Add("Missing canonical wrapper: " + entry.WrapperPath); return; }
            string path = AssetDatabase.GetAssetPath(wrapper);
            if (path != entry.WrapperPath) errors.Add("Wrong canonical wrapper path: " + path);
            if (!AssetDatabase.GetDependencies(path, true).Contains(entry.ModelPath)
                || !IsNativeSt9Importer(AssetImporter.GetAtPath(entry.ModelPath)))
                errors.Add(entry.modelName + ": wrapper must depend on its authentic native .st9 export, not a renamed FBX.");
            if (!Identity(wrapper.transform)) errors.Add(entry.modelName + ": wrapper root must have identity transform.");
            var model = wrapper.transform.Find("SpeedTreeModel");
            if (model == null || !Identity(model)
                || PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model.gameObject) != entry.ModelPath)
                errors.Add(entry.modelName + ": missing identity native nested SpeedTreeModel child.");
            ValidateGeometry(entry, wrapper, errors);
            foreach (var component in wrapper.GetComponentsInChildren<MonoBehaviour>(true))
                if (component != null && component.GetType().Name == "HarvestableTreeRuntime")
                    errors.Add(entry.modelName + ": HarvestableTreeRuntime must be added by the promoter, not baked in a wrapper.");
            if (string.IsNullOrEmpty(entry.gameplayTemplate))
            {
                if (wrapper.GetComponentsInChildren<ResourceNodeView>(true).Length != 0
                    || wrapper.GetComponentsInChildren<FoodSourceView>(true).Length != 0)
                    errors.Add(entry.modelName + ": unexpected baked resource/food components.");
                foreach (var collider in wrapper.GetComponentsInChildren<Collider>(true))
                    if (!(entry.IsTree && collider is CapsuleCollider && collider.GetComponent<VegetationTrunkCollider>() != null))
                        errors.Add(entry.modelName + ": unexpected blocking/visual collider " + collider.name);
                    else
                    {
                        var capsule = (CapsuleCollider)collider;
                        if (capsule.isTrigger || !capsule.enabled || capsule.direction != 1
                            || (capsule.transform.lossyScale - Vector3.one).sqrMagnitude > .000001f
                            || Mathf.Abs(capsule.radius - entry.trunkRadius) > .001f
                            || Mathf.Abs(capsule.height - entry.trunkHeight) > .001f
                            || Mathf.Abs(capsule.transform.TransformPoint(capsule.center).y - entry.trunkCenterY) > .001f)
                            errors.Add(entry.modelName + ": marked trunk collider disagrees with measured dimensions.");
                    }
            }
            else ValidateGameplayContract(wrapper, entry.gameplayTemplate, errors);
        }

        // ST9 uses a distinct scripted importer, rather than the legacy SpeedTreeImporter.
        // Resolve the editor-owned type so a custom importer with the same short name cannot pass.
        public static bool IsNativeSt9Importer(AssetImporter importer)
        {
            var nativeType = typeof(AssetImporter).Assembly.GetType("UnityEditor.SpeedTree.Importer.SpeedTree9Importer");
            return importer != null && nativeType != null && nativeType.IsInstanceOfType(importer);
        }

        // Preserve actual serialized resource/food values, including private fields for which no public accessor exists.
        // Sphere dimensions are converted from the legacy scaled root to meters; no legacy mesh is copied.
        public static void ValidateGameplayContract(GameObject wrapper, string templateName, List<string> errors)
        {
            var template = AssetDatabase.LoadAssetAtPath<GameObject>(TropicalSpeedTreePrefabBuilder.GameplayRoot + templateName);
            if (template == null) { errors.Add("Missing gameplay contract template: " + templateName); return; }
            if (wrapper.layer != template.layer || wrapper.tag != template.tag)
                errors.Add(wrapper.name + ": changed gameplay layer/tag.");
            foreach (var type in new[] { typeof(ResourceNodeView), typeof(FoodSourceView) })
            {
                var expected = template.GetComponent(type);
                var actual = wrapper.GetComponent(type);
                if (expected == null || actual == null || wrapper.GetComponentsInChildren(type, true).Length != 1
                    || (actual is Behaviour behaviour && !behaviour.enabled))
                { errors.Add(wrapper.name + ": missing/duplicate root " + type.Name); continue; }
                var source = new SerializedObject(expected);
                var target = new SerializedObject(actual);
                var field = source.GetIterator();
                while (field.NextVisible(true))
                {
                    if (field.propertyPath.StartsWith("m_", StringComparison.Ordinal)) continue;
                    var other = target.FindProperty(field.propertyPath);
                    if (type == typeof(ResourceNodeView) && field.propertyPath == "interactionRadius")
                    {
                        if (other == null || Mathf.Abs(other.floatValue - field.floatValue * MaxScale(template.transform.localScale)) > .001f)
                            errors.Add(wrapper.name + ": interactionRadius must preserve the scaled template's world footprint.");
                        continue;
                    }
                    if (other == null || !SerializedProperty.DataEquals(field, other))
                        errors.Add(wrapper.name + ": changed gameplay contract field " + field.propertyPath);
                }
            }
            var expectedTrigger = template.GetComponent<SphereCollider>();
            var actualTrigger = wrapper.GetComponent<SphereCollider>();
            if (expectedTrigger == null || actualTrigger == null || !actualTrigger.isTrigger
                || !actualTrigger.enabled || wrapper.GetComponentsInChildren<Collider>(true).Length != 1
                || Vector3.Distance(actualTrigger.center, Vector3.Scale(expectedTrigger.center, template.transform.localScale)) > .001f
                || Mathf.Abs(actualTrigger.radius - expectedTrigger.radius * MaxScale(template.transform.localScale)) > .001f)
                errors.Add(wrapper.name + ": changed food/interaction trigger or unexpected player-blocking collider.");
        }

        public static void ValidateGeometry(TropicalSpeedTreeEntry entry, GameObject prefab, List<string> errors)
        {
            foreach (var child in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
                    errors.Add(entry.modelName + ": missing script.");
                var filter = child.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh == null) errors.Add(entry.modelName + ": missing mesh.");
                var skin = child.GetComponent<SkinnedMeshRenderer>();
                if (skin != null && skin.sharedMesh == null) errors.Add(entry.modelName + ": missing skinned mesh.");
            }
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) { errors.Add(entry.modelName + ": no renderers."); return; }
            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials.Length == 0) errors.Add(entry.modelName + ": no materials.");
                foreach (var material in renderer.sharedMaterials)
                {
                    if (material == null || material.shader == null || material.shader.name != ShaderName)
                    { errors.Add(entry.modelName + ": missing/non-native URP SpeedTree9 material."); continue; }
                    var serialized = new SerializedObject(material);
                    var textures = serialized.FindProperty("m_SavedProperties.m_TexEnvs");
                    for (int i = 0; textures != null && i < textures.arraySize; i++)
                    {
                        var texture = textures.GetArrayElementAtIndex(i).FindPropertyRelative("second.m_Texture");
                        if (texture != null && texture.objectReferenceValue == null && !texture.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId)))
                            errors.Add(entry.modelName + ": missing texture reference.");
                    }
                    // A native foliage material without its color atlas must not pass merely because all optional textures are null.
                    if (!material.HasProperty("_MainTex") || material.GetTexture("_MainTex") == null)
                        errors.Add(entry.modelName + ": missing SpeedTree color atlas (_MainTex).");
                }
            }
            var lod = prefab.GetComponentInChildren<LODGroup>(true);
            if (entry.IsTree && (lod == null || lod.lodCount < 3)) errors.Add(entry.modelName + ": trees require native LOD0/1/2.");
            if (lod != null)
                foreach (var level in lod.GetLODs())
                    if (level.renderers.Length == 0 || level.renderers.Any(r => r == null)) errors.Add(entry.modelName + ": empty/missing LOD renderer.");
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            if (bounds.size.y < entry.minHeight || bounds.size.y > entry.maxHeight)
                errors.Add(entry.modelName + ": height outside meter-scale authoring range.");
            if (Mathf.Abs(bounds.min.y) > Mathf.Max(.05f, bounds.size.y * .02f))
                errors.Add(entry.modelName + ": ground-base pivot is offset from y=0.");
        }

        internal static bool Identity(Transform transform) => transform.localPosition.sqrMagnitude < .000001f
            && Quaternion.Angle(transform.localRotation, Quaternion.identity) < .001f
            && (transform.localScale - Vector3.one).sqrMagnitude < .000001f;
        internal static float MaxScale(Vector3 scale) => Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        private static bool FinitePositive(float value) => value > 0 && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
