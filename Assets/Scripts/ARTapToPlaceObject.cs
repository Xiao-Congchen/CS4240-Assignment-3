using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using System;

public class ARTapToPlaceObject : MonoBehaviour
{
    public GameObject placementIndicator;

    public GameObject objectToPlace;
    private Pose PlacementPose;
    private ARRaycastManager arRaycastManager;
    private bool placementPoseIsValid = false;

    private Camera arCamera;

    private PlayerInput playerInput;
    private InputAction touchAction;
    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();
    void Awake()
    {
        arRaycastManager = UnityEngine.Object.FindFirstObjectByType<ARRaycastManager>();   
        arCamera = Camera.main;
        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
        {
            touchAction = playerInput.actions.FindAction("SingleTouchClick");
        }

        if (placementIndicator == null)
            Debug.LogError("placementIndicator is not assigned.");

        if (objectToPlace == null)
            Debug.LogError("objectToPlace is not assigned.");

        if (arRaycastManager == null)
            Debug.LogError("No ARRaycastManager found in the scene.");

        if (arCamera == null)
            Debug.LogError("Camera.main is null. Make sure your AR Camera is enabled and tagged MainCamera.");

        if (playerInput == null)
            Debug.LogError("No PlayerInput component found on this GameObject.");

        if (touchAction == null)
            Debug.LogError("Input action 'SingleTouchClick' was not found in the PlayerInput actions asset.");
    }


    void OnEnable()
    {
        if (touchAction != null)
        touchAction.started += PlaceObject;
    }

    void OnDisable()
    {
        if (touchAction != null)
        touchAction.started -= PlaceObject;
    }

    private void PlaceObject(InputAction.CallbackContext context)
    {
        if (placementPoseIsValid && objectToPlace != null)
            Instantiate(objectToPlace, PlacementPose.position, PlacementPose.rotation);
    }

    private void UpdatePlacementPose()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        placementPoseIsValid = arRaycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon);
        if (placementPoseIsValid) 
        {
            PlacementPose = hits[0].pose;
        }
    }

    private void UpdatePlacementIndicator()
    {
        if (placementPoseIsValid)
        {
            placementIndicator.SetActive(true);
            placementIndicator.transform.SetPositionAndRotation(PlacementPose.position, PlacementPose.rotation);
        }
        else 
        {
            placementIndicator.SetActive(false);
        }
    }

    void Update()
    {
        UpdatePlacementPose();
        UpdatePlacementIndicator();
    }
}
