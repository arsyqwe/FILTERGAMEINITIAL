using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MovementByHeadGame : MonoBehaviour
{
    public GameObject[] obstaclePrefabs;
    public GameObject groundBlockPrefab;
    public GameObject[] sceneryPrefabs;

    public bool invertAxis = false;
    public float aspectMultiplier = 3.5f;
    public float smoothSpeed = 25f;
    public float maxLimitX = 2.0f;
    public float speed = 15f;
    public float laneDistance = 1.2f;
    public float groundWidthMultiplier = 1.5f;
    public float groundBlockLength = 0.8f;
    public float groundYOffset = -0.8f;
    public float obstacleYOffset = 0.8f;
    public float tallObstacleYOffset = 2.0f;

    public float tiltMultiplier = 15f;

    public float spawnDistance = 55f;
    public float scenerySpawnDistance = 3.5f;

    public float spawnTimer = 0f;
    public bool isGameOver = false;

    public List<Transform> obstacles = new List<Transform>();
    public List<Transform> groundBlocks = new List<Transform>();
    public List<Transform> sceneries = new List<Transform>();

    private Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();

    private float groundSpawnTimer = 0f;
    private float scenerySpawnTimer = 0f;

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

        PreSpawnGround();
    }

    private void PreSpawnGround()
    {
        int blocksNeeded = Mathf.CeilToInt((spawnDistance + 10f) / groundBlockLength);
        float groundY = fixedGroundY + groundYOffset;
        float[] laneOffsets = new float[] { -laneDistance, 0f, laneDistance };

        for (int i = 0; i < blocksNeeded; i++)
        {
            float currentZDistance = i * groundBlockLength;

            foreach (float offset in laneOffsets)
            {
                Vector3 spawnPos = startPos + (camForward * currentZDistance) + (camRight * offset);
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
                Destroy(groundBlock, 10f);
            }
        }
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
        targetOffset = Mathf.Clamp(targetOffset, -laneDistance, laneDistance);

        Vector3 targetPos = startPos + (camRight * targetOffset);
        targetPos.y = transform.position.y;

        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);

        float movementDeltaX = targetPos.x - transform.position.x;
        float targetTiltAngle = movementDeltaX * -tiltMultiplier;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetTiltAngle);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

        float groundY = fixedGroundY + groundYOffset;

        spawnTimer += Time.deltaTime;
        float currentSpawnInterval = Mathf.Max(0.4f, 20f / speed);

        if (spawnTimer > currentSpawnInterval)
        {
            int lane = Random.Range(0, 3);
            while (lane == lastLane) lane = Random.Range(0, 3);
            lastLane = lane;

            float xMult = lane - 1f;
            Vector3 spawnPos = startPos + (camForward * spawnDistance) + (camRight * (xMult * laneDistance));

            GameObject obs = null;

            if (obstaclePrefabs != null && obstaclePrefabs.Length > 0)
            {
                GameObject selectedPrefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
                if (selectedPrefab != null)
                {
                    float currentOffset = obstacleYOffset;
                    if (selectedPrefab.name.ToLower().Contains("tall"))
                    {
                        currentOffset = tallObstacleYOffset;
                    }
                    spawnPos.y = groundY + currentOffset;
                    obs = Instantiate(selectedPrefab, spawnPos, selectedPrefab.transform.rotation);
                }
            }

            if (obs == null)
            {
                spawnPos.y = groundY + obstacleYOffset;
                obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obs.transform.position = spawnPos + Vector3.up * 0.75f;
                obs.transform.localScale = Vector3.one * 1.5f;
                obs.GetComponent<Renderer>().material.color = Color.red;
            }

            Collider col = obs.GetComponent<Collider>();
            if (col != null) Destroy(col);

            RegisterAndScaleObject(obs.transform);
            obstacles.Add(obs.transform);
            Destroy(obs, 10f);
            spawnTimer = 0f;
        }

        scenerySpawnTimer += Time.deltaTime;
        float currentScenerySpawnInterval = Mathf.Max(0.2f, 10f / speed);

        if (scenerySpawnTimer > currentScenerySpawnInterval)
        {
            SpawnScenery(-scenerySpawnDistance);
            SpawnScenery(scenerySpawnDistance);
            scenerySpawnTimer = 0f;
        }

        groundSpawnTimer += Time.deltaTime;
        float currentGroundSpawnInterval = groundBlockLength / speed;

        if (groundSpawnTimer > currentGroundSpawnInterval)
        {
            float[] laneOffsets = new float[] { -laneDistance, 0f, laneDistance };

            foreach (float offset in laneOffsets)
            {
                Vector3 spawnPos = startPos + (camForward * spawnDistance) + (camRight * offset);
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
                Destroy(groundBlock, 10f);
            }

            groundSpawnTimer -= currentGroundSpawnInterval;
            if (groundSpawnTimer > currentGroundSpawnInterval) groundSpawnTimer = 0f;
        }

        MoveList(groundBlocks, false, false);
        MoveList(obstacles, true, true);
        MoveList(sceneries, false, true);
    }

    private void MoveList(List<Transform> list, bool checkCollision, bool applyScale)
    {
        for (int i = list.Count - 1; i >= 0; i--)
        {
            Transform obj = list[i];
            if (obj == null)
            {
                list.RemoveAt(i);
                continue;
            }

            obj.Translate(-camForward * speed * Time.deltaTime, Space.World);

            if (applyScale)
            {
                UpdateObjectScale(obj);
            }

            if (checkCollision)
            {
                Vector3 distOffset = transform.position - obj.position;
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
    }

    private void SpawnScenery(float xOffset)
    {
        Vector3 spawnPos = startPos + (camForward * spawnDistance) + (camRight * xOffset);
        spawnPos.y = fixedGroundY + groundYOffset;

        GameObject scenery = null;
        if (sceneryPrefabs != null && sceneryPrefabs.Length > 0)
        {
            GameObject prefab = sceneryPrefabs[Random.Range(0, sceneryPrefabs.Length)];
            if (prefab != null)
            {
                scenery = Instantiate(prefab, spawnPos, prefab.transform.rotation);
                scenery.transform.rotation *= Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            }
        }

        if (scenery == null)
        {
            scenery = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            scenery.transform.position = spawnPos + Vector3.up * 1f;
            scenery.transform.localScale = new Vector3(0.5f, 2f, 0.5f);
            scenery.GetComponent<Renderer>().material.color = new Color(0.1f, 0.5f, 0.1f);
        }

        Destroy(scenery.GetComponent<Collider>());
        RegisterAndScaleObject(scenery.transform);
        sceneries.Add(scenery.transform);
        Destroy(scenery, 10f);
    }

    private void RegisterAndScaleObject(Transform obj)
    {
        originalScales[obj] = obj.localScale;
        UpdateObjectScale(obj);
    }

    private void UpdateObjectScale(Transform obj)
    {
        if (!originalScales.ContainsKey(obj)) return;

        float distForward = Vector3.Dot(obj.position - transform.position, camForward);

        float t = Mathf.InverseLerp(spawnDistance, spawnDistance - 10f, distForward);
        t = Mathf.Clamp01(t);

        obj.localScale = originalScales[obj] * t;
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
        else if (MovementByHead.Instance.IsCalibrated)
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