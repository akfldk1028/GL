using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Golem.Character.Autonomous
{
    public class MemoryStore : IDisposable
    {
        public EpisodicMemory Episodic { get; }
        public SkillLibrary Skills { get; }

        private readonly MemoryConfigSO _config;
        private readonly string _savePath;
        private int _episodesSinceSave;

        public MemoryStore(MemoryConfigSO config, string characterName)
        {
            _config = config;
            Episodic = new EpisodicMemory(config);
            Skills = new SkillLibrary(config);

            string dir = Path.Combine(Application.persistentDataPath, "GolemMemory");
            _savePath = Path.Combine(dir, $"{characterName}_memory.json");
        }

        public void Load()
        {
            if (!_config.enablePersistence) return;
            if (!File.Exists(_savePath))
            {
                Debug.Log($"[MemoryStore] No saved memory at {_savePath}, starting fresh.");
                return;
            }

            try
            {
                string json = File.ReadAllText(_savePath);
                var snapshot = JsonConvert.DeserializeObject<MemorySnapshot>(json);
                if (snapshot != null)
                {
                    Episodic.LoadFrom(snapshot.episodes);
                    Skills.LoadFrom(snapshot.skills);
                    Debug.Log($"[MemoryStore] Loaded {snapshot.episodes?.Count ?? 0} episodes, {snapshot.skills?.Count ?? 0} skills from {_savePath}");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MemoryStore] Load failed: {e.Message}. Starting with empty memory.");
            }
        }

        public void Save()
        {
            if (!_config.enablePersistence) return;

            try
            {
                string dir = Path.GetDirectoryName(_savePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var snapshot = new MemorySnapshot
                {
                    episodes = Episodic.Episodes,
                    skills = Skills.Skills
                };
                string json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
                File.WriteAllText(_savePath, json);
                _episodesSinceSave = 0;
                Debug.Log($"[MemoryStore] Saved {snapshot.episodes.Count} episodes, {snapshot.skills.Count} skills.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[MemoryStore] Save failed: {e.Message}. Continuing with in-memory only.");
            }
        }

        public void OnEpisodeAdded()
        {
            _episodesSinceSave++;
            if (_episodesSinceSave >= _config.saveInterval)
                Save();
        }

        public void Dispose()
        {
            Save();
        }

        [Serializable]
        private class MemorySnapshot
        {
            public List<EpisodeEntry> episodes;
            public List<SkillEntry> skills;
        }
    }
}
