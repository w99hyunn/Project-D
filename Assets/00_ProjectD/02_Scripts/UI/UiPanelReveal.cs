using System;
using UnityEngine;
namespace DoHwa.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UiPanelReveal : MonoBehaviour
    {
        [SerializeField] private RectTransform[] sections=Array.Empty<RectTransform>();
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationClip closeClip;
        private CanvasGroup group;
        private int revision;
        private bool entering,leaving,closed;
        public void Configure(RectTransform[] targets)=>sections=targets;
        private void Awake()=>TryGetComponent<CanvasGroup>(out group);
        private void OnEnable()=>Replay();
        public void Replay()=>ReplayState("Open");
        public void ReplayState(string state)
        {
            revision++;leaving=closed=false;entering=true;
            group.interactable=false;group.blocksRaycasts=true;
            animator.Play(state,0,0);animator.Update(0);
        }
        public void RefreshContent(int index)
        {
            if(!isActiveAndEnabled || entering || leaving)return;
            animator.Play("Refresh"+index,0,0);
        }
        public async Awaitable HideAsync(string state="Close")
        {
            int token=++revision;entering=false;leaving=true;closed=false;group.interactable=false;
            animator.CrossFadeInFixedTime(state,.04f);
            try
            {
                while(!closed)
                {
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                    if(token!=revision)return;
                }
            }
            catch(OperationCanceledException){}
        }
        public void OnOpened(){if(!leaving){entering=false;group.interactable=true;}}
        public void OnClosed()=>closed=true;
        private void OnDisable(){revision++;entering=leaving=false;}
    }
}
