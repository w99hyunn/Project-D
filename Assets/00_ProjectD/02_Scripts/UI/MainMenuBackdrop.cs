using System;
using UnityEngine;
namespace DoHwa.UI
{
    public sealed class MainMenuBackdrop : MonoBehaviour
    {
        [SerializeField] private MainMenuNavigation navigation;
        [SerializeField] private UiBackdropBlur blur;
        [SerializeField] private GameObject[] popups;
        private bool visible;
        private async void LateUpdate()
        {
            bool requested=navigation.IsSubpage;
            foreach(var popup in popups)requested|=popup.activeInHierarchy;
            if(visible==requested)return;
            visible=requested;
            if(!requested){blur.Hide();return;}
            try{await blur.CaptureAsync();}
            catch(OperationCanceledException){}
        }
    }
}
