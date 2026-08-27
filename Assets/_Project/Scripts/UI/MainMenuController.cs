using System;
using DoNotForgetMe.Network;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主菜单控制器：纯联机入口（ADR 0001 决策——无单机模式）。
/// 职责：
/// 1. 「创建房间」：生成房间码并作为 Host 启动会话，把房间码大字展示给玩家以便口头告知对方；
/// 2. 「加入房间」：校验输入的房间码（4~6 位无歧义字符）并作为 Client 加入；
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
    [SerializeField] private Text statusText;

    [Header("Host 侧房间码展示")]
    [Tooltip("创建房间后大字展示的房间码文本（Host 需念给对方听）")]
    [SerializeField] private Text roomCodeDisplay;

    private SessionState _lastState = SessionState.Disconnected;
    private string _generatedRoomCode;

    private void Start()
    {
        var service = NetworkSessionManager.Service;

        service.StateChanged += OnStateChanged;
        service.Error += OnError;

        createButton.onClick.AddListener(OnCreateClicked);
        joinButton.onClick.AddListener(OnJoinClicked);

        if (roomCodeDisplay != null)
        {
            roomCodeDisplay.text = string.Empty;
        }

        SetStatus(service.IsAvailable
            ? "输入对方给的房间码加入，或创建一个新房间"
            : "网络层未安装：主菜单可预览，联机需先导入 Photon Fusion SDK（docs/install-fusion.md）");
    }

    private void OnDestroy()
    {
        var service = NetworkSessionManager.Service;
        service.StateChanged -= OnStateChanged;
        service.Error -= OnError;
    }

    private void OnCreateClicked()
    {
        _generatedRoomCode = RoomCodeGenerator.Generate();
        if (roomCodeDisplay != null)
        {
            roomCodeDisplay.text = _generatedRoomCode;
        }

        SetStatus("房间码已生成，等待对方加入…");
        SetButtonsEnabled(false);

        NetworkSessionManager.Service.StartHost(_generatedRoomCode);
    }

    private void OnJoinClicked()
    {
        var code = RoomCodeGenerator.Normalize(roomCodeInput != null ? roomCodeInput.text : string.Empty);

        if (!RoomCodeGenerator.IsValid(code))
        {
            SetStatus("房间码应为 4~6 位字母或数字（不含 0/O/1/I/L），请重新输入");
            return;
        }

        SetStatus("正在加入房间 " + code + " …");
        SetButtonsEnabled(false);

        NetworkSessionManager.Service.StartClient(code);
    }

    private void OnStateChanged(SessionState state)
    {
        switch (state)
        {
            case SessionState.Connecting:
                SetStatus("连接中…");
                break;

            case SessionState.Connected:
                // 双端就绪。Host 侧继续展示房间码；场景切换由网络层负责。
                SetStatus("已连接！正在进入游戏…");
                break;

            case SessionState.Disconnected:
                // 决策（ADR 0001 断线策略）：任一方断线即结束会话回主菜单。
                if (_lastState == SessionState.Connected)
                {
                    SetStatus("对方已断开，会话已结束。可重新创建或加入房间。");
                    SetButtonsEnabled(true);
                }
                else if (_lastState == SessionState.Connecting)
                {
                    // 连接失败（非中途断线），恢复按钮。具体原因走 OnError。
                    SetButtonsEnabled(true);
                }
                break;
        }

        _lastState = state;
    }

    private void OnError(string message)
    {
        SetStatus(message);
        SetButtonsEnabled(true);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log("[MainMenu] " + message);
    }

    private void SetButtonsEnabled(bool enabled)
    {
        if (createButton != null) createButton.interactable = enabled;
        if (joinButton != null) joinButton.interactable = enabled;
    }
}
