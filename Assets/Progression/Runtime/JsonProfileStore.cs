using System;
using System.IO;
using UnityEngine;

namespace CampusRift.Progression
{
    public interface IProfileStore
    {
        ProfileData Load();
        void Save(ProfileData data);
    }

    // Same safety rules as the learning store: temp file, File.Replace with a .bak, fall back to the backup when the
    // main file is damaged, and never overwrite a file that could not be read.
    public sealed class JsonProfileStore : IProfileStore
    {
        public readonly string Path;
        public string RecoveryMessage { get; private set; }
        public bool RecoveredFromBackup { get; private set; }
        public bool WriteBlocked { get; private set; }
        public bool FoundExisting { get; private set; }
        public JsonProfileStore(string path) { Path = path; }

        public ProfileData Load()
        {
            RecoveryMessage = null; RecoveredFromBackup = false; FoundExisting = false;
            foreach (var candidate in new[] { Path, Path + ".bak" })
            {
                if (!File.Exists(candidate)) continue;
                FoundExisting = true;
                try
                {
                    var data = JsonUtility.FromJson<ProfileData>(File.ReadAllText(candidate));
                    if (data == null || data.version != ProfileData.CurrentVersion || data.cultivation == null || data.learning == null
                        || data.learning.lessons == null || data.levels == null || data.cultivation.tier < 1 || data.cultivation.tier > 5)
                        throw new InvalidDataException("Unsupported or damaged profile.");
                    if (candidate != Path)
                    {
                        RecoveredFromBackup = true; RecoveryMessage = "Profile recovered from backup.";
                        File.Copy(candidate, Path, true);
                    }
                    WriteBlocked = false;
                    if(data.seenReactions==null)data.seenReactions=new System.Collections.Generic.List<string>();
                    EndgameService.State(data);
                    return data;
                }
                catch (Exception e) when (e is IOException || e is ArgumentException || e is UnauthorizedAccessException || e is InvalidDataException)
                { RecoveryMessage = e.Message; }
            }
            // An unreadable save (damaged, or a newer schema) is never replaced silently.
            WriteBlocked = File.Exists(Path) || File.Exists(Path + ".bak");
            return new ProfileData();
        }

        public void Save(ProfileData data)
        {
            if (WriteBlocked) throw new IOException("Existing profile needs recovery. Original files have been preserved.");
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            string temp = Path + ".tmp";
            File.WriteAllText(temp, JsonUtility.ToJson(data, true));
            if (File.Exists(Path)) File.Replace(temp, Path, Path + ".bak");
            else File.Move(temp, Path);
        }
    }
}
