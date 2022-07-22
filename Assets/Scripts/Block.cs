using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using ExtensionMethods;
using UnityEditor;

[RequireComponent(typeof(AudioSource))]
public class Block : MonoBehaviour, IElectrical
{
    [SerializeField] private float rotationSpeed = 1f;
    [SerializeField] private float movementSpeed = 1f;
    [SerializeField] private Material selectedMaterial;
    [SerializeField] private AudioClip onSelectClip;
    [SerializeField] private AudioClip onDeselectClip;
    [SerializeField] private AudioClip errorClip;
    [SerializeField] private AudioClip movementClip;
    [SerializeField] private bool interactable = true;
    [SerializeField] private bool startPowered = false;
    [SerializeField] private PowerSource powerSource;

    public bool Interactable => interactable;
    public bool IsPowered => _isPowered;
    public bool StartPowered => startPowered;
    public bool IsMoving => _isMoving;
    public PowerSource PowerSource => powerSource;
    public bool HasPowerSource => powerSource != null || startPowered;
    
    private bool _isPowered;
    private bool _isRotating = false;
    private bool _isMoving = false;
    private float _lastEvaluatedTime = -1f;

    private Transform _parentTransform;
    private RailStop _currentRailStop;
    private Material _originalMaterial;
    private MeshRenderer _meshRenderer;
    private AudioSource _audioSource;
    private DataGrid _dataGrid;
    
    private ElectricLine[] _electricLines;
    private ElectricMidpoint _midpoint;
    private IPowerable[] _powerables;

    private void Awake()
    {
        GameObject parent = new GameObject();
        parent.name = name + "_root";
        
        // _parentTransform = parent.transform;
        // transform.parent = _parentTransform;

        _audioSource = GetComponent<AudioSource>();
        _audioSource.clip = onSelectClip;
        
        _meshRenderer = GetComponent<MeshRenderer>();
        _originalMaterial = _meshRenderer.sharedMaterial;

        _midpoint = GetComponentInChildren<ElectricMidpoint>();
        _electricLines = GetComponentsInChildren<ElectricLine>();
        foreach (ElectricLine line in _electricLines)
        {
            line.SetLineState(_isPowered);
        }

        _powerables = GetComponentsInChildren<IPowerable>();
        if (powerSource != null)
            powerSource.SetBlock(this);
    }

    private void Start()
    {
        _dataGrid = GameManager.Instance.dataGrid;
        
        if (Interactable)
        {
            _currentRailStop = GetRailStop();
            if (_currentRailStop == null)
                Debug.LogWarning($"Block ({name}) marked as interactable but it has no rail stop!");
            else
                _currentRailStop.SetCurrentBlock(this);
        }
        
        if (startPowered || (powerSource != null && powerSource.IsPowered))
            PropegatePowerForward();
    }

    public void Rotate(Vector3 callersPos, RotationDirection direction)
    {
        if (_isRotating || _isMoving)
            return;

        // TODO: Use these to determine which direction to turn the block
        Vector3 directionDiff = transform.position - callersPos;
        bool isInFront = Vector3.Dot(transform.forward, directionDiff) > 0f;
        bool isAbove = Vector3.Dot(-transform.up, directionDiff) > 0f;
        bool isAtTheRight = Vector3.Dot(transform.right, directionDiff) > 0f;
        
        float angle = 90f;
        if (direction == RotationDirection.Left)
            angle *= -1;
        
        Vector3 targetRotation = GetTargetRotation(angle);
        
        _dataGrid.PropegatePowerSources(this);
        TogglePower(false, true);

        _isRotating = true;
        Tween rotationTween = transform.DOLocalRotate(targetRotation, rotationSpeed).SetUpdate(UpdateType.Fixed);
        rotationTween.OnComplete(() =>
        {
            _isRotating = false;
            
            _dataGrid.PropegatePowerSources();
        });
    }

    private Vector3 GetTargetRotation(float angle)
    {
        Vector3 target = Vector3.zero;
        target.y = angle;

        return transform.rotation.eulerAngles + target;
    }

    public void Move(Vector3 direction)
    {
        bool canMove = CanMoveInDirection(direction);
        if (!canMove)
        {
            PlayErrorSound();
            return;
        }
        
        if (_isMoving || _isRotating)
        {
            return;
        }

        RailStop nextRailStop = GetNextRailStop(direction);
        if (nextRailStop == null)
        {
            PlayErrorSound();
            return;
        }
        
        _isMoving = true;
        UnmarkAsSelected(false);
        _currentRailStop.SetCurrentBlock(null);

        // TODO: Change movement clip pitch with sin wave based on lenght of movement
        _audioSource.clip = movementClip;
        _audioSource.Play();

        _dataGrid.PropegatePowerSources(this);
        TogglePower(false, true);

        Vector3 oldPosition = transform.position;
        float timeToStop = (nextRailStop.transform.position - transform.position).magnitude / 5f;
        Tween movementTween = transform.DOMove(nextRailStop.transform.position, timeToStop * movementSpeed).SetUpdate(UpdateType.Fixed);
        movementTween.OnComplete(() =>
        {
            _isMoving = false;
            _currentRailStop = nextRailStop;
            _currentRailStop.SetCurrentBlock(this);
            
            _audioSource.Stop();
            
            _dataGrid.UpdateBlockDictionary(oldPosition, this);
            _dataGrid.PropegatePowerSources();
        });
    }

    public bool PropegatePowerBackward(bool isFirstCall = false)
    {
        // Check if block is power block or if block has already been evaluated
        if (HasPowerSource) return true;

        if (_lastEvaluatedTime == Time.time) return _isPowered;
        _lastEvaluatedTime = Time.time;

        bool foundPowerSource = false;
        // Loop through each electric line to move to next blocks
        foreach (ElectricLine electricLine in _electricLines)
        {
            // Check if next blocks have a power source. If so, return true.
            Vector3 direction = -electricLine.transform.forward.Round();
            Debug.DrawLine(electricLine.transform.position,  electricLine.transform.position + electricLine.transform.up, Color.green, 2f);
            Block block = GameManager.Instance.dataGrid.GetBlockFromDirection(transform.position, direction);
            if (block == null)
                continue;
            
            bool shouldBePowered = block.PropegatePowerBackward();
            if (!foundPowerSource && shouldBePowered)
                foundPowerSource = true;
        }
        
        TogglePower(foundPowerSource, true);
        return foundPowerSource;
    }

    public void PropegatePowerForward(Block caller = null)
    {
        // TODO: This does not turn off power for things that get cut off from power.
        
        if (_lastEvaluatedTime == Time.time) return;
        _lastEvaluatedTime = Time.time;
        
        TogglePower(true, true);

        foreach (ElectricLine electricLine in _electricLines)
        {
            Vector3 direction = -electricLine.transform.forward.Round();
            Debug.DrawLine(electricLine.transform.position,  electricLine.transform.position + electricLine.transform.up, Color.green, 2f);
            Block block = GameManager.Instance.dataGrid.GetBlockFromDirection(transform.position, direction);
            if (block == null || block == caller)
            {
                //Debug.DrawLine(electricLine.transform.position,  electricLine.transform.position + electricLine.transform.up, Color.red, 2f);
                continue;
            }

            block.PropegatePowerForward(caller);
        }
    }

    public bool HasConnectingLine(Vector3 direction)
    {
        foreach (ElectricLine electricLine in _electricLines)
        {
            if (electricLine.transform.forward == -direction)
                return true;
        }

        return false;
    }

    public void TogglePower(bool powerState, bool effectLines)
    { 
        TogglePower(powerState);
        
        if (_midpoint != null)
            _midpoint.TogglePower(powerState);
        
        if (_electricLines == null) Debug.Log(name);
        
        if (effectLines)
            foreach (ElectricLine electricLine in _electricLines)
                electricLine.TogglePower(powerState);
    }

    public void TogglePower(bool powerState)
    {
        if (_isPowered != powerState)
        {
            foreach (IPowerable powerable in _powerables)
            {
                powerable.TogglePower(powerState);
            }
        }

        _isPowered = powerState;
    }

    private RailStop GetNextRailStop(Vector3 direction)
    {
        RailStop nextStop = _dataGrid.GetRailGridFromDirection(_currentRailStop, direction);
        if (nextStop != null)
        {
            if (!nextStop.HasBlock())
                return nextStop;
        }

        // This shouldn't happen.
        return null;
    }

    private bool CanMoveInDirection(Vector3 direction)
    {
        if (_currentRailStop == null)
            return false;
        
        return _currentRailStop.ConnectedRails.ContainsKey(direction);
    }

    public void MarkAsSelected(Block selectedBlock)
    {
        if (_isMoving || this == selectedBlock) return;

        if (selectedBlock != null)
            selectedBlock.UnmarkAsSelected(false);

        _audioSource.clip = onSelectClip;
        _audioSource.Play();
        
        _meshRenderer.sharedMaterial = selectedMaterial;
    }

    public void UnmarkAsSelected(bool playSound = true)
    {
        _meshRenderer.sharedMaterial = _originalMaterial;

        if (playSound && !_isMoving)
        {
            _audioSource.clip = onDeselectClip;
            _audioSource.Play();
        }
    }

    public void GetMovementOptions()
    {
        //_currentRailStop.ConnectedRails.Keys
    }

    private void PlayErrorSound()
    {
        if (_audioSource.clip != errorClip || (_audioSource.clip == errorClip && !_audioSource.isPlaying))
        {
            _audioSource.clip = errorClip;
            _audioSource.Play();
        }
    }

    private RailStop GetRailStop()
    {
        if (_dataGrid.TryGetRailStop(transform.position, out RailStop railStop))
        {
            return railStop;
        }

        return null;
    }

    private void OnCollisionStay(Collision collision)
    {
        // TODO: Improve performance of this, probably parent only when movement starts
        // If block is moving, parent anything that is touching it
        if (!_isMoving) return;
        
        collision.transform.parent = transform;
    }

    private void OnCollisionExit(Collision collision)
    {
        collision.transform.parent = null;
    }

    private void OnDrawGizmos()
    {
        if (_currentRailStop != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_currentRailStop.transform.position, 0.5f);
        }
    }
}

public enum RotationDirection
{
    Left,
    Right
}
