using System;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;
using Steamworks.Data;

// ==========================================
// Steam 核心管理器 (单例，跨场景存在)
// 封装所有 Steamworks 初始化、回调轮询和 API 调用
// ==========================================
public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance { get; private set; }

    [Header("Steam Settings")]
    public uint AppId = 480; // 默认使用 Spacewar 测试，正式上线前替换为你自己的 AppID

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeSteam();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeSteam()
    {
        try
        {
            SteamClient.Init(AppId, true);
            Debug.Log($"Steam 初始化成功! 当前用户: {SteamClient.Name}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Steam 初始化失败: {e.Message}");
        }
    }

    private void Update()
    {
        // 维持 Steam 回调心跳
        SteamClient.RunCallbacks();
    }

    private void OnApplicationQuit()
    {
        SteamClient.Shutdown();
    }

    // ==========================================
    // 封装的对外接口
    // ==========================================

    /// <summary>
    /// 获取本地玩家的 SteamID
    /// </summary>
    public ulong GetMySteamId()
    {
        return SteamClient.SteamId;
    }

    /// <summary>
    /// 获取本地玩家的昵称
    /// </summary>
    public string GetMyName()
    {
        return SteamClient.Name;
    }

    /// <summary>
    /// 打开 Steam 游戏内覆盖层 (好友列表)
    /// </summary>
    public void OpenFriendOverlay()
    {
        SteamFriends.OpenOverlay("friends");
    }

    /// <summary>
    /// 异步创建 Steam 大厅
    /// </summary>
    public async Task<Lobby?> CreateLobbyAsync(int maxMembers)
    {
        return await SteamMatchmaking.CreateLobbyAsync(maxMembers);
    }

    /// <summary>
    /// 异步获取指定 SteamID 的高清头像
    /// </summary>
    public async Task<Image?> GetLargeAvatarAsync(ulong steamId)
    {
        return await SteamFriends.GetLargeAvatarAsync(steamId);
    }
}