using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonController : MonoBehaviour
{

    [Header("Movement")]
    public float walkSpeed = 7f;
    public float runSpeed = 11f;

    [Header("Physics")]
    public Rigidbody rb;

    private Vector2 moveInput;
    private bool runInput;

    

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        rb = GetComponent<Rigidbody>();
        
    }

    void Update()
    {
        if (!GameManager.instance.IsPlaying())
        {
            moveInput.x = 0;
            moveInput.y = 0;
            return;
        } 
        moveInput.x = Keyboard.current.dKey.isPressed ? 1 :
                     Keyboard.current.aKey.isPressed ? -1 : 0;

        moveInput.y = Keyboard.current.wKey.isPressed ? 1 :
                     Keyboard.current.sKey.isPressed ? -1 : 0;

        runInput = Keyboard.current.leftShiftKey.isPressed;

        // Actualizar autoAttack automáticamente según condiciones
        

    }

    void FixedUpdate()
    {
        Move();
    }

    void Move()
    {
        float speed = runInput ? runSpeed : walkSpeed;

        Vector3 direction = (transform.forward * moveInput.y +
                             transform.right * moveInput.x).normalized;

        Vector3 targetVelocity = direction * speed;

        Vector3 currentVelocity = rb.linearVelocity;
        
        // Suavizar el cambio de velocidad horizontal
        Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);
        Vector3 targetHorizontalVelocity = new Vector3(targetVelocity.x, 0, targetVelocity.z);
        
        Vector3 smoothedVelocity = Vector3.Lerp(horizontalVelocity, targetHorizontalVelocity, 0.15f);

        rb.linearVelocity = new Vector3(
            smoothedVelocity.x,
            currentVelocity.y,
            smoothedVelocity.z
        );
    }
}
