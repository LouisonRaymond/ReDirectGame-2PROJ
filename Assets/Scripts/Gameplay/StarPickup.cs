// StarPickup.cs
using UnityEngine;
public class StarPickup : MonoBehaviour, IBallInteractor
{
    public void OnBallHit(BallRunner ball)
    {
        LevelEditorController.Instance.PlaytestCollectStar(this);
        gameObject.SetActive(false);
    }
}