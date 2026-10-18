using UnityEngine;
using UnityEngine.InputSystem;

namespace DoHwa.UI
{
    public sealed class MainMenuNavigation : MonoBehaviour
    {
        [SerializeField] private MainMenuController controller;
        [SerializeField] private LoginUtilityPanel utilities;
        [SerializeField] private GameObject home;
        [SerializeField] private GameObject login;
        [SerializeField] private GameObject joinPage;
        [SerializeField] private UnityEngine.UI.Image shade;
        [SerializeField] private GameObject worlds;
        [SerializeField] private GameObject room;
        [SerializeField] private GameObject departure;
        [SerializeField] private GameObject back;
        [SerializeField] private GameObject friends;
        [SerializeField] private GameObject leave;
        [SerializeField] private GameObject invite;
        [SerializeField] private TMPro.TMP_Text heading;
        private bool selecting;
        private bool joining;
        private bool wasInLobby;
        private bool transitioning;
        public bool IsSubpage => selecting || joining || controller.InLobby;
        [SerializeField] private UiPanelReveal[] pageLayers;
        private void Awake()=>controller.enabled=LoginEntry.SignedIn;
        private void Start()=>ShowHome();
        public void CompleteLogin(){controller.enabled=true;ShowHome();}
        public void ShowWorlds()=>Navigate(true,false);
        public void ShowJoin()=>Navigate(false,true);
        public void ShowHome()=>Navigate(false,false);
        private async void Navigate(bool worldPage,bool joinFriends)
        {
            if(transitioning)return;
            transitioning=true;
            try
            {
                var pending=new System.Collections.Generic.List<Awaitable>();
                foreach(var layer in pageLayers)
                    if(layer.gameObject.activeInHierarchy)pending.Add(layer.HideAsync());
                foreach(var task in pending)await task;
                if(destroyCancellationToken.IsCancellationRequested)return;
                selecting=worldPage;joining=joinFriends;Refresh();
                foreach(var layer in pageLayers)
                    if(layer.gameObject.activeInHierarchy)layer.Replay();
            }
            catch(System.OperationCanceledException){}
            finally{transitioning=false;}
        }
        private void Update()
        {
            if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)HandleEscape();
            if(!LoginEntry.SignedIn)return;
            if(wasInLobby!=controller.InLobby){wasInLobby=controller.InLobby;Navigate(true,false);}

        }
        public void HandleEscape()
        {
            if(controller.CloseSavePopup())return;
            if(utilities.CloseTopPopup())return;
            if(controller.IsCreateOpen){controller.CancelCreate();return;}
            utilities.OpenExit();
        }
        private void Refresh()
        {
            bool lobby=controller.InLobby;
            login.SetActive(!LoginEntry.SignedIn);
            home.SetActive(LoginEntry.SignedIn&&!selecting&&!joining&&!lobby);
            joinPage.SetActive(false);
            shade.color=new Color(.025f,.055f,.07f,selecting||joining||lobby?.25f:.20f);
            worlds.SetActive(selecting&&!lobby);
            room.SetActive(lobby);
            departure.SetActive(selecting||lobby);
            back.SetActive((selecting||joining)&&!lobby);
            friends.SetActive(false);
            leave.SetActive(lobby);
            invite.SetActive(lobby);
            heading.gameObject.SetActive(selecting||joining||lobby);
            heading.text=lobby?"함께할 친구":joining?"친구와 함께":selecting?"방 만들기":"";
        }
    }
}




