using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Cinemachine;

[RequireComponent(typeof(Collider))]
public class Selectable : MonoBehaviour
{
    public static event Action<string> onSelectObj;
    public static event Action<string> onDeselectObj;
    
    private static Selectable _currentSelected;
    private static readonly Dictionary<int, Selectable> _shortcuts = new Dictionary<int, Selectable>();

    private bool _isSelected;

    private void Start()
    {
        Deselect();
    }

    private void Update()
    {
        if (!_isSelected) return;

        // 快捷键设置
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
        {
            for (int i = 0; i < 10; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
                {
                    SetShortcut(i);
                    break;
                }
            }
        }
        // 快捷键触发
        else
        {
            CheckShortcutPress();
        }
    }

    private void OnMouseDown()
    {
        Select();
    }

    private void OnDestroy()
    {
        if (_currentSelected == this) _currentSelected = null;
        RemoveFromShortcuts();
    }

    public void Select()
    {
        if (_currentSelected == this) return;

        // 取消之前选中的对象
        if (_currentSelected != null)
        {
            _currentSelected.Deselect();
        }

        // 设置新的选中对象
        _currentSelected = this;
        _isSelected = true;
        onSelectObj?.Invoke(gameObject.name);
        
        GetComponentInChildren<Outline>().enabled = true;
        GetComponent<UnitController>().inControl = true;
        FindAnyObjectByType<CinemachineCamera>().Target.TrackingTarget = transform;
    }

    private void Deselect()
    {
        _isSelected = false;
        onDeselectObj?.Invoke(gameObject.name);
        
        GetComponentInChildren<Outline>().enabled = false;
        GetComponent<UnitController>().inControl = false;
    }

    private void SetShortcut(int number)
    {
        if (_shortcuts.ContainsKey(number))
        {
            _shortcuts[number] = this;
        }
        else
        {
            _shortcuts.Add(number, this);
        }
    }

    private void CheckShortcutPress()
    {
        for (int i = 0; i < 10; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
            {
                if (_shortcuts.TryGetValue(i, out var selectable))
                {
                    selectable.Select();
                }
                return;
            }
        }
    }

    private void RemoveFromShortcuts()
    {
        List<int> keysToRemove = new List<int>();
        foreach (var pair in _shortcuts)
        {
            if (pair.Value == this)
            {
                keysToRemove.Add(pair.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            _shortcuts.Remove(key);
        }
    }
}