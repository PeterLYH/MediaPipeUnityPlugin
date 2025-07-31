using System.Collections;
using Mediapipe.Tasks.Vision.HandLandmarker;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mediapipe.Unity.Sample.HandLandmarkDetection
{
    public class HandLandmarkerRunner : VisionTaskApiRunner<HandLandmarker>
    {
        [SerializeField] private HandLandmarkerResultAnnotationController _handLandmarkerResultAnnotationController;

        private Experimental.TextureFramePool _textureFramePool;

        public readonly HandLandmarkDetectionConfig config = new HandLandmarkDetectionConfig();

        // Delegate includes flow value
        public delegate void HandLandmarkResultHandler(HandLandmarkerResult result, float flow);
        public event HandLandmarkResultHandler OnHandLandmarkResult;

        public override void Stop()
        {
            base.Stop();
            _textureFramePool?.Dispose();
            _textureFramePool = null;
        }

        protected override IEnumerator Run()
        {
            Debug.Log($"Delegate = {config.Delegate}");
            Debug.Log($"Image Read Mode = {config.ImageReadMode}");
            Debug.Log($"Running Mode = {config.RunningMode}");
            Debug.Log($"NumHands = {config.NumHands}");
            Debug.Log($"MinHandDetectionConfidence = {config.MinHandDetectionConfidence}");
            Debug.Log($"MinHandPresenceConfidence = {config.MinHandPresenceConfidence}");
            Debug.Log($"MinTrackingConfidence = {config.MinTrackingConfidence}");

            yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

            var options = config.GetHandLandmarkerOptions(config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnHandLandmarkDetectionOutput : null);
            taskApi = HandLandmarker.CreateFromOptions(options, GpuManager.GpuResources);
            var imageSource = ImageSourceProvider.ImageSource;

            yield return imageSource.Play();

            if (!imageSource.isPrepared)
            {
                Debug.LogError("Failed to start ImageSource, exiting...");
                yield break;
            }

            _textureFramePool = new Experimental.TextureFramePool(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32, 10);
            screen.Initialize(imageSource);
            SetupAnnotationController(_handLandmarkerResultAnnotationController, imageSource);

            var transformationOptions = imageSource.GetTransformationOptions();
            var flipHorizontally = transformationOptions.flipHorizontally;
            var flipVertically = transformationOptions.flipVertically;
            var imageProcessingOptions = new Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: (int)transformationOptions.rotationAngle);

            AsyncGPUReadbackRequest req = default;
            var waitUntilReqDone = new WaitUntil(() => req.done);
            var waitForEndOfFrame = new WaitForEndOfFrame();
            var result = HandLandmarkerResult.Alloc(options.numHands);

            var canUseGpuImage = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && GpuManager.GpuResources != null;
            using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;

            while (true)
            {
                if (isPaused)
                {
                    yield return new WaitWhile(() => isPaused);
                }

                if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }

                Image image;
                switch (config.ImageReadMode)
                {
                    case ImageReadMode.GPU:
                        if (!canUseGpuImage)
                        {
                            throw new System.Exception("ImageReadMode.GPU is not supported");
                        }
                        textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                        image = textureFrame.BuildGPUImage(glContext);
                        yield return waitForEndOfFrame;
                        break;
                    case ImageReadMode.CPU:
                        yield return waitForEndOfFrame;
                        textureFrame.ReadTextureOnCPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                        image = textureFrame.BuildCPUImage();
                        textureFrame.Release();
                        break;
                    case ImageReadMode.CPUAsync:
                    default:
                        req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
                        yield return waitUntilReqDone;

                        if (req.hasError)
                        {
                            Debug.LogWarning($"Failed to read texture from the image source");
                            continue;
                        }
                        image = textureFrame.BuildCPUImage();
                        textureFrame.Release();
                        break;
                }

                switch (taskApi.runningMode)
                {
                    case Tasks.Vision.Core.RunningMode.IMAGE:
                        if (taskApi.TryDetect(image, imageProcessingOptions, ref result))
                        {
                            _handLandmarkerResultAnnotationController.DrawNow(result);
                        }
                        else
                        {
                            _handLandmarkerResultAnnotationController.DrawNow(default);
                        }
                        break;
                    case Tasks.Vision.Core.RunningMode.VIDEO:
                        if (taskApi.TryDetectForVideo(image, GetCurrentTimestampMillisec(), imageProcessingOptions, ref result))
                        {
                            _handLandmarkerResultAnnotationController.DrawNow(result);
                        }
                        else
                        {
                            _handLandmarkerResultAnnotationController.DrawNow(default);
                        }
                        break;
                    case Tasks.Vision.Core.RunningMode.LIVE_STREAM:
                        taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
                        break;
                }
            }
        }

        private void OnHandLandmarkDetectionOutput(HandLandmarkerResult result, Image image, long timestamp)
        {
            float flow = CalculateFlow(result);
            _handLandmarkerResultAnnotationController.DrawLater(result);
            OnHandLandmarkResult?.Invoke(result, flow);
        }

        private float CalculateFlow(HandLandmarkerResult result)
        {
            if (result.handLandmarks == null || result.handLandmarks.Count == 0)
            {
                Debug.Log("No hands detected, flow: 0%");
                return 0f;
            }

            // Use the first detected hand
            var handLandmarks = result.handLandmarks[0];
            if (handLandmarks.landmarks == null || handLandmarks.landmarks.Count < 21)
            {
                Debug.Log("Insufficient landmarks, flow: 0%");
                return 0f;
            }

            // Get landmark positions
            var thumbTip = handLandmarks.landmarks[4]; // Thumb tip
            var indexDip = handLandmarks.landmarks[7]; // Index finger DIP joint
            var littleTip = handLandmarks.landmarks[20]; // Little finger tip

            // Calibrate normalized units per cm using index-to-little distance (~7 cm)
            float indexToLittleDist = Mathf.Sqrt(
                Mathf.Pow(indexDip.x - littleTip.x, 2) +
                Mathf.Pow(indexDip.y - littleTip.y, 2)
            );
            float cmToNormalized = indexToLittleDist / 7f; // ~7 cm real-world distance
            float offsetIndex = -0.5f * cmToNormalized; // -1 cm for index DIP
            float offsetLittle = 0.5f * cmToNormalized; // +1 cm for little tip

            // Adjust positions
            float adjIndexX = indexDip.x + offsetIndex; // Move left/down
            float adjIndexY = indexDip.y + offsetIndex;
            float adjLittleX = littleTip.x + offsetLittle; // Move right/up
            float adjLittleY = littleTip.y + offsetLittle;

            // Log positions
            Debug.Log($"Thumb Tip (4): ({thumbTip.x:F3}, {thumbTip.y:F3})");
            Debug.Log($"Index DIP (7): ({indexDip.x:F3}, {indexDip.y:F3})");
            Debug.Log($"Little Tip (20): ({littleTip.x:F3}, {littleTip.y:F3})");
            Debug.Log($"Adjusted Index: ({adjIndexX:F3}, {adjIndexY:F3})");
            Debug.Log($"Adjusted Little: ({adjLittleX:F3}, {adjLittleY:F3})");
            Debug.Log($"Index-to-Little Distance: {indexToLittleDist:F3}, cmToNormalized: {cmToNormalized:F3}");

            // Calculate 2D Euclidean distances
            float distToIndex = Mathf.Sqrt(
                Mathf.Pow(thumbTip.x - adjIndexX, 2) +
                Mathf.Pow(thumbTip.y - adjIndexY, 2)
            );
            float distToLittle = Mathf.Sqrt(
                Mathf.Pow(thumbTip.x - adjLittleX, 2) +
                Mathf.Pow(thumbTip.y - adjLittleY, 2)
            );

            // Log distances
            Debug.Log($"Distance to Index DIP: {distToIndex:F3}, Distance to Little Tip: {distToLittle:F3}");

            // Linear interpolation for flow (0% at little finger, 100% at index finger DIP)
            float totalDist = distToIndex + distToLittle;
            if (totalDist == 0) // Avoid division by zero
            {
                Debug.Log("Total distance is zero, flow: 0%");
                return 0f;
            }

            float flow = distToLittle / totalDist; // 0 (at little) to 1 (at index DIP)
            flow = Mathf.Clamp01(flow);
            Debug.Log($"Flow: {flow * 100:F1}%");

            return flow;
        }
    }
}