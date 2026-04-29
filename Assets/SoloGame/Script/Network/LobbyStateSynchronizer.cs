using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;

// ==========================================
// 挂载在 LobbyScene 中的一个空物体上，并添加 NetworkObject 组件
// 用于在所有客户端之间同步“准备状态”
// ==========================================
public class LobbyStateSynchronizer : NetworkBehaviour
{
    public static LobbyStateSynchronizer Instance;

    // 修复报错：FishNet v4 移除了 [SyncObject] 特性，直接声明 readonly 即可
    public readonly SyncDictionary<ulong, bool> PlayerReadyStates = new SyncDictionary<ulong, bool>();

    public static event Action OnReadyStatesUpdated;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnStartNetwork()
    {
        PlayerReadyStates.OnChange += PlayerReadyStates_OnChange;
    }

    private void PlayerReadyStates_OnChange(SyncDictionaryOperation op, ulong key, bool value, bool asServer)
    {
        OnReadyStatesUpdated?.Invoke();
    }

    // 任何客户端都可以调用此方法向服务器汇报自己的准备状态
    [ServerRpc(RequireOwnership = false)]
    public void CmdSetReady(ulong steamId, bool isReady)
    {
        PlayerReadyStates[steamId] = isReady;
    }

    [ServerRpc(RequireOwnership = false)]
    public void CmdRemovePlayer(ulong steamId)
    {
        if (PlayerReadyStates.ContainsKey(steamId))
        {
            PlayerReadyStates.Remove(steamId);
        }
    }
}