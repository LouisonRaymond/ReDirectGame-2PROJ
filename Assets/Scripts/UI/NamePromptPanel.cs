using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NamePromptPanel : MonoBehaviour
{
    [Header("Refs")]
    public CanvasGroup layer;         // Layer
    public RectTransform panel;       // Panel
    public TextMeshProUGUI title;     // Title
    public TMP_InputField input;      // Input
    public Button btnOk;              // BtnOk
    public Button btnCancel;          // BtnCancel

    System.Action<string> _onOk;
    System.Action _onCancel;

    void Awake()
    {
        if (btnOk)     btnOk.onClick.AddListener(ClickOk);
        if (btnCancel) btnCancel.onClick.AddListener(Hide);
        gameObject.SetActive(false);
    }

    public void Show(string titleText, string defaultName,
                     System.Action<string> onOk, System.Action onCancel = null)
    {
        _onOk = onOk; _onCancel = onCancel;

        if (title) title.text = titleText;
        if (input) {
            input.text = defaultName ?? "";
            input.onValueChanged.RemoveAllListeners();
            input.onValueChanged.AddListener(OnValueChanged);
            OnValueChanged(input.text);  
        }

        gameObject.SetActive(true);
        if (layer) { layer.alpha = 1; layer.blocksRaycasts = true; layer.interactable = true; }

        // focus
        input?.Select();
        input?.ActivateInputField();
    }

    void OnValueChanged(string s)
    {
        string clean = Sanitize(s);
        if (input && input.text != clean) {
            input.text = clean;
            input.caretPosition = clean.Length;
        }
        if (btnOk) btnOk.interactable = !string.IsNullOrWhiteSpace(clean);
    }

    public static string Sanitize(string s)
    {
        if (s == null) return "";
        foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s.Trim();
    }

    void ClickOk()
    {
        string name = Sanitize(input ? input.text : "");
        if (string.IsNullOrWhiteSpace(name)) return;
        var cb = _onOk;
        Hide();
        cb?.Invoke(name);
    }

    public void Hide()
    {
        if (layer) { layer.alpha = 0; layer.blocksRaycasts = false; layer.interactable = false; }
        gameObject.SetActive(false);
        _onCancel?.Invoke();
        _onOk = null; _onCancel = null;
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;
        if (Input.GetKeyDown(KeyCode.Return)) ClickOk();
        if (Input.GetKeyDown(KeyCode.Escape)) Hide();
    }
}

