using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Selectable))]
public class UIButtonSfx : MonoBehaviour, IPointerEnterHandler
{
    [Header("Override (laisse vide pour utiliser AudioManager)")]
    public AudioClip clickOverride;
    public AudioClip hoverOverride;
    [Range(0f,1f)] public float volume = 1f;

    void Awake()
    {
        var btn = GetComponent<Button>();
        if (btn) btn.onClick.AddListener(PlayClick);
        
        // Si Toggle/Dropdown/Slider → jouer le son à la validation
        var t = GetComponent<Toggle>();
        if (t) t.onValueChanged.AddListener(_ => PlayClick());

        var s = GetComponent<Slider>();
        if (s) s.onValueChanged.AddListener(_ => PlayClick());

        var d = GetComponent<TMPro.TMP_Dropdown>();
        if (d) d.onValueChanged.AddListener(_ => PlayClick());
    }

    public void OnPointerEnter(PointerEventData eventData) => PlayHover();

    void PlayClick()
    {
        if (clickOverride) AudioManager.Instance?.PlaySfx(clickOverride, volume);
        else AudioManager.Instance?.PlayUiClick(volume);
    }

    void PlayHover()
    {
        if (hoverOverride) AudioManager.Instance?.PlaySfx(hoverOverride, volume);
        else AudioManager.Instance?.PlayUiHover(volume);
    }
}
