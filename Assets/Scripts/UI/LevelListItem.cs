using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelListItem : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI dateText;
    [SerializeField] Button btnEdit;
    [SerializeField] Button btnDelete;
    public Button playButton;

    private LevelIO.LevelInfo _info;
    private Action<LevelIO.LevelInfo> _onEdit;
    private Action<LevelIO.LevelInfo> _onDelete;
    Action<LevelIO.LevelInfo> _onPlay;

    public void Init(LevelIO.LevelInfo info, Action<LevelIO.LevelInfo> onEdit, Action<LevelIO.LevelInfo> onDelete, Action<LevelIO.LevelInfo> onPlay)
    {
        _info = info; _onEdit = onEdit; _onDelete = onDelete; _onPlay = onPlay;

        if (nameText) nameText.text = info.name;
        if (dateText) dateText.text = info.modified.ToString("yyyy-MM-dd  HH:mm");

        if (btnEdit) btnEdit.onClick.AddListener(() => _onEdit?.Invoke(_info));
        if (btnDelete) btnDelete.onClick.AddListener(() => _onDelete?.Invoke(_info));
        if (playButton)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() => _onPlay?.Invoke(_info));
        }
    }
}

