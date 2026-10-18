using UnityEngine;
using UnityEngine.SceneManagement;
namespace DoHwa.UI
{
    public sealed class UiSceneTransition : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        private bool finished;
        private static bool loading;
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
            var curtain=Instantiate(Resources.Load<UiSceneTransition>("UI/SceneTransition"));
            DontDestroyOnLoad(curtain.gameObject);
            try
            {
                await curtain.PlayAsync("Cover");
                await Awaitable.FromAsyncOperation(SceneManager.LoadSceneAsync(scene),curtain.destroyCancellationToken);
                await Awaitable.NextFrameAsync(curtain.destroyCancellationToken);
                await curtain.PlayAsync("Reveal");
            }
            finally {loading=false;if(curtain)Destroy(curtain.gameObject);}
        }
    }
}
