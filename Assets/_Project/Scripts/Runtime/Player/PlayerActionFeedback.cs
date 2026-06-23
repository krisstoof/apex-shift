using System.Collections;
using ApexShift.Runtime.PlayerInput;
using UnityEngine;

namespace ApexShift.Runtime.Player
{
    public sealed class PlayerActionFeedback : MonoBehaviour
    {
        [SerializeField]
        private PlayerInputReader inputReader;

        [SerializeField]
        private Renderer visualRenderer;

        [SerializeField]
        private bool logToConsole = true;

        [SerializeField]
        private float flashDuration = 0.12f;

        private Color originalColor = Color.white;
        private Material runtimeMaterial;
        private Coroutine flashRoutine;

        private void Awake()
        {
            if (inputReader == null)
            {
                inputReader = GetComponent<PlayerInputReader>();
            }

            if (visualRenderer == null)
            {
                visualRenderer = GetComponentInChildren<Renderer>();
            }

            if (visualRenderer != null)
            {
                runtimeMaterial = visualRenderer.material;
                if (runtimeMaterial.HasProperty("_BaseColor"))
                {
                    originalColor = runtimeMaterial.GetColor("_BaseColor");
                }
                else if (runtimeMaterial.HasProperty("_Color"))
                {
                    originalColor = runtimeMaterial.color;
                }
            }
        }

        private void OnEnable()
        {
            if (inputReader == null)
            {
                return;
            }

            inputReader.InteractPressed += OnInteract;
            inputReader.AttackPressed += OnAttack;
            inputReader.OpenInventoryPressed += OnOpenInventory;
            inputReader.OpenCraftingPressed += OnOpenCrafting;
            inputReader.ToggleMapPressed += OnToggleMap;
            inputReader.PausePressed += OnPause;
        }

        private void OnDisable()
        {
            if (inputReader == null)
            {
                return;
            }

            inputReader.InteractPressed -= OnInteract;
            inputReader.AttackPressed -= OnAttack;
            inputReader.OpenInventoryPressed -= OnOpenInventory;
            inputReader.OpenCraftingPressed -= OnOpenCrafting;
            inputReader.ToggleMapPressed -= OnToggleMap;
            inputReader.PausePressed -= OnPause;
        }

        public void SetInputReader(PlayerInputReader reader)
        {
            inputReader = reader;
        }

        private void OnInteract()
        {
            TriggerFeedback("Interact", new Color(0.2f, 0.8f, 1f));
        }

        private void OnAttack()
        {
            TriggerFeedback("Attack", new Color(1f, 0.25f, 0.15f));
        }

        private void OnOpenInventory()
        {
            TriggerFeedback("Open Inventory", new Color(1f, 0.85f, 0.2f));
        }

        private void OnOpenCrafting()
        {
            TriggerFeedback("Open Crafting", new Color(0.8f, 0.4f, 1f));
        }

        private void OnToggleMap()
        {
            TriggerFeedback("Toggle Map", new Color(0.4f, 1f, 0.4f));
        }

        private void OnPause()
        {
            TriggerFeedback("Pause", Color.white);
        }

        private void TriggerFeedback(string actionName, Color flashColor)
        {
            if (logToConsole)
            {
                Debug.Log("[PlayerActionFeedback] " + actionName, this);
            }

            if (runtimeMaterial == null)
            {
                return;
            }

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(FlashRoutine(flashColor));
        }

        private IEnumerator FlashRoutine(Color flashColor)
        {
            SetMaterialColor(flashColor);
            yield return new WaitForSeconds(flashDuration);
            SetMaterialColor(originalColor);
            flashRoutine = null;
        }

        private void SetMaterialColor(Color color)
        {
            if (runtimeMaterial == null)
            {
                return;
            }

            if (runtimeMaterial.HasProperty("_BaseColor"))
            {
                runtimeMaterial.SetColor("_BaseColor", color);
            }
            else if (runtimeMaterial.HasProperty("_Color"))
            {
                runtimeMaterial.color = color;
            }
        }
    }
}
