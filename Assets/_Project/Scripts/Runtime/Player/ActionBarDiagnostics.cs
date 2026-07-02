using UnityEngine;
using UnityEngine.InputSystem;

namespace ApexShift.Runtime.Player
{
    /// <summary>
    /// Debug script to diagnose action bar equipping issues.
    /// Attach to Player and check Console output during gameplay.
    /// </summary>
    public sealed class ActionBarDiagnostics : MonoBehaviour
    {
        [SerializeField] private bool logInputEvents = true;
        [SerializeField] private bool logHierarchyState = true;
        [SerializeField] private float hierarchyCheckInterval = 0.5f;
        [SerializeField] private bool autoAssignTestItem = true;
        [SerializeField] private string testItemId = "spear";
        [SerializeField] private int testSlotIndex = 0;

        private ActionBarRuntime actionBar;
        private PlayerHeldItemRuntime heldItem;
        private float nextHierarchyCheck;
        private int lastReportedSlot = -99;
        private bool testItemAssigned = false;

        private void OnEnable()
        {
            Debug.Log("[ActionBarDiagnostics] Initialized. Will monitor action bar equipping.", this);
            testItemAssigned = false;
        }

        private void Update()
        {
            ResolveReferences();

            // Auto-assign test item on first frame
            if (autoAssignTestItem && !testItemAssigned)
            {
                if (actionBar == null)
                {
                    Debug.LogError("[ActionBarDiagnostics] ✗ ActionBar is NULL! Cannot auto-assign.", this);
                    return;
                }

                testItemAssigned = true;
                Debug.Log($"[ActionBarDiagnostics] ✓ Auto-assigning test item '{testItemId}' to slot {testSlotIndex + 1}", this);
                actionBar.AssignItemToSlot(testSlotIndex, testItemId);
            }

            if (logInputEvents)
            {
                CheckKeyboardInput();
            }

            if (logHierarchyState && Time.time >= nextHierarchyCheck)
            {
                ReportHierarchyState();
                nextHierarchyCheck = Time.time + hierarchyCheckInterval;
            }
        }

        private void ResolveReferences()
        {
            if (actionBar == null)
            {
                actionBar = GetComponent<ActionBarRuntime>() ?? ActionBarRuntime.Active;
                if (actionBar == null)
                {
                    Debug.LogError("[ActionBarDiagnostics] ✗ Failed to find ActionBarRuntime (not on this GO, not Active singleton)", this);
                }
            }

            if (heldItem == null)
            {
                heldItem = GetComponent<PlayerHeldItemRuntime>();
                if (heldItem == null)
                {
                    Debug.LogWarning("[ActionBarDiagnostics] ℹ PlayerHeldItemRuntime not on this GameObject", this);
                }
            }
        }

        private void CheckKeyboardInput()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            for (int i = 0; i < 9; i++)
            {
                Key digitKey = (Key)((int)Key.Digit1 + i);
                Key keypadKey = (Key)((int)Key.Numpad1 + i);

                if (Keyboard.current[digitKey].wasPressedThisFrame)
                {
                    Debug.Log($"[ActionBarDiagnostics] ✓ KEY DETECTED: Digit{i + 1} pressed", this);
                }

                if (Keyboard.current[keypadKey].wasPressedThisFrame)
                {
                    Debug.Log($"[ActionBarDiagnostics] ✓ KEY DETECTED: Numpad{i + 1} pressed", this);
                }
            }
        }

        private void ReportHierarchyState()
        {
            if (actionBar == null)
            {
                Debug.LogWarning("[ActionBarDiagnostics] ✗ ActionBarRuntime is NULL! Action bar will not work.", this);
                return;
            }

            int currentSlot = actionBar.ActiveSlotIndex;
            string currentItem = actionBar.ActiveItemId;

            // Only log if slot changed
            if (currentSlot != lastReportedSlot)
            {
                lastReportedSlot = currentSlot;
                Debug.Log(
                    $"[ActionBarDiagnostics] Slot changed:\n" +
                    $"  ActiveSlotIndex: {currentSlot}\n" +
                    $"  ActiveItemId: '{currentItem}'",
                    this);
            }

            // Check HeldItem references
            if (heldItem == null)
            {
                Debug.LogWarning("[ActionBarDiagnostics] ✗ PlayerHeldItemRuntime is NULL! Cannot equip items.", this);
                return;
            }

            // Check hierarchy structure
            Transform fallbackAnchor = heldItem.transform.Find("HeldItemFallbackAnchor");
            if (fallbackAnchor == null)
            {
                Debug.LogWarning("[ActionBarDiagnostics] ✗ HeldItemFallbackAnchor not found in hierarchy.", this);
            }
            else
            {
                int childCount = fallbackAnchor.childCount;
                if (childCount == 0)
                {
                    Debug.Log($"[ActionBarDiagnostics] ℹ HeldItemFallbackAnchor exists but is empty (no held item visual).", this);
                }
                else
                {
                    foreach (Transform child in fallbackAnchor)
                    {
                        if (child != null)
                        {
                            Debug.Log($"[ActionBarDiagnostics] ℹ Found held visual: {child.name} (active: {child.gameObject.activeSelf})", this);
                        }
                    }
                }
            }
        }
    }
}
