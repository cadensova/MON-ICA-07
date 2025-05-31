using UnityEngine;

public class SimplePhysicsObject : MonoBehaviour
{
    private Rigidbody rb;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        FlowPhysics.Instance.Register(rb);
    }

    // Update is called once per frame
    void Update()
    {
        //DO NOTHING
    }
}
