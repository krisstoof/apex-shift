using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ApexShift.Runtime.Ecosystem;
using ApexShift.Runtime.Resources;

namespace ApexShift.Editor.World
{
    public static class TropicalSpeedTreePrefabBuilder
    {
        public const string GameplayRoot = "Assets/_Project/Prefabs/World/Resources/Embersstorm/";

        [MenuItem("Apex Shift/World/Build Tropical SpeedTree Wrappers")]
        public static void BuildAll()
        {
            var manifest = TropicalSpeedTreeManifest.Load();
            var problems = new List<string>();
            // Missing models abort before writing any wrappers. This is not a Modeler/export replacement.
            foreach (var entry in manifest.assets) TropicalSpeedTreeValidator.ValidateModel(entry, problems);
            if (problems.Count != 0) throw new InvalidOperationException(string.Join("\n", problems));
            foreach (var entry in manifest.assets) Build(entry);
        }

        public static GameObject Build(TropicalSpeedTreeEntry entry)
        {
            var errors = new List<string>();
            TropicalSpeedTreeValidator.ValidateModel(entry, errors);
            if (errors.Count != 0) throw new InvalidOperationException(string.Join("\n", errors));
            bool existing = AssetDatabase.LoadAssetAtPath<GameObject>(entry.WrapperPath) != null;
            GameObject root = existing ? PrefabUtility.LoadPrefabContents(entry.WrapperPath) : new GameObject(entry.modelName);
            try
            {
                if (!TropicalSpeedTreeValidator.Identity(root.transform))
                    throw new InvalidOperationException("Fix wrapper root transform before rebuilding: " + entry.WrapperPath);
                Transform model = root.transform.Find("SpeedTreeModel");
                if (model != null && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(model.gameObject) != entry.ModelPath)
                    throw new InvalidOperationException("Unexpected model child; resolve manually before rebuilding: " + entry.WrapperPath);
                if (model == null)
                {
                    var native = AssetDatabase.LoadAssetAtPath<GameObject>(entry.ModelPath);
                    var child = (GameObject)PrefabUtility.InstantiatePrefab(native, root.transform);
                    child.name = "SpeedTreeModel";
                    child.transform.localPosition = Vector3.zero;
                    child.transform.localRotation = Quaternion.identity;
                    child.transform.localScale = Vector3.one;
                }
                // Reimports update the nested native prefab in place. Existing manual gameplay settings and GUID survive.
                if (!existing && !string.IsNullOrEmpty(entry.gameplayTemplate))
                    CopyGameplayContract(root, entry.gameplayTemplate);
                // Validate before saving; canonical path/dependency checks run after the prefab exists.
                TropicalSpeedTreeValidator.ValidateGeometry(entry, root, errors);
                if (!string.IsNullOrEmpty(entry.gameplayTemplate))
                    TropicalSpeedTreeValidator.ValidateGameplayContract(root, entry.gameplayTemplate, errors);
                if (errors.Count != 0) throw new InvalidOperationException(string.Join("\n", errors));
                var result = PrefabUtility.SaveAsPrefabAsset(root, entry.WrapperPath);
                if (result == null) throw new InvalidOperationException("Failed to save wrapper: " + entry.WrapperPath);
                return result;
            }
            finally
            {
                if (existing) PrefabUtility.UnloadPrefabContents(root);
                else UnityEngine.Object.DestroyImmediate(root);
            }
        }

        public static void CopyGameplayContract(GameObject root, string templateName)
        {
            var template = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayRoot + templateName);
            if (template == null) throw new InvalidOperationException("Missing gameplay template: " + GameplayRoot + templateName);
            root.layer = template.layer;
            root.tag = template.tag;
            foreach (var type in new[] { typeof(ResourceNodeView), typeof(FoodSourceView) })
            {
                var source = template.GetComponent(type);
                if (source == null) throw new InvalidOperationException("Missing template component: " + type.Name);
                if (root.GetComponent(type) != null) throw new InvalidOperationException("Refusing to overwrite manual component: " + type.Name);
                EditorUtility.CopySerialized(source, root.AddComponent(type));
            }
            // Awake re-applies interactionRadius to the sphere. Convert that setting too,
            // otherwise the identity wrapper would increase the real berry/grass trigger footprint.
            var nodeSettings = new SerializedObject(root.GetComponent<ResourceNodeView>());
            nodeSettings.FindProperty("interactionRadius").floatValue *= TropicalSpeedTreeValidator.MaxScale(template.transform.localScale);
            nodeSettings.ApplyModifiedPropertiesWithoutUndo();
            var trigger = template.GetComponent<SphereCollider>();
            if (trigger == null || !trigger.isTrigger) throw new InvalidOperationException("Missing template interaction trigger.");
            // ResourceNodeView.Reset may have created its root trigger when AddComponent was called.
            var target = root.GetComponent<SphereCollider>() ?? root.AddComponent<SphereCollider>();
            EditorUtility.CopySerialized(trigger, target);
            target.center = Vector3.Scale(trigger.center, template.transform.localScale);
            target.radius = trigger.radius * TropicalSpeedTreeValidator.MaxScale(template.transform.localScale);
        }
    }
}
