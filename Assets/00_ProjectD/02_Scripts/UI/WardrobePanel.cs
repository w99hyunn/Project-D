using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DoHwa.UI
{
    public sealed class WardrobePanel : MonoBehaviour
    {
        [SerializeField] private CharacterWardrobe wardrobe;
        [SerializeField] private RpgUiController owner;
        [SerializeField] private UnityEngine.UI.RawImage preview;
        [SerializeField] private UnityEngine.UI.Button[] tabs;
        [SerializeField] private Transform content;
        [SerializeField] private WardrobeItemView itemTemplate;
        [SerializeField] private TMP_Text detail;
        [SerializeField] private TMP_Text itemTitle;
        [SerializeField] private UnityEngine.UI.Image itemArt;
        [SerializeField] private TMP_Text status;
        [SerializeField] private TMP_Text directionLabel;
        private readonly List<WardrobeItemView> cards = new();
        private readonly List<Material> previewMaterials = new();
        private DohwaLayeredCharacter previewCharacter;
        private Camera previewCamera;
        private RenderTexture previewTexture;
        private string[] draft;
        private int slot;
        private int direction;
        private bool walking;
        private static readonly string[] SlotNames = { "머리", "상의", "하의", "신발", "장식" };
        private static readonly string[] Directions = { "정면", "오른쪽 앞", "오른쪽", "오른쪽 뒤", "뒷면", "왼쪽 뒤", "왼쪽", "왼쪽 앞" };

        private void Awake()
        {
            for (int i = 0; i < tabs.Length; i++) { int index = i; tabs[i].onClick.AddListener(() => SelectSlot(index)); }
        }

        private void OnEnable()
        {
            wardrobe.Initialize();
            draft = wardrobe.Snapshot();
            if (!previewCharacter) CreatePreview();
            direction = 0;
            directionLabel.text = Directions[0];
            walking = false;
            wardrobe.Render(previewCharacter, draft);
            SelectSlot(0);
        }

        private void CreatePreview()
        {
            previewCharacter = Instantiate(wardrobe.Character, new Vector3(10000,-10000,10000), Quaternion.identity);
            previewCharacter.name = "Wardrobe Preview";
            var renderers = previewCharacter.GetComponentsInChildren<SpriteRenderer>(true);
            var previewBounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                previewBounds.Encapsulate(renderer.bounds);
                renderer.gameObject.layer = 31;
                var material = new Material(renderer.sharedMaterial);
                material.SetFloat("_UprightDepth", 0);
                material.SetFloat("_EnvironmentStrength", 0);
                material.SetFloat("_ShadowInfluence", 0);
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                previewMaterials.Add(material);
            }
            previewTexture = new RenderTexture(512,512,24) { name = "Wardrobe preview", filterMode = FilterMode.Point };
            previewTexture.Create();
            var cameraObject = new GameObject("Wardrobe Preview Camera", typeof(Camera));
            cameraObject.TryGetComponent<Camera>(out previewCamera);
            previewCamera.enabled = false;
            previewCamera.orthographic = true;
            previewCamera.orthographicSize = Mathf.Max(previewBounds.extents.x, previewBounds.extents.y) * 1.1f;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = Color.clear;
            previewCamera.cullingMask = 1 << 31;
            previewCamera.nearClipPlane = .01f;
            previewCamera.farClipPlane = 8;
            previewCamera.transform.position = previewBounds.center + new Vector3(0,0,-3);
            previewCamera.targetTexture = previewTexture;
            preview.texture = previewTexture;
        }

        private void LateUpdate()
        {
            previewCharacter.Animate((AvatarFacing)direction, walking ? AvatarMotion.Walk : AvatarMotion.Idle, Time.unscaledDeltaTime, 1);
            previewCamera.Render();
        }

        public void SelectSlot(int value)
        {
            slot = value;
            RefreshCards();
        }

        private void RefreshCards()
        {
            foreach (var card in cards) Destroy(card.gameObject);
            cards.Clear();
            for (int i = 0; i < tabs.Length; i++)
            {
                tabs[i].TryGetComponent<UiVisualState>(out var visual);
                visual.SetActiveState(i == slot);
            }
            var equipped = wardrobe.Snapshot();
            AddCard("미착용", null, "", equipped[slot]);
            foreach (var item in wardrobe.Catalog.items)
                if ((int)item.Slot == slot) AddCard(item.title, item.icon, item.id, equipped[slot]);
            var selected = wardrobe.Catalog.Find(draft[slot]);
            itemTitle.text = selected == null ? SlotNames[slot] + " / 미착용" : selected.title;
            itemArt.sprite = selected == null ? null : selected.icon;
            itemArt.enabled = itemArt.sprite;
            detail.text = selected == null ? "이 부위의 코디를 착용하지 않습니다." : selected.description;
            bool changed = false;
            for (int i = 0; i < 5; i++) changed |= draft[i] != equipped[i];
            status.text = changed ? "미리보기 중 / 적용하면 코디가 저장됩니다" : "현재 착용 중인 코디입니다";
        }

        private void AddCard(string label, Sprite icon, string id, string equippedId)
        {
            var card = Instantiate(itemTemplate, content);
            card.gameObject.SetActive(true);
            card.Bind(label, icon, draft[slot] == id, equippedId == id, () => Choose(id));
            cards.Add(card);
        }

        public void Choose(string id)
        {
            draft[slot] = id;
            wardrobe.Render(previewCharacter, draft);
            RefreshCards();
        }
        public void Rotate(int step) { direction = (direction + step + 8) % 8; directionLabel.text = Directions[direction]; }
        public void ToggleWalk() { walking = !walking; directionLabel.text = Directions[direction] + (walking ? " / 걷기" : " / 대기"); }
        public void ResetOutfit() { draft = (string[])wardrobe.Catalog.defaults.Clone(); wardrobe.Render(previewCharacter, draft); RefreshCards(); }
        public void Apply() { wardrobe.Apply(draft); owner.Close(); }
        public void Cancel() => owner.Close();

        private void OnDestroy()
        {
            if (previewCharacter) Destroy(previewCharacter.gameObject);
            if (previewCamera) Destroy(previewCamera.gameObject);
            if (previewTexture) { previewTexture.Release(); Destroy(previewTexture); }
            foreach (var material in previewMaterials) Destroy(material);
        }
    }
}

