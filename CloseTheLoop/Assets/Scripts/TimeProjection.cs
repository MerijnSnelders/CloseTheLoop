using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TimeProjection : MonoBehaviour
{
    [Header("Settings")]
    public GameObject ghostPrefab;
    public float projectionTime = 15f;
    public float debtTime = 30f;
    public KeyCode triggerKey = KeyCode.F;
    public float paradoxThreshold = 2.0f;

    [Header("Status")]
    public string currentStatus = "Idle";
    public int debtsPending = 0;

    [Header("Visuals & UI")]
    public GameObject futureFX;         // Drag your 'FutureFX_Volume' here
    public TextMeshProUGUI warningText; // Drag your 'WarningText' here
    public GameObject gameOverPanel;    // Drag your 'GameOverPanel' here
    private bool InAct1 = false;

    // STATE MACHINE
    public enum State { Idle, Act1, Act2, Act3 }
    public State currentState = State.Idle; // Made public to see in Inspector
    private float stateTimer;

    // DATA FOR CURRENT PUZZLE
    private List<Vector3> act1Pos = new List<Vector3>(); // The Future Ghost Path
    private List<Quaternion> act1Rot = new List<Quaternion>();

    private List<Vector3> currentRecPos = new List<Vector3>(); // What we are recording NOW
    private List<Quaternion> currentRecRot = new List<Quaternion>();

    // OBJECTS
    private GameObject activeGhost;
    private Vector3 act1StartPos;
    private Quaternion act1StartRot;

    // DEBT QUEUE
    private List<DebtData> debtQueue = new List<DebtData>();
    private DebtData currentDebtBeingPaid;

    // PAUSE SYSTEM (The Bookmark)
    private PauseData pausedSession = null;

    private Rigidbody myRb;

    void Start()
    {
        myRb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        currentStatus = currentState.ToString();

        // 1. INPUT: Start Act 1 (Only if Idle)
        if (Input.GetKeyDown(triggerKey) && currentState == State.Idle)
        {
            StartAct1();
        }

        // 2. MANAGE DEBTS
        if (debtQueue.Count > 0)
        {
            debtsPending = debtQueue.Count;
            DebtData nextBill = debtQueue[0];
            for (int i = 0; i < debtQueue.Count; i++)
            {
                debtQueue[i].timeUntilDue -= Time.deltaTime;
            }

            // Interrupt Logic
            if (nextBill.timeUntilDue <= 5.0f && nextBill.timeUntilDue > 0f)
            {
                if (warningText)
                {
                    warningText.gameObject.SetActive(true);
                    // Format: "REPOSSESSION IN: 3.2"
                    warningText.text = $"TRAVEL BACK IN: {nextBill.timeUntilDue:F1}";
                }
            }
            else
            {
                // Hide warning if we are safe (or if act 3 started)
                if (warningText) warningText.gameObject.SetActive(false);
            }

            // --- INTERRUPT TRIGGER ---
            if (nextBill.timeUntilDue <= 0 && currentState != State.Act3)
            {
                if (warningText) warningText.gameObject.SetActive(false); // Hide text
                InterruptAndPayDebt(nextBill);
            }
        }
        else
        {
            if (warningText) warningText.gameObject.SetActive(false);
        }
    }

    void FixedUpdate()
    {
        if (currentState == State.Act1)
        {
            stateTimer -= Time.fixedDeltaTime;
            act1Pos.Add(transform.position);
            act1Rot.Add(transform.rotation);
            if (stateTimer <= 0) FinishAct1();
        }
        else if (currentState == State.Act2)
        {
            stateTimer -= Time.fixedDeltaTime;
            currentRecPos.Add(transform.position);
            currentRecRot.Add(transform.rotation);
            if (stateTimer <= 0) FinishAct2();
        }
        else if (currentState == State.Act3)
        {
            stateTimer -= Time.fixedDeltaTime;
            if (stateTimer <= 0) FinishAct3();
        }
    }

    // ================= ACT 1 (Record Future) =================
    void StartAct1()
    {
        currentState = State.Act1;
        stateTimer = projectionTime;
        act1StartPos = transform.position;
        act1StartRot = transform.rotation;
        InAct1 = true;
       if (InAct1) 
            futureFX.SetActive(true);

        SpawnStatue(act1StartPos, act1StartRot);

        act1Pos = new List<Vector3>();
        act1Rot = new List<Quaternion>();
        Debug.Log("ACT 1 START");
    }

    void FinishAct1()
    {
        if (InAct1) 
            futureFX.SetActive(false);
        Vector3 safeSpot = activeGhost.transform.position - (activeGhost.transform.forward * 2f);
        TeleportPlayer(safeSpot, activeGhost.transform.rotation);
        Destroy(activeGhost);
        StartAct2();
        InAct1 = false;
    }

    // ================= ACT 2 (Record Past) =================
    void StartAct2()
    {
        currentState = State.Act2;
        stateTimer = projectionTime;
        currentRecPos = new List<Vector3>();
        currentRecRot = new List<Quaternion>();

        // Spawn Act 1 Ghost
        SpawnGhost(act1Pos, act1Rot);
        Debug.Log("ACT 2 START");
    }

    void FinishAct2()
    {
        if (activeGhost != null) Destroy(activeGhost);

        // Save Debt
        DebtData newDebt = new DebtData();
        newDebt.timeUntilDue = debtTime;
        newDebt.startPosition = act1StartPos;
        newDebt.startRotation = act1StartRot;
        newDebt.ghostPos = new List<Vector3>(currentRecPos);
        newDebt.ghostRot = new List<Quaternion>(currentRecRot);

        debtQueue.Add(newDebt);
        currentState = State.Idle;
        Debug.Log("ACT 2 END. Debt Added.");
    }

    // ================= INTERRUPT & PAUSE SYSTEM =================
    void InterruptAndPayDebt(DebtData debt)
    {
        Debug.Log("!!! INTERRUPT !!! Repossession time.");

        // 1. PAUSE SYSTEM
        if (currentState == State.Act1 || currentState == State.Act2)
        {
            SavePauseState();
        }
        else
        {
            pausedSession = new PauseData();
            pausedSession.wasIdle = true;
            pausedSession.playerResumePos = transform.position;
            pausedSession.playerResumeRot = transform.rotation;
        }

        currentState = State.Act3;
        currentDebtBeingPaid = debt;
        stateTimer = projectionTime;

        if (activeGhost != null) Destroy(activeGhost);

        // --- NEW SMART SPAWN LOGIC ---

        // 1. Calculate the direction "Backwards"
        Vector3 flatForward = debt.startRotation * Vector3.forward;
        flatForward.y = 0;

        // --- FIX: THE LOOK-DOWN SAFETY CHECK ---
        // If we looked straight down, flatForward is roughly (0,0,0). 
        // We must give it a value to avoid dividing by zero or spawning inside the ghost.
        if (flatForward.sqrMagnitude < 0.1f)
        {
            // Default to World Backwards if the player has no horizontal rotation
            flatForward = Vector3.forward;
        }

        flatForward.Normalize();
        // ---------------------------------------

        // 2. Determine ideal spawn point (2 meters back)
        Vector3 idealSpawnPos = debt.startPosition - (flatForward * 2f);
        Vector3 finalSpawnPos = idealSpawnPos;

        // 3. SAFETY CHECK: Wall Detection
        Vector3 rayOrigin = debt.startPosition + Vector3.up * 1.0f;
        float checkDistance = 2.1f;

        if (Physics.Raycast(rayOrigin, -flatForward, out RaycastHit hit, checkDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("Wall detected behind spawn! Adjusting position.");
            Vector3 hitPointFloor = hit.point;
            hitPointFloor.y = debt.startPosition.y;

            finalSpawnPos = hitPointFloor + (flatForward * 0.5f);
        }

        // 4. Teleport
        TeleportPlayer(finalSpawnPos, debt.startRotation);

        // 5. SPAWN DEBT GHOST
        SpawnGhost(debt.ghostPos, debt.ghostRot);
    }

    void SavePauseState()
    {
        Debug.Log("Pausing current puzzle...");
        pausedSession = new PauseData();
        pausedSession.wasIdle = false;
        pausedSession.interruptedState = currentState;
        pausedSession.timeLeft = stateTimer;

        // Save Player Pos (To resume exactly here)
        pausedSession.playerResumePos = transform.position;
        pausedSession.playerResumeRot = transform.rotation;

        // Save Data Lists
        pausedSession.savedAct1Pos = new List<Vector3>(act1Pos);
        pausedSession.savedAct1Rot = new List<Quaternion>(act1Rot);
        pausedSession.savedCurrentRecPos = new List<Vector3>(currentRecPos);
        pausedSession.savedCurrentRecRot = new List<Quaternion>(currentRecRot);

        pausedSession.puzzleStartPos = act1StartPos;
        pausedSession.puzzleStartRot = act1StartRot;

        // If a ghost is running (Act 2), remember where it was!
        if (activeGhost != null)
        {
            GhostRunner runner = activeGhost.GetComponent<GhostRunner>();
            if (runner != null)
            {
                pausedSession.ghostFrameIndex = runner.GetCurrentFrameIndex();
            }
        }
    }

    void ResumePauseState()
    {
        if (pausedSession == null) return;

        Debug.Log("Resuming previous state...");

        // 1. Teleport Player Back
        TeleportPlayer(pausedSession.playerResumePos, pausedSession.playerResumeRot);

        if (pausedSession.wasIdle)
        {
            currentState = State.Idle;
            pausedSession = null;
            return;
        }

        // 2. Restore State
        currentState = pausedSession.interruptedState;
        stateTimer = pausedSession.timeLeft;
        act1StartPos = pausedSession.puzzleStartPos;
        act1StartRot = pausedSession.puzzleStartRot;

        // 3. Restore Lists
        act1Pos = pausedSession.savedAct1Pos;
        act1Rot = pausedSession.savedAct1Rot;
        currentRecPos = pausedSession.savedCurrentRecPos;
        currentRecRot = pausedSession.savedCurrentRecRot;

        // 4. Restore Ghost (The tricky part)
        if (currentState == State.Act1)
        {
            if (InAct1) futureFX.SetActive(true);
            // If Act 1, just put the statue back
            SpawnStatue(act1StartPos, act1StartRot);
        }
        else if (currentState == State.Act2)
        {
            // If Act 2, spawn ghost and FAST FORWARD
            SpawnGhost(act1Pos, act1Rot);
            GhostRunner runner = activeGhost.GetComponent<GhostRunner>();
            if (runner != null)
            {
                runner.SkipToFrame(pausedSession.ghostFrameIndex);
            }
        }

        pausedSession = null; // Clear the bookmark
    }

    // ================= ACT 3 END =================
    void FinishAct3()
    {
        if (activeGhost == null)
        {
            EndAct3AndReturn();
            return;
        }

        Vector3 ghostFinalPos = activeGhost.transform.position;
        Vector3 targetPos = currentDebtBeingPaid.ghostPos[currentDebtBeingPaid.ghostPos.Count - 1];

        float distance = Vector3.Distance(ghostFinalPos, targetPos);
        Debug.Log($"Paradox Check: {distance}m");

        if (distance > paradoxThreshold)
        {
            Debug.LogError("PARADOX DETECTED! GAME OVER.");

            // Show the Game Over Screen
            if (gameOverPanel) gameOverPanel.SetActive(true);

            // Freeze the game
            Time.timeScale = 0;

            // Return here so we DO NOT continue logic
            return;
        }

        Debug.Log("ACT 3 SUCCESS: Debt Paid.");
        EndAct3AndReturn();
    }

    void EndAct3AndReturn()
    {
        if (activeGhost != null) Destroy(activeGhost);

        debtQueue.Remove(currentDebtBeingPaid);
        currentDebtBeingPaid = null;

        // INSTEAD OF JUST IDLE, WE RESUME
        ResumePauseState();
    }

    // ================= HELPERS =================
    void SpawnStatue(Vector3 pos, Quaternion rot)
    {
        activeGhost = Instantiate(ghostPrefab, pos, rot);
        Rigidbody rb = activeGhost.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;
    }

    void SpawnGhost(List<Vector3> posList, List<Quaternion> rotList)
    {
        activeGhost = Instantiate(ghostPrefab, posList[0], rotList[0]);
        Rigidbody rb = activeGhost.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        GhostRunner runner = activeGhost.AddComponent<GhostRunner>();
        runner.Initialize(posList, rotList);
    }

    void TeleportPlayer(Vector3 pos, Quaternion rot)
    {
        myRb.linearVelocity = Vector3.zero; // Stop moving
        transform.position = pos;
        transform.rotation = rot;

        // FORCE UPDATE: Tell physics engine we moved RIGHT NOW
        Physics.SyncTransforms();
    }
}

// ================= DATA CLASSES =================

[System.Serializable]
public class DebtData
{
    public float timeUntilDue;
    public Vector3 startPosition;
    public Quaternion startRotation;
    public List<Vector3> ghostPos;
    public List<Quaternion> ghostRot;
}

[System.Serializable]
public class PauseData
{
    public bool wasIdle;
    public TimeProjection.State interruptedState;
    public float timeLeft;

    // Where was the player?
    public Vector3 playerResumePos;
    public Quaternion playerResumeRot;

    // Data to Restore
    public List<Vector3> savedAct1Pos;
    public List<Quaternion> savedAct1Rot;
    public List<Vector3> savedCurrentRecPos;
    public List<Quaternion> savedCurrentRecRot;

    public Vector3 puzzleStartPos;
    public Quaternion puzzleStartRot;

    // Ghost Frame
    public int ghostFrameIndex;
}