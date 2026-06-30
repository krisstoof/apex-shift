using System;
using ApexShift.Core.Save;
using ApexShift.Runtime.Interaction;
using ApexShift.Runtime.Player;
using UnityEngine;

namespace ApexShift.Runtime.Buildings
{
    [DisallowMultipleComponent]
    public sealed class PlaceableStructureRuntime : MonoBehaviour, IInteractable
    {
        [SerializeField] private string buildingId = "unknown";
        [SerializeField] private string instanceId;
        [SerializeField] private string prompt = "Use structure";
        [SerializeField] private Vector3 footprintSize = new Vector3(2f, 1.5f, 2f);
        [SerializeField] private bool blocksPlacement = true;
        [SerializeField] private float interactionDuration = 0.25f;

        public string BuildingId => Normalize(buildingId, "unknown");
        public string InstanceId => Normalize(instanceId, string.Empty);
        public Vector3 FootprintSize => new Vector3(Mathf.Max(0.25f, footprintSize.x), Mathf.Max(0.25f, footprintSize.y), Mathf.Max(0.25f, footprintSize.z));
        public bool BlocksPlacement => blocksPlacement;
        public string Prompt => string.IsNullOrWhiteSpace(prompt) ? $"Use {BuildingId}" : prompt;
        public int Priority => 25;
        public float InteractionDuration => Mathf.Max(0.05f, interactionDuration);

        private void OnEnable()
        {
            if (!string.IsNullOrWhiteSpace(buildingId))
            {
                BuildingRegistry.Active?.Register(this);
            }
        }

        private void OnDisable()
        {
            BuildingRegistry.Active?.Unregister(this);
        }

        public void Configure(string buildingId, string instanceId = null, Vector3? footprint = null, string prompt = null)
        {
            this.buildingId = Normalize(buildingId, "unknown");
            this.instanceId = string.IsNullOrWhiteSpace(instanceId) ? Guid.NewGuid().ToString("N") : instanceId.Trim();
            if (footprint.HasValue)
            {
                footprintSize = footprint.Value;
            }

            this.prompt = string.IsNullOrWhiteSpace(prompt) ? BuildDefaultPrompt(this.buildingId) : prompt.Trim();
            EnsureCollider();
            BuildingRegistry.Active?.Register(this);
        }

        public BuildingSaveData ToSaveData()
        {
            return new BuildingSaveData(InstanceId, BuildingId, transform.position.x, transform.position.y, transform.position.z, transform.eulerAngles.y, gameObject.activeSelf);
        }

        public bool CanInteract(GameObject actor)
        {
            return actor != null && isActiveAndEnabled;
        }

        public bool Interact(GameObject actor)
        {
            if (actor == null)
            {
                return false;
            }

            Debug.Log($"[Building] Interacted with {BuildingId} ({InstanceId}).", this);
            return true;
        }

        private void EnsureCollider()
        {
            Collider existing = GetComponentInChildren<Collider>();
            if (existing != null)
            {
                return;
            }

            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.size = FootprintSize;
            collider.center = Vector3.up * (collider.size.y * 0.5f);
        }

        private static string BuildDefaultPrompt(string buildingId)
        {
            switch (Normalize(buildingId, "unknown"))
            {
                case "storage_box": return "Open storage box";
                case "campfire": return "Use campfire";
                case "wall": return "Inspect wall";
                case "trap": return "Inspect trap";
                case "tent": return "Rest at tent";
                default: return "Use structure";
            }
        }

        private static string Normalize(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().ToLowerInvariant();
        }
    }
}
