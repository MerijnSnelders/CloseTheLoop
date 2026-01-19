using UnityEngine;

public class PlayerAnimationDriver : MonoBehaviour
{
    [Header("Assign These")]
    public Animator playerAnim;
    public Rigidbody myRb;
    public Transform mainCamera;

    [Header("Settings")]
    public float runTiltAngle = -30f;
    public float jumpTiltAngle = -30f; // NEW: Tilt way back when jumping
    public float tiltSpeed = 5f;
    public float bodyOffsetBack = 0.25f;

    private float currentTilt = 0f;

    void Update()
    {
        if (playerAnim == null || myRb == null || mainCamera == null) return;

        // 1. SPEED
        Vector3 flatVel = myRb.linearVelocity; // Use 'velocity' for older Unity
        flatVel.y = 0;
        float currentSpeed = flatVel.magnitude;
        playerAnim.SetFloat("Speed", currentSpeed * 2f);

        // 2. CALCULATE TILT (Updated Logic)
        float targetTilt = 0f;

        float currentTiltSpeed = tiltSpeed; // Default smooth speed

        // A. Jumping (Top Priority)
        if (Mathf.Abs(myRb.linearVelocity.y) > 1.0f)
        {
            targetTilt = jumpTiltAngle;
            currentTiltSpeed = 20f; // SNAP FAST! Don't let the camera clip.
        }
        // B. Running
        else if (currentSpeed > 2.0f)
        {
            targetTilt = runTiltAngle;

            // THE FIX:
            // If we are transitioning INTO the run (going from 0 to -15), go FAST.
            // If we are stopping (going from -15 to 0), go SLOW (looks natural).
            if (currentTilt > targetTilt)
                currentTiltSpeed = 15f; // Fast Snap into the lean
            else
                currentTiltSpeed = 5f;  // Smooth return to upright
        }
        // C. Idle
        else
        {
            targetTilt = 0f;
            currentTiltSpeed = 5f; // Smooth return to upright
        }

        // Apply the variable speed
        currentTilt = Mathf.MoveTowards(currentTilt, targetTilt, Time.deltaTime * currentTiltSpeed);

        // 3. LOCK ROTATION & POSITION
        Vector3 lookDir = mainCamera.forward;
        lookDir.y = 0;

        if (lookDir.sqrMagnitude > 0.01f)
        {
            // A. Rotation
            Quaternion faceForward = Quaternion.LookRotation(lookDir);
            Quaternion leanBack = Quaternion.Euler(currentTilt, 0, 0);
            playerAnim.transform.rotation = faceForward * leanBack;

            // B. Position Lock
            Vector3 targetPos = mainCamera.position - (lookDir.normalized * bodyOffsetBack);
            targetPos.y = playerAnim.transform.position.y;

            playerAnim.transform.position = targetPos;
        }

        // 4. JUMP TRIGGER
        if (myRb.linearVelocity.y > 2.0f)
        {
            if (!playerAnim.GetCurrentAnimatorStateInfo(0).IsName("Jump"))
                playerAnim.SetTrigger("Jump");
        }
    }
}