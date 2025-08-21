using UnityEngine;

public class Activable : MonoBehaviour
{
    [Tooltip("Si vrai, l'objet n'agit pas au premier passage : il s'active, puis agira aux passages suivants.")]
    public bool startsInactive = true;

    [Header("Feedback visuel (facultatif)")]
    public bool tintWhenInactive = true;
    public float inactiveAlpha = 0.4f;

    private bool _activated; // devient vrai après le premier passage
    private SpriteRenderer[] _srs;
    private Color[] _initialColors;

    void Awake()
    {
        _srs = GetComponentsInChildren<SpriteRenderer>(true);
        _initialColors = new Color[_srs.Length];
        for (int i = 0; i < _srs.Length; i++) _initialColors[i] = _srs[i].color;

        // état visuel initial
        if (startsInactive) SetInactiveVisual(true);
    }

    /// <summary>
    /// Retourne true si l'interaction doit avoir lieu maintenant.
    /// Si l'objet est inactif, il s'active et retourne false (donc pas d'effet cette fois-ci).
    /// </summary>
    public bool AllowInteraction()
    {
        if (!startsInactive) return true;
        if (_activated) return true;

        // 1er passage : on active et on bloque l'effet
        _activated = true;
        SetInactiveVisual(false);
        return false;
    }

    /// <summary> Appelé par l'éditeur quand on arrête le test pour remettre l'état. </summary>
    public void ResetRuntimeState()
    {
        if (startsInactive)
        {
            _activated = false;
            SetInactiveVisual(true);
        }
    }

    private void SetInactiveVisual(bool inactive)
    {
        if (!tintWhenInactive || _srs == null) return;
        for (int i = 0; i < _srs.Length; i++)
        {
            var c = _initialColors[i];
            _srs[i].color = inactive ? new Color(c.r, c.g, c.b, inactiveAlpha) : c;
        }
    }
}

