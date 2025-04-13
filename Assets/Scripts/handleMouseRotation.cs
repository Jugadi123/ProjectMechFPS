using UnityEngine;

public class handleMouseRotation : MonoBehaviour
{

    [SerializeField] Transform upperBody;

    private float lastYaw;
    private float yawDelta;
    private bool isRotating = false;
    private bool wasRotating = false;
    private Vector2 mouse;
    public float mouseSensitivity;
    public float xRotation = 0f;
    public float yRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        mouseSensitivity = Debugging.instance.defaultMouseSensitivity;
    }


    void Update()
    {
        MouseRotation();
    }
    void MouseRotation()
    {

        mouse = new Vector2(Input.GetAxisRaw("Mouse X") * mouseSensitivity, Input.GetAxisRaw("Mouse Y") * mouseSensitivity);

        xRotation += mouse.y;
        xRotation = Mathf.Clamp(xRotation, -60f, 60f);
        yRotation += mouse.x;
   
        // normalize yaw
        // yRotation = (yRotation + 360f) % 360f;
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f); // yaw
        upperBody.localRotation = Quaternion.Euler(xRotation, 0f, 0f); // pitch

        float currentYaw = upperBody.rotation.eulerAngles.y;
        yawDelta = Mathf.Abs(Mathf.DeltaAngle(currentYaw, lastYaw));
        lastYaw = currentYaw;

        wasRotating = isRotating;
        isRotating = yawDelta > 0.1f;
    }
}
