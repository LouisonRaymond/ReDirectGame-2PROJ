// Rotatable.cs
using UnityEngine;

public class Rotatable : MonoBehaviour
{
    public int stepDegrees = 90;

    public void RotateStep()
    {
        transform.Rotate(0, 0, stepDegrees);
        var plc = GetComponent<Placeable>();
        if (plc != null && plc.data != null)
            plc.data.rotationSteps = (plc.data.rotationSteps + ((stepDegrees / 90) & 3)) & 3;
    }
}