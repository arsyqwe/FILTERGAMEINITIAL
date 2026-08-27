using UnityEngine;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.FaceLandmarker;

public class MovementByHead : MonoBehaviour
{
    public static MovementByHead Instance;

    public Vector2 HeadPosition;
    public Vector2 CenterPosition;

    public float FacePositionX;
    public float CenterFaceX = 0.5f;

    public bool IsCalibrated = false;
    public string CalibrationText = "YÜZ BEKLENİYOR...";

    private int _calibrationFrames = 0;
    public const int CALIBRATION_FRAME_TARGET = 60;

    private readonly object _lock = new object();
    private bool _hasNewData = false;
    private Vector2 _pendingHeadPos;
    private float _pendingFaceX;

    private Vector2 _sumPositions = Vector2.zero;
    private float _sumFaceX = 0f;

    public float blinkThreshold = 0.015f;
    private bool _pendingJump = false;
    private bool _isJumpTriggered = false;
    private bool _wasEyeClosedLastFrame = false;

    private void Awake()
    {
        Instance = this;
    }

    public void ProcessLandmarks(FaceLandmarkerResult result)
    {
        if (result.faceLandmarks == null || result.faceLandmarks.Count == 0) return;

        var landmarks = result.faceLandmarks[0].landmarks;

        lock (_lock)
        {
            Vector2 leftSide = new Vector2(landmarks[234].x, landmarks[234].y);
            Vector2 rightSide = new Vector2(landmarks[454].x, landmarks[454].y);

            float dx = rightSide.x - leftSide.x;
            float dy = rightSide.y - leftSide.y;
            float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;

            _pendingHeadPos = new Vector2(angle, 0f);

            _pendingFaceX = landmarks[1].x;

            Vector3 leftUpper = new Vector3(landmarks[159].x, landmarks[159].y, landmarks[159].z);
            Vector3 leftLower = new Vector3(landmarks[145].x, landmarks[145].y, landmarks[145].z);
            Vector3 rightUpper = new Vector3(landmarks[386].x, landmarks[386].y, landmarks[386].z);
            Vector3 rightLower = new Vector3(landmarks[374].x, landmarks[374].y, landmarks[374].z);

            float leftEyeDist = Vector3.Distance(leftUpper, leftLower);
            float rightEyeDist = Vector3.Distance(rightUpper, rightLower);

            bool isEyeClosed = (leftEyeDist < blinkThreshold && rightEyeDist < blinkThreshold);

            if (isEyeClosed && !_wasEyeClosedLastFrame)
            {
                _pendingJump = true;
            }
            _wasEyeClosedLastFrame = isEyeClosed;

            _hasNewData = true;
        }
    }

    private void Update()
    {
        Vector2 currentHeadPos = Vector2.zero;
        float currentFaceX = 0f;
        bool processThisFrame = false;

        lock (_lock)
        {
            if (_hasNewData)
            {
                currentHeadPos = _pendingHeadPos;
                currentFaceX = _pendingFaceX;

                if (_pendingJump)
                {
                    _isJumpTriggered = true;
                    _pendingJump = false;
                }

                _hasNewData = false;
                processThisFrame = true;
            }
        }

        if (processThisFrame)
        {
            HeadPosition = Vector2.Lerp(HeadPosition, currentHeadPos, Time.deltaTime * 15f);

            FacePositionX = currentFaceX;

            if (!IsCalibrated)
            {
                _sumPositions += HeadPosition;
                _sumFaceX += FacePositionX;
                _calibrationFrames++;
                CalibrationText = $"KAFA AÇISI HESAPLANIYOR...\n% {(_calibrationFrames * 100) / CALIBRATION_FRAME_TARGET}";

                if (_calibrationFrames >= CALIBRATION_FRAME_TARGET)
                {
                    CenterPosition = _sumPositions / CALIBRATION_FRAME_TARGET;
                    CenterFaceX = _sumFaceX / CALIBRATION_FRAME_TARGET;
                    IsCalibrated = true;
                    CalibrationText = "HAZIR!";
                }
            }
        }
    }

    public bool ConsumeJump()
    {
        if (_isJumpTriggered)
        {
            _isJumpTriggered = false;
            return true;
        }
        return false;
    }
}