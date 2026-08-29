using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DoNotForgetMe.Dialogue
{
    /// <summary>
    /// 纯文本对白导入器：解析 【角色名】台词 格式的 .txt 文件，
    /// 生成 DialogueSequence ScriptableObject 资产。
    /// 菜单：Tools > Dialogue > Import from Text
    /// </summary>
    public static class DialogueImporter
    {
        private const string OutputFolder = "Assets/_Project/Dialogue";

        [MenuItem("Tools/Dialogue/Import from Text")]
        public static void ImportFromText()
        {
            var sourcePath = EditorUtility.OpenFilePanel(
                "选择对白文本文件", "Assets", "txt");

            if (string.IsNullOrEmpty(sourcePath)) return;

            var content = File.ReadAllText(sourcePath, Encoding.UTF8);
            var fileName = Path.GetFileNameWithoutExtension(sourcePath);
            var sequenceId = fileName.ToLowerInvariant().Replace(' ', '_');

            var asset = ScriptableObject.CreateInstance<DialogueSequence>();

            // 解析文本
            var entries = ParseEntries(content);
            var entriesField = new System.Collections.Generic.List<DialogueEntry>();

            foreach (var parsed in entries)
            {
                entriesField.Add(new DialogueEntry
                {
                    speaker = parsed.speaker,
                    text = parsed.text,
                    isInternal = parsed.isInternal
                });
            }

            // 通过 SerializedObject 设置字段（因为 fields 是 private）
            var so = new SerializedObject(asset);

            var idProp = so.FindProperty("sequenceId");
            if (idProp != null) idProp.stringValue = sequenceId;

            // 将 entries 数组写入资产
            var entriesProp = so.FindProperty("entries");
            if (entriesProp != null)
            {
                entriesProp.arraySize = entriesField.Count;
                for (var i = 0; i < entriesField.Count; i++)
                {
                    var entryProp = entriesProp.GetArrayElementAtIndex(i);
                    entryProp.FindPropertyRelative("speaker").stringValue = entriesField[i].speaker;
                    entryProp.FindPropertyRelative("text").stringValue = entriesField[i].text;
                    entryProp.FindPropertyRelative("isInternal").boolValue = entriesField[i].isInternal;
                    // audioClip 留空，在 Inspector 中手动挂接
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            // 确保输出目录存在
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Dialogue");
            }

            var assetPath = $"{OutputFolder}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<DialogueSequence>(assetPath);
            if (existing != null)
            {
                // 覆盖现有资产的字段
                var existingSo = new SerializedObject(existing);
                var existingIdProp = existingSo.FindProperty("sequenceId");
                if (existingIdProp != null) existingIdProp.stringValue = sequenceId;

                var existingEntriesProp = existingSo.FindProperty("entries");
                if (existingEntriesProp != null)
                {
                    existingEntriesProp.arraySize = entriesField.Count;
                    for (var i = 0; i < entriesField.Count; i++)
                    {
                        var entryProp = existingEntriesProp.GetArrayElementAtIndex(i);
                        entryProp.FindPropertyRelative("speaker").stringValue = entriesField[i].speaker;
                        entryProp.FindPropertyRelative("text").stringValue = entriesField[i].text;
                        entryProp.FindPropertyRelative("isInternal").boolValue = entriesField[i].isInternal;
                        // 保留已有的 audioClip 引用
                    }
                }
                existingSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = existing;
                Debug.Log($"[DialogueImporter] 已更新资产：{assetPath}（{entriesField.Count} 条）");
            }
            else
            {
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = asset;
                Debug.Log($"[DialogueImporter] 已创建资产：{assetPath}（{entriesField.Count} 条）");
            }
        }

        private struct ParsedEntry
        {
            public string speaker;
            public string text;
            public bool isInternal;
        }

        /// <summary>
        /// 解析 【角色名】台词 格式。
        /// 规则：
        /// - 每行匹配 【...】前缀 → speaker，剩余为台词
        /// - speaker 含"内心" → isInternal=true，speaker 去掉"内心"
        /// - 无 【】前缀的续行拼接到上一条台词
        /// - 空行分隔条目
        /// </summary>
        private static System.Collections.Generic.List<ParsedEntry> ParseEntries(string content)
        {
            var result = new System.Collections.Generic.List<ParsedEntry>();
            var lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            ParsedEntry? current = null;

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                // 空行 → 提交当前条目
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (current.HasValue)
                    {
                        result.Add(current.Value);
                        current = null;
                    }
                    continue;
                }

                // 尝试匹配 【角色名】前缀
                if (line.StartsWith("【") && line.IndexOf("】") > 0)
                {
                    // 提交上一条
                    if (current.HasValue)
                    {
                        result.Add(current.Value);
                    }

                    var closeIdx = line.IndexOf("】");
                    var speakerRaw = line.Substring(1, closeIdx - 1);
                    var textPart = line.Substring(closeIdx + 1).Trim();

                    var isInternal = speakerRaw.Contains("内心");
                    var speaker = isInternal
                        ? speakerRaw.Replace("内心", "").Trim()
                        : speakerRaw;

                    current = new ParsedEntry
                    {
                        speaker = speaker,
                        text = textPart,
                        isInternal = isInternal
                    };
                }
                else
                {
                    // 续行：拼接到上一条台词
                    if (current.HasValue)
                    {
                        current = new ParsedEntry
                        {
                            speaker = current.Value.speaker,
                            text = current.Value.text + "\n" + line,
                            isInternal = current.Value.isInternal
                        };
                    }
                }
            }

            // 提交最后一条
            if (current.HasValue)
            {
                result.Add(current.Value);
            }

            return result;
        }
    }
}
