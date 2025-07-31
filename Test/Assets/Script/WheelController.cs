using UnityEngine;
using TMPro;

public class WheelControllerZAxis : MonoBehaviour
{
    public float zMin = -0.212f; // Lower z-axis limit (100%)
    public float zMax = 0.266f; // Upper z-axis limit (0%)
    public float moveSpeed = 0.001f; // Touch sensitivity
    public float rotationSpeed = 1f; // Degrees per unit for rolling
    public TextMeshProUGUI percentageText; // Assign PercentageText (TextMeshProUGUI)

    private Vector3 initialXY; // Lock initial x and y
    private float percentage; // Store current percentage
    private Vector2 touchStartPos;
    private bool isDragging;

    void Start()
    {
        initialXY = transform.localPosition; // Store initial x and y
        // Try to find PercentageText if not assigned
        if (percentageText == null)
        {
            GameObject textObj = GameObject.Find("PercentageText");
            if (textObj != null)
            {
                percentageText = textObj.GetComponent<TextMeshProUGUI>();
                if (percentageText != null)
                {
                    Debug.Log($"Found and assigned PercentageText: {textObj.name}");
                }
                else
                {
                    Debug.LogError($"PercentageText GameObject found, but it lacks TextMeshProUGUI component!");
                }
            }
            else
            {
                Debug.LogError("Could not find GameObject named 'PercentageText' in the scene!");
            }
        }
        else
        {
            Debug.Log($"PercentageText assigned to: {percentageText.gameObject.name}");
        }
        UpdatePercentageText(); // Initialize percentage
        Debug.Log($"Initial Wheel Position: {initialXY}, Initial Percentage: {percentage:F1}%");
    }

    void Update()
    {
        Vector3 newPos = transform.localPosition;

        // Handle touch input for AR
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                touchStartPos = touch.position;
                isDragging = true;
            }
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                // Reversed direction: drag up (positive y) ¡÷ z = -0.212 (100%), drag down (negative y) ¡÷ z = 0.266 (0%)
                float deltaY = touchStartPos.y - touch.position.y; // Negate to reverse
                newPos.z = Mathf.Clamp(newPos.z + deltaY * moveSpeed*0.01f, zMin, zMax);
                float deltaZ = newPos.z - transform.localPosition.z;
                transform.localPosition = newPos;

                // Rotate around local x-axis for rolling
                float rotationAmount = -deltaZ * rotationSpeed;
                transform.Rotate(rotationAmount, 0, 0, Space.Self);

                UpdatePercentageText();
                Debug.Log($"Current Wheel Position: {transform.localPosition}, Percentage: {percentage:F1}%");

                touchStartPos = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isDragging = false;
            }
        }

        // Lock x and y
        newPos.x = initialXY.x;
        newPos.y = initialXY.y;
        transform.localPosition = newPos;
    }

    void UpdatePercentageText()
    {
        // Reverse mapping: zMax (0.266) = 0%, zMin (-0.212) = 100%
        float t = (transform.localPosition.z - zMin) / (zMax - zMin);
        percentage = Mathf.Lerp(100f, 0f, t);
        if (percentageText != null)
        {
            percentageText.text = $"{percentage:F1}%"; // Display with 1 decimal place
            Debug.Log($"Updated PercentageText to: {percentageText.text}");
        }
        else
        {
            Debug.LogWarning("PercentageText is null, cannot update text!");
        }
    }

    void OnDrawGizmos()
    {
        // Visualize z-axis path
        Gizmos.color = Color.green;
        Vector3 start = transform.parent ? transform.parent.TransformPoint(new Vector3(initialXY.x, initialXY.y, zMin)) : new Vector3(initialXY.x, initialXY.y, zMin);
        Vector3 end = transform.parent ? transform.parent.TransformPoint(new Vector3(initialXY.x, initialXY.y, zMax)) : new Vector3(initialXY.x, initialXY.y, zMax);
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(transform.position, 0.01f); // Visualize pivot
    }
}