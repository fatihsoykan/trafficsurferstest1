using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLaneMover : MonoBehaviour
{
    private int currentLane = 0; // 1 = sol, 0 = orta, 2 = sað
    private float laneOffset = 3f; // Þeritler arasý x mesafesi
    private Vector3 targetPosition;

    private void Start()
    {
        targetPosition = transform.position;
    }

    private void Update()
    {
        // Arabayý hedef pozisyona doðru yumuþak hareket ettir
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        Vector2 input = context.ReadValue<Vector2>();

        // sola basýldý
        if (input.x < 0)
        {
            if (currentLane > 0)
            {
                currentLane--;
                UpdateTargetPosition();
            }
        }
        // saða basýldý
        else if (input.x > 0)
        {
            if (currentLane < 2)
            {
                currentLane++;
                UpdateTargetPosition();
            }
        }
    }

    private void UpdateTargetPosition()
    {
        // Þerit x pozisyonunu laneIndex'e göre ayarla
        float xPos = (currentLane - 1) * laneOffset;
        targetPosition = new Vector3(xPos, transform.position.y, transform.position.z);
    }
}
