using UnityEngine;
using UnityEngine.UI;

public class FollowWheelText : MonoBehaviour
{
    public Transform rollerClamp; // Assign RollerClamp
    private Camera mainCamera;
    private RectTransform rectTransform;

    void Start()
    {
        mainCamera = Camera.main;
        rectTransform = GetComponent<RectTransform>();
        if (rollerClamp == null)
        {
            Debug.LogWarning($"RollerClamp is not assigned on {gameObject.name}!");
        }
        if (mainCamera == null)
        {
            Debug.LogError("Camera not found!");
        }
    }

    void LateUpdate()
    {
        if (rollerClamp != null && mainCamera != null)
        {
            Vector3 screenPos = mainCamera.WorldToScreenPoint(rollerClamp.position);
            screenPos.x += 100f;
            rectTransform.position = screenPos;
        }
    }
}