// Assets/Scripts/UI/PopupService.cs
using UnityEngine;

public class PopupService : MonoBehaviour
{
    public static PopupService Instance { get; private set; }

    [SerializeField] private PopupPanel popup;   // réf. au panel dans ton Canvas

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (popup) popup.gameObject.SetActive(false);
    }

    public bool IsOpen => popup && popup.gameObject.activeSelf;

    /// <summary>
    /// Affiche un popup. Si un popup est déjà ouvert:
    ///  - replaceIfOpen=true  => remplace le contenu
    ///  - replaceIfOpen=false => ignore l'appel
    /// </summary>
    public void Show(string title, string msg, PopupType type = PopupType.Info,
        float autoClose = 0f, bool replaceIfOpen = true)
    {
        if (!popup) return;

        if (!IsOpen || replaceIfOpen)
        {
            popup.Show(title, msg, type, autoClose); // onClose pas nécessaire ici
        }
        // sinon: on ignore
    }

    public void Hide()
    {
        if (IsOpen) popup.Hide();
    }

    // Helpers pratiques
    public void Info(string t, string m, float auto = 0f, bool replaceIfOpen = true)
        => Show(t, m, PopupType.Info,    auto, replaceIfOpen);

    public void Success(string t, string m, float auto = 0f, bool replaceIfOpen = true)
        => Show(t, m, PopupType.Success, auto, replaceIfOpen);

    public void Warn(string t, string m, float auto = 0f, bool replaceIfOpen = true)
        => Show(t, m, PopupType.Warning, auto, replaceIfOpen);

    public void Error(string t, string m, float auto = 0f, bool replaceIfOpen = true)
        => Show(t, m, PopupType.Error,   auto, replaceIfOpen);
}
