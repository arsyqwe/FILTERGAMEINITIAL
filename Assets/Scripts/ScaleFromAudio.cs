using UnityEngine;

public class ScaleFromAudio : MonoBehaviour
{
    public AudioDetection detector;

    public float loudnessSens = 80f;
    public float threshold = 0.1f;

    public float jumpMultiplier = 3f;
    public float maxJumpHeight = 6f;

    public float jumpLerpSpeed = 15f; 
    public float gravity = 40f; 

    private float baseY;
    private float currentY;
    private float targetY;

    private bool isJumping = false;
    private bool isFalling = false;
    private float verticalVelocity = 0f;

    void Start()
    {
        baseY = transform.position.y;
        currentY = baseY;
    }

    void Update()
    {
        if (!isJumping)
        {
            float loudness = detector.GetLoudnessFromMicrpohone() * loudnessSens;

            if (loudness > threshold)
            {
                isJumping = true;
                isFalling = false;

                targetY = baseY + (loudness * jumpMultiplier);
                if (targetY > baseY + maxJumpHeight)
                {
                    targetY = baseY + maxJumpHeight;
                }
            }
            else
            {
                currentY = baseY;
            }
        }
        else
        {
            if (!isFalling)
            {
                currentY = Mathf.Lerp(currentY, targetY, Time.deltaTime * jumpLerpSpeed);

                if (Mathf.Abs(currentY - targetY) < 0.1f)
                {
                    isFalling = true;
                    verticalVelocity = 0f;
                }
            }
            else
            {
                verticalVelocity -= gravity * Time.deltaTime;
                currentY += verticalVelocity * Time.deltaTime;

                if (currentY <= baseY)
                {
                    currentY = baseY;
                    isJumping = false; 
                }
            }
        }

        transform.position = new Vector3(transform.position.x, currentY, transform.position.z);
    }
}