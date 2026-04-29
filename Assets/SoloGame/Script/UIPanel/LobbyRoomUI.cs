using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;

// ==========================================
// 大厅房间 UI 控制器 (挂载在 LobbyScene 的 UI Canvas 上)
// ==========================================
public class LobbyRoomUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform _playerCardContainer; // 生成 PlayerCard 的父节点
    [SerializeField] private GameObject _playerCardPrefab;   // PlayerCard 预制体
    [SerializeField] private Button _btnLeave;               // 退出房间按钮
    [SerializeField] private Button _btnInviteFriends;       // 邀请好友按钮

     [Header("Action Button")]
    [SerializeField] private ReadyButton _readyButton;

    // 缓存生成的 Card 字典，方便根据 SteamID 移除 (类型改为 PlayerCardUI)
    private Dictionary<ulong, PlayerCardUI> _spawnedCards = new Dictionary<ulong, PlayerCardUI>();
    
    // 客户端本地的准备状态
    private bool _isLocalReady = false;

    private void Start()
    {
        _btnLeave.onClick.AddListener(() => {
            if (LobbyStateSynchronizer.Instance != null)
                LobbyStateSynchronizer.Instance.CmdRemovePlayer(SteamManager.Instance.GetMySteamId());
            NetworkLobbyManager.Instance.LeaveLobbyAndDisconnect();
        });
        
        _btnInviteFriends.onClick.AddListener(() => {
            SteamManager.Instance.OpenFriendOverlay();
        });

        // 绑定操作按钮事件
        _readyButton.Setup(OnActionButtonClicked);

        // 注册事件
        NetworkLobbyManager.OnPlayerJoinedLobbyUI += AddPlayerCard;
        NetworkLobbyManager.OnPlayerLeftLobbyUI += RemovePlayerCard;
        LobbyStateSynchronizer.OnReadyStatesUpdated += RefreshUIStates; // 监听网络状态变化
        
        // 如果进入场景时大厅已经有数据了（比如作为房主刚创建完加载进来），主动拉取一次
        if (NetworkLobbyManager.Instance != null && NetworkLobbyManager.Instance.CurrentLobby.HasValue)
        {
            var lobby = NetworkLobbyManager.Instance.CurrentLobby.Value;
            foreach (var member in lobby.Members)
            {
                AddPlayerCard(member);
            }
        }
    }

    private void OnDestroy()
    {
        NetworkLobbyManager.OnPlayerJoinedLobbyUI -= AddPlayerCard;
        NetworkLobbyManager.OnPlayerLeftLobbyUI -= RemovePlayerCard;
        LobbyStateSynchronizer.OnReadyStatesUpdated -= RefreshUIStates;
    }

    // 【新增】处理准备/开始按钮的点击逻辑
    private void OnActionButtonClicked()
    {
        ulong mySteamId = SteamManager.Instance.GetMySteamId();
        bool isHost = NetworkLobbyManager.Instance.CurrentLobby.Value.Owner.Id == mySteamId;

        if (isHost)
        {
            // 房主点击：开始游戏
            NetworkLobbyManager.Instance.StartGame();
        }
        else
        {
            // 客户端点击：切换准备状态，并通知服务器
            _isLocalReady = !_isLocalReady;
            if (LobbyStateSynchronizer.Instance != null)
            {
                LobbyStateSynchronizer.Instance.CmdSetReady(mySteamId, _isLocalReady);
            }
            RefreshUIStates(); // 立即刷新本地 UI 表现
        }
    }

    // 【新增】根据网络状态统一刷新大厅 UI 表现
    private void RefreshUIStates()
    {
        if (!NetworkLobbyManager.Instance.CurrentLobby.HasValue) return;

        ulong mySteamId = SteamManager.Instance.GetMySteamId();
        bool isHost = NetworkLobbyManager.Instance.CurrentLobby.Value.Owner.Id == mySteamId;
        bool allClientsReady = true;

        // 1. 更新所有玩家卡片的视觉表现
        foreach (var kvp in _spawnedCards)
        {
            ulong steamId = kvp.Key;
            PlayerCardUI cardUI = kvp.Value;
            
            // 房主默认不需要准备，客户端需查看同步器的数据
            bool isThisPlayerHost = NetworkLobbyManager.Instance.CurrentLobby.Value.Owner.Id == steamId;
            bool isReady = isThisPlayerHost || (LobbyStateSynchronizer.Instance != null && 
                                                LobbyStateSynchronizer.Instance.PlayerReadyStates.TryGetValue(steamId, out bool r) && r);
            
            cardUI.UpdateReadyState(isReady);

            if (!isThisPlayerHost && !isReady)
            {
                allClientsReady = false;
            }
        }

        // 2. 更新右下角操作按钮表现
        if (isHost)
        {
            // 只有自己一个人，或者其他人都准备了，才可以点击
            bool canStart = _spawnedCards.Count == 1 || allClientsReady;
            _readyButton.SetAsHostStartButton(canStart);
        }
        else
        {
            _readyButton.SetAsClientReadyButton(_isLocalReady);
        }
    }

    private async void AddPlayerCard(Friend friend)
    {
        if (_spawnedCards.ContainsKey(friend.Id)) return; // 避免重复生成

        GameObject cardObj = Instantiate(_playerCardPrefab, _playerCardContainer);
        
        // 获取或挂载 PlayerCardUI
        PlayerCardUI cardUI = cardObj.GetComponent<PlayerCardUI>();
        if (cardUI == null) cardUI = cardObj.AddComponent<PlayerCardUI>();
        
        cardUI.SteamId = friend.Id;
        _spawnedCards.Add(friend.Id, cardUI);

        //获取名字
        ulong mySteamId = SteamManager.Instance.GetMySteamId();
        string playerName = (friend.Id == mySteamId) ? SteamManager.Instance.GetMyName() : friend.Name;
        cardUI.NameText.text = string.IsNullOrEmpty(playerName) ? "Unknown Player" : playerName;
        
        RefreshUIStates(); // 新玩家加入后刷新状态判定

        // 4. 异步调用 SteamManager 获取头像并赋值
        var image = await SteamManager.Instance.GetLargeAvatarAsync(friend.Id);
        if (image.HasValue)
        {
            Texture2D tex = GetTextureFromSteamImage(image.Value);
            cardUI.AvatarRaw.texture = tex;
        }
    }

    private void RemovePlayerCard(Friend friend)
    {
        if (_spawnedCards.TryGetValue(friend.Id, out PlayerCardUI cardUI))
        {
            Destroy(cardUI.gameObject);
            _spawnedCards.Remove(friend.Id);
            RefreshUIStates(); // 玩家离开后刷新状态判定 (可能剩下的都准备好了)
        }
    }

    // 辅助方法：将 Steam 的 Image 结构转换为 Unity 的 Texture2D
    private Texture2D GetTextureFromSteamImage(Steamworks.Data.Image image)
    {
        Texture2D texture = new Texture2D((int)image.Width, (int)image.Height, TextureFormat.RGBA32, false);
        texture.LoadRawTextureData(image.Data);
        texture.Apply();

        // Steam 头像数据是上下颠倒的，需要翻转
        UnityEngine.Color[] pixels = texture.GetPixels();
        UnityEngine.Color[] flippedPixels = new UnityEngine.Color[pixels.Length];
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                flippedPixels[y * texture.width + x] = pixels[(texture.height - 1 - y) * texture.width + x];
            }
        }
        texture.SetPixels(flippedPixels);
        texture.Apply();
        return texture;
    }
}