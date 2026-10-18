using System;
using DoHwa.World;
using Steamworks;
using UnityEngine;

namespace DoHwa.UI
{
    public sealed class LoginEntry : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Button startButton;
        [SerializeField] private TMPro.TMP_Text startLabel;
        [SerializeField] private MainMenuNavigation navigation;
        [SerializeField] private Animator startAnimator;
        [SerializeField] private CanvasGroup startGroup;
        [SerializeField] private UnityEngine.UI.Image confetti;
        private Material confettiMaterial;
        private void Awake(){confettiMaterial=new Material(confetti.material);confetti.material=confettiMaterial;}
        private void OnDestroy()=>Destroy(confettiMaterial);
        private bool entering;
        public static bool SignedIn { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()=>SignedIn=false;
        public void Configure(UnityEngine.UI.Button button,TMPro.TMP_Text label) { startButton=button; startLabel=label; }
        private void RestoreStart(string text)
        {
            confettiMaterial.SetFloat("_Clicked",0);
            entering=false;startGroup.blocksRaycasts=true;startGroup.interactable=true;
            startLabel.text=text;startAnimator.Play("Normal",0,0);
        }
        public async void Enter()
        {
            if(entering || !startButton.interactable)return;
            entering=true;startGroup.blocksRaycasts=false;startGroup.interactable=false;
            foreach(var parameter in startAnimator.parameters)
                if(parameter.type==AnimatorControllerParameterType.Trigger)startAnimator.ResetTrigger(parameter.nameHash);
            confettiMaterial.SetFloat("_ClickTime",Time.time);
            confettiMaterial.SetFloat("_Clicked",1);
            startLabel.text="접속…";
            startAnimator.CrossFadeInFixedTime("Commit",.08f,0,0);
            try
            {
                await Awaitable.NextFrameAsync(destroyCancellationToken);
                while(startAnimator.IsInTransition(0) || !startAnimator.GetCurrentAnimatorStateInfo(0).IsName("Commit") || startAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime<1)
                    await Awaitable.NextFrameAsync(destroyCancellationToken);
                if(!SteamRuntime.Ready || !SteamUser.BLoggedOn())
                {
                    RestoreStart("Steam 로그인 후 다시 시도");return;
                }
                SignedIn=true;navigation.CompleteLogin();
            }
            catch(OperationCanceledException){}
            catch(Exception exception){Debug.LogException(exception);RestoreStart("다시 시도");}
        }
    }
}






