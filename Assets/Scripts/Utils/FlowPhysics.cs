using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class FlowPhysics : MonoBehaviour
{
    private class Body
    {
        public Rigidbody rb { get; private set; }
        public bool IsKinematic => rb.isKinematic;
        public bool UseGravity => rb.useGravity;
        public float GravityMultiplier;

        public Body(Rigidbody rb, float gravityMultiplier = 1f)
        {
            this.rb = rb;
            this.GravityMultiplier = gravityMultiplier;
        }
    }

    public static FlowPhysics Instance { get; private set; }

    [Header("Gravity Settings")]
    [field: SerializeField] public Vector3 GravityDirection { get; private set; } = Vector3.down;
    [field: SerializeField] public float GravityStrength { get; private set; } = 9.81f;

    private readonly List<Body> bodies = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("Custom Physics Manager initialized.");
    }

    private void FixedUpdate()
    {
        if (bodies.Count == 0)
            return;

        foreach (var body in bodies)
        {
            if (body == null || body.IsKinematic || !body.UseGravity)
                continue;

            body.rb.AddForce(GravityDirection.normalized * GravityStrength, ForceMode.Acceleration);
        }
    }

    public void Register(Rigidbody rb)
    {
        // check if any of the bodies already contains the rb if not create a new body and add it to the list
        float defMulti = 1f;

        if (bodies.Exists(b => b.rb == rb))
        {
            //Debug.Log("Rigidbody already registered: " + rb.name);
            return;
        }

        var newBody = new Body(rb, defMulti);
        bodies.Add(newBody);
    }

    public void Unregister(Rigidbody rb)
    {
        // check if any of the bodies contains the rb and remove it
        var body = bodies.Find(b => b.rb == rb);

        if (body != null)
        {
            bodies.Remove(body);
            //Debug.Log("Rigidbody unregistered: " + rb.name);
        }
    }
}
