using UnityEngine;

// Простий рух гравця для демонстрації переслідування (WASD).
public class OopPlayerMover : MonoBehaviour
{
    [SerializeField] private float speed = 4f;

    private void Update()
    {
        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        transform.Translate(input.normalized * speed * Time.deltaTime, Space.World);
    }
}
