using System;
using System.Collections;
using System.Collections.Generic;
using ExtensionMethods;
using UnityEditor;
using UnityEngine;

public class Lazer : MonoBehaviour, IPowerable
{
    [SerializeField] private Transform shotLocation;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private Light lazerLight;
    [SerializeField] private LayerMask lazerInteractionLayers;
    
    private bool _isOn = false;
    private Ray _lazerRay;
    private Vector3 _hitPoint;
    private LazerReciever _poweredReciever;
    
    private void Start()
    {
        lineRenderer.gameObject.SetActive(false);
        lazerLight.gameObject.SetActive(false);
        
        lineRenderer.useWorldSpace = true;
        lineRenderer.SetPosition(0, shotLocation.position);
        
        _lazerRay = new Ray(shotLocation.position, transform.forward);
    }

    public void TogglePower(bool powerState)
    {
        ToggleLazer();
    }

    private void ToggleLazer()
    {
        if (!_isOn)
        {
            _lazerRay = new Ray(shotLocation.position, transform.forward);
            if (Physics.Raycast(_lazerRay, out RaycastHit hitInfo, 50f, lazerInteractionLayers))
            {
                HandleHit(hitInfo);
                
                _hitPoint = hitInfo.point;
                UpdateLineRenderer(hitInfo.point);
            }
        }
        else
        {
            if (_poweredReciever != null)
            {
                _poweredReciever.TogglePower(false);
                _poweredReciever = null;
            }

            lineRenderer.gameObject.SetActive(false);
        }

        _isOn = !_isOn;
    }

    private void UpdateLineRenderer(Vector3 hitPos)
    {
        Debug.Log(hitPos);
        lineRenderer.gameObject.SetActive(true);
        lazerLight.gameObject.SetActive(true);
        
        lineRenderer.SetPosition(0, shotLocation.position);
        lineRenderer.SetPosition(1, hitPos);
    }

    private void HandleHit(RaycastHit hitInfo)
    {
        if (hitInfo.collider.gameObject.TryGetComponent(out LazerReciever reciever))
        {
            _poweredReciever = reciever;
            reciever.HandleLazerHit(hitInfo.normal);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(shotLocation.position, shotLocation.position + transform.forward * 0.5f);
        
        Gizmos.DrawSphere(_hitPoint, .5f);
    }
}
