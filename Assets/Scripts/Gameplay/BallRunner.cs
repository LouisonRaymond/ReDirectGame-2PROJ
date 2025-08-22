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

    void FixedUpdate()
    {
        if (!_running) return;

        // prochain point
        Vector2 next = _rb.position + dir * (speed * Time.fixedDeltaTime);

        // verrouille l'axe perpendiculaire au mouvement
        if (Mathf.Abs(dir.x) > 0.5f)       // horizontal -> force Y sur la ligne
            next.y = RoundToLane(_rb.position.y);
        else                               // vertical -> force X sur la colonne
            next.x = RoundToLane(_rb.position.x);

        _rb.MovePosition(next);

        // cooldown anti-retrigger (inchangé)
        if (_cooldown.Count > 0)
        {
            var keys = new List<Collider2D>(_cooldown.Keys);
            foreach (var k in keys)
            {
                _cooldown[k] -= Time.fixedDeltaTime;
                if (_cooldown[k] <= 0f) _cooldown.Remove(k);
            }
        }

        // sortie d'écran (tolérante)
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
                    _running = false;

                    PlaySceneController.Instance?.OnBallOutOfBounds(this);
                    LevelEditorController.Instance?.OnBallOutOfBounds(this);
                    enabled = false; // coupe tout de suite
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
        _notifiedEnd = false;
        _running = true;
        enabled = true;
        SnapToAxisCenter(); // démarre bien centré
    }
    public void StopRun()   { _running = false; }

    // Helpers
    public void SetDirection(Vector2 newDir)
    {
        if (Mathf.Abs(newDir.x) > Mathf.Abs(newDir.y))
            dir = new Vector2(Mathf.Sign(newDir.x), 0f);
        else
            dir = new Vector2(0f, Mathf.Sign(newDir.y));

        SnapToAxisCenter(); // ← important
    }

    public void TeleportTo(Vector3 worldPos)
    {
        _rb.position = worldPos;
        SnapToAxisCenter();
    }
    
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
    
    [SerializeField] public float cellSize = 1f;   // même valeur que ton éditeur/jeu
    [SerializeField] private float snapTolerance = 0.03f; // marge pour snap "dur"

    float RoundToLane(float v) => Mathf.Round(v / cellSize) * cellSize;

    void SnapToAxisCenter()
    {
        var p = _rb.position;
        if (Mathf.Abs(dir.x) > 0.5f)       // on va à gauche/droite
            p.y = RoundToLane(p.y);
        else                               // on va en haut/bas
            p.x = RoundToLane(p.x);
        _rb.position = p;
    }
}