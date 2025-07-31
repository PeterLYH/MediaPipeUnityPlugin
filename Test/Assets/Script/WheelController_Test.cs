using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using System.Collections.Concurrent;
using TMPro;
using UnityEngine;

public class WheelController_Test : MonoBehaviour
{
    public float zMin = -0.212f; // Lower z-axis limit (100%, flow = 1, thumb at index finger)
    public float zMax = 0.266f; // Upper z-axis limit (0%, flow = 0, thumb at little finger)
    public float rotationSpeed = 1f; // Degrees per unit for rolling
    public float smoothingSpeed = 5f; // Smoothing speed (higher = faster, lower = smoother)
    public TextMeshProUGUI percentageText; // Assign PercentageText (TextMeshProUGUI)
    public HandLandmarkerRunner handLandmarkerRunner; // Reference to HandLandmarkerRunner

    private Vector3 initialXY; // Lock initial x and y
    private float percentage; // Store current percentage
    private float previousZ; // Track previous z-position for rotation
    private ConcurrentQueue<float> flowQueue = new ConcurrentQueue<float>(); // Thread-safe queue for flow values

    void Start()
    {
        initialXY = transform.localPosition; // Store initial x and y
        previousZ = transform.localPosition.z; // Initialize previous z

        // Find and assign PercentageText if not set
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

        // Find and assign HandLandmarkerRunner if not set
        if (handLandmarkerRunner == null)
        {
            handLandmarkerRunner = FindObjectOfType<HandLandmarkerRunner>();
            if (handLandmarkerRunner != null)
            {
                Debug.Log($"Found and assigned HandLandmarkerRunner: {handLandmarkerRunner.gameObject.name}");
            }
            else
            {
                Debug.LogError("Could not find HandLandmarkerRunner in the scene!");
            }
        }

        // Subscribe to the HandLandmarkerRunner's event
        if (handLandmarkerRunner != null)
        {
            handLandmarkerRunner.OnHandLandmarkResult += OnHandLandmarkResultHandler;
        }

        UpdatePercentageText(); // Initialize percentage
        Debug.Log($"Initial Wheel Position: {initialXY}, Initial Percentage: {percentage:F1}%");
    }

    void OnDestroy()
    {
        // Unsubscribe from the event to prevent memory leaks
        if (handLandmarkerRunner != null)
        {
            handLandmarkerRunner.OnHandLandmarkResult -= OnHandLandmarkResultHandler;
        }
    }

    void Update()
    {
        // Process only the latest flow value on the main thread
        float latestFlow = 0f;
        bool hasFlow = false;
        while (flowQueue.TryDequeue(out float flow))
        {
            latestFlow = flow;
            hasFlow = true;
        }
        if (hasFlow)
        {
            ProcessFlow(latestFlow);
        }
    }

    void OnHandLandmarkResultHandler(HandLandmarkerResult result, float flow)
    {
        // Queue the flow value for processing on the main thread
        flowQueue.Enqueue(flow);
    }

    void ProcessFlow(float flow)
    {
        // Map flow (0 to 1) to z-position (zMax to zMin, reversed)
        float t = 1f - flow; // flow = 0 -> t = 1 (zMax), flow = 1 -> t = 0 (zMin)
        float targetZ = Mathf.Lerp(zMin, zMax, t); // Target z-position
        float newZ = Mathf.Lerp(transform.localPosition.z, targetZ, Time.deltaTime * smoothingSpeed); // Smooth z-position
        Vector3 newPos = transform.localPosition;
        newPos.z = newZ;

        // Lock x and y
        newPos.x = initialXY.x;
        newPos.y = initialXY.y;
        transform.localPosition = newPos;

        // Rotate based on z-position change
        float deltaZ = newZ - previousZ;
        float rotationAmount = -deltaZ * rotationSpeed;
        transform.Rotate(rotationAmount, 0, 0, Space.Self);

        // Update previous z for next frame
        previousZ = newZ;

        // Update percentage display
        UpdatePercentageText();
        Debug.Log($"Flow: {flow:F3}, Wheel Position: {transform.localPosition}, Percentage: {percentage:F1}%");
    }

    void UpdatePercentageText()
    {
        // Mapping: zMax (0.266) = 0%, zMin (-0.212) = 100%
        float t = (transform.localPosition.z - zMin) / (zMax - zMin);
        percentage = Mathf.Lerp(100f, 0f, t); // zMin -> 100%, zMax -> 0%
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