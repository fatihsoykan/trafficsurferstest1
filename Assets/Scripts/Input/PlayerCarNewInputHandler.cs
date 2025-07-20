
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCarNewInputHandler : MonoBehaviour
{
    public LayerMask groundLayer;
    private bool isGrounded;

    Rigidbody rb;

    PlayerInputAction playerInputAction;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();        
        // Input action'ý oluþtur ve referansý sakla
        playerInputAction = new PlayerInputAction();

        // Aktif et
        playerInputAction.PlayerCarInputs.JUMP.Enable();
        playerInputAction.PlayerCarInputs.MOVE.Enable();

        // Event baðla
        playerInputAction.PlayerCarInputs.JUMP.performed += Jump;
    
    }



    private void FixedUpdate()
    {
        // Zemin kontrolü
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f, groundLayer);

        // Hareket input'u
        Vector2 inputVector = playerInputAction.PlayerCarInputs.MOVE.ReadValue<Vector2>();
        rb.AddForce(new Vector3(inputVector.x, 0, 0) * 10f, ForceMode.Force);
    }


    public void Jump(InputAction.CallbackContext context)
    {
        Debug.Log("Context geldi! Phase: " + context.phase);

        if (context.phase == InputActionPhase.Performed && isGrounded)
        {
            Debug.Log("Jump!" + context.phase);
            rb.AddForce(Vector3.up * 3f, ForceMode.Impulse);
        }
    }

    public void Move(InputAction.CallbackContext context)
    {
        Debug.Log("Context geldi! Phase: " + context.phase);
        Vector2 inputVector = context.ReadValue<Vector2>();
        rb.AddForce(new Vector3(inputVector.x, 0, inputVector.y) * 3f, ForceMode.Impulse);
    }













    private void OnDisable()
    {
        playerInputAction?.PlayerCarInputs.Disable();
    }

    private void OnDestroy()
    {
        playerInputAction.PlayerCarInputs.JUMP.performed -= Jump;
    }












}
