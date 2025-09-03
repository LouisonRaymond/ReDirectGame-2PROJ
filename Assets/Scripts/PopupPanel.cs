using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public enum PopupType { Info, Success, Warning, Error }

public class PopupPanel : MonoBehaviour
{
    [Header("Refs")]
    public CanvasGroup layer;      
    public Image backdrop;         
    public RectTransform panel;    
    public Image panelBG;          
    public TextMeshProUGUI title;
    public TextMeshProUGUI body;
    public Button closeButton;

    [Header("Colors")]
    public Color infoColor    = new Color(0.20f, 0.55f, 1.00f);
    public Color successColor = new Color(0.20f, 0.80f, 0.40f);
    public Color warnColor    = new Color(1.00f, 0.75f, 0.25f);
    public Color errorColor   = new Color(1.00f, 0.35f, 0.35f);

    Coroutine _anim;
    System.Action _onClosed;
    
             
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;
    
    
    [Header("Action Button")]
    public Button actionButton;          
    public TextMeshProUGUI actionLabel;  
    Action _onAction;
    float _autoClose;
    bool _showing;

    void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(Hide);
        gameObject.SetActive(false);
        if (actionButton) actionButton.onClick.AddListener(OnActionClicked);
        
        HideActionButton();
    }

    public void Show(string t, string b, PopupType type = PopupType.Info, float autoClose = 0f, System.Action onClosed = null)
    {
        _onClosed = onClosed;
        gameObject.SetActive(true);

        title.text = t;
        body.text  = b;
        
        HideActionButton();
        
        Color c = type switch
        {
            PopupType.Success => successColor,
            PopupType.Warning => warnColor,
            PopupType.Error   => errorColor,
            _                 => infoColor
        };
        if (panelBG) panelBG.color = c;

        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(CoFadeIn(autoClose));
    }

    public void Hide()
    {
        if (!gameObject.activeSelf) return;
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(CoFadeOut());
    }

    IEnumerator CoFadeIn(float autoClose)
    {
        layer.alpha = 0f;
        layer.blocksRaycasts = true;
        layer.interactable = true;

        panel.localScale = Vector3.one * 0.9f;
        
        float t = 0f;
        while (t < 0.15f)
        {
            t += Time.unscaledDeltaTime;
            float k = t / 0.15f;
            layer.alpha = Mathf.SmoothStep(0f, 1f, k);
            panel.localScale = Vector3.Lerp(Vector3.one * 0.9f, Vector3.one, k);
            yield return null;
        }
        layer.alpha = 1f; panel.localScale = Vector3.one;

        if (autoClose > 0f)
        {
            yield return new WaitForSecondsRealtime(autoClose);
            Hide();
        }
    }

    IEnumerator CoFadeOut()
    {
        float t = 0f;
        while (t < 0.12f)
        {
            t += Time.unscaledDeltaTime;
            float k = 1f - (t / 0.12f);
            layer.alpha = Mathf.Clamp01(k);
            panel.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.92f, 1f - k);
            yield return null;
        }

        layer.alpha = 0f;
        layer.blocksRaycasts = false;
        layer.interactable = false;
        gameObject.SetActive(false);

        _onClosed?.Invoke();
        _onClosed = null;
        _anim = null;
    }
    
    public void ShowWithAction(string title, string msg, PopupType type,
        string actionText, Action onAction,
        Action onClosed)
    {
        _onClosed = onClosed;
        _onAction = onAction;
        _autoClose = 0f; 
        _showing = true;

        if (titleText) titleText.text = title;
        if (bodyText)  bodyText.text  = msg;

        if (actionButton)
        {
            actionButton.gameObject.SetActive(true);
            if (actionLabel) actionLabel.text = actionText;
        }

        gameObject.SetActive(true);
        layer.alpha = 1f;
        layer.blocksRaycasts = true;
        layer.interactable = true;

        StopAllCoroutines();
    }

    System.Collections.IEnumerator AutoClose(float t)
    {
        yield return new WaitForSecondsRealtime(t);
        Close();
    }

    void OnActionClicked()
    {
        var cb = _onAction; 
        Close();
        cb?.Invoke();
    }

    public void Close()
    {
        if (!_showing) return;
        _showing = false;
        
        HideActionButton();

        layer.blocksRaycasts = false;
        layer.interactable = false;
        gameObject.SetActive(false);

        var cb = _onClosed; _onClosed = null; _onAction = null;
        cb?.Invoke();
    }
    
    void HideActionButton()
    {
        if (actionButton) actionButton.gameObject.SetActive(false);
        _onAction = null;
    }
}
