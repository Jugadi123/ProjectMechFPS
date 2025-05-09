
using UnityEngine;

public class handleLowerUpperBodySyncing : MonoBehaviour
{
    [SerializeField] Transform upperBody;
    [SerializeField] Transform lowerBody;
    private handleMovement handle_movement;
    private float rootLerpAngle = 0f;
    private float lbyDelta;
    private float targetYaw = 0f;
    private float lowerBodyYawOffset = 0f;
    public float maxDesyncLimit = 30f;
    public float syncingSpeed = 5f;

    void Start()
    {
        // get the movement handler script
        handle_movement = GetComponent<handleMovement>();
    }

    void Update()
    {
        UpdateLBY();
    }

    void UpdateLBY()
    {
        float dt = Time.deltaTime;
        if (handle_movement.IsMovingHorizontally) {
            // If we move, sync the root with the upper body based on the direction of movement
            rootLerpAngle = Mathf.LerpAngle(transform.rotation.eulerAngles.y, upperBody.rotation.eulerAngles.y, dt * syncingSpeed);
            transform.rotation = Quaternion.Euler(0f, rootLerpAngle, 0f);

            // Convert movement direction to world yaw to rotate lower body
            Vector3 moveDir = handle_movement.horizontalMoveDirection;
            float moveYaw = Mathf.Atan2(-moveDir.x, -moveDir.z) * Mathf.Rad2Deg;
            
            lowerBodyYawOffset = Mathf.LerpAngle(lowerBody.rotation.eulerAngles.y, moveYaw, dt * syncingSpeed);
            lowerBody.rotation = Quaternion.Euler(0f, lowerBodyYawOffset, 0f);
        }
        else {
            // if we are not moving, check for max yaw limit and align the lower body to the upper body.
            lbyDelta = Mathf.Abs(Mathf.DeltaAngle(lowerBody.rotation.eulerAngles.y, upperBody.rotation.eulerAngles.y));
            if (lbyDelta > maxDesyncLimit) {
                targetYaw = upperBody.rotation.eulerAngles.y;
            }

            // sync the lower body
            lowerBodyYawOffset = Mathf.LerpAngle(lowerBody.rotation.eulerAngles.y, targetYaw, dt * syncingSpeed);
            lowerBody.rotation = Quaternion.Euler(0f, lowerBodyYawOffset, 0f);

            // this is the rotation of our player (character controller)
            // it controls the movement direction so this shouldn't change with the lower body. It should ALWAYS face the direction of the upper body
            rootLerpAngle = Mathf.LerpAngle(transform.rotation.eulerAngles.y, upperBody.rotation.eulerAngles.y, dt * syncingSpeed);
            transform.rotation = Quaternion.Euler(0f, rootLerpAngle, 0f);
        }
    }
}
