using UnityEngine;

public class Activable : MonoBehaviour
{
    [Tooltip("state")]
    public bool startsInactive = true;

    [Header("Tint")]
    public bool tintWhenInactive = true;
    public float inactiveAlpha = 0.4f;

    private bool _activated; 
    private SpriteRenderer[] _srs;
    private Color[] _initialColors;

    void Awake()
    {
        _srs = GetComponentsInChildren<SpriteRenderer>(true);
        _initialColors = new Color[_srs.Length];
        for (int i = 0; i < _srs.Length; i++) _initialColors[i] = _srs[i].color;
        
        if (startsInactive) SetInactiveVisual(true);
    }
    
    public bool AllowInteraction()
    {
        if (!startsInactive) return true;
        if (_activated) return true;

        
        _activated = true;
        SetInactiveVisual(false);
        return false;
    }
    
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

