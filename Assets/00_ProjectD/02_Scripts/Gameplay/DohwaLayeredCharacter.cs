using System;
using UnityEngine;

namespace DoHwa
{
    public sealed class DohwaLayeredCharacter : MonoBehaviour
    {
        [Serializable]
        private struct Frame
        {
            public Sprite[] parts;
            public Texture2D bodyMask;
            public Texture2D skinMask;
            public Sprite[] partsWithoutHair;
            public Texture2D bodyMaskWithoutHair;
            public Texture2D skinMaskWithoutHair;
        }

        [Serializable]
        private struct Direction
        {
            public Frame idle;
            public Frame[] walk;
        }

        [SerializeField] private SpriteRenderer[] layers = Array.Empty<SpriteRenderer>();
        [SerializeField] private Direction[] directions = Array.Empty<Direction>();
        [SerializeField] private float walkFramesPerSecond = 14f;
        [SerializeField] private bool hair = true;
        [SerializeField] private bool jacket = true;
        [SerializeField] private bool bottom = true;
        [SerializeField] private bool boots = true;
        [SerializeField] private bool ornament = true;
        [SerializeField] private DohwaEquipmentSet[] equipment = Array.Empty<DohwaEquipmentSet>();
        private static readonly string[] MaskNames = { "_HairMask", "_JacketMask", "_BottomMask", "_BootsMask" };
        private static readonly int[][] SlotLayers =
        {
            new[] { 0, 8 }, new[] { 4, 5, 6, 7 }, new[] { 3 }, new[] { 2 }, new[] { 9 }
        };
        private MaterialPropertyBlock bodyProperties;
        private MaterialPropertyBlock skinProperties;
        private float phase;
        private AvatarFacing facing;
        private AvatarMotion motion;
        private int frameIndex = -1;
        public AvatarFacing Facing => facing;
        public int FrameIndex => Mathf.Max(frameIndex, 0);

        private void Awake()
        {
            bodyProperties = new MaterialPropertyBlock();
            skinProperties = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (layers.Length == 11 && directions.Length == 8 && equipment.Length == 5) Render();
        }

        public void Animate(AvatarFacing direction, AvatarMotion nextMotion, float deltaTime, float playbackRate)
        {
            bool changed = facing != direction || motion != nextMotion;
            if (nextMotion != AvatarMotion.Idle) phase = Mathf.Repeat(phase + deltaTime * walkFramesPerSecond * playbackRate
                * (nextMotion == AvatarMotion.Run ? 1.5f : 1f), 8f);
            facing = direction;
            motion = nextMotion;
            int index = nextMotion == AvatarMotion.Idle ? 0 : Mathf.FloorToInt(phase);
            if (changed || index != frameIndex) Render();
        }

        public void SetEquipped(AvatarSlot slot, bool enabled)
        {
            switch (slot)
            {
                case AvatarSlot.Hair: hair = enabled; break;
                case AvatarSlot.Top: jacket = enabled; break;
                case AvatarSlot.Bottom: bottom = enabled; break;
                case AvatarSlot.Boots: boots = enabled; break;
                case AvatarSlot.Ornaments: ornament = enabled; break;
                default: throw new ArgumentOutOfRangeException(nameof(slot));
            }
            Render();
        }

        public void SetEquipment(DohwaEquipmentSet item)
        {
            int slot = (int)item.slot;
            if (item.frames.Length != 72) throw new ArgumentException("코디는 8방향 × (대기1 + 걷기8) 프레임이 필요합니다.", nameof(item));
            for (int frame = 0; frame < item.frames.Length; frame++)
            {
                var replacement = item.frames[frame];
                if (replacement.parts.Length != SlotLayers[slot].Length
                    || replacement.partsWithoutHair.Length != SlotLayers[slot].Length)
                    throw new ArgumentException("코디 슬롯의 파츠 개수가 다릅니다.", nameof(item));
                for (int variant = 0; variant < 2; variant++)
                {
                    var parts = variant == 0 ? replacement.parts : replacement.partsWithoutHair;
                    for (int part = 0; part < parts.Length; part++)
                    {
                        var sprite = parts[part];
                        var reference = directions[frame / 9].idle.parts[SlotLayers[slot][part]];
                        if (sprite.rect.size != reference.rect.size || sprite.pivot != reference.pivot
                            || !Mathf.Approximately(sprite.pixelsPerUnit, reference.pixelsPerUnit))
                            throw new ArgumentException("코디의 캔버스, 피벗, PPU가 기본 몸체와 다릅니다.", nameof(item));
                    }
                }
                foreach (var mask in new[] { replacement.bodyMask, replacement.skinMask,
                    replacement.bodyMaskWithoutHair, replacement.skinMaskWithoutHair })
                    if (mask.width != 96 || mask.height != 96)
                        throw new ArgumentException("코디 가림 마스크는 96×96이어야 합니다.", nameof(item));
            }
            equipment[slot] = item;
            SetEquipped(item.slot, true);
        }

        private void Render()
        {
            frameIndex = motion == AvatarMotion.Idle ? 0 : Mathf.FloorToInt(phase);
            var direction = directions[(int)facing];
            var frame = motion == AvatarMotion.Idle ? direction.idle : direction.walk[frameIndex];
            bool coveredClothing = hair && equipment[0].coversBackClothing;
            for (int i = 0; i < layers.Length; i++)
            {
                layers[i].sprite = coveredClothing ? frame.parts[i] : frame.partsWithoutHair[i];
                layers[i].enabled = i switch
                {
                    0 or 8 => hair,
                    2 => boots,
                    3 => bottom,
                    4 or 5 or 6 or 7 => jacket,
                    9 => ornament,
                    _ => true
                };
                layers[i].color = Color.white;
            }
            int equipmentFrame = (int)facing * 9 + (motion == AvatarMotion.Idle ? 0 : frameIndex + 1);
            for (int slot = 0; slot < SlotLayers.Length; slot++)
            {
                var item = equipment[slot].frames[equipmentFrame];
                bool coveredVariant = slot == 0 || coveredClothing;
                for (int part = 0; part < SlotLayers[slot].Length; part++)
                    layers[SlotLayers[slot][part]].sprite = coveredVariant ? item.parts[part] : item.partsWithoutHair[part];
                if (slot < 4)
                {
                    bodyProperties.SetTexture(MaskNames[slot], coveredVariant ? item.bodyMask : item.bodyMaskWithoutHair);
                    skinProperties.SetTexture(MaskNames[slot], coveredVariant ? item.skinMask : item.skinMaskWithoutHair);
                }
            }
            Vector4 equipped = new(hair ? 1f : 0f, jacket ? 1f : 0f, bottom ? 1f : 0f, boots ? 1f : 0f);
            bodyProperties.SetVector("_Equipped", equipped);
            layers[1].SetPropertyBlock(bodyProperties);
            skinProperties.SetVector("_Equipped", equipped);
            layers[10].SetPropertyBlock(skinProperties);
        }

        [ContextMenu("코디/기본 코디")]
        private void DefaultOutfit()
        {
            hair = jacket = bottom = boots = ornament = true;
            Render();
        }

        [ContextMenu("코디/의상 해제")]
        private void RemoveClothes()
        {
            jacket = bottom = boots = false;
            Render();
        }

        [ContextMenu("코디/기본 몸체")]
        private void BaseBody()
        {
            hair = jacket = bottom = boots = ornament = false;
            Render();
        }
    }
}
