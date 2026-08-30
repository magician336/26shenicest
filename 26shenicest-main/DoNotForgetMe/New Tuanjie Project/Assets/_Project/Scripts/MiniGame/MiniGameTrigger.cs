using UnityEngine;
using DoNotForgetMe.Network;
using DoNotForgetMe.Network.Gameplay;
using DoNotForgetMe.Network.Local;
using DoNotForgetMe.MiniGame.Cooking;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MiniGameTrigger : MonoBehaviour, IInteractable
{
    [Header("小游戏配置")]
    [SerializeField] private string miniGameId = "SampleGame";
    [SerializeField] private MiniGameSettings settings;

    [Header("前置条件")]
    [Tooltip("需要先完成的菜谱 ID；留空表示无前置")]
    [SerializeField] private string requiresCompletedRecipeId;

    public bool IsCompleted { get; private set; }
    public bool IsLocked => !string.IsNullOrEmpty(requiresCompletedRecipeId) && !_prerequisiteCompleted;

    private bool _debugManagersCreated;
    private bool _prerequisiteCompleted;
    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void TriggerInteract()
    {
        if (NetworkSessionManager.Service.Role != SessionRole.Host)
        {
            return;
        }

        if (IsCompleted || IsLocked)
        {
            return;
        }

        if (MiniGameManager.Instance == null)
        {
            EnsureDebugManagers();
            if (MiniGameManager.Instance == null)
            {
                Debug.LogWarning("[MiniGameTrigger] 场景中未找到 MiniGameManager，且无法创建调试管理器");
                return;
            }
        }

        MiniGameManager.Instance.StartMiniGame(miniGameId, settings);
    }

    private void OnEnable()
    {
        if (MiniGameManager.Instance != null)
        {
            MiniGameManager.Instance.OnMiniGameComplete += OnMiniGameComplete;
        }
    }

    private void Start()
    {
        // 补偿 OnEnable 时 MiniGameManager 尚未初始化的情况
        if (MiniGameManager.Instance != null)
        {
            MiniGameManager.Instance.OnMiniGameComplete -= OnMiniGameComplete;
            MiniGameManager.Instance.OnMiniGameComplete += OnMiniGameComplete;
        }
        UpdateVisual();
    }

    private void OnDisable()
    {
        if (MiniGameManager.Instance != null)
        {
            MiniGameManager.Instance.OnMiniGameComplete -= OnMiniGameComplete;
        }
    }

    private void OnMiniGameComplete(string gameId, bool success)
    {
        if (success && gameId == miniGameId)
        {
            IsCompleted = true;
        }

        if (success && gameId == requiresCompletedRecipeId)
        {
            _prerequisiteCompleted = true;
            UpdateVisual();
        }
    }

    private void UpdateVisual()
    {
        if (_spriteRenderer == null) return;
        if (IsLocked)
        {
            _spriteRenderer.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }
        else if (IsCompleted)
        {
            _spriteRenderer.color = new Color(0.3f, 0.6f, 0.3f, 0.6f);
        }
        else
        {
            // 恢复默认颜色由场景设置决定，这里不做覆盖
        }
    }

    /// <summary>直接打开非 LivingRoom 场景时，自动创建调试用管理器。</summary>
    private void EnsureDebugManagers()
    {
        if (_debugManagersCreated) return;
        _debugManagersCreated = true;

        // LocalDebugService
        if (NetworkSessionManager.Service is NotInstalledSessionService)
        {
            NetworkSessionManager.Register(new LocalDebugService());
        }

#if UNITY_EDITOR
        // 加载配置资产
        var baguaConfig = LoadFirstAsset<DoNotForgetMe.MiniGame.Bagua.BaguaStoryConfig>();
        var albumConfig = LoadFirstAsset<DoNotForgetMe.MiniGame.Album.AlbumConfig>();
        var tomatoEgg = AssetDatabase.LoadAssetAtPath<RecipeConfig>("Assets/_Project/Settings/TomatoEggRecipe.asset");
        var cucumberSalad = AssetDatabase.LoadAssetAtPath<RecipeConfig>("Assets/_Project/Settings/CucumberSaladRecipe.asset");

        // Coordinator
        var coordObj = new GameObject("SessionGameplayCoordinator");
        var coord = coordObj.AddComponent<SessionGameplayCoordinator>();
        var so = new SerializedObject(coord);
        WireArray(so, "recipes", tomatoEgg, cucumberSalad);
        WireArray(so, "baguaConfigs", baguaConfig);
        WireArray(so, "albumConfigs", albumConfig);
        so.FindProperty("debugSingleProcess").boolValue = true;

        var dlgPaths = new[] {
            "Assets/_Project/Audio/Dialogue/DLG_EnterMemory.asset",
            "Assets/_Project/Audio/Dialogue/DLG_Game1ToGame2.asset",
            "Assets/_Project/Audio/Dialogue/DLG_Game2ToGame3.asset",
            "Assets/_Project/Audio/Dialogue/DLG_Ending.asset",
            "Assets/_Project/Audio/Dialogue/DLG_AlbumPrompt.asset",
        };
        var dlgProp = so.FindProperty("dialogueConfigs");
        dlgProp.arraySize = dlgPaths.Length;
        for (int i = 0; i < dlgPaths.Length; i++)
            dlgProp.GetArrayElementAtIndex(i).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<DoNotForgetMe.Dialogue.DialogueSequence>(dlgPaths[i]);
        so.FindProperty("openingDialogueId").stringValue = "DLG_EnterMemory";
        so.ApplyModifiedProperties();

#if !FUSION_PRESENT
        coordObj.AddComponent<LocalGameplayBridge>();
#else
        coordObj.AddComponent<Fusion.NetworkObject>();
        coordObj.AddComponent<DoNotForgetMe.Network.Fusion.FusionGameplayBridge>();
#endif

        // MiniGameManager
        var mgmObj = new GameObject("MiniGameManager");
        var mgm = mgmObj.AddComponent<MiniGameManager>();
        var mgmSo = new SerializedObject(mgm);
        WireArray(mgmSo, "recipes", tomatoEgg, cucumberSalad);
        WireArray(mgmSo, "baguaConfigs", baguaConfig);
        WireArray(mgmSo, "albumConfigs", albumConfig);

        // 自动接入片尾视频
        var outroClip = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>("Assets/_Project/Video/片尾.mp4");
        if (outroClip != null)
        {
            mgmSo.FindProperty("outroVideoClip").objectReferenceValue = outroClip;
        }

        mgmSo.ApplyModifiedProperties();

        // GameManager
        new GameObject("GameManager").AddComponent<GameManager>();

        Debug.Log("[MiniGameTrigger] 调试管理器已创建，可正常测试小游戏。");
#else
        Debug.LogWarning("[MiniGameTrigger] 正式版需要从 LivingRoom 进入。");
#endif
    }

#if UNITY_EDITOR
    private static T LoadFirstAsset<T>() where T : UnityEngine.Object
    {
        var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    private static void WireArray(SerializedObject so, string fieldName,
        params UnityEngine.Object[] assets)
    {
        var prop = so.FindProperty(fieldName);
        if (prop == null) return;
        var valid = System.Array.FindAll(assets, a => a != null);
        prop.arraySize = valid.Length;
        for (int i = 0; i < valid.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = valid[i];
    }
#endif
}
