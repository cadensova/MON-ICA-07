using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "SlideBumpSkill", menuName = "PlayerSkills/SlideBump")]
public class SlideBumpSkill : PlayerSkill
{
    [Header("Bump Settings")]
    [SerializeField] private float slideBumpForce = 10f;
    [SerializeField] private float bumpRange = 1.5f;
    [SerializeField] private float bumpRadius = 0.5f;
    [SerializeField] private Vector3 bumpDirection = Vector3.up;
    [SerializeField] private LayerMask hitableLayers = ~0;

    [field: SerializeField] public List<Rigidbody> bumped { get; private set; }

    public override void OnExecute()
    {
        Vector3 worldCenter = userGO.transform.TransformPoint(col.center);
        Vector3 direction = Camera.main.transform.forward.normalized;

        RaycastHit[] hits = Physics.SphereCastAll(
            worldCenter,
            bumpRadius,
            direction,
            bumpRange,
            hitableLayers
        );

        foreach (var hit in hits)
        {
            Rigidbody hitRb = hit.collider.attachedRigidbody;
            if (hitRb == null || hitRb == rb || bumped.Contains(hitRb)) continue;
            bumped.Add(hitRb);

            hitRb.linearVelocity = Vector3.zero;
            Vector3 knockDir = bumpDirection;
            hitRb.AddForce(knockDir.normalized * slideBumpForce, ForceMode.Impulse);
        }

    }

    public void Clear()
    {
        bumped.Clear();
    }

    public void OnDisable()
    {
        Clear();
    }
}
