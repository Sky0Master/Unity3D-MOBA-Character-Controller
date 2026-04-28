using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using Steamworks;
using Steamworks.Data;

// ==========================================
// 核心大厅管理器 (建议挂载在独立的 NetworkManager 或 Bootstrap 场景中的单例上)
// 负责与 Steam Matchmaking 和 FishNet Transport 交互
// ==========================================
public class NetworkLobbyManager : MonoBehaviour
{
    public static NetworkLobbyManager Instance { get; private set; }

    [Header("FishNet Dependencies")]
    [SerializeField] private NetworkManager _networkManager;

    [Header("Scene Settings")]
    [SerializeField] private string _lobbySceneName = "LobbyScene";
    [SerializeField] private string _mainMenuSceneName = "MainMenuScene";

    // 记录当前所在的 Steam 大厅
    public Lobby? CurrentLobby { get; private set; }
    
    // 事件：当有玩家（包括自己）进入大厅房间时触发，供 UI 更新使用
    public static event Action<Friend> OnPlayerJoinedLobbyUI;
    public static event Action<Friend> OnPlayerLeftLobbyUI;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        // 注册 Steam Matchmaking 回调
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreatedCallback;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEnteredCallback;
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoinedCallback;
        SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberLeftCallback;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeftCallback;
        
        // 注册好友通过 Steam Overlay 加入游戏的回调
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequestedCallback;
    }

    private void OnDisable()
    {
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreatedCallback;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEnteredCallback;
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoinedCallback;
        SteamMatchmaking.OnLobbyMemberDisconnected -= OnLobbyMemberLeftCallback;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeftCallback;
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequestedCallback;
    }

    // --- 核心方法 ---

    public async void CreateLobby()
    {
        Debug.Log("正在创建 Steam 大厅...");
        // 调用封装好的接口
        var lobbyResult = await SteamManager.Instance.CreateLobbyAsync(10);
        if (!lobbyResult.HasValue)
        {
            Debug.LogError("创建大厅失败！");
            return;
        }
        // 回调 OnLobbyCreatedCallback 会被触发
    }

    public async void LeaveLobbyAndDisconnect()
    {
        if (CurrentLobby.HasValue)
        {
            CurrentLobby.Value.Leave();
            CurrentLobby = null;
        }

        if (_networkManager.IsServer)
            _networkManager.ServerManager.StopConnection(true);
        if (_networkManager.IsClient)
            _networkManager.ClientManager.StopConnection();

        // 加载回主菜单，显式指明使用 UnityEngine 的 SceneManager
        UnityEngine.SceneManagement.SceneManager.LoadScene(_mainMenuSceneName);
    }

    // --- Steam 回调处理 ---

    private void OnLobbyCreatedCallback(Result result, Lobby lobby)
    {
        if (result != Result.OK)
        {
            Debug.LogError($"大厅创建失败，错误码: {result}");
            return;
        }

        Debug.Log("大厅创建成功，正在设置属性...");
        CurrentLobby = lobby;
        
        lobby.SetPublic();
        lobby.SetJoinable(true);
        lobby.SetData("HostSteamID", SteamManager.Instance.GetMySteamId().ToString());

        // 【新增】：监听服务端启动状态，确保服务器起来了再加载场景
        _networkManager.ServerManager.OnServerConnectionState += OnServerStartedLoadScene;

        // 作为房主，启动 FishNet 的 Host (Server + Client)
        _networkManager.ServerManager.StartConnection();
        _networkManager.ClientManager.StartConnection();
    }

    // 【新增】：服务端启动成功后的回调方法
    private void OnServerStartedLoadScene(ServerConnectionStateArgs args)
    {
        // 当服务端状态变为 Started 时
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            // 立刻注销事件，防止后续重复触发
            _networkManager.ServerManager.OnServerConnectionState -= OnServerStartedLoadScene;
            
            Debug.Log("FishNet 服务端已启动，开始加载大厅场景...");
            SceneLoadData sld = new SceneLoadData(_lobbySceneName);
            sld.ReplaceScenes = ReplaceOption.All;
            _networkManager.SceneManager.LoadGlobalScenes(sld);
        }
    }
    private void OnLobbyEnteredCallback(Lobby lobby)
    {
        CurrentLobby = lobby;
        Debug.Log($"成功进入大厅: {lobby.Id}");

        // 获取本地 SteamID
        ulong mySteamId = SteamManager.Instance.GetMySteamId();

        // 如果我不是房主，说明我是加入者
        if (lobby.Owner.Id != mySteamId)
        {
            string hostSteamIdStr = lobby.GetData("HostSteamID");
            if (ulong.TryParse(hostSteamIdStr, out ulong hostSteamId))
            {
                // 使用 FishNet 抽象的 Transport 接口，解耦特定的 FishyFacepunch
                _networkManager.TransportManager.Transport.SetClientAddress(hostSteamId.ToString());
                _networkManager.ClientManager.StartConnection();
                // 注意：非房主不需要手动 LoadScene，FishNet 会自动同步场景
            }
        }
        
        // UI 更新：先把自己加进列表，然后再遍历已有成员
        OnPlayerJoinedLobbyUI?.Invoke(new Friend(mySteamId));
        foreach (var member in lobby.Members)
        {
            if (member.Id != mySteamId)
                OnPlayerJoinedLobbyUI?.Invoke(member);
        }
    }

    private void OnLobbyMemberJoinedCallback(Lobby lobby, Friend friend)
    {
        Debug.Log($"玩家 {friend.Name} 加入了大厅");
        OnPlayerJoinedLobbyUI?.Invoke(friend);
    }

    private void OnLobbyMemberLeftCallback(Lobby lobby, Friend friend)
    {
        Debug.Log($"玩家 {friend.Name} 离开了大厅");
        OnPlayerLeftLobbyUI?.Invoke(friend);
    }

    // 当你在 Steam 好友列表里右键朋友点击“加入游戏”时触发
    private async void OnGameLobbyJoinRequestedCallback(Lobby lobby, SteamId id)
    {
        Debug.Log("收到来自 Steam Overlay 的加入请求...");
        await lobby.Join();
    }
}