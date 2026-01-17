using System.Collections.Generic;
using UnityEngine;

public class TimeProjection : MonoBehaviour
{
    [Header("Settings")]
    public GameObject ghostPrefab;
    public float projectionTime = 15f;
    public float debtTime = 30f;
    public KeyCode triggerKey = KeyCode.F;

    [Header("State")]
    public bool isAct1 = false;
    public bool isAct2 = false;
    public bool isAct3 = false;
    public bool debtUnpaid = false;

    public float timeRemaining;

    public float paradoxThreshold = 2.0f; // How far off can the ghost be before reality breaks?

    // References
    private Rigidbody myRb;

    // --- DATA STORAGE ---
    private List<Vector3> futurePos = new List<Vector3>();
    private List<Quaternion> futureRot = new List<Quaternion>();

    private List<Vector3> pastPos = new List<Vector3>();
    private List<Quaternion> pastRot = new List<Quaternion>();

    // --- POSITIONS TO REMEMBER ---
    private GameObject activeGhost;
    private Vector3 act1StartPos;     // Where Act 1 started (The Past)
    private Quaternion act1StartRot;
    private Vector3 returnToFuturePos; // Where we were before Act 3 pulled us back (The Future)
    private Quaternion returnToFutureRot;

    void Start()
    {
        myRb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (Input.GetKeyDown(triggerKey) && !isAct1 && !isAct2 && !isAct3 && !debtUnpaid)
        {
            StartAct1();
        }

        // Debt Timer
        if (debtUnpaid)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0)
            {
                StartAct3();
            }
        }
    }

    void FixedUpdate()
    {
        if (isAct1)
        {
            timeRemaining -= Time.fixedDeltaTime;
            futurePos.Add(transform.position);
            futureRot.Add(transform.rotation);
            if (timeRemaining <= 0) FinishAct1();
        }
        else if (isAct2)
        {
            timeRemaining -= Time.fixedDeltaTime;
            pastPos.Add(transform.position);
            pastRot.Add(transform.rotation);
            if (timeRemaining <= 0) FinishAct2();
        }
        else if (isAct3)
        {
            timeRemaining -= Time.fixedDeltaTime;
            if (timeRemaining <= 0) FinishAct3();
        }
    }

    // ================= ACT 1 =================
    void StartAct1()
    {
        act1StartPos = transform.position;
        act1StartRot = transform.rotation;

        CreateStatue(act1StartPos, act1StartRot);

        futurePos.Clear();
        futureRot.Clear();

        isAct1 = true;
        timeRemaining = projectionTime;
        Debug.Log("ACT 1 START: You are the Future Self.");
    }

    void FinishAct1()
    {
        Debug.Log("ACT 1 END: Returning to Present.");
        isAct1 = false;

        Vector3 safeSpot = activeGhost.transform.position - (activeGhost.transform.forward * 2f);
        TeleportPlayer(safeSpot, activeGhost.transform.rotation);
        Destroy(activeGhost);

        StartAct2();
    }

    // ================= ACT 2 =================
    void StartAct2()
    {
        pastPos.Clear();
        pastRot.Clear();
        SpawnGhost(futurePos, futureRot); // Spawn Future Ghost

        isAct2 = true;
        timeRemaining = projectionTime;
        Debug.Log("ACT 2 START: You are the Past Self.");
    }

    void FinishAct2()
    {
        Debug.Log("ACT 2 END. The Debt is now ticking...");
        isAct2 = false;

        if (activeGhost != null)
        {
            Destroy(activeGhost);
        }

        debtUnpaid = true;
        timeRemaining = debtTime;
    }

    // ================= ACT 3 (The Flashback) =================
    void StartAct3()
    {
        debtUnpaid = false;
        Debug.Log("ACT 3: REPOSSESSION! Paying the price.");

        // 1. SAVE CURRENT SPOT
        returnToFuturePos = transform.position;
        returnToFutureRot = transform.rotation;

        // 2. CALCULATE SAFE SPAWN (Fixing the "In the floor" bug)
        // We take the rotation, but force the X and Z angles to 0 so we don't calculate an offset into the ground.
        Vector3 flatForward = act1StartRot * Vector3.forward;
        flatForward.y = 0; // Flatten it
        flatForward.Normalize();

        // Move 2 meters back horizontally
        Vector3 safeSpot = act1StartPos - (flatForward * 2f);

        TeleportPlayer(safeSpot, act1StartRot);

        // 3. Spawn Past Ghost
        SpawnGhost(pastPos, pastRot);

        isAct3 = true;
        timeRemaining = projectionTime;
    }

    void FinishAct3()
    {
        // Safety: If ghost is missing, just exit to prevent errors
        if (activeGhost == null)
        {
            TeleportPlayer(returnToFuturePos, returnToFutureRot);
            isAct3 = false;
            return;
        }

        // 1. CHECK FOR PARADOX
        Vector3 ghostFinalPos = activeGhost.transform.position;
        Vector3 targetPos = pastPos[pastPos.Count - 1];

        // Measure distance
        float distance = Vector3.Distance(ghostFinalPos, targetPos);
        Debug.Log("Paradox Variance: " + distance + "m");

        // VISUAL DEBUG: Draws a red line in Scene view showing the gap
        Debug.DrawLine(ghostFinalPos, targetPos, Color.red, 5f);

        if (distance > paradoxThreshold)
        {
            Debug.LogError("PARADOX DETECTED! GAME OVER.");
            Time.timeScale = 0; // FREEZE GAME
            return;
        }

        // 2. SUCCESS 
        Debug.Log("ACT 3 END: Loop Closed.");
        isAct3 = false;
        Destroy(activeGhost); // This will now work because activeGhost is assigned!

        // 3. RETURN
        TeleportPlayer(returnToFuturePos, returnToFutureRot);
    }

    // ================= HELPERS =================
    void CreateStatue(Vector3 pos, Quaternion rot)
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
        myRb.linearVelocity = Vector3.zero; // Kill momentum so you don't keep falling
        transform.position = pos;
        transform.rotation = rot;
    }
}