using System;
using DoNotForgetMe.Network;
using DoNotForgetMe.Save;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主菜单控制器：纯联机入口（ADR 0001 决策——无单机模式）。
/// 职责：
/// 1. 「创建会话」：生成房间码并作为 Host 启动会话，把房间码大字展示给玩家以便口头告知对方；
/// 2. 「加入会话」：校验输入的房间码（4~6 位无歧义字符）并作为 Client 加入；
/// 3. 呈现会话状态与错误信息（断线即结束会话，见 ADR 0001 断线策略）。
/// 本类不直接依赖 Photon Fusion，通过 INetworkSessionService 交互，
/// 因此在 Fusion SDK 导入前项目仍可编译、主菜单可预览。
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private InputField roomCodeInput;
    [SerializeField] private Button createButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button singlePlayerButton;
    [SerializeField] private Text statusText;

    [Header("Host 侧房间码展示")]
    [Tooltip("创建会话后大字展示的房间码文本（Host 需念给对方听）")]
    [SerializeField] private Text roomCodeDisplay;

    private SessionState _lastState = SessionState.Disconnected;
    private string _generatedRoomCode;
    private INetworkSessionService _service;
    private bool _isCancellationRequested;
    private bool _hasPendingError;
    private bool _awaitNewGameOverwriteConfirm;
    private bool _joinInputVisible;

    private void Start()
    {
        _service = NetworkSessionManager.Service;

        _service.StateChanged += OnStateChanged;
        _service.Error += OnError;

        if (createButton != null) createButton.onClick.AddListener(OnCreateClicked);
        if (joinButton != null) joinButton.onClick.AddListener(OnJoinClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
        if (singlePlayerButton != null) singlePlayerButton.onClick.AddListener(OnSinglePlayerClicked);
        if (roomCodeInput != null) roomCodeInput.onEndEdit.AddListener(OnRoomCodeInputEndEdit);

        ClearGeneratedRoomCode();
        SetJoinInputVisible(false);
        SetConnectingControls(false);

        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(HostSaveService.Exists());
        }

        SetStatus(_service.IsAvailable
            ? "创建一个新会话或加入会话"
            : "网络层未安装：主菜单可预览，联机需先导入 Photon Fusion SDK（docs/install-fusion.md）");
    }

    private void OnDestroy()
    {
        if (_service != null)
        {
            _service.StateChanged -= OnStateChanged;
            _service.Error -= OnError;
        }
    }

    /// <summary>切换会话服务实现并重挂事件订阅。</summary>
    private void UseService(INetworkSessionService service)
    {
        if (_service != null)
        {
            _service.StateChanged -= OnStateChanged;
            _service.Error -= OnError;
        }
        NetworkSessionManager.Register(service);
        _service = service;
        _service.StateChanged += OnStateChanged;
        _service.Error += OnError;
    }

    /// <summary>联机前确保使用 FusionSessionService（单人模式后切换回来）。</summary>
    private void EnsureFusionService()
    {
#if FUSION_PRESENT
        if (!(_service is DoNotForgetMe.Network.Fusion.FusionSessionService))
        {
            var fusion = FindAnyObjectByType<DoNotForgetMe.Network.Fusion.FusionSessionService>();
            if (fusion != null)
            {
                UseService(fusion);
            }
        }
#endif
    }

    private void OnSinglePlayerClicked()
    {
        SetJoinInputVisible(false);
        HostSaveContext.Clear();

        UseService(new DoNotForgetMe.Network.Local.LocalDebugService());

        SetStatus("正在进入单人模式…");
        SetConnectingControls(true);
        _service.StartHost("SOLO");
    }

    private void OnCreateClicked()
    {
        SetJoinInputVisible(false);

        if (HostSaveService.Exists() && !_awaitNewGameOverwriteConfirm)
        {
            _awaitNewGameOverwriteConfirm = true;
            SetStatus("已有进度。再次点击“创建会话”将开始新游戏并覆盖存档。");
            return;
        }

        if (_awaitNewGameOverwriteConfirm)
        {
            HostSaveService.Delete();
            _awaitNewGameOverwriteConfirm = false;
            if (continueButton != null) continueButton.gameObject.SetActive(false);
        }

        HostSaveContext.Clear();
        StartHost();
    }

    private void OnContinueClicked()
    {
        if (!HostSaveService.TryLoad(out var save))
        {
            SetStatus("存档无法读取，请创建新会话开始游戏。");
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            return;
        }

        HostSaveContext.SetPending(save);
        StartHost();
    }

    private void StartHost()
    {
        EnsureFusionService();

        _generatedRoomCode = RoomCodeGenerator.Generate();
        if (roomCodeDisplay != null)
        {
            roomCodeDisplay.text = _generatedRoomCode;
        }

        SetStatus("房间码已生成，等待对方加入会话…");
        SetConnectingControls(true);

        _service.StartHost(_generatedRoomCode);
    }

    private void OnJoinClicked()
    {
        if (!_joinInputVisible)
        {
            SetJoinInputVisible(true);
            SetStatus("请输入房间码，再次点击「加入会话」或按回车加入");
            return;
        }

        var code = RoomCodeGenerator.Normalize(roomCodeInput != null ? roomCodeInput.text : string.Empty);

        if (!RoomCodeGenerator.IsValid(code))
        {
            SetStatus("房间码应为 4~6 位字母或数字（不含 0/O/1/I/L），请重新输入");
            return;
        }

        SetStatus("正在加入会话 " + code + " …");
        SetConnectingControls(true);

        EnsureFusionService();
        _service.StartClient(code);
    }

    /// <summary>输入框按回车时触发加入会话。</summary>
    private void OnRoomCodeInputEndEdit(string text)
    {
        if (!_joinInputVisible) return;
        // InputField.onEndEdit 在失焦和回车时都会触发，只在有输入内容时处理
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            OnJoinClicked();
        }
    }

    private void OnCancelClicked()
    {
        if (_service == null)
        {
            return;
        }

        // Host 在 Connected 状态等待 Client 加入时也应能取消
        if (_service.State != SessionState.Connecting && _service.State != SessionState.Connected)
        {
            return;
        }

        _isCancellationRequested = true;
        SetStatus("正在取消连接…");
        _service.Leave();
    }

    private void OnStateChanged(SessionState state)
    {
        switch (state)
        {
            case SessionState.Connecting:
                SetStatus("连接中…");
                SetConnectingControls(true);
                break;

            case SessionState.Connected:
                // Host：会话已创建，等待 Client 加入（房间码需保持可见）
                // Client：已加入会话，等待 Host 加载场景
                if (_service.Role == SessionRole.Host)
                {
                    SetStatus("会话已创建，将房间码告知对方，等待加入…");
                    // 保持连接中控件状态（取消按钮可见，创建/加入禁用）
                }
                else
                {
                    SetStatus("已加入会话，正在进入游戏…");
                    SetConnectingControls(false);
                }
                break;

            case SessionState.Disconnected:
                // 决策（ADR 0001 断线策略）：任一方断线即结束会话回主菜单。
                ClearGeneratedRoomCode();
                SetConnectingControls(false);

                if (_isCancellationRequested)
                {
                    SetStatus("已取消连接。可重新创建或加入会话。");
                    _isCancellationRequested = false;
                }
                else if (_lastState == SessionState.Connected)
                {
                    SetStatus("对方已断开，会话已结束。可重新创建或加入会话。");
                }
                else if (_lastState == SessionState.Connecting && !_hasPendingError)
                {
                    SetStatus("连接已结束。可重新创建或加入会话。");
                }
                _hasPendingError = false;
                break;
        }

        _lastState = state;
    }

    private void OnError(string message)
    {
        _hasPendingError = true;
        _isCancellationRequested = false;
        // 不清空房间码：让用户看到生成的码以便排障或重试
        SetStatus(message);
        SetConnectingControls(false);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log("[MainMenu] " + message);
    }

    private void SetConnectingControls(bool isConnecting)
    {
        if (createButton != null) createButton.interactable = !isConnecting;
        if (joinButton != null) joinButton.interactable = !isConnecting;
        if (cancelButton != null) cancelButton.gameObject.SetActive(isConnecting);
        if (isConnecting) SetJoinInputVisible(false);
    }

    private void SetJoinInputVisible(bool visible)
    {
        _joinInputVisible = visible;
        if (roomCodeInput != null)
        {
            roomCodeInput.gameObject.SetActive(visible);
            if (visible) roomCodeInput.ActivateInputField();
        }
    }

    private void ClearGeneratedRoomCode()
    {
        _generatedRoomCode = string.Empty;
        if (roomCodeDisplay != null)
        {
            roomCodeDisplay.text = string.Empty;
        }
    }
}
