using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class Character : MonoBehaviour
{
    public enum MovementStyle
    {
        LaneBasedAngle,
        DirectTracking
    }

    public MovementStyle currentMovementStyle = MovementStyle.LaneBasedAngle;

    public float speed = 15f;
    public float lerpSpeed = 25f;
    public bool invertAxis = false;
    public float laneDistance = 3.0f;

    public int desiredLane = 1;
    [Range(2f, 30f)] public float moveThresholdLeft = 6f;
    [Range(2f, 30f)] public float moveThresholdRight = 6f;
    [Range(0.1f, 1.5f)] public float moveCooldown = 0.5f;

    public float trackingSensitivity = 15f;
    public float spawnTimer = 0f;
    public bool isGameOver = false;
    public List<Transform> obstacles = new List<Transform>();

    public List<Transform> roadLines = new List<Transform>();
    public float roadSpawnTimer = 0f;

    public float _gameStartTime = -1f;
    public float _currentRawAngle = 0f;
    public float _moveCooldownTimer = 0f;
    public float _leftAnchor = 0f;
    public float _rightAnchor = 0f;
    public float _targetLeftWall = 0f;
    public float _targetRightWall = 0f;
    public float _visualLeftWall = 0f;
    public float _visualRightWall = 0f;
    public float _referenceAngle = 0f;

    public Texture2D _guiBackground;

    public void Start()
    {
        _guiBackground = MakeTex(2, 2, new Color(0.05f, 0.05f, 0.05f, 0.85f));

        _targetLeftWall = -moveThresholdLeft;
        _targetRightWall = moveThresholdRight;
        _visualLeftWall = _targetLeftWall;
        _visualRightWall = _targetRightWall;
    }

    public void Update()
    {
        if (Keyboard.current != null && Keyboard.current.digit1Key.isPressed)
        {
            Application.targetFrameRate = 15;
        }

        if (isGameOver) return;
        if (MovementByHead.Instance == null || !MovementByHead.Instance.IsCalibrated) return;

        if (_gameStartTime < 0f) _gameStartTime = Time.time;
        speed += Time.deltaTime * 0.2f;

        float safeDeltaTime = Mathf.Min(Time.deltaTime, 0.05f);
        float targetX = 0f;

        if (currentMovementStyle == MovementStyle.LaneBasedAngle)
        {
            float rawHeadAngle = MovementByHead.Instance.HeadPosition.x;
            if (rawHeadAngle > 45f) rawHeadAngle -= 90f;
            else if (rawHeadAngle < -45f) rawHeadAngle += 90f;

            _currentRawAngle = rawHeadAngle;
            if (invertAxis) _currentRawAngle = -_currentRawAngle;

            if (_moveCooldownTimer > 0f)
            {
                _moveCooldownTimer -= Time.deltaTime;
                if (_referenceAngle > 0f && _currentRawAngle < -2f) _moveCooldownTimer = 0f;
                else if (_referenceAngle < 0f && _currentRawAngle > 2f) _moveCooldownTimer = 0f;
            }

            if (_moveCooldownTimer > 0f)
            {
                if (_currentRawAngle > _rightAnchor) _rightAnchor = _currentRawAngle;
                if (_currentRawAngle < _leftAnchor) _leftAnchor = _currentRawAngle;
                if (_currentRawAngle >= 0) _referenceAngle = _rightAnchor;
                else _referenceAngle = _leftAnchor;
            }
            else
            {
                if (_currentRawAngle > _rightAnchor + moveThresholdRight)
                {
                    if (desiredLane < 2) desiredLane++;
                    _rightAnchor = _currentRawAngle;
                    _referenceAngle = _rightAnchor;
                    _moveCooldownTimer = moveCooldown;
                }
                else if (_currentRawAngle < _leftAnchor - moveThresholdLeft)
                {
                    if (desiredLane > 0) desiredLane--;
                    _leftAnchor = _currentRawAngle;
                    _referenceAngle = _leftAnchor;
                    _moveCooldownTimer = moveCooldown;
                }
                else
                {
                    if (_currentRawAngle >= 0f)
                    {
                        if (_currentRawAngle < moveThresholdRight) _rightAnchor = 0f;
                        else _rightAnchor = Mathf.Min(_rightAnchor, _currentRawAngle);

                        _leftAnchor = 0f;
                        _referenceAngle = _rightAnchor;
                    }
                    else
                    {
                        if (_currentRawAngle > -moveThresholdLeft) _leftAnchor = 0f;
                        else _leftAnchor = Mathf.Max(_leftAnchor, _currentRawAngle);

                        _rightAnchor = 0f;
                        _referenceAngle = _leftAnchor;
                    }
                }
            }

            _targetRightWall = _rightAnchor + moveThresholdRight;
            _targetLeftWall = _leftAnchor - moveThresholdLeft;

            _visualLeftWall = Mathf.Lerp(_visualLeftWall, _targetLeftWall, safeDeltaTime * 15f);
            _visualRightWall = Mathf.Lerp(_visualRightWall, _targetRightWall, safeDeltaTime * 15f);

            targetX = (desiredLane - 1) * laneDistance;
        }
        else if (currentMovementStyle == MovementStyle.DirectTracking)
        {
            float faceOffset = (MovementByHead.Instance.FacePositionX - 0.5f);

            if (invertAxis) faceOffset = -faceOffset;

            targetX = faceOffset * trackingSensitivity;

            float maxRoadWidth = laneDistance * 1.5f;
            targetX = Mathf.Clamp(targetX, -maxRoadWidth, maxRoadWidth);

            _currentRawAngle = faceOffset * 90f;
            _visualLeftWall = -maxRoadWidth;
            _visualRightWall = maxRoadWidth;
        }

        Vector3 targetPos = transform.position;
        targetPos.x = targetX;

        if (currentMovementStyle == MovementStyle.LaneBasedAngle)
        {
            transform.position = Vector3.Lerp(transform.position, targetPos, safeDeltaTime * lerpSpeed);
        }
        else
        {
            transform.position = targetPos;
        }

        spawnTimer += Time.deltaTime;
        float currentSpawnInterval = Mathf.Max(0.4f, 20f / speed);

        if (spawnTimer > currentSpawnInterval)
        {
            int lane = Random.Range(0, 3);
            float xPos = (lane - 1) * laneDistance;
            GameObject obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obs.transform.position = new Vector3(xPos, transform.position.y, transform.position.z + 40f);
            obs.transform.localScale = Vector3.one * 1.5f;
            obs.GetComponent<Renderer>().material.color = Color.red;
            Destroy(obs.GetComponent<Collider>());
            obstacles.Add(obs.transform);
            Destroy(obs, 10f);
            spawnTimer = 0f;
        }

        /*
        roadSpawnTimer += Time.deltaTime;
        if (roadSpawnTimer > 0.15f)
        {
            float[] lineXPositions = new float[] {
                -laneDistance * 1.5f,
                -laneDistance * 0.5f,
                 laneDistance * 0.5f,
                 laneDistance * 1.5f
            };

            foreach (float xPos in lineXPositions)
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.transform.position = new Vector3(xPos, transform.position.y - 0.5f, transform.position.z + 50f);
                line.transform.localScale = new Vector3(0.15f, 0.05f, 3f);

                Renderer lineRenderer = line.GetComponent<Renderer>();
                lineRenderer.material.color = new Color(0f, 0.8f, 1f, 0.8f);

                Destroy(line.GetComponent<Collider>());
                roadLines.Add(line.transform);
                Destroy(line, 4f);
            }
            roadSpawnTimer = 0f;
        }

        for (int i = roadLines.Count - 1; i >= 0; i--)
        {
            if (roadLines[i] == null) { roadLines.RemoveAt(i); continue; }
            roadLines[i].Translate(Vector3.back * speed * Time.deltaTime, Space.World);
        }

        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            if (obstacles[i] == null)
            {
                obstacles.RemoveAt(i);
                continue;
            }

            obstacles[i].Translate(Vector3.back * speed * Time.deltaTime, Space.World);

            if (Vector3.Distance(transform.position, obstacles[i].position) < 1.5f)
            {
                isGameOver = true;
            }
        }*/
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    /*
    private void DrawLine(Vector2 pointA, Vector2 pointB, Color color, float width)
    {
        Matrix4x4 matrix = GUI.matrix;
        Color savedColor = GUI.color;
        GUI.color = color;
        float angle = Mathf.Atan2(pointB.y - pointA.y, pointB.x - pointA.x) * Mathf.Rad2Deg;
        GUIUtility.RotateAroundPivot(angle, pointA);
        GUI.DrawTexture(new Rect(pointA.x, pointA.y, Vector2.Distance(pointA, pointB), width), Texture2D.whiteTexture);
        GUI.matrix = matrix;
        GUI.color = savedColor;
    }
    */

    /*
    private void OnGUI()
    {
        if (MovementByHead.Instance != null && MovementByHead.Instance.IsCalibrated && !isGameOver)
        {
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = _guiBackground;
            GUI.Box(new Rect(20, 20, 380, 280), "", boxStyle);

            GUIStyle textStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
            textStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            GUIStyle valueStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };

            valueStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(35, 30, 200, 30), "MOD:", textStyle);
            GUI.Label(new Rect(210, 30, 150, 30), currentMovementStyle.ToString(), valueStyle);

            GUI.Label(new Rect(35, 65, 200, 30), "HAREKET GÜCÜ:", textStyle);
            GUI.Label(new Rect(210, 65, 150, 30), $"{Mathf.Abs(_currentRawAngle):F1}°", valueStyle);

            if (currentMovementStyle == MovementStyle.LaneBasedAngle)
            {
                GUI.Label(new Rect(35, 100, 200, 30), "DURUM:", textStyle);
                if (_moveCooldownTimer > 0f)
                {
                    valueStyle.normal.textColor = new Color(1f, 0.5f, 0f);
                    GUI.Label(new Rect(210, 100, 150, 30), "BEKLİYOR", valueStyle);
                }
                else
                {
                    valueStyle.normal.textColor = Color.green;
                    GUI.Label(new Rect(210, 100, 150, 30), "HAZIR", valueStyle);
                }
            }

            Vector2 pivot = new Vector2(210, 260);
            float armLength = 100f;

            float refLeftRad = (-90f + _visualLeftWall) * Mathf.Deg2Rad;
            float refRightRad = (-90f + _visualRightWall) * Mathf.Deg2Rad;

            Vector2 refLeftEnd = pivot + new Vector2(Mathf.Cos(refLeftRad), Mathf.Sin(refLeftRad)) * armLength;
            Vector2 refRightEnd = pivot + new Vector2(Mathf.Cos(refRightRad), Mathf.Sin(refRightRad)) * armLength;

            DrawLine(pivot, refLeftEnd, new Color(1, 0, 0, 0.5f), 3f);
            DrawLine(pivot, refRightEnd, new Color(1, 0, 0, 0.5f), 3f);

            float currentAngleRad = (-90f + _currentRawAngle) * Mathf.Deg2Rad;
            Vector2 headEnd = pivot + new Vector2(Mathf.Cos(currentAngleRad), Mathf.Sin(currentAngleRad)) * armLength;

            DrawLine(pivot, headEnd, Color.green, 6f);

            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(pivot.x - 6, pivot.y - 6, 12, 12), Texture2D.whiteTexture);
        }

        GUI.skin.label.fontSize = 32;
        GUI.skin.label.alignment = TextAnchor.MiddleCenter;

        if (MovementByHead.Instance == null || !MovementByHead.Instance.IsCalibrated)
        {
            GUI.color = new Color(0, 0, 0, 0.9f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            GUI.color = Color.yellow;
            if (MovementByHead.Instance != null)
                GUI.Label(new Rect(50, Screen.height / 3f, Screen.width - 100, 300), MovementByHead.Instance.CalibrationText);
        }
        else if (isGameOver)
        {
            GUI.color = Color.red;
            GUI.skin.label.fontSize = 50;
            GUI.Label(new Rect(0, Screen.height / 2f - 100, Screen.width, 100), "GAME OVER");

            GUI.color = Color.white;
            GUI.skin.label.fontSize = 30;
            GUI.Label(new Rect(0, Screen.height / 2f + 20, Screen.width, 100), "Tekrar oynamak için ekrana dokun");

            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.TouchDown)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
    */
}