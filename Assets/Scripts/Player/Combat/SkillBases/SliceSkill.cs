// SliceSkill.cs
using UnityEngine;
using System.Collections;
using Unity.VisualScripting;

[CreateAssetMenu(fileName = "SliceSkill", menuName = "PlayerSkills/Slice")]
public class SliceSkill : PlayerSkill
{
    [Header("Slice Settings")]
    [SerializeField] private float sliceRange = 2f;
    [SerializeField] private float sliceRadius = 1f;

    [Tooltip("How much extra punch force each VelocityBurst stack grants.")]
    [SerializeField] private float extraForcePerStack = 3f;

    [Tooltip("The maximum number of VelocityBurst stacks an enemy can have.")]
    [SerializeField] private int velocityBurstMaxStacks = 3;

    [SerializeField] private LayerMask sliceHitableLayers = ~0;


    public override void OnExecute()
    {
        if (col == null) return;

        Vector3 worldCenter = userGO.transform.TransformPoint(col.center);
        Vector3 direction = Camera.main.transform.forward.normalized;

        // SphereCast to see everything in front of us
        RaycastHit[] hits = Physics.SphereCastAll(
            worldCenter,
            sliceRadius,      // slice radius
            direction,
            sliceRange,
            sliceHitableLayers
        );

        foreach (var hit in hits)
        {
            var targetCombat = hit.collider.GetComponentInParent<CombatActor>();
            if (targetCombat != null)
            {
                // Create a brand‐new VelocityBurstAffliction instance
                var burstAff = new VelocityBurstAffliction(
                    extraForcePerStack,
                    velocityBurstMaxStacks
                );

                // Add it (or stack it) on the target
                targetCombat.AddAffliction(burstAff);
            }
        }

        Vector3 endCenter = worldCenter + direction * sliceRange;
        Draw.Instance.Hitbox(endCenter, Vector3.one * sliceRadius, ori, 0.05f, HitBoxType.SPHERE);
    }
}
