using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerSource : MonoBehaviour, IElectrical
{
    [SerializeField] private bool startPowered = false;
    [SerializeField] private Light poweredLight;
    
    private bool _isPowered = false;
    private Block _parentBlock;

    public bool IsPowered => _isPowered;

    private void Start()
    {
        TogglePower(startPowered);
    }

    public void SetBlock(Block block) => _parentBlock = block;
    
    public void TogglePower(bool powerState)
    {
        _isPowered = powerState;
        _parentBlock.TogglePower(powerState, true);
        
        poweredLight.gameObject.SetActive(powerState);
    }
}
