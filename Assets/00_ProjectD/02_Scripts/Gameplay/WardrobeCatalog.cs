using System;
using UnityEngine;

namespace DoHwa
{
    [CreateAssetMenu(menuName = "DoHwa/옷장 카탈로그")]
    public sealed class WardrobeCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class Item
        {
            public string id;
            public string title;
            public string description;
            public Sprite icon;
            public DohwaEquipmentSet equipment;
            public AvatarSlot Slot => equipment.slot;
        }
        public Item[] items = Array.Empty<Item>();
        public string[] defaults = new string[5];
        public Item Find(string id) => Array.Find(items, item => item.id == id);
    }
}
