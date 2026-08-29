namespace DoNotForgetMe.Core
{
    /// <summary>
    /// 全局场景名常量。6 场景编排：
    /// 0. MainMenu   — 标题页 / 房间码连接
    /// 1. Intro       — Scene0：开场过场（黑屏文字 + 内心OS + AIGC视频 + 时光回溯）
    /// 2. LivingRoom  — Scene1：客厅书桌（醒来 → 走到门口进厨房；后期回到此处做相册小游戏）
    /// 3. Kitchen     — Scene2：厨房（做饭小游戏，两道菜链式）
    /// 4. Courtyard   — Scene3：庭院（八卦/偷听小游戏）
    ///
    /// 流转：MainMenu → Intro → LivingRoom → Kitchen → Courtyard → LivingRoom(相册) → GameEnded
    /// </summary>
    public static class SceneNames
    {
        public const string MainMenu = "MainMenu";
        public const string Intro = "Intro";
        public const string LivingRoom = "LivingRoom";
        public const string Kitchen    = "Kitchen";
        public const string Courtyard   = "Courtyard";

        /// <summary>所有需要 Coordinator/MiniGameManager 持久化的游戏场景。</summary>
        private static readonly string[] GameScenes = { Intro, LivingRoom, Kitchen, Courtyard };

        /// <summary>当前活动场景是否为游戏场景（非 MainMenu）。</summary>
        public static bool IsGameScene(string sceneName)
        {
            foreach (var s in GameScenes)
            {
                if (s == sceneName) return true;
            }
            return false;
        }

        /// <summary>指定场景名是否存在于 Build Settings。</summary>
        public static bool ExistsInBuildSettings(string sceneName)
        {
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
            {
                var path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName) return true;
            }
            return false;
        }
    }
}
