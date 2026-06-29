using System.Collections;
using UnityEngine;

/// <summary>
/// Latch: sprint (run + shift/toggle) + attack (Fire) press while roughly facing AI, phir latch point par chipakna.
/// </summary>
public class LatchAttackManager : MonoBehaviour
{
    public static LatchAttackManager Instance { get; private set; }

    [Header("References")]
    public Manager manager;

    [Header("Targeting")]
    [Min(0.5f)] public float maxLatchDistance = 6f;
    [Min(0f)] public float minLatchDistance = 0.75f;
    [Min(0f)] public float minTargetWithersSize = 4f;

    [Tooltip("Player forward aur AI ke beech max angle (degrees) — realistic approach cone.")]
    [Range(15f, 120f)] public float maxApproachAngleDeg = 52f;

    [Header("Latch trigger")]
    [Tooltip("Sprint/run (Animator Move==2) + Shift/toggle hold zaroori; saath Fire press se latch.")]
    public bool requireSprintForLatch = true;

    [Header("Smooth Latch Camera Transition")]
    [Tooltip("Agar true ho to player latch point par ekdam teleport nahi karega, smoothly move hoga.")]
    public bool useSmoothLatchCameraTransition = true;

    [Tooltip("Player ko latch point tak smoothly le jane ka waqt.")]
    [Min(0.01f)] public float smoothLatchDuration = 0.18f;

    [Tooltip("Smooth entry ke dauran curve jaisi feel ke liye easing.")]
    [Range(0.1f, 4f)] public float smoothLatchEase = 2f;

    [Header("Latch")]
    [Min(0.1f)] public float latchDurationSeconds = 5f;
    public Vector3 endDropOffset = new Vector3(0f, -0.35f, 0f);
    [Min(0f)] public float cooldownSeconds = 2f;

    [Header("Latch attack input (CF2)")]
    public string[] latchFireAxisNames = { "Fire", "Fire1", "Target Fire" };
    [Range(0.05f, 1f)] public float latchFireAxisThreshold = 0.45f;

    [Header("Animations (specie + suffix)")]
    public string groundAtkSuffix = "|GroundAtk";
    public string[] idleAtkSuffixFallbacks = { "|IdleAtk3", "|IdleAtk2", "|IdleAtk1", "|IdleAtk" };

    private bool _busy;
    private float _cooldownUntil;
    private float _latchPrevCombinedFireAxis;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private bool GetFireDown()
    {
        if (ControlFreak2.CF2Input.GetKeyDown(KeyCode.Mouse0) || Input.GetMouseButtonDown(0))
            return true;

        if (latchFireAxisNames != null)
        {
            for (int i = 0; i < latchFireAxisNames.Length; i++)
            {
                if (string.IsNullOrEmpty(latchFireAxisNames[i]))
                    continue;

                if (ControlFreak2.CF2Input.GetButtonDown(latchFireAxisNames[i]))
                    return true;
            }
        }

        return false;
    }

    private bool IsPlayerSprinting(Creature player)
    {
        if (player == null || player.anm == null)
            return false;

        // Animator: terrestrial run = Move 2 (Creature GetUserInputs).
        return player.anm.GetInteger("Move") == 2;
    }

    private bool SprintInputHeld(Creature player)
    {
        bool runRequested =
            ControlFreak2.CF2Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.LeftShift) ||
            (SprintToggle.IsOn);

        if (player.sprintLockedByStamina)
            return false;

        return runRequested;
    }

    private bool PassesLatchInputGate(Creature player)
    {
        // Latch sirf is frame attack press se (Mouse0 / CF2 Fire buttons).
        if (!GetFireDown())
            return false;

        if (requireSprintForLatch)
        {
            if (!IsPlayerSprinting(player))
                return false;

            if (!SprintInputHeld(player))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Player se AI ki taraf horizontal angle check.
    /// </summary>
    private bool IsFacingTowards(Creature player, Vector3 aiWorldPos)
    {
        Vector3 to = aiWorldPos - player.transform.position;
        to.y = 0f;

        if (to.sqrMagnitude < 0.01f)
            return true;

        Vector3 fwd = player.transform.forward;
        fwd.y = 0f;

        if (fwd.sqrMagnitude < 0.01f)
            return false;

        return Vector3.Angle(fwd.normalized, to.normalized) <= maxApproachAngleDeg;
    }

    public void TryTriggerFromPlayer(Creature player)
    {
        if (!enabled || player == null || player.useAI || !player.canLatchAttack)
            return;

        if (player.health <= 0.01f || player.latchAttackActive || _busy)
            return;

        if (Time.time < _cooldownUntil)
            return;

        if (!PassesLatchInputGate(player))
            return;

        if (manager == null)
            manager = FindFirstObjectByType<Manager>();

        if (manager == null || manager.creaturesList == null)
            return;

        Vector3 pPos = player.transform.position;
        float maxSqr = maxLatchDistance * maxLatchDistance;
        float minSqr = minLatchDistance * minLatchDistance;

        Creature best = null;
        Transform bestPoint = null;
        float bestDist = float.MaxValue;

        for (int i = 0; i < manager.creaturesList.Count; i++)
        {
            GameObject go = manager.creaturesList[i];
            if (go == null || go == player.gameObject)
                continue;

            Creature c = go.GetComponent<Creature>();
            if (c == null || !c.useAI || !c.latchAttackAcceptor || c.health <= 0.01f)
                continue;

            if (c.withersSize < minTargetWithersSize)
                continue;

            Vector3 aiCenter = c.body != null ? c.body.worldCenterOfMass : c.transform.position;
            if (!IsFacingTowards(player, aiCenter))
                continue;

            Vector3 d = go.transform.position - pPos;
            d.y = 0f;
            float s = d.sqrMagnitude;

            if (s > maxSqr || s < minSqr)
                continue;

            bool right = Vector3.Dot(d.normalized, go.transform.right) >= 0f;
            Transform pt = right ? c.latchPointRight : c.latchPointLeft;
            if (pt == null)
                continue;

            if (s < bestDist)
            {
                bestDist = s;
                best = c;
                bestPoint = pt;
            }
        }

        if (best == null || bestPoint == null)
            return;

        // Approach: AI ki taraf muh (ek frame) — phir latch point rotation override karega.
        Vector3 aim = best.body != null ? best.body.worldCenterOfMass : best.transform.position;
        aim.y = player.transform.position.y;
        Vector3 flat = aim - player.transform.position;

        if (flat.sqrMagnitude > 0.1f)
            player.transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);

        GameAnalytics.Event("latch_started",
    GameAnalytics.P("target_species", best.specie),
    GameAnalytics.P("distance", Mathf.RoundToInt(Mathf.Sqrt(bestDist))));

        StartCoroutine(LatchSimple(player, best, bestPoint));
    }

    public void ProcessLatchAttackInput(Creature player)
    {
        if (player == null || !player.latchAttackActive || !player.canAttack || player.anm == null)
            return;

        float axisCombined = 0f;
        if (latchFireAxisNames != null)
        {
            for (int i = 0; i < latchFireAxisNames.Length; i++)
            {
                if (string.IsNullOrEmpty(latchFireAxisNames[i]))
                    continue;

                string n = latchFireAxisNames[i];
                axisCombined = Mathf.Max(axisCombined, Mathf.Abs(ControlFreak2.CF2Input.GetAxis(n)));
            }
        }

        bool axisHeld = axisCombined >= latchFireAxisThreshold;
        bool axisDown = axisHeld && _latchPrevCombinedFireAxis < latchFireAxisThreshold;
        _latchPrevCombinedFireAxis = axisCombined;

        bool btnHeld = ControlFreak2.CF2Input.GetKey(KeyCode.Mouse0) || Input.GetMouseButton(0);
        bool btnDown = ControlFreak2.CF2Input.GetKeyDown(KeyCode.Mouse0) || Input.GetMouseButtonDown(0);

        if (latchFireAxisNames != null)
        {
            for (int i = 0; i < latchFireAxisNames.Length; i++)
            {
                if (string.IsNullOrEmpty(latchFireAxisNames[i]))
                    continue;

                string n = latchFireAxisNames[i];
                btnHeld |= ControlFreak2.CF2Input.GetButton(n);
                btnDown |= ControlFreak2.CF2Input.GetButtonDown(n);
            }
        }

        bool fireHeld = btnHeld || axisHeld;
        bool fireDown = btnDown || axisDown;

        if (fireDown)
            PlayGroundAttack(player);

        if (fireHeld)
        {
            player.behaviorCount = 500f;
            player.behavior = "Hunt";
            player.anm.SetBool("Attack", true);
        }
        else
        {
            player.anm.SetBool("Attack", false);
        }
    }

    private IEnumerator LatchSimple(Creature player, Creature targetAi, Transform latchPoint)
    {
        _busy = true;
        player.latchAttackActive = true;
        player.behavior = "Player";
        player.behaviorCount = 0f;
        player.objTGT = targetAi.gameObject;

        if (targetAi.body != null)
            player.posTGT = targetAi.body.worldCenterOfMass;
        else
            player.posTGT = targetAi.transform.position;

        float end = Time.time + latchDurationSeconds;
        _latchPrevCombinedFireAxis = 0f;

        void StickToPoint()
        {
            if (player == null || latchPoint == null)
                return;

            player.transform.SetPositionAndRotation(latchPoint.position, latchPoint.rotation);

            if (player.body != null)
            {
                player.body.velocity = Vector3.zero;
                player.body.angularVelocity = Vector3.zero;
            }
        }

        if (useSmoothLatchCameraTransition)
        {
            yield return StartCoroutine(SmoothMoveToLatchPoint(player, latchPoint));
        }
        else
        {
            StickToPoint();
        }

        try
        {
            while (Time.time < end)
            {
                if (player == null || player.health <= 0.01f || latchPoint == null || targetAi == null)
                    break;

                StickToPoint();
                yield return new WaitForFixedUpdate();
            }

            if (player != null)
            {
                player.transform.position += endDropOffset;

                if (player.body != null)
                {
                    player.body.velocity = Vector3.zero;
                    player.body.angularVelocity = Vector3.zero;
                }
            }
        }
        finally
        {
            GameAnalytics.Event("latch_ended",
            GameAnalytics.P("target_species", targetAi != null ? targetAi.specie : "unknown"));

            if (player != null)
            {
                player.latchAttackActive = false;
                player.objTGT = null;
                player.posTGT = Vector3.zero;
                player.behavior = "Player";
                player.behaviorCount = 0f;

                // Latch point ne tedha/ulta pose de diya hota hai — neeche utar ke seedha khara (sirf yaw, default idle jaisa).
                ResetRotationUprightWorld(player.transform);

                if (player.anm != null)
                {
                    // Jaise latch se pehle: walk + default idle (no CrossFade chain).
                    player.anm.SetBool("Attack", false);
                    player.anm.SetInteger("Idle", 0);
                    player.anm.SetInteger("Move", 1);
                }
            }

            _busy = false;
            _cooldownUntil = Time.time + Mathf.Max(0f, cooldownSeconds);
            _latchPrevCombinedFireAxis = 0f;
        }

        if (SideMissionManager.Instance != null)
            SideMissionManager.Instance.NotifyLatchAttackUsed();

    }

    private IEnumerator SmoothMoveToLatchPoint(Creature player, Transform latchPoint)
    {
        if (player == null || latchPoint == null)
            yield break;

        float duration = Mathf.Max(0.01f, smoothLatchDuration);

        Vector3 startPos = player.transform.position;
        Quaternion startRot = player.transform.rotation;

        Vector3 targetPos = latchPoint.position;
        Quaternion targetRot = latchPoint.rotation;

        if (player.body != null)
        {
            player.body.velocity = Vector3.zero;
            player.body.angularVelocity = Vector3.zero;
            player.body.isKinematic = true;
        }

        float t = 0f;

        while (t < 1f)
        {
            if (player == null || latchPoint == null)
                break;

            t += Time.deltaTime / duration;
            float easedT = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), smoothLatchEase);

            targetPos = latchPoint.position;
            targetRot = latchPoint.rotation;

            player.transform.SetPositionAndRotation(
                Vector3.Lerp(startPos, targetPos, easedT),
                Quaternion.Slerp(startRot, targetRot, easedT)
            );

            yield return null;
        }

        if (player != null && latchPoint != null)
            player.transform.SetPositionAndRotation(latchPoint.position, latchPoint.rotation);

        if (player != null && player.body != null)
        {
            player.body.isKinematic = false;
            player.body.velocity = Vector3.zero;
            player.body.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Pitch/roll hata kar world-up par seedha khara — idle/walk state ke saath sahi lagta hai.
    /// </summary>
    private static void ResetRotationUprightWorld(Transform t)
    {
        if (t == null)
            return;

        float y = t.eulerAngles.y;
        t.rotation = Quaternion.Euler(0f, y, 0f);
    }

    private void PlayGroundAttack(Creature player)
    {
        if (player == null || player.anm == null || string.IsNullOrEmpty(player.specie))
            return;

        string g = player.specie + groundAtkSuffix;
        if (player.anm.HasState(0, Animator.StringToHash(g)))
        {
            player.anm.CrossFade(g, 0.12f, 0, 0f);
            return;
        }

        if (idleAtkSuffixFallbacks != null)
        {
            for (int i = 0; i < idleAtkSuffixFallbacks.Length; i++)
            {
                if (string.IsNullOrEmpty(idleAtkSuffixFallbacks[i]))
                    continue;

                string s = player.specie + idleAtkSuffixFallbacks[i];
                if (player.anm.HasState(0, Animator.StringToHash(s)))
                {
                    player.anm.CrossFade(s, 0.12f, 0, 0f);
                    return;
                }
            }
        }

        player.anm.SetBool("Attack", true);
    }
}


//using System.Collections;
//using UnityEngine;

///// <summary>
///// Latch: sprint (run + shift/toggle) + attack (Fire) press while roughly facing AI, phir latch point par chipakna.
///// </summary>
//public class LatchAttackManager : MonoBehaviour
//{
//    public static LatchAttackManager Instance { get; private set; }

//    [Header("References")]
//    public Manager manager;

//    [Header("Targeting")]
//    [Min(0.5f)] public float maxLatchDistance = 6f;
//    [Min(0f)] public float minLatchDistance = 0.75f;
//    [Min(0f)] public float minTargetWithersSize = 4f;

//    [Tooltip("Player forward aur AI ke beech max angle (degrees) — realistic approach cone.")]
//    [Range(15f, 120f)] public float maxApproachAngleDeg = 52f;

//    [Header("Latch trigger")]
//    [Tooltip("Sprint/run (Animator Move==2) + Shift/toggle hold zaroori; saath Fire press se latch.")]
//    public bool requireSprintForLatch = true;

//    [Header("Latch")]
//    [Min(0.1f)] public float latchDurationSeconds = 5f;
//    public Vector3 endDropOffset = new Vector3(0f, -0.35f, 0f);
//    [Min(0f)] public float cooldownSeconds = 2f;

//    [Header("Latch attack input (CF2)")]
//    public string[] latchFireAxisNames = { "Fire", "Fire1", "Target Fire" };
//    [Range(0.05f, 1f)] public float latchFireAxisThreshold = 0.45f;

//    [Header("Animations (specie + suffix)")]
//    public string groundAtkSuffix = "|GroundAtk";
//    public string[] idleAtkSuffixFallbacks = { "|IdleAtk3", "|IdleAtk2", "|IdleAtk1", "|IdleAtk" };

//    private bool _busy;
//    private float _cooldownUntil;
//    private float _latchPrevCombinedFireAxis;

//    private void Awake()
//    {
//        if (Instance != null && Instance != this)
//        {
//            Destroy(gameObject);
//            return;
//        }
//        Instance = this;
//    }

//    private void OnDestroy()
//    {
//        if (Instance == this)
//            Instance = null;
//    }

//    private bool GetFireDown()
//    {
//        if (ControlFreak2.CF2Input.GetKeyDown(KeyCode.Mouse0) || Input.GetMouseButtonDown(0))
//            return true;
//        if (latchFireAxisNames != null)
//        {
//            for (int i = 0; i < latchFireAxisNames.Length; i++)
//            {
//                if (string.IsNullOrEmpty(latchFireAxisNames[i]))
//                    continue;
//                if (ControlFreak2.CF2Input.GetButtonDown(latchFireAxisNames[i]))
//                    return true;
//            }
//        }
//        return false;
//    }

//    private bool IsPlayerSprinting(Creature player)
//    {
//        if (player == null || player.anm == null)
//            return false;
//        // Animator: terrestrial run = Move 2 (Creature GetUserInputs).
//        return player.anm.GetInteger("Move") == 2;
//    }

//    private bool SprintInputHeld(Creature player)
//    {
//        bool runRequested =
//            ControlFreak2.CF2Input.GetKey(KeyCode.LeftShift) ||
//            Input.GetKey(KeyCode.LeftShift) ||
//            (SprintToggle.IsOn);
//        if (player.sprintLockedByStamina)
//            return false;
//        return runRequested;
//    }

//    private bool PassesLatchInputGate(Creature player)
//    {
//        // Latch sirf is frame attack press se (Mouse0 / CF2 Fire buttons).
//        if (!GetFireDown())
//            return false;

//        if (requireSprintForLatch)
//        {
//            if (!IsPlayerSprinting(player))
//                return false;
//            if (!SprintInputHeld(player))
//                return false;
//        }

//        return true;
//    }

//    /// <summary>
//    /// Player se AI ki taraf horizontal angle check.
//    /// </summary>
//    private bool IsFacingTowards(Creature player, Vector3 aiWorldPos)
//    {
//        Vector3 to = aiWorldPos - player.transform.position;
//        to.y = 0f;
//        if (to.sqrMagnitude < 0.01f)
//            return true;
//        Vector3 fwd = player.transform.forward;
//        fwd.y = 0f;
//        if (fwd.sqrMagnitude < 0.01f)
//            return false;
//        return Vector3.Angle(fwd.normalized, to.normalized) <= maxApproachAngleDeg;
//    }

//    public void TryTriggerFromPlayer(Creature player)
//    {
//        if (!enabled || player == null || player.useAI || !player.canLatchAttack)
//            return;
//        if (player.health <= 0.01f || player.latchAttackActive || _busy)
//            return;
//        if (Time.time < _cooldownUntil)
//            return;

//        if (!PassesLatchInputGate(player))
//            return;

//        if (manager == null)
//            manager = FindFirstObjectByType<Manager>();
//        if (manager == null || manager.creaturesList == null)
//            return;

//        Vector3 pPos = player.transform.position;
//        float maxSqr = maxLatchDistance * maxLatchDistance;
//        float minSqr = minLatchDistance * minLatchDistance;

//        Creature best = null;
//        Transform bestPoint = null;
//        float bestDist = float.MaxValue;

//        for (int i = 0; i < manager.creaturesList.Count; i++)
//        {
//            GameObject go = manager.creaturesList[i];
//            if (go == null || go == player.gameObject)
//                continue;

//            Creature c = go.GetComponent<Creature>();
//            if (c == null || !c.useAI || !c.latchAttackAcceptor || c.health <= 0.01f)
//                continue;
//            if (c.withersSize < minTargetWithersSize)
//                continue;

//            Vector3 aiCenter = c.body != null ? c.body.worldCenterOfMass : c.transform.position;
//            if (!IsFacingTowards(player, aiCenter))
//                continue;

//            Vector3 d = go.transform.position - pPos;
//            d.y = 0f;
//            float s = d.sqrMagnitude;
//            if (s > maxSqr || s < minSqr)
//                continue;

//            bool right = Vector3.Dot(d.normalized, go.transform.right) >= 0f;
//            Transform pt = right ? c.latchPointRight : c.latchPointLeft;
//            if (pt == null)
//                continue;

//            if (s < bestDist)
//            {
//                bestDist = s;
//                best = c;
//                bestPoint = pt;
//            }
//        }

//        if (best == null || bestPoint == null)
//            return;

//        // Approach: AI ki taraf muh (ek frame) — phir latch point rotation override karega.
//        Vector3 aim = best.body != null ? best.body.worldCenterOfMass : best.transform.position;
//        aim.y = player.transform.position.y;
//        Vector3 flat = aim - player.transform.position;
//        if (flat.sqrMagnitude > 0.1f)
//            player.transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);

//        StartCoroutine(LatchSimple(player, best, bestPoint));
//    }

//    public void ProcessLatchAttackInput(Creature player)
//    {
//        if (player == null || !player.latchAttackActive || !player.canAttack || player.anm == null)
//            return;

//        float axisCombined = 0f;
//        if (latchFireAxisNames != null)
//        {
//            for (int i = 0; i < latchFireAxisNames.Length; i++)
//            {
//                if (string.IsNullOrEmpty(latchFireAxisNames[i]))
//                    continue;
//                string n = latchFireAxisNames[i];
//                axisCombined = Mathf.Max(axisCombined, Mathf.Abs(ControlFreak2.CF2Input.GetAxis(n)));
//            }
//        }

//        bool axisHeld = axisCombined >= latchFireAxisThreshold;
//        bool axisDown = axisHeld && _latchPrevCombinedFireAxis < latchFireAxisThreshold;
//        _latchPrevCombinedFireAxis = axisCombined;

//        bool btnHeld = ControlFreak2.CF2Input.GetKey(KeyCode.Mouse0) || Input.GetMouseButton(0);
//        bool btnDown = ControlFreak2.CF2Input.GetKeyDown(KeyCode.Mouse0) || Input.GetMouseButtonDown(0);

//        if (latchFireAxisNames != null)
//        {
//            for (int i = 0; i < latchFireAxisNames.Length; i++)
//            {
//                if (string.IsNullOrEmpty(latchFireAxisNames[i]))
//                    continue;
//                string n = latchFireAxisNames[i];
//                btnHeld |= ControlFreak2.CF2Input.GetButton(n);
//                btnDown |= ControlFreak2.CF2Input.GetButtonDown(n);
//            }
//        }

//        bool fireHeld = btnHeld || axisHeld;
//        bool fireDown = btnDown || axisDown;

//        if (fireDown)
//            PlayGroundAttack(player);

//        if (fireHeld)
//        {
//            player.behaviorCount = 500f;
//            player.behavior = "Hunt";
//            player.anm.SetBool("Attack", true);
//        }
//        else
//            player.anm.SetBool("Attack", false);
//    }

//    private IEnumerator LatchSimple(Creature player, Creature targetAi, Transform latchPoint)
//    {
//        _busy = true;
//        player.latchAttackActive = true;
//        player.behavior = "Player";
//        player.behaviorCount = 0f;
//        player.objTGT = targetAi.gameObject;
//        if (targetAi.body != null)
//            player.posTGT = targetAi.body.worldCenterOfMass;
//        else
//            player.posTGT = targetAi.transform.position;

//        float end = Time.time + latchDurationSeconds;
//        _latchPrevCombinedFireAxis = 0f;

//        void StickToPoint()
//        {
//            if (player == null || latchPoint == null)
//                return;
//            player.transform.SetPositionAndRotation(latchPoint.position, latchPoint.rotation);
//            if (player.body != null)
//            {
//                player.body.velocity = Vector3.zero;
//                player.body.angularVelocity = Vector3.zero;
//            }
//        }

//        StickToPoint();

//        try
//        {
//            while (Time.time < end)
//            {
//                if (player == null || player.health <= 0.01f || latchPoint == null || targetAi == null)
//                    break;
//                StickToPoint();
//                yield return new WaitForFixedUpdate();
//            }

//            if (player != null)
//            {
//                player.transform.position += endDropOffset;
//                if (player.body != null)
//                {
//                    player.body.velocity = Vector3.zero;
//                    player.body.angularVelocity = Vector3.zero;
//                }
//            }
//        }
//        finally
//        {
//            if (player != null)
//            {
//                player.latchAttackActive = false;
//                player.objTGT = null;
//                player.posTGT = Vector3.zero;
//                player.behavior = "Player";
//                player.behaviorCount = 0f;
//                // Latch point ne tedha/ulta pose de diya hota hai — neeche utar ke seedha khara (sirf yaw, default idle jaisa).
//                ResetRotationUprightWorld(player.transform);
//                if (player.anm != null)
//                {
//                    // Jaise latch se pehle: walk + default idle (no CrossFade chain).
//                    player.anm.SetBool("Attack", false);
//                    player.anm.SetInteger("Idle", 0);
//                    player.anm.SetInteger("Move", 1);
//                }
//            }
//            _busy = false;
//            _cooldownUntil = Time.time + Mathf.Max(0f, cooldownSeconds);
//            _latchPrevCombinedFireAxis = 0f;
//        }
//    }

//    /// <summary>
//    /// Pitch/roll hata kar world-up par seedha khara — idle/walk state ke saath sahi lagta hai.
//    /// </summary>
//    private static void ResetRotationUprightWorld(Transform t)
//    {
//        if (t == null)
//            return;
//        float y = t.eulerAngles.y;
//        t.rotation = Quaternion.Euler(0f, y, 0f);
//    }

//    private void PlayGroundAttack(Creature player)
//    {
//        if (player == null || player.anm == null || string.IsNullOrEmpty(player.specie))
//            return;

//        string g = player.specie + groundAtkSuffix;
//        if (player.anm.HasState(0, Animator.StringToHash(g)))
//        {
//            player.anm.CrossFade(g, 0.12f, 0, 0f);
//            return;
//        }

//        if (idleAtkSuffixFallbacks != null)
//        {
//            for (int i = 0; i < idleAtkSuffixFallbacks.Length; i++)
//            {
//                if (string.IsNullOrEmpty(idleAtkSuffixFallbacks[i]))
//                    continue;
//                string s = player.specie + idleAtkSuffixFallbacks[i];
//                if (player.anm.HasState(0, Animator.StringToHash(s)))
//                {
//                    player.anm.CrossFade(s, 0.12f, 0, 0f);
//                    return;
//                }
//            }
//        }

//        player.anm.SetBool("Attack", true);
//    }
//}

