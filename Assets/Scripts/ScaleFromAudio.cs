using UnityEngine;

public class ScaleFromAudio : MonoBehaviour
{
    public AudioDetection detector;
    public GameObject landingParticlePrefab;

    public float loudnessSens = 80f;
    public float threshold = 2f;

    public float jumpMultiplier = 3f;
    public float maxJumpHeight = 6f;

    public float jumpLerpSpeed = 8f;
    public float gravity = 35f;

    public float scaleStiffness = 250f;
    public float scaleDamping = 12f;

    public float bounciness = 0.35f;
    public float bounceThreshold = 5f;

    public float wobbleStiffness = 300f;
    public float wobbleDamping = 15f;

    public float hoverBobAmount = 0.15f;
    public float hoverBobSpeed = 8f;

    private Vector3 currentScale = Vector3.one;
    private Vector3 targetScale = Vector3.one;
    private Vector3 scaleVelocity = Vector3.zero;

    private float currentWobbleAngle = 0f;
    private float wobbleVelocity = 0f;

    private float baseY;
    private float currentY;
    private float targetY;

    private bool isJumping = false;
    private bool isFalling = false;
    private float verticalVelocity = 0f;

    private float yHoverVelocity = 0f;

    void Start()
    {
        baseY = transform.position.y;
        currentY = baseY;
    }

    void Update()
    {
        float loudness = detector.GetLoudnessFromMicrpohone() * loudnessSens;

        if (!isJumping)
        {
            if (loudness > threshold)
            {
                isJumping = true;
                isFalling = false;
                verticalVelocity = 0f;
            }
        }
        else
        {
            if (loudness > threshold)
            {
                isFalling = false;

                float baseTarget = baseY + (loudness * jumpMultiplier);
                if (baseTarget > baseY + maxJumpHeight)
                {
                    baseTarget = baseY + maxJumpHeight;
                }

                float bobbing = Mathf.Sin(Time.time * hoverBobSpeed) * hoverBobAmount;
                targetY = baseTarget + bobbing;

                float smoothTime = 1f / jumpLerpSpeed;
                currentY = Mathf.SmoothDamp(currentY, targetY, ref yHoverVelocity, smoothTime);

                verticalVelocity = 0f;
            }
            else
            {
                isFalling = true;
            }

            if (isFalling)
            {
                verticalVelocity -= gravity * Time.deltaTime;
                currentY += verticalVelocity * Time.deltaTime;

                if (currentY <= baseY)
                {
                    float impactForce = Mathf.Abs(verticalVelocity);

                    float squashImpact = Mathf.Max(impactForce, 15f);
                    scaleVelocity += new Vector3(squashImpact * 0.4f, -squashImpact * 0.8f, squashImpact * 0.4f);

                    wobbleVelocity += Random.Range(-impactForce, impactForce) * 5f;

                    if (landingParticlePrefab != null)
                    {
                        Vector3 particlePos = new Vector3(transform.position.x, baseY, transform.position.z);
                        GameObject particle = Instantiate(landingParticlePrefab, particlePos, Quaternion.identity);
                        Destroy(particle, 1f);
                    }

                    if (impactForce > bounceThreshold)
                    {
                        verticalVelocity = impactForce * bounciness;
                        currentY = baseY + 0.05f;
                    }
                    else
                    {
                        currentY = baseY;
                        isJumping = false;
                        isFalling = false;
                        verticalVelocity = 0f;
                    }
                }
            }
        }

        transform.position = new Vector3(transform.position.x, currentY, transform.position.z);

        if (isJumping)
        {
            float currentSpeed = (!isFalling) ? Mathf.Abs(yHoverVelocity) : Mathf.Abs(verticalVelocity);
            float stretch = Mathf.Clamp(currentSpeed * 0.04f, 0f, 0.6f);
            targetScale = new Vector3(1f - (stretch * 0.4f), 1f + stretch, 1f - (stretch * 0.4f));
        }
        else
        {
            targetScale = Vector3.one;
        }

        Vector3 displacement = targetScale - currentScale;
        Vector3 springForce = (displacement * scaleStiffness) - (scaleVelocity * scaleDamping);

        scaleVelocity += springForce * Time.deltaTime;
        currentScale += scaleVelocity * Time.deltaTime;
        transform.localScale = currentScale;

        float wobbleForce = (0f - currentWobbleAngle) * wobbleStiffness - (wobbleVelocity * wobbleDamping);
        wobbleVelocity += wobbleForce * Time.deltaTime;
        currentWobbleAngle += wobbleVelocity * Time.deltaTime;
    }

    void LateUpdate()
    {
        if (Mathf.Abs(currentWobbleAngle) > 0.01f)
        {
            transform.Rotate(0, 0, currentWobbleAngle);
        }
    }
}