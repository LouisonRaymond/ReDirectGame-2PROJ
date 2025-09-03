// Assets/Scripts/FX/IdleSpin.cs
using UnityEngine;

public class IdleSpin : MonoBehaviour
{
    public enum SpinDirection { Clockwise, CounterClockwise }

    [Header("Objects to rotate")]
    public Transform target;

    [Header("Parameters")]
    public SpinDirection direction = SpinDirection.Clockwise;
    [Tooltip("Rotation Speed")]
    public float speed = 90f;

#if UNITY_EDITOR
    [Tooltip("Show in editMode")]
    public bool previewInEditor = false;   
#endif

    void Reset()
    {
        var art = transform.Find("Art");
        target = art ? art : transform;
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && !previewInEditor) return;
#endif
        if (!target) target = transform;
        
        float signedSpeed = (direction == SpinDirection.Clockwise ? -speed : speed);
        target.Rotate(0f, 0f, signedSpeed * Time.deltaTime, Space.Self);
    }
}

