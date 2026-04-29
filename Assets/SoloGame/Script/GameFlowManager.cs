using UnityEngine;
using FishNet.Object;
using FishNet.Connection;
using System.Collections.Generic;

// ==========================================
// 挂载在 GameScene 中的流程控制物体上，必须带有 NetworkObject 组件
// 负责在游戏开始时生成玩家模型
// ==========================================
public class GameFlowManager : NetworkBehaviour
{
    [Header("Player Spawning")]
    [SerializeField] private NetworkObject _playerPrefab; // 你的玩家角色预制体 (必须带 NetworkObject)
    [SerializeField] private Vector2 _spawnAreaSize = new Vector2(20f, 20f); // XZ 平面生成的区域大小
    [SerializeField] private Vector3 _spawnCenter = Vector3.zero; // 区域中心点

    // 当服务器加载完这个场景，并在该场景中激活了此 NetworkBehaviour 时触发
    public override void OnStartServer()
    {
        base.OnStartServer();
        SpawnAllPlayers();
    }

    private void SpawnAllPlayers()
    {
        if (_playerPrefab == null)
        {
            Debug.LogError("未配置 Player 预制体！");
            return;
        }

        // 遍历当前所有连入服务器的客户端
        foreach (NetworkConnection conn in ServerManager.Clients.Values)
        {
            // 计算随机生成点 (y 固定为 0)
            float randomX = _spawnCenter.x + UnityEngine.Random.Range(-_spawnAreaSize.x / 2f, _spawnAreaSize.x / 2f);
            float randomZ = _spawnCenter.z + UnityEngine.Random.Range(-_spawnAreaSize.y / 2f, _spawnAreaSize.y / 2f);
            Vector3 spawnPos = new Vector3(randomX, 0f, randomZ);

            // 实例化预制体
            NetworkObject playerInstance = Instantiate(_playerPrefab, spawnPos, Quaternion.identity);
            
            // 在网络上生成，并赋予对应的客户端控制权 (Ownership)
            ServerManager.Spawn(playerInstance, conn);
            
            Debug.Log($"已为客户端 {conn.ClientId} 在 {spawnPos} 生成玩家对象。");
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 在编辑器里画一个青色的框，方便可视化查看生成区域
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(_spawnCenter, new Vector3(_spawnAreaSize.x, 0.1f, _spawnAreaSize.y));
    }
}