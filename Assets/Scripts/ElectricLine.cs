using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElectricLine : MonoBehaviour, IElectrical
{
    [SerializeField] private Material poweredMat;
    [SerializeField] private Material unpoweredMat;
    
    private bool _isPowered = false;
    private Vector3 _connectionDirection;
    private Renderer _renderer;

    public Vector3 ConnectionDirection => _connectionDirection;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    public void SetLineState(bool powerState)
    {
        _connectionDirection = transform.forward;
        
        TogglePower(powerState);
    }

    public void TogglePower(bool poweredState)
    {
        if (_renderer == null)
            _renderer = GetComponent<Renderer>();
        
        _isPowered = poweredState;
        
        // TODO: Convert this to a shader for better performance.
        if (poweredState)
            _renderer.sharedMaterial = poweredMat;
        else
            _renderer.sharedMaterial = unpoweredMat;
    }
}
