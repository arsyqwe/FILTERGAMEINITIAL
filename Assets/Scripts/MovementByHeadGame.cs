using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class MovementByHeadGame : MonoBehaviour
{
    public GameObject[] obstaclePrefabs;
    public GameObject[] groundBlockPrefabs;
    public GameObject[] sceneryPrefabs;
    public GameObject[] bushPrefabs;
    public GameObject[] sideGroundPrefabs;

    public GameObject wallObstaclePrefab;

    public float wallSpawnProbability = 0.1f;
    public float delayAfterWall = 5.0f;

    public bool invertAxis = false;
    public float aspectMultiplier = 3.5f;
    public float smoothSpeed = 25f;
    public float maxLimitX = 2.0f;

    public float speed = 15f;
    public float speedIncreaseRate = 0.5f;

    public float laneDistance = 1.2f;
    public float groundWidthMultiplier = 1.5f;
    public float groundBlockLength = 0.8f;

    public float groundYOffset = -0.8f;
    public float obstacleYOffset = 0.16f;
    public float tallObstacleYOffset = 0.42f;

    public float tiltMultiplier = 15f;
    public float spawnDistance = 55f;
    public float scenerySpawnDistance = 3.5f;

    public float sideGroundOffset = 3.5f;
    public float sideGroundStep = 2.8f;
    public float sideGroundWidthMultiplier = 2.5f;

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

    void Start()
    {
        startPos = transform.position;
        maxScore = PlayerPrefs.GetInt("MaxScore", 0);

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
        float maxGroundDistance = spawnDistance + 20f;
        int blocksNeeded = Mathf.CeilToInt(maxGroundDistance / groundBlockLength);

        float[] laneOffsets = new float[] { -laneDistance, 0f, laneDistance };
        float[] sideOffsets = new float[] {-sideGroundOffset,sideGroundOffset,-sideGroundOffset - sideGroundStep,sideGroundOffset + sideGroundStep };

        for (int i = 0; i < blocksNeeded; i++)
        {
            float currentZDistance = i * groundBlockLength;
            foreach (float offset in laneOffsets) SpawnMainGroundTile(currentZDistance, offset);
            foreach (float offset in sideOffsets) SpawnSideGroundTile(currentZDistance, offset);
        }
    }

    public void Update()
    {
        if (isGameOver)
        {
            bool mouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool touched = Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;

            if (mouseClicked || touched)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            return;
        }

        if (MovementByHead.Instance == null || !MovementByHead.Instance.IsCalibrated) return;

        speed += Time.deltaTime * speedIncreaseRate;

        float distanceThisFrame = speed * Time.deltaTime;
        distanceTraveled += distanceThisFrame;
        currentScore = distanceTraveled * scoreMultiplier;

        float faceX = MovementByHead.Instance.FacePositionX;
        if (invertAxis) faceX = 1f - faceX;

        float targetOffset = ((faceX - 0.5f) * aspectMultiplier) * 6f;
        targetOffset = Mathf.Clamp(targetOffset, -laneDistance, laneDistance);

        Vector3 targetPos = startPos + (camRight * targetOffset);
        Vector3 currentPos = transform.position;
        currentPos.x = Mathf.Lerp(currentPos.x, targetPos.x, Time.deltaTime * smoothSpeed);
        transform.position = currentPos;

        float movementDeltaX = targetPos.x - transform.position.x;
        float targetTiltAngle = movementDeltaX * -tiltMultiplier;
        Quaternion targetRotation = Quaternion.Euler(0, 0, targetTiltAngle);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 10f);

        spawnTimer += Time.deltaTime;

        if (spawnTimer > 0f)
        {
            float currentSpawnInterval = Mathf.Max(0.4f, 20f / speed);

            if (spawnTimer > currentSpawnInterval)
            {
                if (Random.value < wallSpawnProbability)
                {
                    SpawnWallObstacle();
                    spawnTimer = -delayAfterWall; 
                }
                else
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
                            if (selectedPrefab.name.ToLower().Contains("tall")) spawnPos.y = tallObstacleYOffset;
                            else spawnPos.y = obstacleYOffset;

                            obs = Instantiate(selectedPrefab, spawnPos, selectedPrefab.transform.rotation);
                        }
                    }

                    if (obs == null)
                    {
                        spawnPos.y = obstacleYOffset;
                        obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        obs.transform.position = spawnPos;
                        obs.transform.localScale = Vector3.one * 1.5f;
                        obs.GetComponent<Renderer>().material.color = Color.red;
                        Destroy(obs.GetComponent<Collider>());
                    }

                    RegisterAndScaleObject(obs.transform);
                    obstacles.Add(obs.transform);
                    Destroy(obs, 10f);

                    spawnTimer = 0f;
                }
            }
        }

        scenerySpawnTimer += Time.deltaTime;
        float currentScenerySpawnInterval = Mathf.Max(0.2f, 10f / speed);

        if (scenerySpawnTimer > currentScenerySpawnInterval)
        {
            float depthSpread = 12f;
            float widthSpread = 4.0f;

            int treeCount = Random.Range(2, 4);
            for (int i = 0; i < treeCount; i++)
            {
                SpawnScenery(-scenerySpawnDistance - Random.Range(0f, widthSpread), Random.Range(-depthSpread / 2f, depthSpread / 2f));
                SpawnScenery(scenerySpawnDistance + Random.Range(0f, widthSpread), Random.Range(-depthSpread / 2f, depthSpread / 2f));
            }

            int bushCount = Random.Range(4, 8);
            for (int i = 0; i < bushCount; i++)
            {
                SpawnBush(-scenerySpawnDistance - Random.Range(-0.5f, widthSpread + 1f), Random.Range(-depthSpread, depthSpread / 2f));
                SpawnBush(scenerySpawnDistance + Random.Range(-0.5f, widthSpread + 1f), Random.Range(-depthSpread, depthSpread / 2f));
            }

            scenerySpawnTimer = 0f;
        }

        groundSpawnTimer += Time.deltaTime;
        float currentGroundSpawnInterval = groundBlockLength / speed;

        if (groundSpawnTimer > currentGroundSpawnInterval)
        {
            float targetSpawnDist = spawnDistance + 20f;
            float[] laneOffsets = new float[] { -laneDistance, 0f, laneDistance };
            foreach (float offset in laneOffsets) SpawnMainGroundTile(targetSpawnDist, offset);

            float[] sideOffsets = new float[] {
                -sideGroundOffset, sideGroundOffset,
                -sideGroundOffset - sideGroundStep, sideGroundOffset + sideGroundStep
            };
            foreach (float offset in sideOffsets) SpawnSideGroundTile(targetSpawnDist, offset);

            groundSpawnTimer -= currentGroundSpawnInterval;
            if (groundSpawnTimer > currentGroundSpawnInterval) groundSpawnTimer = 0f;
        }

        MoveList(groundBlocks, false, false);
        MoveList(obstacles, true, true);
        MoveList(sceneries, false, true);
    }

    private void SpawnWallObstacle()
    {
        float[] laneOffsets = new float[] { -laneDistance, 0f, laneDistance };
        GameObject prefabToUse = wallObstaclePrefab;

        if (prefabToUse == null && obstaclePrefabs != null && obstaclePrefabs.Length > 0)
        {
            foreach (var p in obstaclePrefabs)
            {
                if (p != null && p.name.ToLower().Contains("tall"))
                {
                    prefabToUse = p;
                    break;
                }
            }
            if (prefabToUse == null) prefabToUse = obstaclePrefabs[0];
        }

        float safeWallDistance = spawnDistance + 25f;

        foreach (float offset in laneOffsets)
        {
            Vector3 spawnPos = startPos + (camForward * safeWallDistance) + (camRight * offset);
            spawnPos.y = tallObstacleYOffset;

            GameObject obs = null;
            if (prefabToUse != null)
            {
                obs = Instantiate(prefabToUse, spawnPos, prefabToUse.transform.rotation);
            }

            if (obs != null)
            {
                RegisterAndScaleObject(obs.transform);
                obstacles.Add(obs.transform);
                Destroy(obs, 15f);
            }
        }
    }

    private void SpawnMainGroundTile(float zDistance, float xOffset)
    {
        Vector3 spawnPos = startPos + (camForward * zDistance) + (camRight * xOffset);
        spawnPos.y = groundYOffset;
        GameObject groundObj = null;

        if (groundBlockPrefabs != null && groundBlockPrefabs.Length > 0)
        {
            GameObject selectedPrefab = groundBlockPrefabs[Random.Range(0, groundBlockPrefabs.Length)];
            if (selectedPrefab != null)
            {
                groundObj = Instantiate(selectedPrefab, spawnPos, selectedPrefab.transform.rotation);
                Vector3 newScale = groundObj.transform.localScale;
                newScale.x *= groundWidthMultiplier;
                groundObj.transform.localScale = newScale;
            }
        }

        if (groundObj == null)
        {
            groundObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            groundObj.transform.position = spawnPos;
            groundObj.transform.localScale = new Vector3(laneDistance, 0.2f, groundBlockLength);
            Destroy(groundObj.GetComponent<Collider>());
        }
        else
        {
            groundBlocks.Add(groundObj.transform);
            Destroy(groundObj, 15f);
        }
    }

    private void SpawnSideGroundTile(float zDistance, float xOffset)
    {
        Vector3 spawnPos = startPos + (camForward * zDistance) + (camRight * xOffset);
        spawnPos.y = groundYOffset - 0.05f;

        if (sideGroundPrefabs == null || sideGroundPrefabs.Length == 0) return;

        GameObject selectedPrefab = sideGroundPrefabs[Random.Range(0, sideGroundPrefabs.Length)];
        if (selectedPrefab == null) return;

        GameObject groundObj = Instantiate(selectedPrefab, spawnPos, selectedPrefab.transform.rotation);

        if (xOffset > 0) groundObj.transform.rotation *= Quaternion.Euler(0, 180f, 0);

        Vector3 newScale = groundObj.transform.localScale;
        newScale.x *= sideGroundWidthMultiplier;
        groundObj.transform.localScale = newScale;

        groundBlocks.Add(groundObj.transform);
        Destroy(groundObj, 15f);
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

            if (applyScale) UpdateObjectScale(obj);

            if (checkCollision)
            {
                Vector3 distOffset = transform.position - obj.position;
                float distSide = Mathf.Abs(Vector3.Dot(distOffset, camRight));
                float distForward = Mathf.Abs(Vector3.Dot(distOffset, camForward));
                float distUp = Mathf.Abs(distOffset.y);

                float hitBoxX = 0.9f;
                float hitBoxY = 1.4f;
                float hitBoxZ = Mathf.Max(0.9f, obj.localScale.z * 0.45f);

                if (obj.name.ToLower().Contains("long"))
                {
                    hitBoxY = 3.0f;
                }
                else if (obj.name.ToLower().Contains("tall"))
                {
                    hitBoxY = 2.5f;
                }

                if (distSide < hitBoxX && distUp < hitBoxY && distForward < hitBoxZ)
                {
                    isGameOver = true;
                    Time.timeScale = 0f;

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

    private void SpawnScenery(float xOffset, float zOffset)
    {
        Vector3 spawnPos = startPos + (camForward * (spawnDistance + zOffset)) + (camRight * xOffset);
        spawnPos.y = groundYOffset;

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

        if (scenery == null) return;
        Destroy(scenery.GetComponent<Collider>());
        RegisterAndScaleObject(scenery.transform);
        sceneries.Add(scenery.transform);
        Destroy(scenery, 15f);
    }

    private void SpawnBush(float xOffset, float zOffset)
    {
        Vector3 spawnPos = startPos + (camForward * (spawnDistance + zOffset)) + (camRight * xOffset);
        spawnPos.y = groundYOffset;

        GameObject bush = null;
        if (bushPrefabs != null && bushPrefabs.Length > 0)
        {
            GameObject prefab = bushPrefabs[Random.Range(0, bushPrefabs.Length)];
            if (prefab != null)
            {
                bush = Instantiate(prefab, spawnPos, prefab.transform.rotation);
                bush.transform.rotation *= Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            }
        }

        if (bush == null) return;
        Destroy(bush.GetComponent<Collider>());
        RegisterAndScaleObject(bush.transform);
        sceneries.Add(bush.transform);
        Destroy(bush, 15f);
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

        float t = Mathf.InverseLerp(spawnDistance + 25f, spawnDistance + 5f, distForward);
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