using System;
using System.IO;
using UnityEngine;

namespace DoNotForgetMe.Save
{
    /// <summary>
    /// Host 本地存档服务。Photon AppId 等本机密钥仍交给 Fusion 配置资产管理，不写入存档。
    /// </summary>
    public static class HostSaveService
    {
        private const string FileName = "host-progress.json";

        private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public static bool Exists()
        {
            return File.Exists(SavePath);
        }

        public static void Save(GameProgressSave save)
        {
            if (save == null) return;

            save.UpdatedAtUtc = DateTime.UtcNow.ToString("O");

            var directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(SavePath, JsonUtility.ToJson(save, true));
        }

        public static bool TryLoad(out GameProgressSave save)
        {
            save = null;
            if (!Exists()) return false;

            try
            {
                save = JsonUtility.FromJson<GameProgressSave>(File.ReadAllText(SavePath));
                return save != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[HostSave] 读取存档失败：" + ex.Message);
                return false;
            }
        }

        public static void Delete()
        {
            if (!Exists()) return;

            try
            {
                File.Delete(SavePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[HostSave] 删除存档失败：" + ex.Message);
            }
        }
    }
}
