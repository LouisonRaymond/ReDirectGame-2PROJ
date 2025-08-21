// BreakableOnce.cs
using UnityEngine;

public class BreakableOnce : MonoBehaviour
{
    [Tooltip("Si actif, l'objet disparaît au premier contact de la balle.")]
    public bool breakOnUse = true;

    [Tooltip("True = on cache et on restaure au Stop; False = on détruit réellement (perd l'état en éditeur).")]
    public bool disableInsteadOfDestroy = true;

    private bool _used;

    public void Consume()
    {
        if (_used || !breakOnUse) return;
        _used = true;

        if (disableInsteadOfDestroy)
        {
            // Cache visuel + collision, et enregistre pour restauration au Stop
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

