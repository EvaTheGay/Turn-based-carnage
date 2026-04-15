using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 80f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.15f;
    [SerializeField] private float dashCooldown = 0.5f;

    [Header("Jump (Top Down Bounce)")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float jumpDuration = 0.3f;

    [Header("Click To Move")]
    [SerializeField] private float arrivalThreshold = 0.15f;
    [SerializeField] private GameObject clickEffectPrefab;

    [Header("Rotation")]
    [SerializeField] private bool rotatesTowardsMouse = true;
    [SerializeField] private float rotationSpeed = 20f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector2 currentVelocity;

    private Vector2 clickTarget;
    private bool isClickMoving = false;

    // Dash
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private Vector2 dashDirection;

    // Jump (simulated via scale for top-down)
    private bool isJumping = false;
    private float jumpTimer = 0f;
    private Vector3 originalScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
            rb = FindFirstObjectByType<Rigidbody2D>();

        if (rb == null)
            Debug.LogError("No Rigidbody2D found!", this);
        else
            rb.gravityScale = 0;

        originalScale = transform.localScale;
    }

    void Update()
    {
        HandleKeyboardInput();
        HandleClickToMove();
        HandleRotation();
        HandleJumpVisual();

        // Timers
        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f)
                isDashing = false;
        }
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            rb.linearVelocity = dashDirection * dashSpeed;
            return;
        }

        Vector2 movementDirection;

        if (isClickMoving)
        {
            Vector2 toTarget = clickTarget - (Vector2)transform.position;

            if (toTarget.magnitude < arrivalThreshold)
            {
                isClickMoving = false;
                movementDirection = Vector2.zero;
                currentVelocity = Vector2.zero;
            }
            else
            {
                float speedScale = Mathf.Clamp01(toTarget.magnitude / (arrivalThreshold * 3f));
                movementDirection = toTarget.normalized * speedScale;
            }
        }
        else
        {
            movementDirection = moveInput;
        }

        Vector2 targetVelocity = movementDirection * moveSpeed;

        currentVelocity = Vector2.MoveTowards(
            currentVelocity,
            targetVelocity,
            acceleration * Time.fixedDeltaTime
        );

        rb.linearVelocity = currentVelocity;
    }

    private void HandleKeyboardInput()
    {
        if (Keyboard.current == null) return;

        Vector2 wasd = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) wasd.y += 1f;
        if (Keyboard.current.sKey.isPressed) wasd.y -= 1f;
        if (Keyboard.current.aKey.isPressed) wasd.x -= 1f;
        if (Keyboard.current.dKey.isPressed) wasd.x += 1f;
        moveInput = wasd.normalized;

        if (moveInput.magnitude > 0.1f)
            isClickMoving = false;

        // Dash on K key
        if (Keyboard.current.kKey.wasPressedThisFrame && !isDashing && dashCooldownTimer <= 0f)
        {
            Vector2 dir = moveInput.magnitude > 0.1f ? moveInput : (Vector2)transform.up;
            StartDash(dir);
        }

        // Jump on Space
        if (Keyboard.current.spaceKey.wasPressedThisFrame && !isJumping)
            StartJump();
    }

    private void StartDash(Vector2 direction)
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        dashDirection = direction.normalized;
        isClickMoving = false;
    }

    private void StartJump()
    {
        isJumping = true;
        jumpTimer = 0f;
    }

    private void HandleJumpVisual()
    {
        if (!isJumping) return;

        jumpTimer += Time.deltaTime;
        float progress = jumpTimer / jumpDuration;

        if (progress >= 1f)
        {
            isJumping = false;
            transform.localScale = originalScale;
            return;
        }

        // Arc: scale up then back down to simulate jump height
        float arc = Mathf.Sin(progress * Mathf.PI);
        float scaleBoost = 1f + arc * (jumpHeight * 0.3f);
        transform.localScale = new Vector3(
            originalScale.x * scaleBoost,
            originalScale.y * scaleBoost,
            originalScale.z
        );
    }

    private void HandleClickToMove()
    {
        if (Mouse.current == null) return;
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(
            new Vector3(Mouse.current.position.value.x,
                        Mouse.current.position.value.y, 0f)
        );

        Vector2 clickPos = new Vector2(mousePos.x, mousePos.y);
        clickTarget = clickPos;
        isClickMoving = true;

        if (clickEffectPrefab != null)
            Instantiate(clickEffectPrefab,
                new Vector3(clickPos.x, clickPos.y, 0f),
                Quaternion.identity);
    }

    private void HandleRotation()
    {
        if (!rotatesTowardsMouse) return;
        if (Mouse.current == null) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(
            new Vector3(Mouse.current.position.value.x,
                        Mouse.current.position.value.y, 0f)
        );

        Vector2 direction = new Vector2(
            mousePos.x - transform.position.x,
            mousePos.y - transform.position.y
        );

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle - 90f);
        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}