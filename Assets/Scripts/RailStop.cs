using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RailStop : MonoBehaviour
{
    private Dictionary<Vector3, Rail> _connectedRails = new Dictionary<Vector3, Rail>();
    public Dictionary<Vector3, Rail> ConnectedRails => _connectedRails;

    private Block _currentBlock;

    private void OnValidate()
    {
        PopulateRails();
    }
    
    public void PopulateRails()
    {
        _connectedRails.Clear();
        Collider[] colliders = Physics.OverlapSphere(transform.position, 1f, LayerMask.GetMask("Rail"));
        foreach (Collider col in colliders)
        {
            Vector3 dir = (col.transform.position - transform.position).normalized;
            AddRail(dir, col.GetComponent<Rail>());
        }
    }

    public bool AddRail(Vector3 placementNormal, Rail rail)
    {
        if (_connectedRails.ContainsKey(placementNormal))
            return false;
        
        _connectedRails.Add(placementNormal, rail);
        return true;
    }

    public void SetCurrentBlock(Block block)
    {
        _currentBlock = block;
    }

    public bool HasBlock()
    {
        return _currentBlock != null;
    }

    public bool AllowLongRail(Vector3 placementNormal)
    {
        // Check if stop has two rails parallel to each other and no others
        bool hasParallel = _connectedRails.ContainsKey(-placementNormal);
        if (_connectedRails.Count > 2)
            return false;
        
        return hasParallel;
    }
}
