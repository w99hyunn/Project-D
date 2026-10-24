using System;
using UnityEngine;

namespace DoHwa.World
{
    [Serializable] public sealed class ItemDefinition
    {
        public string title;
        public string category;
        [TextArea] public string description;
        public Sprite icon;
        public DoHwa.UI.EquipmentPart equipmentPart = DoHwa.UI.EquipmentPart.None;
    }

    [CreateAssetMenu(menuName = "DoHwa/World/Item Catalog")]
    public sealed class ItemCatalog : ScriptableObject
    {
        // Array indices remain the persisted item IDs; do not reorder existing entries.
        public ItemDefinition[] items;
    }
}
