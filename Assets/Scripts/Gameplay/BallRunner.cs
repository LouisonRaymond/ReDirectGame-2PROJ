using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BallRunner : MonoBehaviour
{
    public float speed = 6f;             // vitesse constante
    public Vector2 dir = Vector2.down;   // direction courante (U/D/L/R)

    private bool _running = false;
    private Rigidbody2D _rb;
    private readonly Dictionary<Collider2D, float> _cooldown = new();

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType =  RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Update()
    {
        if (!_running) return;

        // déplacement pur dans la direction
        _rb.MovePosition(_rb.position + dir * (speed * Time.deltaTime));

        // cooldown anti-retrigger
        if (_cooldown.Count > 0)
        {
            var keys = new List<Collider2D>(_cooldown.Keys);
            foreach (var k in keys)
            {
                _cooldown[k] -= Time.deltaTime;
                if (_cooldown[k] <= 0f) _cooldown.Remove(k);
            }
        }
        
        // Fin si hors écran (viewport)
        if (!_notifiedEnd)
        {
            var cam = Camera.main;
            if (cam)
            {
                Vector3 vp = cam.WorldToViewportPoint(transform.position);
                if (vp.x < -viewportMargin || vp.x > 1f + viewportMargin ||
                    vp.y < -viewportMargin || vp.y > 1f + viewportMargin)
                {
                    _notifiedEnd = true;
                    _running = false; // stop locomotion pour éviter spam
                    PlaySceneController.Instance?.OnBallOutOfBounds(this);
                    LevelEditorController.Instance?.OnBallOutOfBounds(this);
                    enabled = false; // coupe Update tout de suite après l’event
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!_running) return;
        if (_cooldown.ContainsKey(other)) return;

        var interactor = other.GetComponentInParent<IBallInteractor>();
        if (interactor != null)
        {
            interactor.OnBallHit(this);
            _cooldown[other] = 0.05f;
        }
    }

    // Contrôle
    public void StartRun()
    {
        dir = Vector2.down;
        _notifiedEnd = false;  // ← important
        _running = true;
        enabled = true;        // au cas où
    }
    public void StopRun()   { _running = false; }

    // Helpers
    public void SetDirection(Vector2 newDir)
    {
        // Snap aux 4 axes
        if (Mathf.Abs(newDir.x) > Mathf.Abs(newDir.y))
            dir = new Vector2(Mathf.Sign(newDir.x), 0f);
        else
            dir = new Vector2(0f, Mathf.Sign(newDir.y));
    }

    public void TeleportTo(Vector3 worldPos) => _rb.position = worldPos;
    
    // Ajoute/force un cooldown pour un collider donné
    public void AddColliderCooldown(Collider2D col, float seconds)
    {
        if (col == null) return;
        if (_cooldown.ContainsKey(col)) _cooldown[col] = Mathf.Max(_cooldown[col], seconds);
        else _cooldown[col] = seconds;
    }

// Ajoute un cooldown à TOUS les colliders d'un transform (root + enfants)
    public void AddCooldownForHierarchy(Transform root, float seconds)
    {
        if (root == null) return;
        var cols = root.GetComponentsInChildren<Collider2D>(true);
        foreach (var c in cols) AddColliderCooldown(c, seconds);
    }
    
    [SerializeField] private float viewportMargin = 0.05f; // tolérance
    private bool _notifiedEnd = false;
}