using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Door : MonoBehaviour, IPowerable
{
    [SerializeField] private float toggleSpeed = 0.2f;
    
    private Dictionary<Transform, Vector3> _doors = new Dictionary<Transform, Vector3>();
    private bool _isToggling;
    private bool _isPowered = false;
    private bool _isOpen = false;
    
    // Start is called before the first frame update
    void Start()
    { 
        Transform[] doors = GetComponentsInChildren<Transform>();
        foreach (Transform door in doors)
        {
            _doors.Add(door, door.position);
        }
    }

    public void ToggleOpen()
    {
        // TODO: Check if damageable is in range of closing door. If so, damage them and then push them out of the way.
        
        float offset = 1.75f;
        foreach (KeyValuePair<Transform,Vector3> doorData in _doors)
        {
            Transform door = doorData.Key;
            Vector3 doorPos = doorData.Value;
            
            if (!_isOpen)
            {
                float sign = Mathf.Sign(doorPos.x);

                float movePosition = doorPos.x + offset * sign;
                door.DOMoveX(movePosition, toggleSpeed);
            }
            else
            {
                door.DOMoveX(doorPos.x, toggleSpeed);
            }
        }
        
        _isOpen = !_isOpen;
    }

    public void TogglePower(bool powerState)
    {
        _isPowered = powerState;
        
        ToggleOpen();
    }
}
