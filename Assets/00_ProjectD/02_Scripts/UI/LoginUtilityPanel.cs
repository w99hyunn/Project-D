using UnityEngine;
using UnityEngine.UI;

namespace DoHwa.UI
{
    public sealed class LoginUtilityPanel : MonoBehaviour
    {
        [SerializeField] private GameObject settings;
        [SerializeField] private GameObject notices;
        [SerializeField] private GameObject language;
        [SerializeField] private GameObject exit;
        [SerializeField] private Slider volume;
        [SerializeField] private Canvas[] underlayCanvases;
        private int transition;
        private bool returnToSettings;

        public bool CloseTopPopup()
        {
            if(exit.activeSelf){CloseExit();return true;}
            if(language.activeSelf){CloseLanguage();return true;}
            if(notices.activeSelf){CloseNotices();return true;}
            if(settings.activeSelf){CloseSettings();return true;}
            return false;
        }
        public void OpenSettings()
        {
            volume.SetValueWithoutNotify(AudioListener.volume);
            OpenPanel(settings);
        }

        public void OpenNotices() => OpenPanel(notices);
        public void OpenLanguage(){returnToSettings=false;OpenPanel(language);}
        public void OpenLanguageFromSettings(){returnToSettings=true;OpenPanel(language);}
        public void OpenExit() => OpenPanel(exit);
        private async void OpenPanel(GameObject panel)
        {
            foreach(var canvas in underlayCanvases)canvas.enabled=false;
            int current = ++transition;
            bool alreadyOpen = panel.activeSelf;
            GameObject outgoing = settings.activeSelf ? settings : notices.activeSelf ? notices : language.activeSelf ? language : exit.activeSelf ? exit : null;
            if(outgoing && outgoing != panel && outgoing.TryGetComponent<UiPanelReveal>(out var exitMotion))
            {
                await exitMotion.HideAsync();
                if(!this || current != transition) return;
            }
            settings.SetActive(panel == settings);
            notices.SetActive(panel == notices);
            language.SetActive(panel == language);
            exit.SetActive(panel == exit);
            if(alreadyOpen && panel.TryGetComponent<UiPanelReveal>(out var reveal)) reveal.Replay();
        }
        public void CloseSettings() => ClosePanel(settings);
        public void CloseNotices() => ClosePanel(notices);
        public void CloseLanguage(){if(returnToSettings){returnToSettings=false;OpenSettings();}else ClosePanel(language);}
        public void CloseExit() => ClosePanel(exit);
        private async void ClosePanel(GameObject panel)
        {
            int current = ++transition;
            if(panel.TryGetComponent<UiPanelReveal>(out var reveal)) await reveal.HideAsync();
            if(panel && current == transition)
            {
                panel.SetActive(false);
                foreach(var canvas in underlayCanvases)canvas.enabled=true;
            }
        }
        public void SetVolume(float value) => AudioListener.volume = value;

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}

