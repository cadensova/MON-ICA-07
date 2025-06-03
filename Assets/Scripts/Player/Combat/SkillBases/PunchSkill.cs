// PunchSkill.cs
using UnityEngine;

[CreateAssetMenu(fileName = "PunchSkill", menuName = "PlayerSkills/Punch")]
public class PunchSkill : PlayerSkill
{
    [Header("Punch Settings")]
    [SerializeField] private float punchRange = 1.5f;
    [SerializeField] private float punchRadius = 0.5f;
    [SerializeField] private float baseKnockback = 10f;
    [SerializeField] private LayerMask hitableLayers = ~0;

    // If you want the extraForcePerStack to come from the affliction itself,
    // you don't need to duplicate it here. But if you want to clamp or adjust,
    // you can also define a maxExtraForce. For now, we’ll read it from the affliction.
    public override void OnExecute()
    {
        if (col == null)
        {
            Debug.LogWarning("PunchSkill: no CapsuleCollider found on user.");
            return;
        }

        Vector3 worldCenter = userGO.transform.TransformPoint(col.center);
        Vector3 direction = Camera.main.transform.forward.normalized;

        RaycastHit[] hits = Physics.SphereCastAll(
            worldCenter,
            punchRadius,
            direction,
            punchRange,
            hitableLayers
        );

        foreach (var hit in hits)
        {
            Rigidbody hitRb = hit.collider.attachedRigidbody;
            if (hitRb == null || hitRb == rb) continue;

            // 1) See if this object has a CombatActor
            var targetCombat = hit.collider.GetComponentInParent<CombatActor>();
            float finalKnockback = baseKnockback;

            if (targetCombat != null)
            {
                // 2) Query how many stacks of VelocityBurst they have
                int burstStacks = targetCombat.GetAfflictionStacks("VelocityBurst");

                if (burstStacks > 0)
                {
                    var vb = targetCombat.GetAfflictionInstance("VelocityBurst") as VelocityBurstAffliction;
                    if (vb != null)
                    {
                        finalKnockback += vb.GetExtraForcePerStack() * burstStacks;
                    }

                    // 4) Remove / “consume” all VelocityBurst stacks
                    targetCombat.ConsumeAffliction("VelocityBurst");
                }
            }

            // 5) Apply the finalKnockback
            Vector3 knockDir = direction;
            hitRb.AddForce(knockDir * finalKnockback, ForceMode.Impulse);
        }


        Vector3 endCenter = worldCenter + direction * punchRange;
        Draw.Instance.Hitbox(endCenter, Vector3.one * punchRadius, ori, 0.05f, HitBoxType.SPHERE);
    }
}
