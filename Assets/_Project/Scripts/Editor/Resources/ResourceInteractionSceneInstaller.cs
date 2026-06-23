using System.IO;
using ApexShift.Runtime.Interaction;
using ApexShift.Runtime.Player;
using ApexShift.Runtime.PlayerInput;
using ApexShift.Runtime.Resources;
using ApexShift.Presentation.Interaction;
using ApexShift.Presentation.HUD;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ApexShift.EditorTools.Resources
{
    public static class ResourceInteractionSceneInstaller
    {
        [MenuItem("Tools/Apex Shift/Install Resource Interaction In Open Scene")]
        public static void InstallInOpenScene()
        {
            GameObject player = FindPlayer();
            if (player == null)
            {
                Debug.LogError("Could not find Player in the open scene. Create or select a Player object first.");
                return;
            }

            PlayerInputReader inputReader = player.GetComponent<PlayerInputReader>();
            if (inputReader == null)
            {
                inputReader = player.AddComponent<PlayerInputReader>();
                Debug.LogWarning("Added PlayerInputReader, but you may still need to assign the InputActionAsset.");
            }

            PlayerInventoryRuntime inventory = player.GetComponent<PlayerInventoryRuntime>();
            if (inventory == null)
            {
                inventory = player.AddComponent<PlayerInventoryRuntime>();
            }

            PlayerInteractionController interaction = player.GetComponent<PlayerInteractionController>();
            if (interaction == null)
            {
                interaction = player.AddComponent<PlayerInteractionController>();
            }

            interaction.SetInputReader(inputReader);
            interaction.SetInteractionOrigin(player.transform);

            PlayerInteractionOverlay overlay = player.GetComponent<PlayerInteractionOverlay>();
            if (overlay == null)
            {
                overlay = player.AddComponent<PlayerInteractionOverlay>();
            }

            PlayerSurvivalOverlay survivalOverlay = player.GetComponent<PlayerSurvivalOverlay>();
            if (survivalOverlay == null)
            {
                survivalOverlay = player.AddComponent<PlayerSurvivalOverlay>();
            }

            GameObject resourceRoot = GameObject.Find("ResourceRoot");
            if (resourceRoot == null)
            {
                resourceRoot = new GameObject("ResourceRoot");
            }

            if (Object.FindAnyObjectByType<ResourceNodeView>() == null)
            {
                CreateSampleResource(resourceRoot.transform, "Tree_Resource_Test", "conifer_tree", PrimitiveType.Cylinder, player.transform.position + new Vector3(3f, 0f, 0f), new Vector3(0.85f, 2.4f, 0.85f), new Color(0.08f, 0.36f, 0.16f, 1f));
                CreateSampleResource(resourceRoot.transform, "Rock_Resource_Test", "rock", PrimitiveType.Cube, player.transform.position + new Vector3(-3f, 0f, 0f), new Vector3(1.4f, 0.8f, 1.1f), new Color(0.45f, 0.45f, 0.5f, 1f));
                CreateSampleResource(resourceRoot.transform, "Bush_Resource_Test", "bush", PrimitiveType.Sphere, player.transform.position + new Vector3(0f, 0f, 3f), new Vector3(1.3f, 0.85f, 1.3f), new Color(0.45f, 0.9f, 0.28f, 1f));
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("Resource interaction installed. Press Play, walk near a sample resource, then press Interact.");
        }

        private static GameObject FindPlayer()
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer != null)
            {
                return taggedPlayer;
            }

            IsometricPlayerController controller = Object.FindObjectOfType<IsometricPlayerController>();
            if (controller != null)
            {
                return controller.gameObject;
            }

            return GameObject.Find("Player");
        }

        private static void CreateSampleResource(Transform parent, string name, string kind, PrimitiveType primitive, Vector3 position, Vector3 scale, Color color)
        {
            GameObject resource = GameObject.CreatePrimitive(primitive);
            resource.name = name;
            resource.transform.SetParent(parent, true);
            resource.transform.position = position;
            resource.transform.localScale = scale;

            Collider collider = resource.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            SphereCollider trigger = resource.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.6f;

            ResourceNodeView nodeView = resource.AddComponent<ResourceNodeView>();
            nodeView.ConfigureDefault(kind);

            MeshRenderer renderer = resource.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = LoadOrCreateMaterial(name + "_Material", color);
            }
        }

        private static Material LoadOrCreateMaterial(string name, Color color)
        {
            const string folder = "Assets/_Project/Materials/Resources";
            EnsureFolder("Assets/_Project");
            EnsureFolder("Assets/_Project/Materials");
            EnsureFolder(folder);

            string path = folder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                name = name,
                color = color
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
