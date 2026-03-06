using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System;
using TMPro;

public class ARTapToPlaceObjects : MonoBehaviour
{
    [Header("AR Placement")]
    private GameObject previewObject;
    [SerializeField] private Material previewGhostMaterial;
    private enum EditMode
    {
        None,
        Move
    }
    private EditMode currentMode = EditMode.None;
    private GameObject selectedPlacedObject;

    [Header("Furniture Prefabs")]
    [SerializeField] private List<GameObject> furniturePrefabs = new List<GameObject>();

    [Header("UI")]
    [SerializeField] private GameObject furnitureSelectionPanel;
    [SerializeField] private GameObject editControls;
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

    private void Start()
    {
        ResetFurnitureSelection();
    }
    private void Update()
    {
        UpdatePlacementPose();
        UpdatePlacementIndicator();
        UpdateSelectedFurnitureMovement();
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
        if (previewObject == null)
            return;

        bool shouldShow = placementPoseIsValid && selectedFurniturePrefab != null;
        previewObject.SetActive(shouldShow);

        if (shouldShow)
        {
            previewObject.transform.SetPositionAndRotation(
                placementPose.position,
                placementPose.rotation
            );
        }
    }  

    private void UpdateSelectedFurnitureMovement()
    {
        if (currentMode != EditMode.Move)
            return;

        if (selectedPlacedObject == null || !placementPoseIsValid)
            return;

        selectedPlacedObject.transform.position = placementPose.position;
    }

    private void DeselectSelectedFurniture()
    {
        selectedPlacedObject = null;
        currentMode = EditMode.None;

        if (editControls != null)
            editControls.SetActive(false);
    }

    // Prevent placement when interacting with UI
    private bool IsTouchOverUI()
    {
        if (EventSystem.current == null || Touchscreen.current == null)
            return false;
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Touchscreen.current.primaryTouch.position.ReadValue();

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        return results.Count > 0;
    }

    // Select exising furniture if it exists
    private bool TrySelectPlacedFurniture(InputAction.CallbackContext context)
    {
        if (arCamera == null)
            return false;

        Vector2 touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();

        Ray ray = arCamera.ScreenPointToRay(touchPosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject.CompareTag("PlacedFurniture"))
            {
                selectedPlacedObject = hitObject;
                if (editControls != null)
                    editControls.SetActive(true);       
                return true;
            }

            if (hitObject.transform.root.CompareTag("PlacedFurniture"))
            {
                selectedPlacedObject = hitObject.transform.root.gameObject;
                if (editControls != null)
                    editControls.SetActive(true);    
                return true;
            }
        }

        return false;
    }

    // Handle preview display
    private void CreatePreviewObject()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
        }

        if (selectedFurniturePrefab == null)
            return;

        previewObject = Instantiate(selectedFurniturePrefab);
        previewObject.name = selectedFurniturePrefab.name + "_Preview";

        // Disable colliders so the preview doesn't interfere with raycasts
        Collider[] colliders = previewObject.GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        // Apply preview material
        if (previewGhostMaterial != null)
        {
            Renderer[] renderers = previewObject.GetComponentsInChildren<Renderer>();

            foreach (Renderer rend in renderers)
            {
                Material[] mats = new Material[rend.materials.Length];

                for (int i = 0; i < mats.Length; i++)
                    mats[i] = previewGhostMaterial;

                rend.materials = mats;
            }
        }
    }

    private void PlaceSelectedFurniture(InputAction.CallbackContext context)
    {
        if (IsTouchOverUI())
            return;

        if (TrySelectPlacedFurniture(context))
            return;

        // If currently moving, tap confirms the move
        if (currentMode == EditMode.Move)
        {
            ConfirmMoveSelectedFurniture();
            return;
        }
        // If something is selected but user tapped empty space, just deselect
        if (selectedPlacedObject != null)
        {
            DeselectSelectedFurniture();
            return;
        }

        if (currentMode == EditMode.Move)
        {
            ConfirmMoveSelectedFurniture();
            return;
        }

        if (!placementPoseIsValid || selectedFurniturePrefab == null)
            return;

        GameObject placedObject = Instantiate(
            selectedFurniturePrefab,
            placementPose.position,
            placementPose.rotation
        );

        placedObject.tag = "PlacedFurniture";
        placedObjects.Add(placedObject);

        ResetFurnitureSelection();
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

        CreatePreviewObject();

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

    public void ResetFurnitureSelection()
    {
        selectedFurniturePrefab = null;

        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }

        if (selectedFurnitureButtonText != null)
        {
            selectedFurnitureButtonText.text = "Select Furniture";
        }

        if (editControls != null)
            editControls.SetActive(false);
    }

    public void DeleteLastPlacedObject()
    {
        if (placedObjects.Count == 0)
            return;

        GameObject lastObject = placedObjects[placedObjects.Count - 1];
        placedObjects.RemoveAt(placedObjects.Count - 1);

        if (lastObject == selectedPlacedObject)
        {
            selectedPlacedObject = null;
            currentMode = EditMode.None;

            if (editControls != null)
                editControls.SetActive(false);
        }

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
        selectedPlacedObject = null;
        currentMode = EditMode.None;

        if (editControls != null)
            editControls.SetActive(false);
    }
    public void StartMoveSelectedFurniture()
{
    if (selectedPlacedObject == null)
        return;

    currentMode = EditMode.Move;
}

    public void RotateSelectedFurniture()
    {
        if (selectedPlacedObject == null)
            return;

        selectedPlacedObject.transform.Rotate(0f, 45f, 0f);
    }

    public void DeleteSelectedFurniture()
    {
        if (selectedPlacedObject == null)
            return;

        placedObjects.Remove(selectedPlacedObject);
        Destroy(selectedPlacedObject);
        DeselectSelectedFurniture();
    }

    private void ConfirmMoveSelectedFurniture()
    {
        if (selectedPlacedObject == null || !placementPoseIsValid)
            return;

        selectedPlacedObject.transform.SetPositionAndRotation(
            placementPose.position,
            selectedPlacedObject.transform.rotation
        );

        DeselectSelectedFurniture();
    }
}