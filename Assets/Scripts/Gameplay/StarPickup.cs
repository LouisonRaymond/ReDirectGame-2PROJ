using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class StarPickup : MonoBehaviour, IBallInteractor
{
    void Awake()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true; 
    }

    public void OnBallHit(BallRunner ball)
    {
        var act = GetComponent<Activable>();
        if (act != null && !act.AllowInteraction())
        {
            ball.AddCooldownForHierarchy(transform, 0.15f);
            return;
        }
        
        AudioManager.Instance?.PlayHit(0.7f);
        
        gameObject.SetActive(false);
        
        if (PlaySceneController.Instance != null)
            PlaySceneController.Instance.RegisterCollectedStar(gameObject);
        else if (LevelEditorController.Instance != null)
            LevelEditorController.Instance.PlaytestCollectStar(this);
        
        ball.AddCooldownForHierarchy(transform, 0.1f);
    }
}