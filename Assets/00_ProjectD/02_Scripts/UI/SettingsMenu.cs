using UnityEngine;
using UnityEngine.UI;

namespace DoHwa.UI
{
    public sealed class SettingsMenu : MonoBehaviour
    {
        [SerializeField] private GameObject preferences, exitPanel;
        [SerializeField] private Image settingsTab, exitTab;
        [SerializeField] private Toggle settingsSelection, coopSelection;
        [SerializeField] private CoopRoomPanel coopPanel;
        [SerializeField] private WorldNavigation worldNavigation;
        [SerializeField] private TMPro.TMP_Text confirmationTitle, confirmationMessage, confirmationLabel;
        private bool returningToMain;
        private int popupRevision;
        public bool ExitOpen => exitPanel.activeSelf;
        public void Configure(GameObject settings, GameObject exit, Image first, Image second)
        { preferences=settings;exitPanel=exit;settingsTab=first;exitTab=second; }
        private void OnEnable()
        {
            preferences.SetActive(true);
            exitPanel.SetActive(false);
            coopPanel.ShowSettings();
            settingsSelection.isOn=true;
        }
        public void ShowSettings(){if(ExitOpen)CloseExit();coopPanel.ShowSettings();settingsSelection.isOn=true;}
        public void ShowCoop(){if(ExitOpen)CloseExit();coopPanel.ShowRoom();coopSelection.isOn=true;}
        public void ShowExit()
        {
            returningToMain=false;
            confirmationTitle.text="게임 종료";
            confirmationMessage.text="게임을 종료하시겠습니까?";
            confirmationLabel.text="종료";
            OpenConfirmation();
        }
        public void ShowReturnToMain()
        {
            returningToMain=true;
            confirmationTitle.text="메인 메뉴";
            confirmationMessage.text="현재 방을 나가고 메인 메뉴로 돌아가시겠습니까?";
            confirmationLabel.text="돌아가기";
            OpenConfirmation();
        }
        private void OpenConfirmation()
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
            if(returningToMain){worldNavigation.ReturnToMain();return;}
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
    }
}
