// Assets/Scripts/Gameplay/StarPickup.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StarPickup : MonoBehaviour, IBallInteractor
{
    void Awake()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true; // sécurité
    }

    public void OnBallHit(BallRunner ball)
    {
        // activable ? (si tu utilises l’effet “activable” sur la star)
        var act = GetComponent<Activable>();
        if (act != null && !act.AllowInteraction())
        {
            ball.AddCooldownForHierarchy(transform, 0.15f);
            return;
        }

        // FX + audio
        //FXManager.Instance?.PlayStar(transform.position);
        AudioManager.Instance?.PlayHit(0.7f);

        // Masquer la star
        gameObject.SetActive(false);

        // Notifier le contrôleur présent (éditeur OU play)
        if (PlaySceneController.Instance != null)
            PlaySceneController.Instance.RegisterCollectedStar(gameObject);
        else if (LevelEditorController.Instance != null)
            LevelEditorController.Instance.PlaytestCollectStar(this);

        // petit cooldown pour éviter retrigger immédiat si jamais
        ball.AddCooldownForHierarchy(transform, 0.1f);
    }
}