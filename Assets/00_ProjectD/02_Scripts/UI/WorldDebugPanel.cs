using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using DoHwa.World;

namespace DoHwa.UI
{
    [DefaultExecutionOrder(-100)]
    public sealed class WorldDebugPanel : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text status;
        [SerializeField] private OpenWorldDefinition definition;
        private bool travelling;
        private bool restoreMotor;
        public static bool CapturingInput { get; private set; }
        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.f8Key.wasPressedThisFrame) Toggle();
            else if (panel.activeSelf && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }
        public void Toggle()
        {
            if (travelling) return;
            if (panel.activeSelf) { Close(); return; }
            restoreMotor = WorldRuntime.Instance.Hero.enabled;
            WorldRuntime.Instance.Hero.enabled = false;
            CapturingInput = true;
            panel.SetActive(true);
            status.text = WorldRuntime.Instance.IsAuthority ? "이동할 지역을 선택하세요" : "지역 이동은 호스트만 사용할 수 있습니다";
        }
        public void Close()
        {
            if (travelling) return;
            panel.SetActive(false);
            // Keep ESC consumed through the remainder of this frame.
            ReleaseInput();
            WorldRuntime.Instance.Hero.enabled = restoreMotor;
        }
        private async void ReleaseInput()
        {
            await Awaitable.NextFrameAsync(destroyCancellationToken);
            CapturingInput = panel.activeSelf;
        }
        public async void Travel(int index)
        {
            var world = WorldRuntime.Instance;
            if (travelling || !world.IsAuthority) return;
            travelling = true;
            string title = index == definition.regions.Length ? "달그늘 길" : definition.regions[index].title;
            status.text = title + " / 지형 불러오는 중";
            try
            {
                string scene = "Moonshadow";
                Vector3 position = new(0,80.3f,2);
                if (index < definition.regions.Length)
                {
                    scene = definition.regions[index].sceneName;
                    // The authored blossom valley is the heart of PeachValley, not its outer camp.
                    if (index == 0) position = world.Catalog.spawnPoint;
                    else
                    {
                        var place = Array.Find(definition.places, p => p.id == $"district-{index}-0");
                        position = new Vector3(place.center.x, 0, place.center.y - 6);
                    }
                    position.y = definition.Height(position.x, position.z, world.State.seed) + .3f;
                }
                await WorldMapSession.Instance.DebugTravelAsync(scene, position);
                status.text = title + " / 이동 완료";
            }
            catch (OperationCanceledException) { }
            catch (Exception e) { status.text = "이동 실패: " + e.Message; Debug.LogException(e); }
            finally { travelling = false; }
        }
        private void OnDestroy() => CapturingInput = false;
    }
}
