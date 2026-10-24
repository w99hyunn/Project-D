using UnityEngine;
using UnityEngine.SceneManagement;
namespace DoHwa.UI
{
    public sealed class UiSceneTransition : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private UnityEngine.UI.Image[] progressMarks;
        [SerializeField] private TMPro.TMP_Text tipTitle;
        [SerializeField] private TMPro.TMP_Text tipBody;
        [SerializeField] private string[] tipTitles;
        [SerializeField, TextArea] private string[] tipBodies;
        private int tipIndex;
        [SerializeField] private Animator tipAnimator;
        private bool changingTip;
        public void NextTip()
        {
            if(changingTip)return;
            changingTip=true;
            tipAnimator.Play("ChangeTip",tipAnimator.GetLayerIndex("TipTransition"),0);
        }
        public void ApplyNextTip()
        {
            tipIndex=(tipIndex+1)%tipTitles.Length;
            tipTitle.text=tipTitles[tipIndex];
            tipBody.text=tipBodies[tipIndex];
        }
        public void FinishTipChange()=>changingTip=false;
        private bool finished;
        private static bool loading;
        private static UiSceneTransition instance;
        private void Awake() { instance=this; gameObject.SetActive(false); }
        private void SetProgress(float value)
        {
            for(int i=0;i<progressMarks.Length;i++)
                progressMarks[i].fillAmount=Mathf.Clamp01(value*progressMarks.Length-i);
        }
        public void OnFadeFinished()=>finished=true;
        private async Awaitable PlayAsync(string state)
        {
            finished=false;animator.Play(state,0,0);
            while(!finished)await Awaitable.NextFrameAsync(destroyCancellationToken);
        }
        public static async Awaitable LoadAsync(string scene)
        {
            if(loading)return;
            loading=true;
            DoHwa.GameSupport.EnsureLoaded();
            var curtain=instance;
            curtain.gameObject.SetActive(true);
            var previous=SceneManager.GetActiveScene();
            if(DoHwa.World.WorldMapSession.Instance)
                previous=DoHwa.World.WorldMapSession.Instance.gameObject.scene;
            var suspended=new System.Collections.Generic.List<Behaviour>();
            try
            {
                curtain.SetProgress(0);
                await curtain.PlayAsync("Cover");
                var operation=SceneManager.LoadSceneAsync(scene,LoadSceneMode.Additive);
                operation.allowSceneActivation=false;
                while(operation.progress<.9f)
                {
                    curtain.SetProgress(operation.progress/.9f);
                    await Awaitable.NextFrameAsync(curtain.destroyCancellationToken);
                }
                curtain.SetProgress(1);
                // Additive activation overlaps both screens for a frame; hand input/audio over first.
                foreach(var root in previous.GetRootGameObjects())
                {
                    foreach(var input in root.GetComponentsInChildren<UnityEngine.EventSystems.EventSystem>())
                        if(input.enabled){suspended.Add(input);input.enabled=false;}
                    foreach(var listener in root.GetComponentsInChildren<AudioListener>())
                        if(listener.enabled){suspended.Add(listener);listener.enabled=false;}
                }
                operation.allowSceneActivation=true;
                await Awaitable.FromAsyncOperation(operation,curtain.destroyCancellationToken);
                for(int i=0;i<SceneManager.sceneCount;i++)
                {
                    var loaded=SceneManager.GetSceneAt(i);
                    if(loaded.name==scene && loaded.handle!=previous.handle)
                    { SceneManager.SetActiveScene(loaded); break; }
                }
                if(previous.name=="Ingame" && DoHwa.World.WorldMapSession.Instance)
                    await DoHwa.World.WorldMapSession.Instance.UnloadMapsAsync();
                if(previous.name!="System")
                    await Awaitable.FromAsyncOperation(SceneManager.UnloadSceneAsync(previous),curtain.destroyCancellationToken);
                foreach(var root in SceneManager.GetActiveScene().GetRootGameObjects())
                    foreach(var ui in root.GetComponentsInChildren<RpgUiController>(true))
                        await ui.PrepareMenusAsync();
                await Awaitable.NextFrameAsync(curtain.destroyCancellationToken);
                await curtain.PlayAsync("Reveal");
            }
            finally {
                foreach(var component in suspended)if(component)component.enabled=true;
                loading=false;curtain.gameObject.SetActive(false);
            }
        }
    }
}
