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
    private ARRaycastManager aRRaycastManager;
    private bool placementPoseIsValid = false;

    private Camera arCamera;

    private PlayerInput playerInput;
    private InputAction touchAction;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        aRRaycastManager = UnityEngine.Object.FindFirstObjectByType<ARRaycastManager>();   
        arCamera = Camera.main;
    }

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        touchAction = playerInput.actions.FindAction("SingleTouchClick");
    }

    void OnEnable()
    {
        touchAction.started += PlaceObject;
    }

    void OnDisable()
    {
        touchAction.started -= PlaceObject;
    }

    private void UpdatePlacementPose()
    {
        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
        var hits = new List<ARRaycastHit>();
        aRRaycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon);

        placementPoseIsValid = hits.Count > 0;
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

    private void PlaceObject(InputAction.CallbackContext context)
    {
        if (placementPoseIsValid)
            Instantiate(objectToPlace, PlacementPose.position, PlacementPose.rotation);
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePlacementPose();
        UpdatePlacementIndicator();
    }
}
