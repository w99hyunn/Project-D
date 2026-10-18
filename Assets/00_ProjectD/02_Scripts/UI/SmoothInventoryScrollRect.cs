using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DoHwa.UI
{
    public sealed class SmoothInventoryScrollRect : ScrollRect
    {
        private float target;
        private bool smoothing, applying;

        public override void OnScroll(PointerEventData data)
        {
            if (!IsActive()) return;
            float range = content.rect.height - viewport.rect.height;
            if (range <= 0) return;
            if (!smoothing) target = verticalNormalizedPosition;
            target = Mathf.Clamp01(target + data.scrollDelta.y * scrollSensitivity / range);
            smoothing = true;
            StopMovement();
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            if (!smoothing) return;
            float range = content.rect.height - viewport.rect.height;
            if (range <= 0) { smoothing = false; return; }
            float next = Mathf.Lerp(verticalNormalizedPosition, target, 1 - Mathf.Exp(-18 * Time.unscaledDeltaTime));
            if (Mathf.Abs(next - target) * range < .1f) { next = target; smoothing = false; }
            applying = true;
            verticalNormalizedPosition = next;
            applying = false;
        }

        protected override void SetNormalizedPosition(float value, int axis)
        {
            if (!applying) smoothing = false;
            base.SetNormalizedPosition(value, axis);
        }

        public override void OnBeginDrag(PointerEventData data)
        {
            smoothing = false;
            base.OnBeginDrag(data);
        }

        protected override void OnDisable()
        {
            smoothing = false;
            base.OnDisable();
        }
    }
}
