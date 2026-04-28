using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Steamworks;
using Steamworks.Data;

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

    // 缓存生成的 Card 字典，方便根据 SteamID 移除
    private Dictionary<ulong, GameObject> _spawnedCards = new Dictionary<ulong, GameObject>();

    private void Start()
    {
        _btnLeave.onClick.AddListener(() => NetworkLobbyManager.Instance.LeaveLobbyAndDisconnect());
        
        _btnInviteFriends.onClick.AddListener(() => {
            SteamManager.Instance.OpenFriendOverlay();
        });

        // 注册事件
        NetworkLobbyManager.OnPlayerJoinedLobbyUI += AddPlayerCard;
        NetworkLobbyManager.OnPlayerLeftLobbyUI += RemovePlayerCard;
        
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
    }

    private async void AddPlayerCard(Friend friend)
    {
        if (_spawnedCards.ContainsKey(friend.Id)) return; // 避免重复生成

        GameObject cardObj = Instantiate(_playerCardPrefab, _playerCardContainer);
        _spawnedCards.Add(friend.Id, cardObj);

        // 查找子节点组件 (根据你的需求，通过名称获取)
        Transform nameTransform = cardObj.transform.Find("Name");
        Transform avatarTransform = cardObj.transform.Find("Avatar");

        if (nameTransform != null && nameTransform.TryGetComponent(out TextMeshProUGUI nameText))
        {
            nameText.text = friend.Name;
        }

        if (avatarTransform != null && avatarTransform.TryGetComponent(out RawImage avatarImage))
        {
            // 异步调用 SteamManager 封装的接口获取头像
            var image = await SteamManager.Instance.GetLargeAvatarAsync(friend.Id);
            if (image.HasValue)
            {
                Texture2D tex = GetTextureFromSteamImage(image.Value);
                avatarImage.texture = tex;
            }
        }
    }

    private void RemovePlayerCard(Friend friend)
    {
        if (_spawnedCards.TryGetValue(friend.Id, out GameObject cardObj))
        {
            Destroy(cardObj);
            _spawnedCards.Remove(friend.Id);
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