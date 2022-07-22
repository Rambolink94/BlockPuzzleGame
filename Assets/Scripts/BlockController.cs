using System;
using ExtensionMethods;
using UnityEngine;

public class BlockController : MonoBehaviour
{
    [SerializeField] private float blockInteractDistance = 10f;
    [SerializeField] private bool useMousePos = false;

    private Camera _cam;
    private Block _currentBlock;

    public bool HasBlock => _currentBlock != null;

    private void Start()
    {
        _cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool left = Input.GetKeyDown(KeyCode.Q);
        bool right = Input.GetKeyDown(KeyCode.E);
        bool up = Input.GetKeyDown(KeyCode.Space);
        bool down = Input.GetKeyDown(KeyCode.C) | Input.GetKeyDown(KeyCode.LeftControl);

        float yMovement = up.GetHashCode() + down.GetHashCode() * -1;
        
        // Get rid of diagonal movement
        if (horizontal > 0f || horizontal < 0f)
        {
            vertical = 0f;
            yMovement = 0f;
        }
        else if(vertical > 0f || vertical < 0f)
        {
            horizontal = 0f;
            yMovement = 0f;
        }

        Vector3 movement = new Vector3(horizontal, yMovement, vertical).normalized;

        
        if (Input.GetMouseButton(1))
        {
            Block block = GetBlock();
            if (block != null && block.Interactable && !block.IsMoving)
            {
                //CalculateMoveDirection(block, movement);
                //CalculateMovement(block, movement);
                //Vector3 rotationVec = GetRotationVector(block);

                block.MarkAsSelected(_currentBlock);
                _currentBlock = block;
                
                 if (left)
                     block.Rotate(transform.position, RotationDirection.Left);
                 else if (right)
                     block.Rotate(transform.position, RotationDirection.Right);
                 else if (movement != Vector3.zero)
                     block.Move(movement);
            }
            else
            {
                if (_currentBlock != null)
                    _currentBlock.UnmarkAsSelected(false);

                _currentBlock = null;
            }
        }
        else if (Input.GetMouseButtonUp(1))
        {
            if (_currentBlock == null) return;
                
            _currentBlock.UnmarkAsSelected();
            _currentBlock = null;
        }
    }

    private Block GetBlock()
    {
        Ray blockCheckRay;

        if (useMousePos)
            blockCheckRay = _cam.ScreenPointToRay(Input.mousePosition);
        else
            blockCheckRay = new Ray(transform.position, transform.forward);
        
        if (Physics.Raycast(blockCheckRay, out RaycastHit hitInfo, blockInteractDistance, LayerMask.GetMask("Block")))
        {
            return hitInfo.collider.gameObject.GetComponentInParent<Block>();
        }

        return null;
    }

    private Vector3 CalculateMovement(Block block, Vector3 movement)
    {
        Vector3 diff = (block.transform.position - transform.position).normalized.Round();
        Debug.DrawLine(block.transform.position, block.transform.position + movement, Color.yellow, 2f);
        Debug.DrawLine(block.transform.position, block.transform.position + diff);

        return movement;
    }

    private Vector3 CalculateMoveDirection(Block block, Vector3 movement)
    {
        Vector3 diff = (block.transform.position - transform.position).normalized;

        bool isInFront = Vector3.Dot(transform.forward, diff) > 0f;
        bool isAbove = Vector3.Dot(-transform.up, diff) > 0f;
        bool isAtTheRight = Vector3.Dot(transform.right, diff) > 0f;
        
        Debug.Log($"Front: {isInFront} Above: {isAbove} Right: {isAtTheRight}");

        // Return the forward of new space
        return default;
    }

    private Vector3 GetRotationVector(Block block)
    {
        Vector3 rotationVec = new Vector3();
        Collider blockCol = block.gameObject.GetComponent<Collider>();
        if (blockCol.bounds.Contains(transform.position))
        {
            rotationVec.z = 90f;
            Debug.Log("Inside");
            return rotationVec;
        }

        Vector3 relativePos = block.gameObject.transform.position - transform.position;

        bool isInFront = Vector3.Dot(transform.forward, relativePos) > 0f;
        bool isAbove = Vector3.Dot(transform.up, relativePos) > 0f;
        bool isAtTheRight = Vector3.Dot(transform.right, relativePos) > 0f;
        
        Debug.Log($"Front: {isInFront} Above: {isAbove} Right: {isAtTheRight}");
        return default;
    }
}
