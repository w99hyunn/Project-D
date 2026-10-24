using DoHwa.World;
using TMPro;
using UnityEngine;

namespace DoHwa.UI
{
    public sealed class JourneyPanel : MonoBehaviour
    {
        [SerializeField] private WorldRuntime world;
        [SerializeField] private JourneyDefinition journey;
        [SerializeField] private JourneyDefinition[] followingChapters = System.Array.Empty<JourneyDefinition>();
        [SerializeField] private TMP_Text chapterTitle;
        [SerializeField] private TMP_Text coverChapter;
        private JourneyDefinition activeChapter;
        [SerializeField] private TMP_Text heading;
        [SerializeField] private TMP_Text objective;
        [SerializeField] private TMP_Text hint;
        [SerializeField] private JourneyStepView[] steps;
        [SerializeField] private TMP_Text chapterProgress;
        [SerializeField] private TMP_Text detailTitle;
        [SerializeField] private TMP_Text detailHint;
        [SerializeField] private TMP_Text detailStatus;
        [SerializeField] private UnityEngine.UI.Image detailIcon;
        [SerializeField] private UnityEngine.UI.Image progressFill;
        private int selected = -1;

        public void SelectStep(int index) { selected = index; Refresh(); }

        private void Start() { world.Changed += Refresh; Refresh(); }
        private void OnDestroy() { world.Changed -= Refresh; }

        private void Refresh()
        {
            var state = world.State;
            var next = this.journey;
            foreach (var chapter in followingChapters)
            {
                if (next.Current(state) < next.tasks.Length) break;
                next = chapter;
            }
            if (activeChapter != next) { activeChapter = next; selected = -1; }
            var journey = activeChapter;
            int current = journey.Current(state);
            heading.text = journey.title;
            chapterTitle.text = journey.title;
            coverChapter.text = journey.title;
            if (current == journey.tasks.Length)
            {
                objective.text = journey.completionTitle;
                hint.text = journey.completionHint;
            }
            else
            {
                var task = journey.tasks[current];
                objective.text = task.title;
                hint.text = task.hint + $"\n{journey.Progress(state, task)} / {task.amount}";
            }
            int shown = selected < 0 ? Mathf.Min(current, journey.tasks.Length - 1) : selected;
            int completed = 0;
            for (int i = 0; i < journey.tasks.Length; i++)
            {
                var task = journey.tasks[i];
                int progress = journey.Progress(state, task);
                bool done = progress >= task.amount;
                if (done) completed++;
                steps[i].Show(task.icon, task.title, done, i == current, i == shown);
            }
            chapterProgress.text = $"여정 기록   {completed} / {journey.tasks.Length}";
            var shownTask = journey.tasks[shown];
            int value = journey.Progress(state, shownTask);
            detailTitle.text = shownTask.title;
            detailHint.text = shownTask.hint;
            detailIcon.sprite = shownTask.icon;
            detailStatus.text = value >= shownTask.amount ? "완료한 여정" : $"{value} / {shownTask.amount}";
            progressFill.fillAmount = value / (float)shownTask.amount;
        }
    }
}
