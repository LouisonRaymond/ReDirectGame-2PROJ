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

    private LevelIO.LevelInfo _info;
    private Action<LevelIO.LevelInfo> _onEdit;
    private Action<LevelIO.LevelInfo> _onDelete;

    public void Init(LevelIO.LevelInfo info, Action<LevelIO.LevelInfo> onEdit, Action<LevelIO.LevelInfo> onDelete)
    {
        _info = info; _onEdit = onEdit; _onDelete = onDelete;

        if (nameText) nameText.text = info.name;
        if (dateText) dateText.text = info.modified.ToString("yyyy-MM-dd  HH:mm");

        if (btnEdit) btnEdit.onClick.AddListener(() => _onEdit?.Invoke(_info));
        if (btnDelete) btnDelete.onClick.AddListener(() => _onDelete?.Invoke(_info));
    }
}

