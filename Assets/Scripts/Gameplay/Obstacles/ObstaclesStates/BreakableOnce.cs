// BreakableOnce.cs
using UnityEngine;

public class BreakableOnce : MonoBehaviour
{
    [Tooltip("True = break on use; False = unbreakable.")]
    public bool breakOnUse = true;

    [Tooltip("True = disable the object (and colliders) instead of destroying it.")]
    public bool disableInsteadOfDestroy = true;

    private bool _used;

    public void Consume()
    {
        if (_used || !breakOnUse) return;
        _used = true;

        if (disableInsteadOfDestroy)
        {
            foreach (var c in GetComponentsInChildren<Collider2D>(true)) c.enabled = false;
            foreach (var r in GetComponentsInChildren<Renderer>(true))   r.enabled = false;
            LevelEditorController.Instance?.RegisterTempDisabled(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Restore()
    {
        _used = false;
        foreach (var c in GetComponentsInChildren<Collider2D>(true)) c.enabled = true;
        foreach (var r in GetComponentsInChildren<Renderer>(true))   r.enabled = true;
        gameObject.SetActive(true);
    }
}

