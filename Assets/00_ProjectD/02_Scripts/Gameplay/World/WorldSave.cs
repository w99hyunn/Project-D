using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace DoHwa.World
{
    public static class WorldSave
    {
        public static string PathFor(string slot)
        {
            if (string.IsNullOrWhiteSpace(slot) || slot.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || slot.Contains(".."))
                throw new ArgumentException("Invalid world slot", nameof(slot));
            return Path.Combine(Application.persistentDataPath, "Worlds", slot + ".json");
        }

        public static void Write(WorldState state, string slot)
        {
            string path = PathFor(slot);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(state, true), Encoding.UTF8);
            if (File.Exists(path)) File.Replace(temporary, path, path + ".bak");
            else File.Move(temporary, path);
        }

        public static WorldState Read(string slot)
        {
            string path = PathFor(slot);
            if (!File.Exists(path)) return new WorldState();
            var state = JsonUtility.FromJson<WorldState>(File.ReadAllText(path));
            if (state == null || state.version != 1 || state.players == null || state.buildings == null)
                throw new InvalidDataException("지원되지 않거나 손상된 월드 저장 파일입니다.");
            return state;
        }
    }
}
