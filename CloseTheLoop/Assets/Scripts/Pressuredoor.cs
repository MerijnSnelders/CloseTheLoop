using System.Collections.Generic;
using UnityEngine;

public class PressureDoor : MonoBehaviour
{
    [Header("Settings")]
    public Transform doorObject;
    public float openHeight = 4f;
    public float speed = 5f;

    private Vector3 closedPos;
    private Vector3 openPos;
    private Vector3 targetPos;

    // We store WHO is on the plate. 
    // This allows us to handle multiple objects (You + Ghost) and destroyed objects safely.
    private List<Collider> objectsOnPlate = new List<Collider>();

    void Start()
    {
        closedPos = doorObject.position;
        openPos = closedPos + Vector3.up * openHeight;
        targetPos = closedPos;
    }

    void Update()
    {
        // 1. CLEANUP STEP: Remove "Dead" Ghosts
        // We loop backwards to safely remove items from the list
        for (int i = objectsOnPlate.Count - 1; i >= 0; i--)
        {
            // If the object is null (Destroyed) or disabled
            if (objectsOnPlate[i] == null || !objectsOnPlate[i].gameObject.activeInHierarchy)
            {
                objectsOnPlate.RemoveAt(i);
            }
        }

        // 2. DECISION STEP: Is the list empty?
        if (objectsOnPlate.Count > 0)
        {
            targetPos = openPos;
        }
        else
        {
            targetPos = closedPos;
        }

        // 3. MOVEMENT STEP
        doorObject.position = Vector3.MoveTowards(doorObject.position, targetPos, speed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Only care about Player or Ghost
        if (other.CompareTag("Player") || other.CompareTag("Ghost"))
        {
            if (!objectsOnPlate.Contains(other))
            {
                objectsOnPlate.Add(other);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (objectsOnPlate.Contains(other))
        {
            objectsOnPlate.Remove(other);
        }
    }
}