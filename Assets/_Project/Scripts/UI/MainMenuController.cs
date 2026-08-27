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
    [SerializeField] private Button continueButton;
    [SerializeField] private Button cancelButton;
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

    private void Start()
    {
        _service = NetworkSessionManager.Service;

        _service.StateChanged += OnStateChanged;
        _service.Error += OnError;

        if (createButton != null) createButton.onClick.AddListener(OnCreateClicked);
        if (joinButton != null) joinButton.onClick.AddListener(OnJoinClicked);
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClicked);

        ClearGeneratedRoomCode();
        SetConnectingControls(false);
        RefreshContinueButton();

        SetStatus(_service.IsAvailable
            ? "输入对方给的房间码加入，或创建一个新会话"
            : "Fusion 未启用：主菜单可预览，联机需按 docs/install-fusion.md 导入 SDK 并添加 FUSION_PRESENT");
    }

    private void OnDestroy()
    {
        if (_service != null)
        {
            _service.StateChanged -= OnStateChanged;
            _service.Error -= OnError;
        }
    }

    private void OnCreateClicked()
    {
        if (HostSaveService.Exists() && !_awaitNewGameOverwriteConfirm)
        {
            _awaitNewGameOverwriteConfirm = true;
            SetStatus("已有 Host 侧存档。再次点击创建会话会覆盖继续游戏进度。");
            return;
        }

        _awaitNewGameOverwriteConfirm = false;
        HostSaveService.Delete();
        HostSaveContext.Clear();
        RefreshContinueButton();

        StartHostWithNewRoomCode();
    }

    private void OnJoinClicked()
    {
        _awaitNewGameOverwriteConfirm = false;

        var code = RoomCodeGenerator.Normalize(roomCodeInput != null ? roomCodeInput.text : string.Empty);

        if (!RoomCodeGenerator.IsValid(code))
        {
            SetStatus("房间码应为 4~6 位字母或数字（不含 0/O/1/I/L），请重新输入");
            return;
        }

        SetStatus("正在加入会话 " + code + " …");
        SetConnectingControls(true);

        _service.StartClient(code);
    }

    private void OnContinueClicked()
    {
        _awaitNewGameOverwriteConfirm = false;

        if (!HostSaveService.TryLoad(out var save))
        {
            SetStatus("没有可继续的 Host 存档。");
            RefreshContinueButton();
            return;
        }

        HostSaveContext.SetPending(save);
        StartHostWithNewRoomCode("已加载 Host 存档，新房间码已生成，等待对方加入会话…");
    }

    private void StartHostWithNewRoomCode(string status = "房间码已生成，等待对方加入会话…")
    {
        _generatedRoomCode = RoomCodeGenerator.Generate();
        if (roomCodeDisplay != null)
        {
            roomCodeDisplay.text = _generatedRoomCode;
        }

        SetStatus(status);
        SetConnectingControls(true);

        _service.StartHost(_generatedRoomCode);
    }

    private void OnCancelClicked()
    {
        if (_service == null || _service.State != SessionState.Connecting)
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
                // 双端就绪。Host 侧继续展示房间码；场景切换由网络层负责。
                SetStatus("已连接！正在进入游戏…");
                SetConnectingControls(false);
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
        _awaitNewGameOverwriteConfirm = false;
        ClearGeneratedRoomCode();
        SetStatus(message);
        SetConnectingControls(false);
        RefreshContinueButton();
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
        if (continueButton != null) continueButton.interactable = !isConnecting;
        if (cancelButton != null) cancelButton.gameObject.SetActive(isConnecting);
    }

    private void RefreshContinueButton()
    {
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(HostSaveService.Exists());
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
