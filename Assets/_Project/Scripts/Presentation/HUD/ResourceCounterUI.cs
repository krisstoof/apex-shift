using UnityEngine;
using UnityEngine.UI;

namespace ApexShift.Presentation.HUD
{
    public sealed class ResourceCounterUI : MonoBehaviour
    {
        [SerializeField] private string itemId;
        [SerializeField] private Text countText;

        public void Configure(string id, Text textComp)
        {
            itemId = id;
            countText = textComp;
        }

        public string ItemId => itemId;

        public void UpdateCount(int count)
        {
            if (countText != null)
            {
                countText.text = count.ToString();
                Debug.Log($"[HUD] Updated {itemId} count to {count}", this);
            }
        }
}
}
