using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class TeleporterElement : MonoBehaviour, IBallInteractor
{
    [HideInInspector] public string pairId;
    [HideInInspector] public TeleporterElement paired;

    private SpriteRenderer[] _srs;
    private Color[] _baseColors;

    void Awake()
    {
        _srs = GetComponentsInChildren<SpriteRenderer>(true);
        _baseColors = new Color[_srs.Length];
        for (int i = 0; i < _srs.Length; i++) _baseColors[i] = _srs[i].color;

        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    public void SetTint(Color c)
    {
        if (_srs == null) return;
        for (int i = 0; i < _srs.Length; i++) _srs[i].color = c;
    }

    public void ResetTint()
    {
        if (_srs == null) return;
        for (int i = 0; i < _srs.Length; i++) _srs[i].color = _baseColors[i];
    }

    public void OnBallHit(BallRunner ball)
    {
        var act = GetComponent<Activable>();
        if (act != null && !act.AllowInteraction())
        {
            ball.AddCooldownForHierarchy(transform, 0.20f);
            return;
        }

        if (paired == null) return;

        // FX + SFX d’ENTRÉE (au point actuel de la balle)
        FXManager.Instance?.PlayTeleport(ball.transform.position, ball.dir);
        AudioManager.Instance?.PlayTeleport(); // <-- entrée

        // calcul de la sortie (léger offset)
        Vector3 exit = paired.transform.position + (Vector3)(ball.dir * 0.20f);

        // téléporte
        ball.TeleportTo(exit);

        // FX + SFX de SORTIE décalés d’1 frame pour ne pas "manger" l’entrée
        StartCoroutine(PlayExitFxAndSfxNextFrame(ball.dir));

        // anti ping-pong
        ball.AddCooldownForHierarchy(transform, 0.15f);
        ball.AddCooldownForHierarchy(paired.transform, 0.15f);
    }

    private IEnumerator PlayExitFxAndSfxNextFrame(Vector2 dir)
    {
        yield return null; // 1 frame plus tard
        FXManager.Instance?.PlayTeleport(paired.transform.position, dir);
        AudioManager.Instance?.PlayTeleport();    // <-- sortie
    }
}