using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace DoHwa.World
{
    [Serializable] public sealed class WorldSlot
    {
        public string id;
        public string title;
        public int seed;
        public long savedAt;
        public bool valid;
    }
    public static class WorldSelection
    {
        public static string Slot { get => PlayerPrefs.GetString("SelectedWorldSlot", "peach-valley"); set { PlayerPrefs.SetString("SelectedWorldSlot", value); PlayerPrefs.Save(); } }
        public static ulong Lobby;
        public static bool HostLobby;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() { Lobby = 0; HostLobby = false; }
        public static List<WorldSlot> List()
        {
            var entries = new List<WorldSlot>();
            string directory = Path.GetDirectoryName(WorldSave.PathFor("index"));
            if (!Directory.Exists(directory)) return entries;
            foreach (string path in Directory.GetFiles(directory, "*.json"))
            {
                string id = Path.GetFileNameWithoutExtension(path);
                try
                {
                    var state = WorldSave.Read(id);
                    entries.Add(new WorldSlot { id=id, title=string.IsNullOrWhiteSpace(state.title)?"도화림":state.title,
                        seed=state.seed, savedAt=new DateTimeOffset(File.GetLastWriteTimeUtc(path)).ToUnixTimeSeconds(), valid=true });
                }
                catch (Exception e) when (e is IOException || e is InvalidDataException || e is ArgumentException)
                { entries.Add(new WorldSlot { id=id, title=id+" (읽을 수 없음)", valid=false }); }
            }
            entries.Sort((a,b)=>b.savedAt.CompareTo(a.savedAt)); return entries;
        }
        public static void Rename(string id,string title)
        {
            title=title.Trim();
            if(title.Length==0||title.Length>24)throw new ArgumentException("월드 이름은 1~24자로 입력해 주세요");
            string path=WorldSave.PathFor(id);
            if(!File.Exists(path))throw new FileNotFoundException("저장 파일을 찾을 수 없습니다");
            var state=WorldSave.Read(id);state.title=title;WorldSave.Write(state,id);
        }
        public static void Delete(string id)
        {
            string path=WorldSave.PathFor(id);
            File.Delete(path);File.Delete(path+".bak");File.Delete(path+".tmp");
            if(Slot==id)PlayerPrefs.DeleteKey("SelectedWorldSlot");
        }
        public static string Create(string title, string seedText)
        {
            title=title.Trim(); if(title.Length==0 || title.Length>24) throw new ArgumentException("월드 이름은 1~24자로 입력해 주세요");
            int seed;
            if(string.IsNullOrWhiteSpace(seedText))seed=Guid.NewGuid().GetHashCode() & int.MaxValue;
            else if(!int.TryParse(seedText,out seed)) { unchecked { uint hash=2166136261;foreach(char c in seedText)hash=(hash^c)*16777619;seed=(int)(hash&int.MaxValue); } }
            string id=Guid.NewGuid().ToString("N");var state=new WorldState {id=id,title=title,seed=seed,createdAt=DateTimeOffset.UtcNow.ToUnixTimeSeconds()};
            WorldSave.Write(state,id); Slot=id;return id;
        }
    }
}

