using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class MovimientoJugador : MonoBehaviour
{
    public Transform camara;
    public float velocidad = 3.6f;
    public float sensibilidad = 0.12f;
    [Tooltip("Velocidad de giro con el stick derecho, en grados por segundo.")]
    public float sensibilidadControl = 120f;

    private CharacterController controlador;
    private float inclinacion;
    private float velocidadVertical;

    void Start()
    {
        controlador = GetComponent<CharacterController>();
        BloquearCursor();
    }

    void Update()
    {
        var teclado = Keyboard.current;
        var raton = Mouse.current;
        var control = Gamepad.current;

        if (teclado != null && teclado.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if ((raton != null && raton.leftButton.wasPressedThisFrame)
                 || (control != null && control.startButton.wasPressedThisFrame))
        {
            BloquearCursor();
        }

        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        // El ratón entrega un desplazamiento; el stick, una velocidad de giro.
        Vector2 mirada = raton != null
            ? raton.delta.ReadValue() * sensibilidad
            : Vector2.zero;

        if (control != null)
            mirada += control.rightStick.ReadValue() * sensibilidadControl * Time.deltaTime;

        transform.Rotate(0f, mirada.x, 0f);

        inclinacion = Mathf.Clamp(
            inclinacion - mirada.y, -80f, 80f
        );

        camara.localRotation = Quaternion.Euler(inclinacion, 0f, 0f);

        // Caminar con las flechas del teclado.
        Vector2 entrada = Vector2.zero;
        if (teclado != null)
        {
            entrada.x = (teclado.rightArrowKey.isPressed ? 1f : 0f)
                      - (teclado.leftArrowKey.isPressed ? 1f : 0f);
            entrada.y = (teclado.upArrowKey.isPressed ? 1f : 0f)
                      - (teclado.downArrowKey.isPressed ? 1f : 0f);
        }

        // ReadValue aplica la zona muerta del Input System y conserva la intensidad.
        if (control != null)
            entrada += control.leftStick.ReadValue();

        Vector3 direccion =
            Vector3.ClampMagnitude(
                transform.right * entrada.x + transform.forward * entrada.y, 1f
            );

        // Mantener al jugador sobre el piso.
        if (controlador.isGrounded && velocidadVertical < 0f)
            velocidadVertical = -2f;

        velocidadVertical += Physics.gravity.y * Time.deltaTime;

        Vector3 movimiento = direccion * velocidad;
        movimiento.y = velocidadVertical;

        controlador.Move(movimiento * Time.deltaTime);
    }

    void BloquearCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
