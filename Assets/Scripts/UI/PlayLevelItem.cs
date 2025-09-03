using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class PlayLevelItem : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] CanvasGroup cg;
    [SerializeField] TextMeshProUGUI label;

    [Header("Icons")]
    [SerializeField] Image icon;     
    [SerializeField] GameObject lockGO; 

    int _index;
    Action<int> _onClick;

    public void Init(int index, bool locked, Action<int> onClick)
    {
        _index = index;
        _onClick = onClick;

        if (label) label.text = $"Level {_index + 1}";

        if (button)
        {
            button.interactable = !locked;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => _onClick?.Invoke(_index));
        }

        if (cg) cg.alpha = locked ? 0.6f : 1f;

        
        if (icon)   icon.enabled = !locked;        
        if (lockGO) lockGO.SetActive(locked);      
    }

#if UNITY_EDITOR
    
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            if (icon)   icon.enabled = true;
            if (lockGO) lockGO.SetActive(false);
        }
    }
#endif
}