using UnityEngine;

public class PlaceOnGround : MonoBehaviour
{
    public GameObject rollerClampPrefab; // Assign RollerClamp prefab
    public GameObject groundPlane; // Assign Ground Plane
    public FollowWheelText followWheelText; // Assign PercentageText¡¦s FollowWheelText
    private GameObject currentInstance; // Track current instance
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (followWheelText == null)
        {
            Debug.LogWarning("FollowWheelText is not assigned!");
        }
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit) && hit.collider.gameObject == groundPlane)
            {
                if (currentInstance != null)
                {
                    Destroy(currentInstance);
                }
                currentInstance = Instantiate(rollerClampPrefab, hit.point, rollerClampPrefab.transform.rotation);
                // Add WaterDropController
                GameObject waterDropController = new GameObject("WaterDropController");
                waterDropController.transform.SetParent(currentInstance.transform);
                waterDropController.transform.localPosition = new Vector3(0f, 2f, -0.1f); // Below clamp
                WaterDrop waterDrop = waterDropController.AddComponent<WaterDrop>();
                waterDrop.waterDropPrefab = Resources.Load<GameObject>("WaterDrop"); // Load prefab
                waterDrop.waterMaterial = Resources.Load<Material>("WaterMaterial"); // Load material
                waterDrop.wheelController = currentInstance.GetComponentInChildren<WheelController_Test>();
                if (followWheelText != null)
                {
                    followWheelText.rollerClamp = currentInstance.transform;
                    Debug.Log($"Assigned RollerClamp to FollowWheelText: {currentInstance.name}");
                }
                WheelController_Test wheelController = currentInstance.GetComponentInChildren<WheelController_Test>();
                if (wheelController != null && wheelController.percentageText == null)
                {
                    Debug.LogWarning("PercentageText is not assigned in WheelController_Test!");
                }
            }
            else
            {
                Debug.LogWarning("No ground plane detected!");
            }
        }
    }
}