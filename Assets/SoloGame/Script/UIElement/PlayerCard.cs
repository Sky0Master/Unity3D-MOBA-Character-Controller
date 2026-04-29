using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ==========================================
// 挂载在 PlayerCard 预制体根节点上
// 管理单个玩家卡片的视觉表现
// ==========================================
public class PlayerCardUI : MonoBehaviour
{
    public TextMeshProUGUI NameText;
    public RawImage AvatarRaw;
    
    [Header("Ready State UI")]
    public Image Background;       // 卡片底板
    public GameObject ReadyIcon;   // 准备完毕的图标 (比如一个对勾)

    [Header("Colors")]
    [SerializeField] private Color _readyColor = Color.green;
    [SerializeField] private Color _unreadyColor = Color.red;

    public ulong SteamId { get; set; }

    // 更新卡片的准备状态视觉，现在只需要传入 isReady
    public void UpdateReadyState(bool isReady)
    {
        if (Background != null)
            Background.color = isReady ? _readyColor : _unreadyColor;
            
        if (ReadyIcon != null)
            ReadyIcon.SetActive(isReady);
    }
}