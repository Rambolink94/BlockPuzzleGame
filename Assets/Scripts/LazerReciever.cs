using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LazerReciever : PowerSource
{
    public void HandleLazerHit(Vector3 hitDirection)
    {
        TogglePower(true);
    }
}
