using UnityEngine;

public class WaterDrop : MonoBehaviour
{
    public float yStart = 2f; // Starting y-position (y = 2)
    public float yEnd = 1f; // Ending y-position (y = 1)
    public float minDropSpeed = 0f; // Speed at 0% (z = 0.266)
    public float maxDropSpeed = 2f; // Speed at 100% (z = -0.212)
    public float cycleDelay = 1f; // Delay between cycles (seconds)
    public GameObject waterDropPrefab; // Assign WaterDrop 3D model prefab
    public Material waterMaterial; // Assign WaterMaterial (URP Lit)
    public WheelController_Test wheelController; // Reference to wheel controller

    private Vector3 initialPosition; // Base position for drops
    private GameObject dropInstance; // Current drop
    private bool isDropping; // Track if drop is active
    private float timer; // Timer for cycle delay

    void Start()
    {
        initialPosition = transform.position;
        timer = 0f;
        if (waterDropPrefab == null)
        {
            Debug.LogError("WaterDropPrefab is not assigned in WaterDrop!");
            enabled = false; // Disable script
            return;
        }
        if (waterMaterial == null)
        {
            Debug.LogWarning("WaterMaterial is not assigned! Using default URP Lit material.");
            Shader urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader != null)
            {
                waterMaterial = new Material(urpLitShader);
                waterMaterial.color = new Color(0f, 0.5f, 1f, 0.8f);
                waterMaterial.SetFloat("_Surface", 1f); // Transparent
                waterMaterial.SetFloat("_Blend", 1f); // Alpha blending
                waterMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                Debug.LogError("URP Lit shader not found! Check URP setup.");
            }
        }
        if (wheelController == null)
        {
            Debug.LogWarning("WheelController is not assigned in WaterDrop! Attempting to find it.");
            wheelController = FindObjectOfType<WheelController_Test>();
            if (wheelController == null)
            {
                Debug.LogError("Could not find WheelController_Test in scene! Drop will not follow percentage.");
            }
        }
        CreateNewDrop();
    }

    void Update()
    {
        if (wheelController == null)
        {
            Debug.LogWarning("WheelController is null! Drop cannot follow percentage.");
            return;
        }

        float percentage = GetWheelPercentage();
        float dropSpeed = Mathf.Lerp(minDropSpeed, maxDropSpeed, percentage / 100f);
        Debug.Log($"WaterDrop: Drop Speed = {dropSpeed:F2}, Percentage = {percentage:F1}%");

        if (percentage <= 0f && isDropping && dropInstance != null)
        {
            // Destroy drop if percentage is 0%
            Destroy(dropInstance);
            isDropping = false;
            timer = cycleDelay;
            Debug.Log("Percentage is 0%, water drop destroyed.");
        }
        else if (isDropping && dropInstance != null && dropSpeed > 0f)
        {
            // Move drop if percentage > 0%
            Vector3 newPos = dropInstance.transform.position;
            newPos.y -= dropSpeed * Time.deltaTime;
            dropInstance.transform.position = newPos;

            if (newPos.y <= initialPosition.y + yEnd - yStart)
            {
                Destroy(dropInstance);
                isDropping = false;
                timer = cycleDelay;
            }
        }
        else if (!isDropping)
        {
            // Spawn new drop only if percentage > 0%
            timer -= Time.deltaTime;
            if (timer <= 0f && percentage > 0f)
            {
                CreateNewDrop();
            }
        }
    }

    void CreateNewDrop()
    {
        if (!isDropping && waterDropPrefab != null && GetWheelPercentage() > 0f)
        {
            dropInstance = Instantiate(waterDropPrefab, initialPosition, Quaternion.identity);
            dropInstance.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            dropInstance.name = "WaterDropInstance";

            Renderer[] renderers = dropInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                foreach (Renderer renderer in renderers)
                {
                    if (waterMaterial != null)
                    {
                        renderer.material = waterMaterial;
                        Debug.Log($"Applied WaterMaterial to {renderer.gameObject.name}.");
                    }
                    else
                    {
                        Debug.LogError($"WaterMaterial is null! Cannot apply to {renderer.gameObject.name}.");
                    }
                }
            }
            else
            {
                Debug.LogWarning("No renderers found in WaterDrop prefab!");
            }

            isDropping = true;
            Debug.Log($"Created new water drop at y = {initialPosition.y}");
        }
    }

    float GetWheelPercentage()
    {
        if (wheelController == null)
        {
            return 0f; // Default to 0% if no wheel
        }
        float t = (wheelController.transform.localPosition.z - wheelController.zMin) / (wheelController.zMax - wheelController.zMin);
        return Mathf.Lerp(100f, 0f, t);
    }
}