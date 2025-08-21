using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EndPointGoal : MonoBehaviour, IBallInteractor
{
    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true; // OBLIGATOIRE
    }

    public void OnBallHit(BallRunner ball)
    {
        // Feedback
        FXManager.Instance?.PlayGoal(transform.position);
        AudioManager.Instance?.PlayGoal();

        // Notifie la scène active (Play OU Editeur)
        if (PlaySceneController.Instance != null)
            PlaySceneController.Instance.OnReachedEndpoint(ball);
        else if (LevelEditorController.Instance != null)
            LevelEditorController.Instance.OnReachedEndpoint(ball);
    }
}
