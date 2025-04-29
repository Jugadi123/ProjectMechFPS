using UnityEngine;
using Unity.Netcode;
public class handleMouseRotation : NetworkBehaviour
{
    [SerializeField] Transform upperBody;
    private Vector2 mouse;
    public float mouseSensitivity;
    public float xRotation = 0f;
    public float yRotation = 0f;

    void Start()
    {
        mouseSensitivity = Debugging.instance.defaultMouseSensitivity;
    }

    void Update()
    {
        if (IsOwner) {
            MouseRotation();
        }
        else {
            // interpolate other player's rotations
        }
    }

    void MouseRotation() {
        mouse = new Vector2(Input.GetAxisRaw("Mouse X") * mouseSensitivity, Input.GetAxisRaw("Mouse Y") * mouseSensitivity);
        xRotation += mouse.y;
        xRotation = Mathf.Clamp(xRotation, -60f, 60f);
        yRotation += mouse.x;
        yRotation = (yRotation + 360f) % 360f;
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f); // yaw
        upperBody.localRotation = Quaternion.Euler(xRotation, 0f, 0f); // pitch
    }
}
