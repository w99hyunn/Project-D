using System;
using UnityEngine;

namespace DoHwa.World
{
    public enum JourneyTaskKind { Gather, Build, Craft, Upgrade, Boss, Harvest, Pact, Cache }

    [Serializable] public sealed class JourneyTask
    {
        public string title;
        public Sprite icon;
        [TextArea] public string hint;
        public JourneyTaskKind kind;
        public string target;
        public int item;
        [Min(1)] public int amount = 1;
    }

    [Serializable] public sealed class JourneyCounter
    {
        public WorldAction action;
        public string target;
        public int item;
        public int amount;
    }

    [CreateAssetMenu(menuName = "DoHwa/World/Journey")]
    public sealed class JourneyDefinition : ScriptableObject
    {
        public string title;
        public JourneyTask[] tasks;
        public string completionTitle = "여정을 마쳤습니다";
        public string completionHint = "거점을 꾸미고 더 먼 지역을 탐험하세요";

        public int Progress(WorldState state, JourneyTask task)
        {
            int amount;
            if (task.kind == JourneyTaskKind.Build)
            {
                amount = state.buildings.FindAll(b => b.definition == task.target).Count;
                var built = state.journey.Find(c => c.action == WorldAction.Build && c.target == task.target);
                if (built != null) amount = Mathf.Max(amount, built.amount);
            }
            else if (task.kind == JourneyTaskKind.Boss)
                amount = state.defeatedBosses.Contains(task.target) ? 1 : 0;
            else if (task.kind == JourneyTaskKind.Pact)
                amount = state.pacts.Contains(task.target) ? 1 : 0;
            else if (task.kind == JourneyTaskKind.Cache)
                amount = state.openedCaches.Contains(task.target) ? 1 : 0;
            else if (task.kind == JourneyTaskKind.Harvest)
            {
                var harvested = state.journey.Find(c => c.action == WorldAction.UseBuilding && c.target == task.target && c.item == task.item);
                amount = harvested == null ? 0 : harvested.amount;
            }
            else if (task.kind == JourneyTaskKind.Upgrade)
            {
                amount = 0;
                foreach (var player in state.players)
                    foreach (int level in player.upgrades) amount = Mathf.Max(amount, level);
            }
            else
            {
                var action = task.kind == JourneyTaskKind.Gather ? WorldAction.Gather : WorldAction.Craft;
                var counter = state.journey.Find(c => c.action == action && c.item == task.item
                    && (task.kind == JourneyTaskKind.Gather || c.target == task.target));
                amount = counter == null ? 0 : counter.amount;
            }
            return Mathf.Clamp(amount, 0, task.amount);
        }

        public int Current(WorldState state)
        {
            for (int i = 0; i < tasks.Length; i++)
                if (Progress(state, tasks[i]) < tasks[i].amount) return i;
            return tasks.Length;
        }

        public static void Record(WorldState state, WorldAction action, string target, int item, int amount)
        {
            var counter = state.journey.Find(c => c.action == action && c.target == target && c.item == item);
            if (counter == null)
            {
                counter = new JourneyCounter { action = action, target = target, item = item };
                state.journey.Add(counter);
            }
            counter.amount = (int)Math.Min(int.MaxValue, (long)counter.amount + amount);
        }
    }
}
