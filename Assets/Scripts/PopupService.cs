using UnityEngine;
using System;
using System.Collections.Generic;

public class PopupService : MonoBehaviour
{
    public static PopupService Instance { get; private set; }

    [SerializeField] private PopupPanel popup;   
    
    struct Req {
        public string title, msg, actionText;
        public PopupType type;
        public float autoClose;
        public Action onClosed, onAction;
        public bool hasAction;
    }

    Queue<Req> _queue = new();
    bool _showing;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (popup) popup.gameObject.SetActive(false);
    }

    public bool IsOpen => popup && popup.gameObject.activeSelf;
    
    public void Show(string title, string msg, PopupType type = PopupType.Info,
        float autoClose = 0f, bool replaceIfOpen = true)
    {
        if (!popup) return;

        if (!IsOpen || replaceIfOpen)
        {
            popup.Show(title, msg, type, autoClose); 
        }
        
    }

    public void Hide()
    {
        if (IsOpen) popup.Hide();
    }
    
    public void ShowWithAction(string title, string msg, string actionText, Action onAction,
        PopupType type = PopupType.Info)
    {
        Enqueue(new Req{ title=title, msg=msg, type=type, actionText=actionText, onAction=onAction, hasAction=true });
        TryShowNext();
    }
    
    void Enqueue(Req r) => _queue.Enqueue(r);

    void TryShowNext()
    {
        if (_showing || popup == null) return;
        if (_queue.Count == 0) return;

        var r = _queue.Dequeue();
        _showing = true;

        if (r.hasAction)
            popup.ShowWithAction(r.title, r.msg, r.type, r.actionText, () => {
                _showing = false;
                r.onAction?.Invoke();
                TryShowNext();
            }, OnClosed);
        else
            popup.Show(r.title, r.msg, r.type, r.autoClose, OnClosed);
    }

    void OnClosed()
    {
        _showing = false;
        TryShowNext();
    }
    
    public void Info(string t, string m, float auto = 0f, bool replaceIfOpen = true)
        => Show(t, m, PopupType.Info,    auto, replaceIfOpen);

    public void Success(string t, string m, float auto = 0f, bool replaceIfOpen = true)
        => Show(t, m, PopupType.Success, auto, replaceIfOpen);

    public void Warn(string t, string m, float auto = 0f, bool replaceIfOpen = true)
        => Show(t, m, PopupType.Warning, auto, replaceIfOpen);

    public void Error(string t, string m, float auto = 0f, bool replaceIfOpen = true)
        => Show(t, m, PopupType.Error,   auto, replaceIfOpen);
}
