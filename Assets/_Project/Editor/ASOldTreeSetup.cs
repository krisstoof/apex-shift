using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ASOldTreeSetup
{
    private const string Folder = "Assets/_Project/Environment/Trees";
    private const string PrefabPath = Folder + "/AS_OldTree.prefab";
    private const string TestScene = "Assets/_Project/Scenes/BiomeWorldTest.unity";

    [MenuItem("Apex Shift/Setup AS OldTree")]
    public static void Setup()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "_Project/Environment/Trees"));

        var lodAssets = new[]
        {
            AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/AS_OldTree_LOD0.fbx"),
            AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/AS_OldTree_LOD1.fbx"),
            AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/AS_OldTree_LOD2.fbx")
        };

        if (lodAssets.Any(x => x == null))
            throw new InvalidOperationException("One or more AS_OldTree LOD FBX assets failed to import.");

        var root = new GameObject("AS_OldTree");
        var lodRenderers = new Renderer[3][];
        for (var i = 0; i < lodAssets.Length; i++)
        {
            var child = PrefabUtility.InstantiatePrefab(lodAssets[i]) as GameObject;
            child.name = "AS_OldTree_LOD" + i;
            child.transform.SetParent(root.transform, false);
            lodRenderers[i] = child.GetComponentsInChildren<Renderer>(true);
        }

        var bounds = new Bounds(Vector3.zero, Vector3.zero);
        var hasBounds = false;
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
            else bounds.Encapsulate(renderer.bounds);
        }

        var lodGroup = root.AddComponent<LODGroup>();
        lodGroup.SetLODs(new[]
        {
            new LOD(0.60f, lodRenderers[0]),
            new LOD(0.30f, lodRenderers[1]),
            new LOD(0.08f, lodRenderers[2])
        });
        lodGroup.RecalculateBounds();

        var collider = root.AddComponent<CapsuleCollider>();
        collider.direction = 1;
        collider.center = root.transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.center.y, bounds.center.z));
        collider.height = Mathf.Max(0.1f, bounds.size.y);
        collider.radius = Mathf.Max(0.05f, Mathf.Min(bounds.size.x, bounds.size.z) * 0.18f);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        if (prefab == null) throw new InvalidOperationException("Prefab creation failed.");

        var scene = EditorSceneManager.OpenScene(TestScene, OpenSceneMode.Single);
        var oldInstance = GameObject.Find("AS_OldTree_TestInstance");
        if (oldInstance != null) UnityEngine.Object.DestroyImmediate(oldInstance);
        var instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        instance.name = "AS_OldTree_TestInstance";
        instance.transform.position = Vector3.zero;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        var validation = Validate(prefab, instance);
        var reportPath = Path.Combine(Application.dataPath, "_Project/Environment/Trees/AS_OldTree_validation.txt");
        File.WriteAllText(reportPath, validation);
        AssetDatabase.Refresh();
        if (validation.Contains("FAIL")) throw new InvalidOperationException(validation);
        Debug.Log(validation);
    }

    private static string Validate(GameObject prefab, GameObject instance)
    {
        var group = prefab.GetComponent<LODGroup>();
        var collider = prefab.GetComponent<CapsuleCollider>();
        var lods = group == null ? Array.Empty<LOD>() : group.GetLODs();
        var ok = prefab != null && instance != null && group != null && collider != null && lods.Length == 3 && lods.All(l => l.renderers != null && l.renderers.Length > 0);
        return string.Join("\n", new[]
        {
            ok ? "PASS: AS_OldTree validation" : "FAIL: AS_OldTree validation",
            "Prefab: " + PrefabPath,
            "Scene: " + TestScene,
            "LODGroup: " + (group != null) + " (levels=" + lods.Length + ")",
            "CapsuleCollider: " + (collider != null),
            "Scene instance: " + (instance != null)
        });
    }
}
