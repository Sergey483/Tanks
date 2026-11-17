using UnityEngine;

public class Enemy : MonoBehaviour
{
    public int Health { get; protected set; } = 100;

    [SerializeField] protected int maxHealth = 100;
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float stopDistance = 1.5f;

    protected Transform playerTransform;

    protected virtual void Start()
    {
        Health = maxHealth;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    protected virtual void Update()
    {
        Move();
    }

    public virtual void Move()
    {
        if (playerTransform == null)
        {
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
            return;
        }

        Vector3 target = playerTransform.position;
        target.y = transform.position.y;
        transform.LookAt(target);

        if (Vector3.Distance(transform.position, target) > stopDistance)
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
    }

    public virtual void TakeDamage(int amount)
    {
        if (amount <= 0)
            return;

        Health = Mathf.Max(0, Health - amount);
        Debug.Log($"{gameObject.name} отримав {amount} шкоди! Залишилось ХП: {Health}");
    }
}
