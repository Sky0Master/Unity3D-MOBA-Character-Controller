using System;
using UnityEngine;
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
    [SerializeField] private string _gameSceneName = "GameScene"; // 【新增】游戏战斗场景名称

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
        SteamMatchmaking.OnLobbyCreated += OnLobbyCreatedCallback;
        SteamMatchmaking.OnLobbyEntered += OnLobbyEnteredCallback;
        SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoinedCallback;
        SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberLeftCallback;
        SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeftCallback;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequestedCallback;

        // 监听 FishNet 连接状态（用于排错）
        if (_networkManager != null)
        {
            _networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
        }
    }

    private void OnDisable()
    {
        SteamMatchmaking.OnLobbyCreated -= OnLobbyCreatedCallback;
        SteamMatchmaking.OnLobbyEntered -= OnLobbyEnteredCallback;
        SteamMatchmaking.OnLobbyMemberJoined -= OnLobbyMemberJoinedCallback;
        SteamMatchmaking.OnLobbyMemberDisconnected -= OnLobbyMemberLeftCallback;
        SteamMatchmaking.OnLobbyMemberLeave -= OnLobbyMemberLeftCallback;
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequestedCallback;

        if (_networkManager != null)
        {
            _networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
        }
    }

    // --- 核心方法 ---

    public async void CreateLobby()
    {
        Debug.Log("正在请求 Steam 创建大厅...");
        var lobbyResult = await SteamManager.Instance.CreateLobbyAsync(10);
        if (!lobbyResult.HasValue)
        {
            Debug.LogError("Steam 大厅创建失败！");
        }
    }

    public void LeaveLobbyAndDisconnect()
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

        UnityEngine.SceneManagement.SceneManager.LoadScene(_mainMenuSceneName);
    }

    // 【新增】开始游戏，加载战斗场景
    public void StartGame()
    {
        if (!_networkManager.IsServer) return;

        Debug.Log("房主启动游戏，正在广播加载 GameScene...");
        SceneLoadData sld = new SceneLoadData(_gameSceneName);
        sld.ReplaceScenes = ReplaceOption.All; // 卸载大厅场景，加载游戏场景
        _networkManager.SceneManager.LoadGlobalScenes(sld);
    }

    // --- Steam 回调处理 ---

    private void OnLobbyCreatedCallback(Result result, Lobby lobby)
    {
        if (result != Result.OK)
        {
            Debug.LogError($"大厅创建失败，错误码: {result}");
            return;
        }

        Debug.Log("Steam 大厅创建成功，正在启动 FishNet 服务端...");
        CurrentLobby = lobby;
        
        lobby.SetPublic();
        lobby.SetJoinable(true);

        // 监听服务端启动成功事件，必须等服务端 Started 才能加载全局场景
        _networkManager.ServerManager.OnServerConnectionState += OnServerStartedLoadScene;

        // 作为房主，启动 FishNet 的 Host (Server + Client)
        _networkManager.ServerManager.StartConnection();
        _networkManager.ClientManager.StartConnection();
    }

    private void OnServerStartedLoadScene(ServerConnectionStateArgs args)
    {
        // 确保服务端真的启动成功了
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            _networkManager.ServerManager.OnServerConnectionState -= OnServerStartedLoadScene;
            
            Debug.Log("FishNet 服务端已就绪，正在广播加载 LobbyScene...");
            SceneLoadData sld = new SceneLoadData(_lobbySceneName);
            sld.ReplaceScenes = ReplaceOption.All;
            _networkManager.SceneManager.LoadGlobalScenes(sld);
        }
    }

    private void OnLobbyEnteredCallback(Lobby lobby)
    {
        CurrentLobby = lobby;
        Debug.Log($"成功进入 Steam 大厅: {lobby.Id}");

        ulong mySteamId = SteamManager.Instance.GetMySteamId();

        // 如果我不是房主，说明我是加入者
        if (lobby.Owner.Id != mySteamId)
        {
            // 完美解决方案：大厅的 Owner 就是房主，直接获取它的 ID，不存在任何同步延迟！
            string hostSteamIdStr = lobby.Owner.Id.ToString();
            Debug.Log($"正在通过 FishNet 连接到房主: {hostSteamIdStr}");

            _networkManager.TransportManager.Transport.SetClientAddress(hostSteamIdStr);
            _networkManager.ClientManager.StartConnection();
        }
        
        // UI 更新
        OnPlayerJoinedLobbyUI?.Invoke(new Friend(mySteamId));
        foreach (var member in lobby.Members)
        {
            if (member.Id != mySteamId)
                OnPlayerJoinedLobbyUI?.Invoke(member);
        }
    }

    private void OnLobbyMemberJoinedCallback(Lobby lobby, Friend friend)
    {
        Debug.Log($"[Steam] 玩家 {friend.Name} 加入了大厅");
        OnPlayerJoinedLobbyUI?.Invoke(friend);
    }

    private void OnLobbyMemberLeftCallback(Lobby lobby, Friend friend)
    {
        Debug.Log($"[Steam] 玩家 {friend.Name} 离开了大厅");
        OnPlayerLeftLobbyUI?.Invoke(friend);
    }

    private async void OnGameLobbyJoinRequestedCallback(Lobby lobby, SteamId id)
    {
        Debug.Log("收到来自 Steam Overlay 的加入请求，正在进入...");
        await lobby.Join();
    }

    // --- FishNet 状态监控 ---

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
        Debug.Log($"[FishNet] 客户端连接状态改变: {args.ConnectionState}");
        if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            Debug.LogWarning("[FishNet] 客户端已断开或连接失败！如果一直卡在主菜单，说明 FishyFacepunch 握手失败。");
        }
    }
}