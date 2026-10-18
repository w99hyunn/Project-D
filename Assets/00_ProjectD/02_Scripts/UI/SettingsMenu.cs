using UnityEngine;
using UnityEngine.UI;

namespace DoHwa.UI
{
    public sealed class SettingsMenu : MonoBehaviour
    {
        [SerializeField] private GameObject preferences, exitPanel;
        [SerializeField] private Image settingsTab, exitTab;
        private int popupRevision;
        public bool ExitOpen => exitPanel.activeSelf;
        public void Configure(GameObject settings, GameObject exit, Image first, Image second)
        { preferences=settings;exitPanel=exit;settingsTab=first;exitTab=second; }
        private void OnEnable()
        {
            preferences.SetActive(true);
            exitPanel.SetActive(false);
            settingsTab.color=new Color(.29f,.32f,.39f);
            exitTab.color=Color.clear;
        }
        public void ShowSettings(){if(ExitOpen)CloseExit();}
        public void ShowExit()
        {
            popupRevision++;
            bool alreadyOpen=ExitOpen;
            exitPanel.transform.SetAsLastSibling();
            exitPanel.SetActive(true);
            if(alreadyOpen && exitPanel.TryGetComponent<UiPanelReveal>(out var reveal))reveal.Replay();
        }
        public async void CloseExit()
        {
            int current=++popupRevision;
            exitPanel.TryGetComponent<UiPanelReveal>(out var reveal);
            await reveal.HideAsync();
            if(this && current==popupRevision)exitPanel.SetActive(false);
        }
        private void OnDisable(){popupRevision++;exitPanel.SetActive(false);}
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
    }
}
