using UnityEngine;
using TMPro;

public class IVFormulaDisplay : MonoBehaviour
{
    public WheelController_Test wheelController; // Reference to wheel controller
    public TextMeshProUGUI formulaText; // Assign IVFormulaText (TextMeshProUGUI)
    public Transform rollerClamp; // Assign RollerClamp for positioning
    public float maxFlowRate = 1500f; // Max flow rate in mL/hour at 100%
    private Camera mainCamera;
    private RectTransform rectTransform;

    void Start()
    {
        mainCamera = Camera.main;
        rectTransform = GetComponent<RectTransform>();
        if (wheelController == null)
        {
            Debug.LogWarning("WheelController is not assigned in IVFormulaDisplay!");
            wheelController = FindObjectOfType<WheelController_Test>();
        }
        if (formulaText == null)
        {
            Debug.LogError("IVFormulaText is not assigned in IVFormulaDisplay!");
        }
        if (rollerClamp == null)
        {
            Debug.LogWarning("RollerClamp is not assigned in IVFormulaDisplay!");
        }
        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
        }
        UpdateFormulaText();
    }

    void LateUpdate()
    {
        if (rollerClamp != null && mainCamera != null)
        {
            // Position text to the left of RollerClamp
            Vector3 screenPos = mainCamera.WorldToScreenPoint(rollerClamp.position);
            screenPos.x -= 100f; // Offset left (adjust as needed)
            rectTransform.position = screenPos;
        }
        UpdateFormulaText();
    }

    void UpdateFormulaText()
    {
        if (wheelController == null || formulaText == null)
        {
            return;
        }
        // Get percentage (0% at zMax = 0.266, 100% at zMin = -0.212)
        float t = (wheelController.transform.localPosition.z - wheelController.zMin) / (wheelController.zMax - wheelController.zMin);
        float percentage = Mathf.Lerp(100f, 0f, t);
        // Calculate flow rate (mL/hour)
        float flowRate = (percentage / 100f) * maxFlowRate;
        formulaText.text = $"Flow Rate: {flowRate:F0} mL/h";
        Debug.Log($"IVFormulaText updated: {formulaText.text}, Percentage: {percentage:F1}%");
    }
}