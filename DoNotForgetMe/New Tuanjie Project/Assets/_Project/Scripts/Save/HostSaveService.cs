using System;
using System.IO;
using UnityEngine;

namespace DoNotForgetMe.Save
{
    /// <summary>ADR 0003：唯一的 Host 本地自动存档槽位。</summary>
    public static class HostSaveService
    {
        private const string FileName = "do-not-forget-me-host-save.json";

        public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool Exists()
        {
            return File.Exists(SavePath);
        }

        public static bool TryLoad(out GameProgressSave save)
        {
            save = null;
            if (!Exists()) return false;

            try
            {
                var json = File.ReadAllText(SavePath);
                save = JsonUtility.FromJson<GameProgressSave>(json);
                return save != null && save.version == GameProgressSave.CurrentVersion;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("[Save] 读取 Host 存档失败：" + exception.Message);
                return false;
            }
        }

        public static void Save(GameProgressSave save)
        {
            if (save == null) return;

            try
            {
                var directory = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                File.WriteAllText(SavePath, JsonUtility.ToJson(save, true));
            }
            catch (Exception exception)
            {
                Debug.LogError("[Save] 写入 Host 存档失败：" + exception.Message);
            }
        }

        public static void Delete()
        {
            if (Exists()) File.Delete(SavePath);
        }
    }
}
