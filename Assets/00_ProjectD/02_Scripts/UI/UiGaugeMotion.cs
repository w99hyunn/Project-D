using UnityEngine;
namespace DoHwa.UI
{
    public sealed class UiGaugeMotion : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image fill;
        [SerializeField] private Color normalColor=Color.white;
        [SerializeField] private bool warnWhenLow;
        [SerializeField] private Animator animator;
        [SerializeField] private float valueProgress=1, resultPulse, warningOpacity=1;
        private float start,target;
        private bool initialized;
        private bool pendingPulse;
        public void Pulse(){if(isActiveAndEnabled)animator.SetTrigger("ItemChanged");else pendingPulse=true;}
        private void OnEnable()
        {
            animator.Update(0);
            if(initialized)SyncState();
            if(pendingPulse){animator.SetTrigger("ItemChanged");pendingPulse=false;}
        }
        public void Configure(UnityEngine.UI.Image image,Color color,bool warning)
        {fill=image;normalColor=color;warnWhenLow=warning;}
        public void SetValue(float value)
        {
            value=Mathf.Clamp01(value);
            if(initialized && Mathf.Approximately(value,target))return;
            start=initialized?fill.fillAmount:value; target=value;
            if(!initialized){fill.fillAmount=value;initialized=true;}
            valueProgress=0;
            if(isActiveAndEnabled)SyncState();
        }
        private void SyncState()
        {
            animator.Play("ValueChange",0,0);
            animator.SetBool("Low",warnWhenLow && target<=.25f);
        }
        private void OnDidApplyAnimationProperties()
        {
            if(!initialized)return;
            fill.fillAmount=Mathf.Lerp(start,target,valueProgress);
            Color color=warnWhenLow && target<=.25f?new Color(1,.39f,.34f):normalColor;
            color=Color.Lerp(color,Color.white,resultPulse);
            color.a*=warningOpacity;
            fill.color=color;
        }
    }
}
