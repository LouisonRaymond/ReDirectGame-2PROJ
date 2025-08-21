// Assets/Scripts/UI/PopupPanel.cs
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum PopupType { Info, Success, Warning, Error }

public class PopupPanel : MonoBehaviour
{
    [Header("Refs")]
    public CanvasGroup layer;      // sur PopupLayer
    public Image backdrop;         // Backdrop
    public RectTransform panel;    // PopupPanel
    public Image panelBG;          // PanelBG
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

    void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(Hide);
        gameObject.SetActive(false);
    }

    public void Show(string t, string b, PopupType type = PopupType.Info, float autoClose = 0f, System.Action onClosed = null)
    {
        _onClosed = onClosed;
        gameObject.SetActive(true);

        title.text = t;
        body.text  = b;

        // couleur d'accent simple via PanelBG
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
        // état initial
        layer.alpha = 0f;
        layer.blocksRaycasts = true;
        layer.interactable = true;

        panel.localScale = Vector3.one * 0.9f;

        // fade
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
}
