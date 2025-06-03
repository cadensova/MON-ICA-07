using UnityEngine;


public enum HitBoxType { BOX, SPHERE } 
public class Draw : MonoBehaviour
{
    public static Draw Instance { get; private set; }

    [SerializeField] private LayerMask DebugLayer;
    [SerializeField] private GameObject hitboxPrefab;
    [SerializeField] private GameObject hitSpherePrefab;

    static readonly Vector3 defSize = new Vector3(1f, 1f, 1f);

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("Custom Draw Manager initialized.");
    }

    public void Hitbox(Vector3 _center, Vector3 _size, Transform _parent, float lifetime = 1f, HitBoxType _type = HitBoxType.BOX)
    {
        GameObject hit;
        GameObject prefab;
        switch (_type)
        {
            case HitBoxType.BOX:
                prefab = hitboxPrefab;
                break;
            case HitBoxType.SPHERE:
                prefab = hitSpherePrefab;
                break;
            default:
                prefab = hitboxPrefab;
                break;
        }

        hit = Instantiate(prefab, _center, Quaternion.identity);

        hit.transform.position = _center;

        if (_size == null)
            _size = Vector3.one;
        hit.transform.localScale = _size;


        hit.transform.parent = _parent != null ? _parent : this.transform;
        hit.transform.rotation = hit.transform.parent.rotation;

        hit.layer = Mathf.RoundToInt(Mathf.Log(DebugLayer.value, 2));

        Destroy(hit, lifetime);
    }

    public void Hitbox(Vector3 _center, Transform _parent = null)
    {
        Hitbox(_center, defSize, _parent);
    }
}