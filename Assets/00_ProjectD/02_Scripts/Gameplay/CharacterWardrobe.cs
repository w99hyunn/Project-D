using System;
using UnityEngine;

namespace DoHwa
{
    public sealed class CharacterWardrobe : MonoBehaviour
    {
        [Serializable] private sealed class SavedOutfit { public string[] items; }
        [SerializeField] private DohwaLayeredCharacter character;
        [SerializeField] private WardrobeCatalog catalog;
        private string[] equipped;
        private string saveKey;
        public WardrobeCatalog Catalog => catalog;
        public DohwaLayeredCharacter Character => character;
        public event Action Changed;

        private void Start() => Initialize();

        public void Initialize()
        {
            if (equipped != null && equipped.Length == 5) return;
            saveKey = "DoHwa.Wardrobe." + World.PlayerIdentity.Get();
            equipped = (string[])catalog.defaults.Clone();
            if (PlayerPrefs.HasKey(saveKey))
            {
                SavedOutfit saved;
                try { saved = JsonUtility.FromJson<SavedOutfit>(PlayerPrefs.GetString(saveKey)); }
                catch (ArgumentException) { saved = new SavedOutfit { items = (string[])catalog.defaults.Clone() }; }
                if (saved != null && saved.items != null && saved.items.Length == 5)
                    for (int slot = 0; slot < 5; slot++)
                    {
                        string id = saved.items[slot];
                        var item = catalog.Find(id);
                        if (string.IsNullOrEmpty(id) || item != null && (int)item.Slot == slot) equipped[slot] = id;
                    }
            }
            Render(character, equipped);
        }

        public string[] Snapshot() { Initialize(); return (string[])equipped.Clone(); }

        public void Render(DohwaLayeredCharacter target, string[] outfit)
        {
            for (int slot = 0; slot < 5; slot++)
            {
                if (string.IsNullOrEmpty(outfit[slot])) target.SetEquipped((AvatarSlot)slot, false);
                else target.SetEquipment(catalog.Find(outfit[slot]).equipment);
            }
        }

        public void Apply(string[] outfit)
        {
            Initialize();
            if (outfit.Length != 5) throw new ArgumentException("코디는 다섯 부위가 필요합니다.", nameof(outfit));
            for (int slot = 0; slot < 5; slot++)
                if (!string.IsNullOrEmpty(outfit[slot]))
                {
                    var item = catalog.Find(outfit[slot]);
                    if (item == null || (int)item.Slot != slot) throw new ArgumentException("코디 부위가 일치하지 않습니다.", nameof(outfit));
                }
            equipped = (string[])outfit.Clone();
            Render(character, equipped);
            PlayerPrefs.SetString(saveKey, JsonUtility.ToJson(new SavedOutfit { items = equipped }));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}

