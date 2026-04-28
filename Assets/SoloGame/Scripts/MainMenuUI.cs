using System;
using UnityEngine;
using UnityEngine.UI;

// ==========================================
// 主菜单 UI 控制器 (挂载在 MainMenuScene 的 UI Canvas 上)
// ==========================================
public class MainMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _btnCreateRoom;
    [SerializeField] private Button _btnJoinRoom;
    [SerializeField] private Button _btnSettings;
    [SerializeField] private Button _btnQuit;

    private void Start()
    {
        _btnCreateRoom.onClick.AddListener(OnCreateRoomClicked);
        _btnJoinRoom.onClick.AddListener(OnJoinRoomClicked);
        _btnSettings.onClick.AddListener(OnSettingsClicked);
        _btnQuit.onClick.AddListener(OnQuitClicked);
    }

    private void OnCreateRoomClicked()
    {
        _btnCreateRoom.interactable = false; // 防抖
        NetworkLobbyManager.Instance.CreateLobby();
    }

    private void OnJoinRoomClicked()
    {
        // 唤起 Steam 原生的好友列表/联机面板让玩家选择
        SteamManager.Instance.OpenFriendOverlay();
    }

    private void OnSettingsClicked()
    {
        Debug.Log("打开设置面板");
        // TODO: 显示设置 UI
    }

    private void OnQuitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}