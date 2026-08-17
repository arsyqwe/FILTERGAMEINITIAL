using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MovementByHeadGame : MonoBehaviour
{
    public bool invertAxis = false;
    public float aspectMultiplier = 1.5f;
    public float smoothSpeed = 25f;
    public float maxLimitX = 4.0f;

    public float speed = 15f;
    public float laneDistance = 3.0f;

    public float spawnTimer = 0f;
    public bool isGameOver = false;
    public List<Transform> obstacles = new List<Transform>();
    public List<Transform> roadLines = new List<Transform>();
    public float roadSpawnTimer = 0f;

    private float distanceTraveled = 0f; 
    private float currentScore = 0f;
    private int maxScore = 0;

    public float scoreMultiplier = 0.25f;

    private float baseY;

    void Start()
    {
        baseY = transform.position.y;

        maxScore = PlayerPrefs.GetInt("MaxScore", 0);
    }

    public void Update()
    {
        if (isGameOver)
        {
            bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool touched = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

            if (mouseClicked || touched)
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

        if (MovementByHead.Instance == null || !MovementByHead.Instance.IsCalibrated) return;

        float distanceThisFrame = speed * Time.deltaTime;
        distanceTraveled += distanceThisFrame;

        currentScore = distanceTraveled * scoreMultiplier;

        //speed += Time.deltaTime * 0.2f;

        float faceX = MovementByHead.Instance.FacePositionX;
        if (invertAxis) faceX = 1f - faceX;

        float viewportX = 0.5f + ((faceX - 0.5f) * aspectMultiplier);
        Vector3 cubeViewportPos = UnityEngine.Camera.main.WorldToViewportPoint(transform.position);
        Vector3 exactWorldPos = UnityEngine.Camera.main.ViewportToWorldPoint(new Vector3(viewportX, cubeViewportPos.y, cubeViewportPos.z));

        float targetX = exactWorldPos.x;
        targetX = Mathf.Clamp(targetX, -maxLimitX, maxLimitX);

        Vector3 targetPos = new Vector3(targetX, transform.position.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);

        float groundY = baseY - (transform.localScale.y / 2f);

        spawnTimer += Time.deltaTime;
        float currentSpawnInterval = Mathf.Max(0.4f, 20f / speed);

        if (spawnTimer > currentSpawnInterval)
        {
            int lane = Random.Range(0, 3);
            float xPos = (lane - 1) * laneDistance;
            GameObject obs = GameObject.CreatePrimitive(PrimitiveType.Cube);

            float obsScale = 1.5f;
            obs.transform.position = new Vector3(xPos, groundY + (obsScale / 2f), transform.position.z + 40f);
            obs.transform.localScale = Vector3.one * obsScale;
            obs.GetComponent<Renderer>().material.color = Color.red;
            Destroy(obs.GetComponent<Collider>());
            obstacles.Add(obs.transform);
            Destroy(obs, 10f);
            spawnTimer = 0f;
        }

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

                line.transform.position = new Vector3(xPos, groundY + 0.01f, transform.position.z + 50f);
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

            float distX = Mathf.Abs(transform.position.x - obstacles[i].position.x);
            float distY = Mathf.Abs(transform.position.y - obstacles[i].position.y);
            float distZ = Mathf.Abs(transform.position.z - obstacles[i].position.z);

            if (distX < 1.2f && distY < 1.5f && distZ < 1.2f)
            {
                isGameOver = true;
                Debug.Log("ÇARPTIN! OYUN BİTTİ.");

                int finalScore = Mathf.FloorToInt(currentScore);
                if (finalScore > maxScore)
                {
                    maxScore = finalScore;
                    PlayerPrefs.SetInt("MaxScore", maxScore);
                    PlayerPrefs.Save();
                }
            }
        }
    }

    private void OnGUI()
    {
        if (isGameOver)
        {
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

            GUI.color = Color.red;
            GUI.skin.label.fontSize = 50;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(0, Screen.height / 2f - 150, Screen.width, 100), "GAME OVER");

            GUI.color = Color.yellow;
            GUI.skin.label.fontSize = 35;
            GUI.Label(new Rect(0, Screen.height / 2f - 40, Screen.width, 50), $"SKOR: {Mathf.FloorToInt(currentScore)} m");
            GUI.Label(new Rect(0, Screen.height / 2f + 10, Screen.width, 50), $"MAX SKOR: {maxScore} m");

            GUI.color = Color.white;
            GUI.skin.label.fontSize = 25;
            GUI.skin.label.fontStyle = FontStyle.Normal;
            GUI.Label(new Rect(0, Screen.height / 2f + 90, Screen.width, 50), "Tekrar oynamak için ekrana tıkla");
        }
        else if (MovementByHead.Instance != null && MovementByHead.Instance.IsCalibrated)
        {
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            GUI.skin.label.fontSize = 28;
            GUI.skin.label.fontStyle = FontStyle.Bold;

            GUI.color = Color.white;
            GUI.Label(new Rect(20, 20, 300, 40), $"Mesafe: {Mathf.FloorToInt(currentScore)} m");

            GUI.color = new Color(1f, 0.8f, 0f);
            GUI.Label(new Rect(20, 55, 300, 40), $"Max: {maxScore} m");
        }
    }
}