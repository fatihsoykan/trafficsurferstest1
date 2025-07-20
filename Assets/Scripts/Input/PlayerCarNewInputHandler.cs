
using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerCarNewInputHandler : MonoBehaviour
{
    public LayerMask groundLayer;
    private bool isGrounded;
    private bool canJump = false;
    
    Rigidbody rb;


    private int laneIndex = 1; // 0: sol, 1: orta, 2: sa�
    private float[] lanePositions = { -2f, 0f, 2f }; // X konumlar� (�eritler)
    private float laneSwitchSpeed = 10f; // �erit ge�i� h�z�
    private Vector2 moveInput;

    [Header("References")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float jumpCooldownTime = 0.2f;
    [SerializeField] private float moveCooldownTime = 0.3f; // 0.3 saniye bekle
    private float lastJumpTime;
    private float lastMoveTime;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private Transform groundCheck;




    PlayerInputAction playerInputAction;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();        
        // Input action'� olu�tur ve referans� sakla
        playerInputAction = new PlayerInputAction();

        // Aktif et
        playerInputAction.PlayerCarInputs.JUMP.Enable();
        playerInputAction.PlayerCarInputs.MOVE.Enable();

        // Event ba�la
        playerInputAction.PlayerCarInputs.JUMP.performed += Jump;
        playerInputAction.PlayerCarInputs.MOVE.performed += Move;

    }


    
    private void FixedUpdate()
    {
        // Zemin kontrol� i�in raycast
        canJump = Physics.Raycast(groundCheck.position, Vector3.down, groundCheckDistance, groundLayer);
        if (isGrounded)
        {
            canJump = true; // Yere de�diyse tekrar z�playabilir
        }

        // �erit hedef pozisyonuna yumu�ak ge�i�
        Vector3 targetPosition = new Vector3(lanePositions[laneIndex], transform.position.y, transform.position.z);
        Vector3 moveDirection = (targetPosition - transform.position);
        rb.linearVelocity = new Vector3(moveDirection.x * laneSwitchSpeed, rb.linearVelocity.y, rb.linearVelocity.z);


    }


    public void Jump(InputAction.CallbackContext context)
    {
        if (context.phase != InputActionPhase.Performed) { return; }
        if (Time.time < lastJumpTime + jumpCooldownTime) { return; }
        if (!canJump) { return; }


        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        lastJumpTime = Time.time;
        canJump = false;


    }

    public void Move(InputAction.CallbackContext context)
    {       
        Vector2 inputVector = context.ReadValue<Vector2>();

        if (Mathf.Abs(inputVector.x) < 0.5f) { return; } // �ok k���k y�nlerde hi�bir �ey yapma. Deadzone

        if (Time.time < lastMoveTime + moveCooldownTime) { return; }

        if (inputVector.x < 0 && laneIndex > 0)
        {
            laneIndex--;
            lastMoveTime = Time.time;
        }
        else if (inputVector.x > 0 && laneIndex < lanePositions.Length - 1)
        {
            laneIndex++;
            lastMoveTime = Time.time;
        }
    }



    private void OnDisable()
    {
        playerInputAction?.PlayerCarInputs.Disable();
    }

    private void OnDestroy()
    {
        playerInputAction.PlayerCarInputs.JUMP.performed -= Jump;
        playerInputAction.PlayerCarInputs.MOVE.performed -= Move;

    }

}
