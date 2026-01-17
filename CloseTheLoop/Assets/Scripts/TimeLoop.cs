using System.Collections.Generic;
using UnityEngine;

public class TimeLoop : MonoBehaviour
{
    [Header("Settings")]
    public GameObject ghostPrefab; // Drag your Ghost Prefab here
    public Transform spawnPoint;   // Where you start the level
    public KeyCode resetKey = KeyCode.R;

    [Header("Debug")]
    public bool isRecording = true;

    // We store the history here
    private List<Vector3> recordedPositions = new List<Vector3>();
    private List<Quaternion> recordedRotations = new List<Quaternion>();

    // The active ghost object
    private GameObject currentGhost;
    private int ghostFrameIndex = 0;

    void Start()
    {
        // Start recording immediately
        StartRecording();
    }

    void FixedUpdate() // We use FixedUpdate for smooth physics replay
    {
        if (isRecording)
        {
            // 1. SAVE the player's current spot
            recordedPositions.Add(transform.position);
            recordedRotations.Add(transform.rotation);
        }
        else if (currentGhost != null)
        {
            // 2. PLAY BACK the ghost
            if (ghostFrameIndex < recordedPositions.Count)
            {
                // Move the ghost to the saved position for this frame
                currentGhost.transform.position = recordedPositions[ghostFrameIndex];
                currentGhost.transform.rotation = recordedRotations[ghostFrameIndex];
                ghostFrameIndex++;
            }
            else
            {
                // Ghost finished the run
                Destroy(currentGhost);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(resetKey))
        {
            StartReplay();
        }
    }

    void StartRecording()
    {
        // Clear old data
        recordedPositions.Clear();
        recordedRotations.Clear();
        isRecording = true;

        if (currentGhost != null) Destroy(currentGhost);
    }

    void StartReplay()
    {
        isRecording = false;

        // 1. Teleport Player back to start
        // (We disable CharacterController/Rigidbody briefly to stop physics glitches)
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.linearVelocity = Vector3.zero;
        transform.position = spawnPoint.position;

        // 2. Spawn the Ghost at the start
        currentGhost = Instantiate(ghostPrefab, spawnPoint.position, spawnPoint.rotation);

        // 3. Reset the playback counter
        ghostFrameIndex = 0;
    }
}
