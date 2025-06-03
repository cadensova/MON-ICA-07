using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerCombat : MonoBehaviour
{
    [Header("Hit Settings")]
    [SerializeField] private LayerMask hitable;

    [Tooltip("How far out from the player the punch can hit.")]
    [SerializeField] private float punchRange = 1.5f;

    [Tooltip("Radius of the sphere used to detect punch collisions.")]
    [SerializeField] private float punchRadius = 0.5f;

    [Tooltip("Impulse force applied to any hit Rigidbody.")]
    [SerializeField] private float knockbackForce = 10f;

    public CapsuleCollider col { get; private set; }
    public Rigidbody rb { get; private set; }
    public InputProvider input { get; private set; }
    public Transform ori { get; private set; }
    public PlayerStateMachine sm { get; private set; }

    #region  SUBSCRIBE STUFF
    private Coroutine subscribeCoroutine;

    private void OnEnable()
    {
        subscribeCoroutine = StartCoroutine(SubscribeWhenReady());
    }

    private void OnDisable()
    {
        if (subscribeCoroutine != null)
            StopCoroutine(subscribeCoroutine);

        Unsubscribe();
    }

    public bool subscribed { get; private set; } = false;
    private IEnumerator SubscribeWhenReady()
    {
        // Wait for dependencies
        while (InputProvider.Instance == null || FlowPhysics.Instance == null)
        {
            yield return new WaitForSeconds(0.05f);
        }

        input = InputProvider.Instance;
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        ori = transform.Find("Orientation");
        sm = GetComponent<PlayerStateMachine>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody not found on PlayerCombat. Please ensure the player has a Rigidbody component.");
            yield break;
        }

        if (col == null)
        {
            Debug.LogError("CapsuleCollider not found on PlayerCombat. Please ensure the player has a CapsuleCollider component.");
            yield break;
        }

        if (sm == null)
        {
            Debug.LogError("PlayerStateMachine not found on PlayerCombat. Please ensure the player has a PlayerStateMachine component.");
            yield break;
        }

        AssignInputs();

        Debug.Log("PlayerCombat subscribed to input.");
        subscribed = true;
    }

    private void AssignInputs()
    {
        input.Punch.OnPressed += Punch;
        input.Slice.OnPressed += Slice;
    }

    private void Unsubscribe()
    {
        FlowPhysics.Instance.Unregister(rb);
        subscribed = false;
    }
    #endregion

    void Start()
    {
        // Nothing special here for now
    }

    void Update()
    {
        // You could put combo logic or animation triggers here later
    }

    private void Slice()
    {
        Debug.Log("swing swing");
    }

    private void Punch()
    {
        Debug.Log("Punch Punch");

        // 1) Determine the world-space origin of the punch. 
        Vector3 localCenter = col.center;
        Vector3 worldCenter = transform.TransformPoint(localCenter);

        // 2) Shoot a sphere forward from that point
        Vector3 direction = Camera.main.transform.forward;
        RaycastHit[] hits = Physics.SphereCastAll(
            worldCenter, 
            punchRadius, 
            direction, 
            punchRange, 
            hitable
        );

        // 3) For every hit, if it has a Rigidbody, apply knockback
        foreach (var hit in hits)
        {
            Rigidbody hitRb = hit.collider.attachedRigidbody;
            if (hitRb != null && hitRb != rb)
            {
                // Calculate a knockback vector: push directly away from the player
                Vector3 knockDir = Camera.main.transform.forward.normalized;
                hitRb.AddForce(knockDir * knockbackForce, ForceMode.Impulse);
            }
        }

        // (Optional) Draw a debug line to show direction in Scene view:
        Debug.DrawLine(worldCenter, worldCenter + direction * punchRange, Color.red, 0.5f);
    }

    
    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (col == null || ori == null) return;

        // Recompute the world-space center and direction even if we're not playing
        Vector3 worldCenter = transform.TransformPoint(col.center);
        Vector3 direction = ori.forward;
        Vector3 sphereCenter = worldCenter + direction * punchRange;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(sphereCenter, punchRadius);

        // Also draw a short line from origin to sphere center
        Gizmos.DrawLine(worldCenter, sphereCenter);
    }
    #endif
}
