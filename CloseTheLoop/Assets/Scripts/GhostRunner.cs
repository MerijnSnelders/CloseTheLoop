using System.Collections.Generic;
using UnityEngine;

public class GhostRunner : MonoBehaviour
{
    private List<Vector3> positions;
    private List<Quaternion> rotations;
    private int index = 0;
    private float stopDistance = 0.5f;

    public void Initialize(List<Vector3> posList, List<Quaternion> rotList)
    {
        positions = new List<Vector3>(posList);
        rotations = new List<Quaternion>(rotList);
    }

    void FixedUpdate()
    {
        // If finished, just stand still and wait for the Manager to delete us
        if (positions == null || index >= positions.Count) return;

        Vector3 targetPos = positions[index];
        Vector3 direction = targetPos - transform.position;
        float distanceToNextStep = direction.magnitude;
        bool blocked = false;

        // Wall Check
        if (distanceToNextStep > 0.001f)
        {
            if (Physics.Raycast(transform.position, direction.normalized, out RaycastHit hit, stopDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.CompareTag("Player") && !hit.collider.CompareTag("Ghost"))
                {
                    blocked = true;
                }
            }
        }

        if (!blocked)
        {
            transform.position = targetPos;
            transform.rotation = rotations[index];
            index++;
        }
        else
        {
            transform.rotation = rotations[index];
        }
    }
}