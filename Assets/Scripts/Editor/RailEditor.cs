using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using ExtensionMethods;
using UnityEngine.ProBuilder;
using HandleUtility = UnityEditor.HandleUtility;

public class RailEditor : EditorWindow
{
    public GameObject railObject;
    public GameObject railStopObject;
    public GameObject electricLineObject;
    public GameObject electricMidpointObject;
    public Material previewMaterial;
    public bool showRailStopRails;
    
    private SerializedObject _so;
    private SerializedProperty _propRailObject;
    private SerializedProperty _propRailStopObject;
    private SerializedProperty _propElectricLineObject;
    private SerializedProperty _propElectricMidpointObject;
    private SerializedProperty _propPreviewMaterial;
    private SerializedProperty _propShowRailStopRails;

    private RailStop _lastInteractedRailStop;
    private bool _railEditorActive;
    private bool _powerEditorActive;
    
    // Layer Masks
    private LayerMask _blockMask;
    private LayerMask _railMask;
    private LayerMask _railStopMask;
    private LayerMask _electricLineMask;
    private LayerMask _electricMidpointMask;

    [MenuItem("Tools/Rail Editor")]
    public static void OpenWindow() => GetWindow<RailEditor>();

    private void OnEnable()
    {
        // Set Layer Masks
        _blockMask = LayerMask.GetMask("Block");
        _railMask = LayerMask.GetMask("Rail");
        _railStopMask = LayerMask.GetMask("RailStop");
        _electricLineMask = LayerMask.GetMask("ElectricLine");
        _electricMidpointMask = LayerMask.GetMask("ElectricMidpoint");
        
        _so = new SerializedObject(this);
        _propRailObject = _so.FindProperty("railObject");
        _propRailStopObject = _so.FindProperty("railStopObject");
        _propElectricLineObject = _so.FindProperty("electricLineObject");
        _propElectricMidpointObject = _so.FindProperty("electricMidpointObject");
        _propPreviewMaterial = _so.FindProperty("previewMaterial");
        _propShowRailStopRails = _so.FindProperty("showRailStopRails");

        Undo.undoRedoPerformed += RecalculateRails;
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void OnDisable()
    {
        Undo.undoRedoPerformed -= RecalculateRails;
        SceneView.duringSceneGui -= DuringSceneGUI;
    }


    private void OnGUI()
    {
        // Switch between Power and Rail Editing
        EditorGUILayout.BeginHorizontal();
        if (_powerEditorActive) _railEditorActive = false;
        _railEditorActive = GUILayout.Toggle(_railEditorActive, "Edit Rails", "Button");

        if (_railEditorActive) _powerEditorActive = false;
        _powerEditorActive = GUILayout.Toggle(_powerEditorActive, "Edit Power", "Button");
        EditorGUILayout.EndHorizontal();
        
        // TODO: Add save system
        _so.Update();
        EditorGUILayout.PropertyField(_propRailObject);
        EditorGUILayout.PropertyField(_propRailStopObject);
        EditorGUILayout.PropertyField(_propElectricLineObject);
        EditorGUILayout.PropertyField(_propElectricMidpointObject);
        EditorGUILayout.PropertyField(_propPreviewMaterial);
        EditorGUILayout.PropertyField(_propShowRailStopRails);
        _so.ApplyModifiedProperties();
    }
    
    private void DuringSceneGUI(SceneView sceneView)
    {
        // TODO: Consider making a file system that saves locations of stops and such for quicker access.
        // Repaint scene on mouse move
        if (Event.current.type == EventType.MouseMove)
            sceneView.Repaint();

        if (_railEditorActive || _powerEditorActive)
        {
            bool holdingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;
            bool holdingShift = (Event.current.modifiers & EventModifiers.Shift) != 0;
            bool leftMouseDown = Event.current.type == EventType.MouseDown && Event.current.button == 0;
            bool rightMouseDown = Event.current.type == EventType.MouseDown && Event.current.button == 1;

            // Alt + leftClick == Place Object
            // Alt + Shift + leftClick == Place Alt. Object
            // Alt + rightClick == Destroy Object

            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, _blockMask | _railMask | _railStopMask))
            {
                GameObject hitObject = hit.collider.gameObject;

                // TODO: Add check for Rail and placement accordingly.
                if (hit.collider.TryGetComponent(out RailStop railStop) && _railEditorActive)
                {
                    // Rail stop hit, so try and place a rail.

                    Vector3 hitLocation = hit.transform.position;
                    Vector3 spawnLocation = hitLocation + hit.normal * 2.5f;

                    bool railExists = HasObjectAtLocation(spawnLocation, _railMask);

                    Handles.color = railExists ? Color.red : Color.cyan;

                    // TODO: Draw models instead
                    // TODO: Draw a square on normal
                    Handles.DrawWireCube(spawnLocation,
                        GetRailGizmoSize(hit.normal)); // +0.25 for wire cube sizing issue.
                    Handles.DrawWireCube(spawnLocation + hit.normal * 2.5f, Vector3.one * 0.5f);

                    if (showRailStopRails)
                        DrawRailStopRails(railStop);

                    if (holdingAlt && holdingShift && leftMouseDown && !railExists &&
                        railStop.AllowLongRail(hit.normal))
                    {
                        Debug.Log("Alt Rail Placement");
                        Undo.SetCurrentGroupName("Place Rail");
                        int group = Undo.GetCurrentGroup();

                        CreateRailSegment(railStop, spawnLocation, hit.normal);

                        // Destroy rail stop to create long rail
                        Undo.DestroyObjectImmediate(hitObject);
                        Undo.CollapseUndoOperations(group);
                    }
                    else if (!holdingShift && holdingAlt && leftMouseDown && !railExists)
                    {
                        Debug.Log("Regular Rail Placement");
                        Undo.SetCurrentGroupName("Place Rail");
                        int group = Undo.GetCurrentGroup();

                        CreateRailSegment(railStop, spawnLocation, hit.normal);

                        Undo.CollapseUndoOperations(group);
                    }
                    else if (holdingAlt && rightMouseDown)
                    {
                        // Destroy Rail Stop
                        Undo.DestroyObjectImmediate(hitObject);
                    }
                }
                else if (hit.collider.TryGetComponent(out Rail rail))
                {
                    // Rail hit, so try and place another rail or rail stop.

                    if (holdingAlt && rightMouseDown)
                    {
                        // Destroy Rail
                        Undo.DestroyObjectImmediate(hitObject);
                    }
                }
                else if (hit.collider.TryGetComponent(out Block hitBlock))
                {
                    if (_railEditorActive)
                    {
                        // Block hit, so try and place rail stop.
                        Vector3 spawnLocation = hitBlock.transform.position;

                        bool railStopExists = HasObjectAtLocation(spawnLocation, _railStopMask);

                        Handles.color = railStopExists || !hitBlock.Interactable ? Color.red : Color.cyan;

                        if (!hitBlock.Interactable)
                        {
                            Vector3 labelPos = hitBlock.transform.position + Vector3.up;
                            DrawCenteredLabel(labelPos, "Not Interactable");
                        }

                        // Render Mesh
                        if (previewMaterial != null)
                        {
                            Handles.DrawWireCube(spawnLocation, Vector3.one * 0.5f);

                            if (holdingAlt && leftMouseDown && !railStopExists)
                            {
                                CreateSingleRailStop(spawnLocation);
                            }
                        }
                    }
                    else if (_powerEditorActive)
                    {
                        DrawPlacementButtons(hitBlock.transform, hit.normal, hit.point);
                    }
                }
            }
        }
    }

    private void DrawPlacementButtons(Transform targetTransform, Vector3 normal, Vector3 hitPoint)
    {
        
        Vector3 sideCenter = targetTransform.position - normal * 2f;

        Vector3 refVector = Vector3.up;
        if (normal.y != 0f) refVector = Vector3.right;
        
        Vector3 tangent = Vector3.Cross(normal, refVector);
        Vector3 bitangent = Vector3.Cross(normal, tangent);

        float offset = 1.5f;
        Vector3[] placementPoints = new Vector3[]
        {
            tangent * offset,
            tangent * offset * -1f,
            bitangent * offset,
            bitangent * offset * -1f,
        };
        
        float size = 0.5f;
        float pickSize = size;

        Handles.color = Color.white;
        bool hasNoLines = true;
        Vector3 midpointOffset = normal * 0.025f;
        for (int i = 0; i < placementPoints.Length; i++)
        {
            Vector3 placementPos = sideCenter + placementPoints[i];
            Vector3 placementDirection = (sideCenter - placementPos).normalized;
            Handles.DrawLine(targetTransform.position, targetTransform.position + placementDirection);

            bool hasElectricLine = HasObjectAtLocation(placementPos, _electricLineMask, out GameObject objectAtLocation);
            
            if (hasNoLines)
                hasNoLines = !hasElectricLine;
            
            ToggleColor(Color.white, Color.red, !hasElectricLine);
            if (Handles.Button(placementPos, Quaternion.identity, size, pickSize,
                    Handles.SphereHandleCap))
            {
                if (!hasElectricLine)
                {
                    // If no electric line has been placed
                    GameObject spawnedElectricLine = (GameObject)PrefabUtility.InstantiatePrefab(electricLineObject);
                    Undo.SetCurrentGroupName("Place Electric Line");
                    int group = Undo.GetCurrentGroup();
                    Undo.RegisterCreatedObjectUndo(spawnedElectricLine, "Electric Line Instantiation");

                    Quaternion rotation = Quaternion.LookRotation(placementDirection, normal);
                    
                    spawnedElectricLine.transform.position = placementPos;
                    spawnedElectricLine.transform.rotation = rotation;
                    Undo.SetTransformParent(spawnedElectricLine.transform, targetTransform, "Electric Line Parenting");

                    if (!HasObjectAtLocation(sideCenter, _electricMidpointMask))
                    {
                        Debug.Log("Creating Midpoint");
                        GameObject spawnedElectricMidpoint = (GameObject)PrefabUtility.InstantiatePrefab(electricMidpointObject);
                        Undo.RegisterCreatedObjectUndo(spawnedElectricMidpoint, "Electric Line Instantiation");
                    
                        spawnedElectricMidpoint.transform.position = sideCenter + midpointOffset;
                        spawnedElectricMidpoint.transform.rotation = Quaternion.FromToRotation(spawnedElectricMidpoint.transform.up, normal);
                        Undo.SetTransformParent(spawnedElectricMidpoint.transform, targetTransform, "Electric Line Midpoint Parenting");
                    }
                    
                    hasNoLines = false;
                    Undo.CollapseUndoOperations(group);
                }
                else
                {
                    // If electric line has already been placed
                    Undo.DestroyObjectImmediate(objectAtLocation);
                }
            }
        }

        if (hasNoLines && HasObjectAtLocation(sideCenter, _electricMidpointMask, out GameObject midpointObject))
        {
            Debug.Log("Destroying Midpoint");
            Undo.DestroyObjectImmediate(midpointObject);
        }
    }

    private void ToggleColor(Color successColor, Color failedColor, bool condition)
    {
        Handles.color = condition ? successColor : failedColor;
    }

    private void DrawCenteredLabel(Vector3 center, string text)
    {
        GUIStyle centeredStyle = GUI.skin.GetStyle("Label");
        centeredStyle.alignment = TextAnchor.UpperCenter;
        Handles.Label(center, text, centeredStyle);
    }

    private void DrawRailStopRails(RailStop railStop)
    {
        Color originalColor = Handles.color;
        Handles.color = Color.green;
        foreach (Vector3 normal in railStop.ConnectedRails.Keys)
        {
            Handles.DrawAAPolyLine(2f, railStop.transform.position, railStop.transform.position + normal);
        }

        Handles.color = originalColor;
    }

    private void RecalculateRails()
    {
        if (_lastInteractedRailStop != null)
            _lastInteractedRailStop.PopulateRails();
    }

    private Vector3 GetRailGizmoSize(Vector3 normal)
    {
        // Calculate size for rail Gizmo
        Vector3 finalSize;
        Vector3 expandedSize = normal * 5f;
        const float sizeUnit = 0.25f;

        finalSize.x = expandedSize.x == 0 ? sizeUnit : expandedSize.x;
        finalSize.y = expandedSize.y == 0 ? sizeUnit : expandedSize.y;
        finalSize.z = expandedSize.z == 0 ? sizeUnit : expandedSize.z;

        return finalSize;
    }

    private void DrawSphere(Vector3 pos)
    {
        Handles.SphereHandleCap(-1, pos, Quaternion.identity, 0.1f, EventType.Repaint);
    }

    private void CreateRailSegment(RailStop railStop, Vector3 spawnLocation, Vector3 hitNormal)
    {
        Rail newRail = CreateSingleRail(railStop, spawnLocation, hitNormal);
                    
        bool hasRailStop = HasObjectAtLocation(spawnLocation + hitNormal * 2.5f, _railStopMask);
        Debug.Log("Has Rail Stop: " + hasRailStop);
        if (!hasRailStop)
        {
            RailStop newRailStop = CreateSingleRailStop(spawnLocation + hitNormal * 2.5f);
            newRailStop.AddRail(-hitNormal, newRail);
        }
    }
    
    private Rail CreateSingleRail(RailStop railStop, Vector3 spawnLocation, Vector3 hitNormal)
    {
        GameObject spawnedObject = (GameObject)PrefabUtility.InstantiatePrefab(railObject);
        Rail rail = spawnedObject.GetComponent<Rail>();
        Undo.RegisterCreatedObjectUndo(spawnedObject, "Rail Instantiation");

        _lastInteractedRailStop = railStop;
        railStop.PopulateRails();
        spawnedObject.transform.position = spawnLocation;
        spawnedObject.transform.rotation = Quaternion.FromToRotation(spawnedObject.transform.forward, hitNormal);

        return rail;
    }

    private RailStop CreateSingleRailStop(Vector3 spawnLocation)
    {
        GameObject spawnedObject = (GameObject)PrefabUtility.InstantiatePrefab(railStopObject);
        RailStop railStop = spawnedObject.GetComponent<RailStop>();
        Undo.RegisterCreatedObjectUndo(spawnedObject, "Rail Stop Instantiation");

        spawnedObject.transform.position = spawnLocation;

        return railStop;
    }

    private bool HasObjectAtLocation(Vector3 spawnLocation, LayerMask layerMask) => Physics.OverlapSphereNonAlloc(spawnLocation, 0.1f, new Collider[1], layerMask) > 0;

    private bool HasObjectAtLocation(Vector3 spawnLocation, LayerMask layerMask, out GameObject objectAtLocation)
    {
        Collider[] result = new Collider[1];
        if (Physics.OverlapSphereNonAlloc(spawnLocation, 0.1f, result, layerMask) > 0)
        {
            objectAtLocation = result[0].gameObject;
            return true;
        }

        objectAtLocation = null;
        return false;
    }
    
    private Vector3 GetOrientation(Transform targetTransform, Vector3 checkPos, Vector3 normal)
    {
        Vector3 placementRef = targetTransform.position - normal * 2f;
        Vector3 directionDiff = (checkPos - placementRef).normalized;
        
        bool isInFront = Vector3.Dot(targetTransform.forward, directionDiff) > 0.5f;
        bool isInBack = Vector3.Dot(-targetTransform.forward, directionDiff) > 0.5f;
        bool isAbove = Vector3.Dot(targetTransform.up, directionDiff) > 0.5f;
        bool isBelow = Vector3.Dot(-targetTransform.up, directionDiff) > 0.5f;
        bool isAtRight = Vector3.Dot(targetTransform.right, directionDiff) > 0.5f;
        bool isAtLeft = Vector3.Dot(-targetTransform.right, directionDiff) > 0.5f;

        Handles.color = Color.blue;
        Handles.DrawLine(placementRef, placementRef + directionDiff);
        
        ToggleColor(Color.green, Color.red, isInFront);
        Handles.DrawLine(placementRef, placementRef + targetTransform.forward);
        
        ToggleColor(Color.green, Color.red, isInBack);
        Handles.DrawLine(placementRef, placementRef - targetTransform.forward);
        
        ToggleColor(Color.green, Color.red, isAbove);
        Handles.DrawLine(placementRef, placementRef + targetTransform.up);
        
        ToggleColor(Color.green, Color.red, isBelow);
        Handles.DrawLine(placementRef, placementRef - targetTransform.up);
        
        ToggleColor(Color.green, Color.red, isAtRight);
        Handles.DrawLine(placementRef, placementRef + targetTransform.right);
        
        ToggleColor(Color.green, Color.red, isAtLeft);
        Handles.DrawLine(placementRef, placementRef - targetTransform.right);

        Vector3 placementVector = Vector3.zero;
        
        if (isInFront)
            placementVector.x = 1f;
        else if (isInBack)
            placementVector.x = -1f;
        
        if (isAbove)
            placementVector.y = 1f;
        else if (isBelow)
            placementVector.y = -1f;
        
        if (isAtRight)
            placementVector.z = 1f;
        else if (isAtLeft)
            placementVector.z = -1f;
        
        Handles.Label(placementRef, normal.ToString());

        Handles.color = Color.magenta;
        DrawSphere(placementRef);
        
        Handles.color = Color.cyan;
        DrawSphere(placementRef + placementVector);
        Handles.Label(placementRef + placementVector, placementVector.ToString());

        return default;
    }
}
