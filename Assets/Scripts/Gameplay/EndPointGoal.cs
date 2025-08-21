using UnityEngine;

public class EndPointGoal : MonoBehaviour, IBallInteractor
{
    public void OnBallHit(BallRunner ball)
    {
        // Laisse le contrôleur décider (succès/échec) et gérer la balle
        LevelEditorController.Instance.OnReachedEndpoint(ball);
    }
}
