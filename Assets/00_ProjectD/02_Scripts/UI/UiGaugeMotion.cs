using UnityEngine;

namespace DoHwa.UI
{
    public sealed class UiGaugeMotion : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image fill;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private bool warnWhenLow;
        private float target;
        private bool initialized;
        private float resultPulse;
        public void Pulse() => resultPulse=1;

        public void Configure(UnityEngine.UI.Image image, Color color, bool warning)
        { fill = image; normalColor = color; warnWhenLow = warning; }

        public void SetValue(float value)
        {
            target = Mathf.Clamp01(value);
            if (initialized) return;
            fill.fillAmount = target;
            initialized = true;
        }

        private void Update()
        {
            if (!initialized) return;
            float blend = 1 - Mathf.Exp(-12 * Time.unscaledDeltaTime);
            fill.fillAmount = Mathf.Lerp(fill.fillAmount, target, blend);
            Color color = warnWhenLow && target <= .25f ? new Color(1, .39f, .34f) : normalColor;
            resultPulse=Mathf.MoveTowards(resultPulse,0,Time.unscaledDeltaTime*1.4f);
            color=Color.Lerp(color,Color.white,Mathf.Sin(resultPulse*Mathf.PI)*.75f);
            if (warnWhenLow && target <= .25f) color.a = .82f + .18f * Mathf.Sin(Time.unscaledTime * 4);
            fill.color = Color.Lerp(fill.color, color, blend);
        }
    }
}
