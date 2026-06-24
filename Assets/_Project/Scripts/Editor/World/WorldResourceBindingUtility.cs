using UnityEditor;
using UnityEngine;
using ApexShift.Runtime.Resources;
using System.Collections.Generic;

namespace ApexShift.EditorTools.World
{
    public static class WorldResourceBindingUtility
    {
        [MenuItem("Tools/Apex Shift/World/Bind Existing World Objects As Resources")]
        public static void BindExistingWorldObjects()
        {
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude);
            int boundCount = 0;

            foreach (GameObject go in allObjects)
            {
                if (IsExcluded(go.name)) continue;

                string resourceKind = DetermineResourceKind(go.name);
                if (resourceKind == null) continue;

                float radius = DetermineRadius(resourceKind);
                
                ResourceNodeView view = go.GetComponent<ResourceNodeView>();
                if (view == null)
                {
                    view = go.AddComponent<ResourceNodeView>();
                }

                view.ConfigureDefault(resourceKind);

                SerializedObject so = new SerializedObject(view);
                so.FindProperty("interactionRadius").floatValue = radius;
                so.ApplyModifiedProperties();

                SphereCollider trigger = go.GetComponent<SphereCollider>();
                if (trigger == null)
                {
                    trigger = go.AddComponent<SphereCollider>();
                }

                trigger.isTrigger = true;
                trigger.radius = radius;

                if (resourceKind.Contains("tree"))
                {
                    Transform existingStump = go.transform.Find("Stump");
                    GameObject stump;
                    if (existingStump == null)
                    {
                        stump = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        stump.name = "Stump";
                        stump.transform.SetParent(go.transform, false);
                        stump.transform.localPosition = new Vector3(0, 0.25f, 0);
                        stump.transform.localScale = new Vector3(0.4f, 0.25f, 0.4f);
                        Object.DestroyImmediate(stump.GetComponent<Collider>());
                        
                        var originalRenderer = go.GetComponentInChildren<Renderer>();
                        if (originalRenderer != null)
                        {
                            stump.GetComponent<Renderer>().sharedMaterial = originalRenderer.sharedMaterial;
                        }
                        
                        ApplyTint(stump, new Color(0.35f, 0.2f, 0.1f));
                    }
                    else
                    {
                        stump = existingStump.gameObject;
                    }

                    stump.SetActive(false);
                    so.FindProperty("depletedVisual").objectReferenceValue = stump;
                    so.ApplyModifiedProperties();
                }
                
                boundCount++;
            }

            Debug.Log($"Successfully bound {boundCount} world objects as resources.");
        }

        private static void ApplyTint(GameObject target, Color color, string materialName = null)
        {
            if (target == null) return;
            Renderer r = target.GetComponent<Renderer>();
            if (r != null)
            {
                Material material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                if (material.shader == null)
                {
                    material = new Material(Shader.Find("Standard"));
                }
                
                material.name = string.IsNullOrEmpty(materialName) ? "Stump_Material" : materialName;
                
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", color);
                else if (material.HasProperty("_Color"))
                    material.SetColor("_Color", color);
                
                r.sharedMaterial = material;
            }
        }

        private static bool IsExcluded(string name)
        {
            string lowerName = name.ToLowerInvariant();
            return lowerName.Contains("boundaryrock") || lowerName.Contains("worldboundary");
        }

        private static string DetermineResourceKind(string name)
        {
            string lowerName = name.ToLowerInvariant();
            
            if (ContainsAny(lowerName, "conifertree", "tree_02.1", "pine", "spruce")) return "conifer_tree";
            if (ContainsAny(lowerName, "drytree", "tree_04.4", "deadtree")) return "dry_tree";
            if (ContainsAny(lowerName, "leafytree", "tree_04", "leaf", "oak")) return "leafy_tree";
            if (ContainsAny(lowerName, "rock", "stone", "boulder")) return "rock";
            if (ContainsAny(lowerName, "greenbush", "bush_02.1")) return "bush";
            if (ContainsAny(lowerName, "drybush", "bush_02.2")) return "dry_bush";
            
            return null;
        }

        private static bool ContainsAny(string text, params string[] keywords)
        {
            foreach (string keyword in keywords)
            {
                if (text.Contains(keyword.ToLowerInvariant())) return true;
            }
            return false;
        }

        private static float DetermineRadius(string kind)
        {
            return kind switch
            {
                "conifer_tree" => 2.4f,
                "leafy_tree" => 2.4f,
                "dry_tree" => 2.2f,
                "rock" => 1.8f,
                "bush" => 1.4f,
                "dry_bush" => 1.4f,
                _ => 1.5f
            };
        }
    }
}
