using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MovementByHeadGame : MonoBehaviour
{
    public GameObject obstaclePrefab;
    public GameObject groundBlockPrefab;

    public bool invertAxis = false;
    public float aspectMultiplier = 1.5f;
    public float smoothSpeed = 25f;
    public float maxLimitX = 2.0f;

    public float speed = 15f;
    public float laneDistance = 1.2f;

    public float groundWidthMultiplier = 1.5f;

    public float groundBlockLength = 1.4f;

    public float groundYOffset = -0.8f;

    public float obstacleYOffset = 1.2f;

    public float spawnTimer = 0f;
    public bool isGameOver = false;
    public List<Transform> obstacles = new List<Transform>();
    public List<Transform> groundBlocks = new List<Transform>();
    private float groundSpawnTimer = 0f;

    private int lastLane = -1;
    private float distanceTraveled = 0f;
    private float currentScore = 0f;
    private int maxScore = 0;
    public float scoreMultiplier = 0.25f;

    private Vector3 camForward;
    private Vector3 camRight;
    private Vector3 startPos;
    private float fixedGroundY;

    void Start()
    {
        startPos = transform.position;
        maxScore = PlayerPrefs.GetInt("MaxScore", 0);

        fixedGroundY = transform.position.y - (transform.localScale.y / 2f);

        camForward = UnityEngine.Camera.main.transform.forward;
        camForward.y = 0;
        if (camForward.sqrMagnitude < 0.01f) camForward = Vector3.forward;
        camForward.Normalize();

        camRight = UnityEngine.Camera.main.transform.right;
        camRight.y = 0;
        if (camRight.sqrMagnitude < 0.01f) camRight = Vector3.right;
        camRight.Normalize();
    }

    public void Update()
    {
        if (isGameOver)
        {
            bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool touched = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

            if (mouseClicked || touched) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (MovementByHead.Instance == null || !MovementByHead.Instance.IsCalibrated) return;

        float distanceThisFrame = speed * Time.deltaTime;
        distanceTraveled += distanceThisFrame;
        currentScore = distanceTraveled * scoreMultiplier;
        speed += Time.deltaTime * 0.2f;

        float faceX = MovementByHead.Instance.FacePositionX;
        if (invertAxis) faceX = 1f - faceX;

        float targetOffset = ((faceX - 0.5f) * aspectMultiplier) * 6f;
        targetOffset = Mathf.Clamp(targetOffset, -maxLimitX, maxLimitX);

        Vector3 targetPos = startPos + (camRight * targetOffset);
        targetPos.y = transform.position.y;

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);

        float groundY = fixedGroundY + groundYOffset;

        spawnTimer += Time.deltaTime;
        float currentSpawnInterval = Mathf.Max(0.4f, 20f / speed);

        if (spawnTimer > currentSpawnInterval)
        {
            int lane = Random.Range(0, 3);
            while (lane == lastLane) lane = Random.Range(0, 3);
            lastLane = lane;

            float xMult = lane - 1f;
            Vector3 spawnPos = startPos + (camForward * 35f) + (camRight * (xMult * laneDistance));

            spawnPos.y = groundY + obstacleYOffset;

            GameObject obs;
            if (obstaclePrefab != null)
            {
                obs = Instantiate(obstaclePrefab, spawnPos, obstaclePrefab.transform.rotation);
            }
            else
            {
                obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obs.transform.position = spawnPos + Vector3.up * 0.75f;
                obs.transform.localScale = Vector3.one * 1.5f;
                obs.GetComponent<Renderer>().material.color = Color.red;
            }

            Collider col = obs.GetComponent<Collider>();
            if (col != null) Destroy(col);

            obstacles.Add(obs.transform);
            Destroy(obs, 8f);
            spawnTimer = 0f;
        }

        groundSpawnTimer += Time.deltaTime;
        float currentGroundSpawnInterval = groundBlockLength / speed;

        if (groundSpawnTimer > currentGroundSpawnInterval)
        {
            float[] laneOffsets = new float[] { -laneDistance, 0f, laneDistance };

            foreach (float offset in laneOffsets)
            {
                Vector3 spawnPos = startPos + (camForward * 35f) + (camRight * offset);
                spawnPos.y = groundY;

                GameObject groundBlock;
                if (groundBlockPrefab != null)
                {
                    groundBlock = Instantiate(groundBlockPrefab, spawnPos, groundBlockPrefab.transform.rotation);

                    Vector3 newScale = groundBlock.transform.localScale;
                    newScale.x *= groundWidthMultiplier;
                    groundBlock.transform.localScale = newScale;
                }
                else
                {
                    groundBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    groundBlock.transform.position = spawnPos;
                    groundBlock.transform.localScale = new Vector3(laneDistance, 0.2f, groundBlockLength);
                    Destroy(groundBlock.GetComponent<Collider>());
                }

                groundBlocks.Add(groundBlock.transform);
                Destroy(groundBlock, 8f);
            }

            groundSpawnTimer -= currentGroundSpawnInterval;
            if (groundSpawnTimer > currentGroundSpawnInterval) groundSpawnTimer = 0f;
        }

        for (int i = groundBlocks.Count - 1; i >= 0; i--)
        {
            if (groundBlocks[i] == null) { groundBlocks.RemoveAt(i); continue; }
            groundBlocks[i].Translate(-camForward * speed * Time.deltaTime, Space.World);
        }

        for (int i = obstacles.Count - 1; i >= 0; i--)
        {
            if (obstacles[i] == null) { obstacles.RemoveAt(i); continue; }
            obstacles[i].Translate(-camForward * speed * Time.deltaTime, Space.World);

            Vector3 distOffset = transform.position - obstacles[i].position;
            float distSide = Mathf.Abs(Vector3.Dot(distOffset, camRight));
            float distForward = Mathf.Abs(Vector3.Dot(distOffset, camForward));
            float distUp = Mathf.Abs(distOffset.y);

            if (distSide < 0.9f && distUp < 1.4f && distForward < 0.9f)
            {
                isGameOver = true;
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