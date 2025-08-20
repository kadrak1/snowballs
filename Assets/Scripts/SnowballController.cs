
using UnityEngine;

public class SnowballController : MonoBehaviour
{
    public float lifetime = 5f;
    public int damage = 10;

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Snowball hit: " + collision.gameObject.name);

        Health health = collision.gameObject.GetComponent<Health>();
        if (health == null)
        {
            health = collision.gameObject.GetComponentInParent<Health>();
        }
        if (health != null)
        {
            health.TakeDamageFrom(gameObject, damage);
        }
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        Health health = other.GetComponent<Health>();
        if (health == null)
        {
            health = other.GetComponentInParent<Health>();
        }
        if (health != null)
        {
            health.TakeDamageFrom(gameObject, damage);
        }
        Destroy(gameObject);
    }
}
