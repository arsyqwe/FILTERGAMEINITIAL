using UnityEngine;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.FaceLandmarker;

public class Wrapper : MonoBehaviour
{
    public static Wrapper Instance { get; private set; }

    public bool flipEyes = false;
    [Range(0.08f, 0.30f)] public float blinkSensitivity = 0.16f;
    [Range(0.05f, 0.25f)] public float dominanceMargin = 0.12f;
    public float blinkCooldown = 0.28f;

    public bool IsCalibrated;
    public string CalibrationText  = "YÜZ BEKLENİYOR";

    public bool _leftBlinkReady = false;
    public bool _rightBlinkReady = false;
    public float _lastBlinkTime = 0f;
    public float _leftMaxEAR = -1f, _leftMinEAR = -1f;
    public float _rightMaxEAR = -1f, _rightMinEAR = -1f;
    public float _prevLeftEAR = 0.20f, _prevRightEAR = 0.20f;
    public int _calibrationFrames = 0;
    public const int CALIBRATION_FRAME_TARGET = 120;

    public readonly object _lock = new object();
    public bool _hasNewData = false;
    public float _pendingLeftEAR = 0f;
    public float _pendingRightEAR = 0f;

    private void Awake()
    {
        Instance = this;
    }

    public bool ConsumeLeftBlink()
    {
        bool state = _leftBlinkReady;
        _leftBlinkReady = false;
        return state;
    }

    public bool ConsumeRightBlink()
    {
        bool state = _rightBlinkReady;
        _rightBlinkReady = false;
        return state;
    }

    public void ProcessLandmarks(FaceLandmarkerResult result)
    {
        if (result.faceLandmarks == null || result.faceLandmarks.Count == 0) return;
        var landmarks = result.faceLandmarks[0].landmarks;

        float leftEAR = CalculateEAR(landmarks[33], landmarks[133], landmarks[159], landmarks[145], landmarks[158], landmarks[153]);
        float rightEAR = CalculateEAR(landmarks[362], landmarks[263], landmarks[386], landmarks[374], landmarks[380], landmarks[373]);

        lock (_lock)
        {
            _pendingLeftEAR = flipEyes ? rightEAR : leftEAR;
            _pendingRightEAR = flipEyes ? leftEAR : rightEAR;
            _hasNewData = true;
        }
    }

    public void Update()
    {
        float currentLeftEAR = 0f, currentRightEAR = 0f;
        bool processThisFrame = false;

        lock (_lock)
        {
            if (_hasNewData)
            {
                currentLeftEAR = _pendingLeftEAR;
                currentRightEAR = _pendingRightEAR;
                _hasNewData = false;
                processThisFrame = true;
            }
        }

        if (processThisFrame) ProcessEARDataOnMainThread(currentLeftEAR, currentRightEAR);
    }

    public void ProcessEARDataOnMainThread(float actualLeft, float actualRight)
    {
        if (_leftMaxEAR < 0)
        {
            _leftMaxEAR = actualLeft; _leftMinEAR = actualLeft;
            _rightMaxEAR = actualRight; _rightMinEAR = actualRight;
            _prevLeftEAR = actualLeft; _prevRightEAR = actualRight;
            return;
        }

        _leftMinEAR = Mathf.Lerp(_leftMinEAR, actualLeft, 0.005f);
        _rightMinEAR = Mathf.Lerp(_rightMinEAR, actualRight, 0.005f);
        _leftMaxEAR = Mathf.Max(_leftMaxEAR, actualLeft);
        _leftMinEAR = Mathf.Min(_leftMinEAR, actualLeft);
        _rightMaxEAR = Mathf.Max(_rightMaxEAR, actualRight);
        _rightMinEAR = Mathf.Min(_rightMinEAR, actualRight);

        if (!IsCalibrated)
        {
            _calibrationFrames++;
            CalibrationText = $"GÖZ HASSASİYETİ HESAPLANIYOR...\n% {(_calibrationFrames * 100) / CALIBRATION_FRAME_TARGET}";
            if (_calibrationFrames >= CALIBRATION_FRAME_TARGET)
            {
                IsCalibrated = true;
                CalibrationText = "HAZIR!";
            }
            return;
        }

        if (Time.unscaledTime - _lastBlinkTime < blinkCooldown) return;

        float leftRange = Mathf.Clamp(_leftMaxEAR - _leftMinEAR, 0.04f, 0.18f);
        float rightRange = Mathf.Clamp(_rightMaxEAR - _rightMinEAR, 0.04f, 0.18f);

        float leftRelativeVelocity = (_prevLeftEAR - actualLeft) / leftRange;
        float rightRelativeVelocity = (_prevRightEAR - actualRight) / rightRange;

        _prevLeftEAR = Mathf.Lerp(_prevLeftEAR, actualLeft, (actualLeft > _prevLeftEAR) ? 0.60f : 0.15f);
        _prevRightEAR = Mathf.Lerp(_prevRightEAR, actualRight, (actualRight > _prevRightEAR) ? 0.60f : 0.15f);

        if ((leftRelativeVelocity >= blinkSensitivity) && (leftRelativeVelocity - rightRelativeVelocity >= dominanceMargin))
        {
            _leftBlinkReady = true;
            _lastBlinkTime = Time.unscaledTime;
        }
        else if ((rightRelativeVelocity >= blinkSensitivity) && (rightRelativeVelocity - leftRelativeVelocity >= dominanceMargin))
        {
            _rightBlinkReady = true;
            _lastBlinkTime = Time.unscaledTime;
        }
    }

    private float CalculateEAR(NormalizedLandmark p1, NormalizedLandmark p4, NormalizedLandmark p2, NormalizedLandmark p6, NormalizedLandmark p3, NormalizedLandmark p5)
    {
        float v1 = Vector2.Distance(new Vector2(p2.x, p2.y), new Vector2(p6.x, p6.y));
        float v2 = Vector2.Distance(new Vector2(p3.x, p3.y), new Vector2(p5.x, p5.y));
        float h = Vector2.Distance(new Vector2(p1.x, p1.y), new Vector2(p4.x, p4.y));
        return (h <= 0.0001f) ? 0.3f : (v1 + v2) / (2.0f * h);
    }
}