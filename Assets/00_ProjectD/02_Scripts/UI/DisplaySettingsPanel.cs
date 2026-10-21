using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DoHwa.UI
{
    public sealed class DisplaySettingsPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text[] values;
        [SerializeField] private Button[] previous, next;
        [SerializeField] private Button apply, revert;
        [SerializeField] private TMP_Text applyLabel, status;
        [SerializeField] private Slider volume;
        private readonly List<Vector2Int> resolutions = new();
        private readonly int[] limits = { 30, 60, 90, 120, 144, 165, 240, -1 };
        private readonly FullScreenMode[] modes = { FullScreenMode.Windowed, FullScreenMode.FullScreenWindow, FullScreenMode.ExclusiveFullScreen };
        private int[] selected = new int[6];
        private Snapshot before;
        private float deadline;
        private bool confirming;

        [Serializable] private struct Snapshot
        {
            public int width, height, mode, quality, sync, fps, texture;
            public static Snapshot Capture() => new() { width=Screen.width, height=Screen.height,
                mode=(int)Screen.fullScreenMode, quality=QualitySettings.GetQualityLevel(),
                sync=QualitySettings.vSyncCount, fps=Application.targetFrameRate,
                texture=QualitySettings.globalTextureMipmapLimit };
            public void Apply()
            {
                QualitySettings.SetQualityLevel(quality, true);
                QualitySettings.vSyncCount=sync;
                Application.targetFrameRate=fps;
                QualitySettings.globalTextureMipmapLimit=texture;
#if !UNITY_EDITOR
                Screen.SetResolution(width,height,(FullScreenMode)mode);
#endif
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Restore()
        {
            AudioListener.volume=PlayerPrefs.GetFloat("Settings.Volume",1);
            if(PlayerPrefs.HasKey("Settings.Display"))
                JsonUtility.FromJson<Snapshot>(PlayerPrefs.GetString("Settings.Display")).Apply();
        }
        private void Awake()
        {
            foreach(var r in Screen.resolutions)
            {
                var size=new Vector2Int(r.width,r.height);
                if(!resolutions.Contains(size))resolutions.Add(size);
            }
            var current=new Vector2Int(Screen.width,Screen.height);
            if(!resolutions.Contains(current))resolutions.Add(current);
            resolutions.Sort((a,b)=>a.x==b.x?a.y.CompareTo(b.y):a.x.CompareTo(b.x));
            for(int i=0;i<6;i++)
            {
                int row=i;
                previous[i].onClick.AddListener(()=>Change(row,-1));
                next[i].onClick.AddListener(()=>Change(row,1));
            }
            apply.onClick.AddListener(Apply);
            revert.onClick.AddListener(Revert);
            volume.onValueChanged.AddListener(SaveVolume);
        }
        private void OnEnable(){ReadCurrent();status.text="";}
        private void ReadCurrent() => Read(Snapshot.Capture());
        private void Read(Snapshot snapshot)
        {
            selected[0]=Math.Max(0,resolutions.IndexOf(new Vector2Int(snapshot.width,snapshot.height)));
            selected[1]=Math.Max(0,Array.IndexOf(modes,(FullScreenMode)snapshot.mode));
            selected[2]=snapshot.quality;
            selected[3]=snapshot.sync>0?1:0;
            selected[4]=Math.Max(0,Array.IndexOf(limits,snapshot.fps));
            selected[5]=Mathf.Clamp(snapshot.texture,0,2);
            volume.SetValueWithoutNotify(AudioListener.volume);
            Refresh();
        }
        private void Change(int row,int direction)
        {
            if(confirming)Revert();
            int count=row switch {0=>resolutions.Count,1=>modes.Length,2=>QualitySettings.names.Length,3=>2,4=>limits.Length,_=>3};
            selected[row]=(selected[row]+direction+count)%count;
            Refresh();
            status.text="변경 사항을 적용해주세요.";
        }
        private void Refresh()
        {
            var r=resolutions[selected[0]];
            values[0].text=$"{r.x} x {r.y}";
            values[1].text=new[]{"창 모드","테두리 없는 전체 화면","전체 화면"}[selected[1]];
            string quality=QualitySettings.names[selected[2]];
            values[2].text=quality=="Mobile"?"낮음":quality=="PC"?"높음":quality;
            values[3].text=selected[3]==1?"켜짐":"꺼짐";
            values[4].text=selected[3]==1?"모니터 주사율":limits[selected[4]]<0?"제한 없음":limits[selected[4]]+" FPS";
            values[5].text=new[]{"원본","1/2","1/4"}[selected[5]];
            previous[4].interactable=next[4].interactable=selected[3]==0;
            applyLabel.text="적용";
        }
        private void Apply()
        {
            if(confirming)
            {
                confirming=false;
                PlayerPrefs.SetString("Settings.Display",JsonUtility.ToJson(pending));
                PlayerPrefs.Save();Refresh();status.text="설정을 저장했습니다.";return;
            }
            before=Snapshot.Capture();
            var r=resolutions[selected[0]];
            pending=new Snapshot {width=r.x,height=r.y,mode=(int)modes[selected[1]],quality=selected[2],
                sync=selected[3],fps=limits[selected[4]],texture=selected[5]};
            pending.Apply();confirming=true;deadline=Time.unscaledTime+15;
        }
        private Snapshot pending;
        private void Update()
        {
            if(!confirming)return;
            int seconds=Mathf.CeilToInt(deadline-Time.unscaledTime);
            if(seconds<=0){Revert();return;}
            applyLabel.text=$"유지 ({seconds})";
            status.text="화면이 정상적으로 보이나요? 유지하지 않으면 이전 설정으로 돌아갑니다.";
        }
        private void Revert()
        {
            if(confirming){before.Apply();confirming=false;Read(before);}
            else ReadCurrent();
            status.text="변경을 취소했습니다.";
        }
        public void CancelPreview(){if(confirming)Revert();}
        private void OnDisable(){if(confirming){before.Apply();confirming=false;}}
        private static void SaveVolume(float value)
        {
            AudioListener.volume=value;PlayerPrefs.SetFloat("Settings.Volume",value);
        }
        private void OnApplicationQuit()=>PlayerPrefs.Save();
    }
}

