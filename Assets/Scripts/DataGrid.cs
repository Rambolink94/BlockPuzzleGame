using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class DataGrid : MonoBehaviour
{
    [SerializeField] private int mapSize = 9;
    [SerializeField] private bool showRailStops = false;
    [SerializeField] private bool showBlocks = false;
    
    private Dictionary<Vector3, RailStop> _railStops = new Dictionary<Vector3, RailStop>();
    private Dictionary<Vector3, Block> _blocks = new Dictionary<Vector3, Block>();
    private Dictionary<Vector3, Block> _powerSources = new Dictionary<Vector3, Block>();

    public void GenerateRailGrid()
    {
        RailStop[] railStopsOnMap = FindObjectsOfType<RailStop>();
        foreach (RailStop railStop in railStopsOnMap)
        {
            _railStops.Add(railStop.transform.position, railStop);
        }
    }

    public void GenerateBlockDictionary()
    {
        List<Block> blocksOnMap = FindObjectsOfType<Block>().ToList();
        // Necessary to handle proper order of power propagation later on.
        blocksOnMap = blocksOnMap.OrderBy(block => !block.StartPowered).ThenBy(block => block.StartPowered)
            .ThenBy(block => block.PowerSource != null).ToList();
        foreach (Block block in blocksOnMap)
        {
            _blocks.Add(block.transform.position, block);
            if (block.PowerSource != null || block.StartPowered)
                _powerSources.Add(block.transform.position, block);
        }
    }

    public void UpdateBlockDictionary(Vector3 oldPosition, Block block)
    {
        if (_blocks.ContainsKey(oldPosition))
        {
            _blocks.Remove(oldPosition);
            _blocks.Add(block.transform.position, block);

            if (block.StartPowered)
            {
                _powerSources.Remove(oldPosition);
                _powerSources.Add(block.transform.position, block);
            }
        }
    }

    public RailStop GetRailGridFromDirection(RailStop currentStop, Vector3 direction)
    {
        Debug.Log("Direction: " + direction);
        // TODO: Fix this to not make unnecessary checks.
        for (int i = 1; i < mapSize; i++)
        {
            Vector3 checkPos = currentStop.transform.position + direction * (5f * i);
            if (TryGetRailStop(checkPos, out RailStop railStop))
            {
                return railStop;
            }
        }

        return null;
    }

    public Block GetBlockFromDirection(Vector3 blockPosition, Vector3 direction)
    {
        Vector3 checkPos = blockPosition + direction * 5f;
        if (TryGetBlock(checkPos, out Block block))
        {
            //if (block.HasConnectingLine(direction)) 
            return block;
        }

        return null;
    }

    public void PropegatePowerSources(Block caller = null)
    {
        foreach (Block block in _blocks.Values)
        {
            if (block.HasPowerSource) continue;
            
            block.TogglePower(false, true);
            Debug.DrawLine(block.transform.position, block.transform.position + block.transform.up, Color.blue, 2f);
        }
        
        foreach (Block block in _powerSources.Values)
        {
            if (block.IsPowered || (block.PowerSource != null && block.PowerSource.IsPowered))
                block.PropegatePowerForward(caller);
        }
    }

    public bool TryGetRailStop(Vector3 pos, out RailStop railStop)
    {
        if (_railStops.ContainsKey(pos))
        {
            railStop = _railStops[pos];
            return true;
        }

        railStop = null;
        return false;
    }

    public bool TryGetBlock(Vector3 pos, out Block block)
    {
        if (_blocks.ContainsKey(pos))
        {
            block = _blocks[pos];
            return true;
        }

        block = null;
        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (showRailStops)
        {
            foreach (KeyValuePair<Vector3, RailStop> railStop in _railStops)
            {
                Gizmos.color = railStop.Value.HasBlock() ? Color.red : Color.green;

                Gizmos.DrawSphere(railStop.Key, 0.75f);
            }
        }
        else if (showBlocks)
        {
            foreach (KeyValuePair<Vector3, Block> blockData in _blocks)
            {
                Vector3 pos = blockData.Key;
                Block block = blockData.Value;

                Gizmos.color = block.HasPowerSource ? Color.green : Color.blue;
                Gizmos.DrawSphere(pos, 0.5f);
                Handles.Label(pos + Vector3.up, block.name);
            }
        }
    }
#endif
}
