using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

// ==========================================
// 挂载在 LobbyScene 的准备/开始按钮上
// 负责封装自身的视觉状态切换逻辑（颜色、文字、图标）
// ==========================================
public class ReadyButton : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button _button;
    [SerializeField] private TextMeshProUGUI _buttonText;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private GameObject _readyIcon; // 准备好的图标 (如打钩)

    [Header("Colors")]
    [SerializeField] private Color _readyColor = Color.green;
    [SerializeField] private Color _unreadyColor = Color.red;
    [SerializeField] private Color _disabledColor = Color.gray;

    /// <summary>
    /// 初始化按钮并绑定点击事件
    /// </summary>
    public void Setup(UnityAction onClickAction)
    {
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(onClickAction);
    }

    /// <summary>
    /// 设置为房主的“开始游戏”模式
    /// </summary>
    public void SetAsHostStartButton(bool canStart)
    {
        _buttonText.text = "开始游戏";
        
        if (_readyIcon != null) 
            _readyIcon.SetActive(false);

        _button.interactable = canStart;
        _backgroundImage.color = canStart ? _readyColor : _disabledColor;
    }

    /// <summary>
    /// 设置为客户端的“准备/取消准备”模式
    /// </summary>
    public void SetAsClientReadyButton(bool isReady)
    {
        _buttonText.text = isReady ? "取消准备" : "准备";
        
        if (_readyIcon != null) 
            _readyIcon.SetActive(isReady);

        _button.interactable = true; // 客户端的按钮始终可以点击
        _backgroundImage.color = isReady ? _readyColor : _unreadyColor;
    }
}