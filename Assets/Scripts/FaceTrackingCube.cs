using UnityEngine;
using Mediapipe.Tasks.Components.Containers;

public class FaceTrackingCube : MonoBehaviour
{
    public static FaceTrackingCube Instance;

  
    public float minX = -3f;
    public float maxX = 3f;
    [Range(0f, 1f)] public float smoothTime = 0.15f;

    public float jumpForce = 5f;
    public float returnSpeed = 2f;

  
    public float blinkThreshold = 0.015f;

    public float _targetX;
    public float _currentVelocityX;
    public float _baseY;
    public float _currentY;
    public Renderer _cubeRenderer;

    public volatile bool _hasNewData = false;
    public volatile float _latestFaceY = 0f;
    public volatile bool _latestEyeClosed = false;
    public bool _wasEyeClosedLastFrame = false;

    public GUIStyle _guiStyle;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _targetX = transform.position.x;
        _baseY = transform.position.y;
        _currentY = _baseY;
        _cubeRenderer = GetComponent<Renderer>();

        _guiStyle = new GUIStyle();
        _guiStyle.fontSize = 60;
        _guiStyle.fontStyle = FontStyle.Bold;
    }

    void Update()
    {
        if (_hasNewData)
        {
            _targetX = Mathf.Lerp(minX, maxX, _latestFaceY);

            if (_cubeRenderer != null)
            {
                _cubeRenderer.material.color = _latestEyeClosed ? Color.red : Color.white;
            }

            _hasNewData = false;
        }

        if (_latestEyeClosed && !_wasEyeClosedLastFrame)
        {
            _currentY += jumpForce;
        }
        _wasEyeClosedLastFrame = _latestEyeClosed;

        _currentY = Mathf.MoveTowards(_currentY, _baseY, returnSpeed * Time.deltaTime);

        float maxAllowedY = _baseY + 15.0f;
        if (_currentY > maxAllowedY)
        {
            _currentY = maxAllowedY;
        }

        Vector3 currentPos = transform.position;
        float newX = Mathf.SmoothDamp(currentPos.x, _targetX, ref _currentVelocityX, smoothTime);

        transform.position = new Vector3(newX, _currentY, currentPos.z);
    }

    private void OnGUI()
    {
        _guiStyle.normal.textColor = _latestEyeClosed ? Color.red : Color.green;
        string statusText = _latestEyeClosed ? "GOZ KAPALI" : "GOZ ACIK";

        GUI.Label(new UnityEngine.Rect(50, 100, 600, 150), statusText, _guiStyle);
    }

    public void ProcessFaceData(NormalizedLandmarks faceData)
    {
        if (faceData.landmarks == null || faceData.landmarks.Count == 0) return;

        var list = faceData.landmarks;
        float faceY = list[4].y;

        Vector3 leftUpper = new Vector3(list[159].x, list[159].y, list[159].z);
        Vector3 leftLower = new Vector3(list[145].x, list[145].y, list[145].z);
        Vector3 rightUpper = new Vector3(list[386].x, list[386].y, list[386].z);
        Vector3 rightLower = new Vector3(list[374].x, list[374].y, list[374].z);

        float leftEyeDist = Vector3.Distance(leftUpper, leftLower);
        float rightEyeDist = Vector3.Distance(rightUpper, rightLower);
        Debug.Log(leftEyeDist);
        Debug.Log(rightEyeDist);
        _latestFaceY = faceY;
        _latestEyeClosed = (leftEyeDist < blinkThreshold || rightEyeDist < blinkThreshold);
        _hasNewData = true;
    }
}