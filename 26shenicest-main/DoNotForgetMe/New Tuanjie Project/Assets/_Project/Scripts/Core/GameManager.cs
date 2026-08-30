using System;
using System.Collections;
using DoNotForgetMe.Audio;
using DoNotForgetMe.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("玩家生成")]
    [SerializeField] private PlayerController playerPrefab;
    [SerializeField] private string playerSpawnTag = "PlayerSpawn";
    [SerializeField] private bool autoSpawnPlayer = true;

    [Header("游戏流程")]
    [Tooltip("玩家死亡后等待多久重生（秒）")]
    [SerializeField] private float respawnDelay = 3.0f;

    public bool IsPaused { get; private set; }
    public PlayerController Player { get; private set; }

    public event Action OnGameStarted;
    public event Action OnGamePaused;
    public event Action OnGameResumed;
    public event Action<PlayerController> OnPlayerSpawned;
    public event Action OnPlayerDead;
    public event Action OnPlayerRespawn;

    private Vector3 currentRespawnPoint;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            DoNotForgetMe.Network.Gameplay.SessionGameplayCoordinator.Instance?.SaveLastStableState();
        }
    }

    private void Start()
    {
        StartGame();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == SceneNames.MainMenu)
        {
            CleanupPersistentObjects();
            return;
        }

        // Courtyard 是子页面场景，不需要 Player
        if (scene.name == SceneNames.Courtyard)
        {
            AudioManager.PlayAmb(AmbId.Courtyard);
            return;
        }

        // 离开 Courtyard 时停止环境音
        AudioManager.StopAmb();

        if (autoSpawnPlayer)
        {
            SpawnOrFindPlayer();
        }
    }

    /// <summary>返回主菜单时销毁跨场景持久化的游戏对象，避免状态残留。</summary>
    private void CleanupPersistentObjects()
    {
        var coordinator = DoNotForgetMe.Network.Gameplay.SessionGameplayCoordinator.Instance;
        if (coordinator != null) Destroy(coordinator.gameObject);

        var miniGameManager = MiniGameManager.Instance;
        if (miniGameManager != null) Destroy(miniGameManager.gameObject);
    }

    public void StartGame()
    {
        OnGameStarted?.Invoke();
        if (autoSpawnPlayer)
        {
            SpawnOrFindPlayer();
        }
    }

    public void RegisterPlayer(PlayerController controller)
    {
        Player = controller;
        OnPlayerSpawned?.Invoke(Player);
    }

    public void SetRespawnPoint(Vector3 point)
    {
        currentRespawnPoint = point;
    }

    public void HandlePlayerDeath(HealthController health)
    {
        if (IsPaused) return;

        OnPlayerDead?.Invoke();
        StartCoroutine(RespawnRoutine(health));
    }

    private IEnumerator RespawnRoutine(HealthController health)
    {
        yield return new WaitForSeconds(respawnDelay);

        if (Player == null)
        {
            Debug.LogError("[GameManager] Player 引用丢失，无法传送！");
            yield break;
        }

        Player.Teleport(currentRespawnPoint);

        if (health != null) health.ResetHealth();

        Player.Revive();

        OnPlayerRespawn?.Invoke();
    }

    private void SpawnOrFindPlayer()
    {
        var existing = FindObjectOfType<PlayerController>();
        if (existing != null)
        {
            RegisterPlayer(existing);
            currentRespawnPoint = existing.transform.position;

            if (RoomCameraController.Instance != null)
            {
                RoomCameraController.Instance.SetTarget(existing.transform);
                RoomCameraController.Instance.SnapToTarget();
            }
            return;
        }

        if (playerPrefab == null)
        {
            return;
        }

        Transform spawnPoint = null;
        var taggedNodes = GameObject.FindGameObjectsWithTag(playerSpawnTag);
        if (taggedNodes.Length > 0)
        {
            spawnPoint = taggedNodes[0].transform;
        }

        var instance = Instantiate(playerPrefab, spawnPoint != null ? spawnPoint.position : Vector3.zero, Quaternion.identity);
        RegisterPlayer(instance);
        currentRespawnPoint = instance.transform.position;

        if (RoomCameraController.Instance != null)
        {
            RoomCameraController.Instance.SetTarget(instance.transform);
            RoomCameraController.Instance.SnapToTarget();
        }
    }

    public void Pause()
    {
        if (IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f;
        OnGamePaused?.Invoke();
    }

    public void Resume()
    {
        if (!IsPaused) return;
        IsPaused = false;
        Time.timeScale = 1f;
        OnGameResumed?.Invoke();
    }

    public void TogglePause()
    {
        if (IsPaused) Resume(); else Pause();
    }

    public void RestartLevel()
    {
        var idx = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(idx);
    }

    public void QuitToDesktop()
    {
        Application.Quit();
    }
}
