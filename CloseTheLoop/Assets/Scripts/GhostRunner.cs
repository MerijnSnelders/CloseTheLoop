using System.Collections.Generic;
using UnityEngine;

public class GhostRunner : MonoBehaviour
{
    private List<Vector3> positions;
    private List<Quaternion> rotations;
    private int index = 0;
    private float stopDistance = 0.5f;

    // ANIMATION
    private Animator myAnim;
    private Vector3 lastPosition;
    private float smoothedSpeed = 0f; // NEW: Helps keep running smooth

    public void Initialize(List<Vector3> posList, List<Quaternion> rotList)
    {
        positions = new List<Vector3>(posList);
        rotations = new List<Quaternion>(rotList);

        myAnim = GetComponentInChildren<Animator>();
        lastPosition = transform.position;
    }

    public void SkipToFrame(int targetIndex)
    {
        if (positions == null) return;
        index = Mathf.Clamp(targetIndex, 0, positions.Count - 1);
        transform.position = positions[index];
        transform.rotation = rotations[index];
        lastPosition = transform.position;
        smoothedSpeed = 0f;
    }

    public int GetCurrentFrameIndex()
    {
        return index;
    }

    void FixedUpdate()
    {
        if (positions == null || index >= positions.Count)
        {
            if (myAnim) myAnim.SetFloat("Speed", 0f);
            return;
        }

        Vector3 targetPos = positions[index];
        Vector3 moveDir = targetPos - transform.position;
        float distanceToNextStep = moveDir.magnitude;
        bool blocked = false;

        // Wall Check
        if (distanceToNextStep > 0.001f)
        {
            if (Physics.Raycast(transform.position, moveDir.normalized, out RaycastHit hit, stopDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.CompareTag("Player") && !hit.collider.CompareTag("Ghost"))
                {
                    blocked = true;
                }
            }
        }

        if (!blocked)
        {
            // 1. ROTATE (Look ahead)
            Vector3 lookDir = moveDir;
            lookDir.y = 0;

            if (lookDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 20f * Time.fixedDeltaTime);
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, rotations[index], 10f * Time.fixedDeltaTime);
            }

            // 2. MOVE
            transform.position = targetPos;
            index++;
        }
        else
        {
            // Blocked: Just rotate
            transform.rotation = Quaternion.Slerp(transform.rotation, rotations[index], 10f * Time.fixedDeltaTime);
        }

        // --- ANIMATION LOGIC (SMOOTHED) ---
        if (myAnim)
        {
            // A. Calculate Raw Speed
            Vector3 horizontalMove = (transform.position - lastPosition);
            horizontalMove.y = 0;
            float rawSpeed = (horizontalMove.magnitude / Time.fixedDeltaTime) * 500f;

            // B. Smooth it out! (This ignores tiny stutters)
            // Lerp from current value to raw value. The '5f' controls how "lazy" the smoothing is.
            smoothedSpeed = Mathf.Lerp(smoothedSpeed, rawSpeed, 10f * Time.fixedDeltaTime);

            // C. Send to Animator
            myAnim.SetFloat("Speed", smoothedSpeed);

            // Detect Jump (Sudden upward movement)
            float verticalChange = transform.position.y - lastPosition.y;
            if (verticalChange > 0.1f)
            {
                myAnim.SetTrigger("Jump");
            }
        }

        lastPosition = transform.position;
    }
}