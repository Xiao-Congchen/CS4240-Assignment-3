using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using System;
using TMPro;

public class ARTapToPlaceObjects : MonoBehaviour
{
    [Header("AR Placement")]
    [SerializeField] private GameObject placementIndicator;

    [Header("Furniture Prefabs")]
    [SerializeField] private List<GameObject> furniturePrefabs = new List<GameObject>();

    [Header("UI")]
    [SerializeField] private GameObject furnitureSelectionPanel;
    [SerializeField] private TMP_Text selectedFurnitureButtonText;
    private GameObject selectedFurniturePrefab;

    private Pose placementPose;
    private bool placementPoseIsValid;

    private ARRaycastManager arRaycastManager;
    private Camera arCamera;

    private PlayerInput playerInput;
    private InputAction touchAction;

    private static readonly List<ARRaycastHit> arHits = new List<ARRaycastHit>();
    private readonly List<GameObject> placedObjects = new List<GameObject>();

    private void Awake()
    {
        arRaycastManager = FindFirstObjectByType<ARRaycastManager>();
        arCamera = Camera.main;

        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null && playerInput.actions != null)
        {
            touchAction = playerInput.actions.FindAction("SingleTouchClick");
        }

        if (placementIndicator == null)
            Debug.LogError("Placement indicator is not assigned.");

        if (arRaycastManager == null)
            Debug.LogError("ARRaycastManager not found in scene.");

        if (arCamera == null)
            Debug.LogError("AR Camera not found. Make sure the AR Camera is tagged MainCamera.");

        if (playerInput == null)
            Debug.LogError("PlayerInput not found on this GameObject.");

        if (touchAction == null)
            Debug.LogError("Input action 'SingleTouchClick' not found.");
    }

    private void OnEnable()
    {
        if (touchAction != null)
            touchAction.started += PlaceSelectedFurniture;
    }

    private void OnDisable()
    {
        if (touchAction != null)
            touchAction.started -= PlaceSelectedFurniture;
    }

    private void Update()
    {
        UpdatePlacementPose();
        UpdatePlacementIndicator();
    }

    private void UpdatePlacementPose()
    {
        if (arRaycastManager == null || arCamera == null)
            return;

        Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);

        placementPoseIsValid = arRaycastManager.Raycast(
            screenCenter,
            arHits,
            TrackableType.PlaneWithinPolygon
        );

        if (placementPoseIsValid)
        {
            placementPose = arHits[0].pose;
        }
    }

    private void UpdatePlacementIndicator()
    {
        if (placementIndicator == null)
            return;

        bool shouldShow = placementPoseIsValid && selectedFurniturePrefab != null;
        placementIndicator.SetActive(shouldShow);

        if (shouldShow)
        {
            placementIndicator.transform.SetPositionAndRotation(
                placementPose.position,
                placementPose.rotation
            );
        }
    }

    private void PlaceSelectedFurniture(InputAction.CallbackContext context)
    {
        if (!placementPoseIsValid || selectedFurniturePrefab == null)
            return;

        GameObject placedObject = Instantiate(
            selectedFurniturePrefab,
            placementPose.position,
            placementPose.rotation
        );

        placedObjects.Add(placedObject);
    }

    public void SelectFurnitureByIndex(int index)
    {
        if (index < 0 || index >= furniturePrefabs.Count)
        {
            Debug.LogWarning("Invalid furniture index selected.");
            return;
        }

        selectedFurniturePrefab = furniturePrefabs[index];
        Debug.Log("Selected furniture: " + selectedFurniturePrefab.name);

        if (selectedFurnitureButtonText != null)
            selectedFurnitureButtonText.text = selectedFurniturePrefab.name;

        CloseFurnitureMenu();
    }

    public void OpenFurnitureMenu()
    {
        Debug.Log("Attempting to open menu");
        if (furnitureSelectionPanel != null)
            furnitureSelectionPanel.SetActive(true);
    }

    public void CloseFurnitureMenu()
    {
        if (furnitureSelectionPanel != null)
            furnitureSelectionPanel.SetActive(false);
    }
    public void DeleteLastPlacedObject()
    {
        if (placedObjects.Count == 0)
            return;

        GameObject lastObject = placedObjects[placedObjects.Count - 1];
        placedObjects.RemoveAt(placedObjects.Count - 1);

        if (lastObject != null)
            Destroy(lastObject);
    }

    public void ClearAllPlacedObjects()
    {
        foreach (GameObject obj in placedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }

        placedObjects.Clear();
    }
}