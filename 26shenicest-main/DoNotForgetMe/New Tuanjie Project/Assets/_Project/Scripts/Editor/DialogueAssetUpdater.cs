#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using DoNotForgetMe.Dialogue;

/// <summary>
/// 根据 subtitle-flow-audit 审计报告更新对白资产。
/// 菜单：Tools/Dialogue/Update Dialogue Assets (Per Audit)
///
/// 修改内容：
/// 1. DLG_EnterMemory: 在开头插入2条觉醒内心独白（isInternal=true）
/// 2. DLG_Game1ToGame2: 在索引2插入洪芳"洪菊小时候特别爱捣蛋。"（挂接 VO_Hongfang_HF02_Hongju.wav）
/// 3. 创建 DLG_AlbumPrompt: 相册前系统提示，nextMiniGameId="album_family_portrait"
/// 4. DLG_Ending: 在开头插入系统提问"所以，你猜出来小岩到底是谁了吗？"
///
/// 已有的 audioClip 引用会被保留。
/// </summary>
public static class DialogueAssetUpdater
{
    private const string DIALOGUE_FOLDER = "Assets/_Project/Audio/Dialogue";

    [MenuItem("Tools/Dialogue/Update Dialogue Assets (Per Audit)")]
    public static void UpdateAll()
    {
        UpdateEnterMemory();
        UpdateGame1ToGame2();
        CreateAlbumPrompt();
        UpdateEnding();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[DialogueAssetUpdater] 全部对白资产已更新。");
    }

    // ==============================
    // 1. DLG_EnterMemory — 插入觉醒内心独白
    // ==============================

    private static void UpdateEnterMemory()
    {
        var asset = LoadAsset<DialogueSequence>($"{DIALOGUE_FOLDER}/DLG_EnterMemory.asset");
        if (asset == null)
        {
            Debug.LogWarning("[DialogueAssetUpdater] DLG_EnterMemory.asset 未找到");
            return;
        }

        var so = new SerializedObject(asset);
        var entriesProp = so.FindProperty("entries");

        // 保存现有条目（第0条有 audioClip）
        var existingCount = entriesProp.arraySize;
        var existingAudio = existingCount > 0
            ? entriesProp.GetArrayElementAtIndex(0).FindPropertyRelative("audioClip").objectReferenceValue
            : null;
        var existingSpeaker = existingCount > 0
            ? entriesProp.GetArrayElementAtIndex(0).FindPropertyRelative("speaker").stringValue
            : "";
        var existingText = existingCount > 0
            ? entriesProp.GetArrayElementAtIndex(0).FindPropertyRelative("text").stringValue
            : "";

        // 新数组：2条觉醒 + 原有条目
        entriesProp.arraySize = existingCount + 2;

        // 将原有条目后移2位
        for (int i = existingCount - 1; i >= 0; i--)
        {
            MoveEntry(entriesProp, i, i + 2);
        }

        // [0] 觉醒独白1
        SetEntry(entriesProp, 0, "我", "这是哪里……这双手，不是我的。", true, null);
        // [1] 觉醒独白2
        SetEntry(entriesProp, 1, "我", "他们叫我洪梅……难道，这是妈妈小时候住过的地方？", true, null);
        // [2] 原有条目恢复
        SetEntry(entriesProp, 2, existingSpeaker, existingText, false, existingAudio as AudioClip);

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        Debug.Log("[DialogueAssetUpdater] DLG_EnterMemory: 已插入2条觉醒内心独白");
    }

    // ==============================
    // 2. DLG_Game1ToGame2 — 插入洪芳台词
    // ==============================

    private static void UpdateGame1ToGame2()
    {
        var asset = LoadAsset<DialogueSequence>($"{DIALOGUE_FOLDER}/DLG_Game1ToGame2.asset");
        if (asset == null)
        {
            Debug.LogWarning("[DialogueAssetUpdater] DLG_Game1ToGame2.asset 未找到");
            return;
        }

        var so = new SerializedObject(asset);
        var entriesProp = so.FindProperty("entries");
        int existingCount = entriesProp.arraySize;

        // 在索引2插入新条目，原有[2]和[3]后移
        entriesProp.arraySize = existingCount + 1;
        for (int i = existingCount - 1; i >= 2; i--)
        {
            MoveEntry(entriesProp, i, i + 1);
        }

        // 加载 VO_Hongfang_HF02_Hongju.wav
        var hongjuClip = AssetDatabase.LoadAssetAtPath<AudioClip>(
            "Assets/_Project/Audio/Voice/VO_Hongfang_HF02_Hongju.wav");

        SetEntry(entriesProp, 2, "刘洪芳", "洪菊小时候特别爱捣蛋。", false, hongjuClip);

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        Debug.Log($"[DialogueAssetUpdater] DLG_Game1ToGame2: 已插入洪芳台词（索引2），audio={(hongjuClip != null ? "已挂接" : "未找到")}）");
    }

    // ==============================
    // 3. 创建 DLG_AlbumPrompt — 相册前系统提示
    // ==============================

    private static void CreateAlbumPrompt()
    {
        var path = $"{DIALOGUE_FOLDER}/DLG_AlbumPrompt.asset";
        var existing = AssetDatabase.LoadAssetAtPath<DialogueSequence>(path);

        DialogueSequence asset;
        bool isNew = false;

        if (existing != null)
        {
            asset = existing;
        }
        else
        {
            asset = ScriptableObject.CreateInstance<DialogueSequence>();
            isNew = true;
        }

        var so = new SerializedObject(asset);
        so.FindProperty("sequenceId").stringValue = "DLG_AlbumPrompt";

        var entriesProp = so.FindProperty("entries");
        entriesProp.arraySize = 1;
        SetEntry(entriesProp, 0, "系统", "已经收集到所有五名家人的照片了，现在回到书桌，把他们放回全家福吧", false, null);

        so.FindProperty("cinematicMode").boolValue = true;
        so.FindProperty("nextMiniGameId").stringValue = "album_family_portrait";
        so.FindProperty("nextDialogueId").stringValue = "";
        so.FindProperty("nextSceneName").stringValue = "";
        so.FindProperty("triggerGameEnded").boolValue = false;

        so.ApplyModifiedPropertiesWithoutUndo();

        if (isNew)
        {
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log("[DialogueAssetUpdater] DLG_AlbumPrompt.asset 已创建（系统提示 + nextMiniGameId=album_family_portrait）");
        }
        else
        {
            EditorUtility.SetDirty(asset);
            Debug.Log("[DialogueAssetUpdater] DLG_AlbumPrompt.asset 已更新");
        }
    }

    // ==============================
    // 4. DLG_Ending — 插入系统提问
    // ==============================

    private static void UpdateEnding()
    {
        var asset = LoadAsset<DialogueSequence>($"{DIALOGUE_FOLDER}/DLG_Ending.asset");
        if (asset == null)
        {
            Debug.LogWarning("[DialogueAssetUpdater] DLG_Ending.asset 未找到");
            return;
        }

        var so = new SerializedObject(asset);
        var entriesProp = so.FindProperty("entries");
        int existingCount = entriesProp.arraySize;

        // 保存现有条目的 audioClip
        var savedClips = new AudioClip[existingCount];
        var savedSpeakers = new string[existingCount];
        var savedTexts = new string[existingCount];
        var savedInternal = new bool[existingCount];
        for (int i = 0; i < existingCount; i++)
        {
            var entry = entriesProp.GetArrayElementAtIndex(i);
            savedSpeakers[i] = entry.FindPropertyRelative("speaker").stringValue;
            savedTexts[i] = entry.FindPropertyRelative("text").stringValue;
            savedInternal[i] = entry.FindPropertyRelative("isInternal").boolValue;
            savedClips[i] = entry.FindPropertyRelative("audioClip").objectReferenceValue as AudioClip;
        }

        // 新数组：1条系统提问 + 原有条目
        entriesProp.arraySize = existingCount + 1;

        // 后移原有条目
        for (int i = existingCount - 1; i >= 0; i--)
        {
            MoveEntry(entriesProp, i, i + 1);
        }

        // [0] 系统提问
        SetEntry(entriesProp, 0, "系统", "所以，你猜出来小岩到底是谁了吗？", false, null);

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(asset);
        Debug.Log("[DialogueAssetUpdater] DLG_Ending: 已插入系统提问（索引0）");
    }

    // ==============================
    // 辅助方法
    // ==============================

    private static T LoadAsset<T>(string path) where T : UnityEngine.Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }

    private static void SetEntry(SerializedProperty arr, int index,
        string speaker, string text, bool isInternal, AudioClip audioClip)
    {
        var entry = arr.GetArrayElementAtIndex(index);
        entry.FindPropertyRelative("speaker").stringValue = speaker;
        entry.FindPropertyRelative("text").stringValue = text;
        entry.FindPropertyRelative("isInternal").boolValue = isInternal;
        entry.FindPropertyRelative("audioClip").objectReferenceValue = audioClip;
    }

    private static void MoveEntry(SerializedProperty arr, int from, int to)
    {
        var src = arr.GetArrayElementAtIndex(from);
        var dst = arr.GetArrayElementAtIndex(to);
        dst.FindPropertyRelative("speaker").stringValue = src.FindPropertyRelative("speaker").stringValue;
        dst.FindPropertyRelative("text").stringValue = src.FindPropertyRelative("text").stringValue;
        dst.FindPropertyRelative("isInternal").boolValue = src.FindPropertyRelative("isInternal").boolValue;
        dst.FindPropertyRelative("audioClip").objectReferenceValue = src.FindPropertyRelative("audioClip").objectReferenceValue;
    }
}
#endif
