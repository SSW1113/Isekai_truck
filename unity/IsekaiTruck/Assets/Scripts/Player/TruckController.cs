using UnityEngine;
using UnityEngine.InputSystem;

namespace IsekaiTruck.Player
{
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class TruckController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float acceleration = 12f;
        [SerializeField, Min(0.1f)] private float maxSpeed = 8f;
        [SerializeField, Min(0.1f)] private float deceleration = 10f;
        [SerializeField, Min(0.1f)] private float turnSpeed = 360f;

        private Rigidbody truckRigidbody;
        private Vector2 moveInput;
        private float currentSpeed;

        public float CurrentSpeed
        {
            get
            {
                if (truckRigidbody == null)
                {
                    return 0f;
                }

                Vector3 velocity = truckRigidbody.linearVelocity;
                return new Vector2(velocity.x, velocity.z).magnitude;
            }
        }

        public float MaxSpeed => maxSpeed;

        private void Awake()
        {
            truckRigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            moveInput = ReadMoveInput();
        }

        private void FixedUpdate()
        {
            Vector3 desiredDirection = new Vector3(moveInput.x, 0f, moveInput.y);

            if (desiredDirection.sqrMagnitude > 1f)
            {
                desiredDirection.Normalize();
            }

            Quaternion movementRotation = truckRigidbody.rotation;

            if (desiredDirection.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(desiredDirection, Vector3.up);
                Quaternion nextRotation = Quaternion.RotateTowards(
                    truckRigidbody.rotation,
                    targetRotation,
                    turnSpeed * Time.fixedDeltaTime);

                truckRigidbody.MoveRotation(nextRotation);
                movementRotation = nextRotation;
                currentSpeed = Mathf.MoveTowards(
                    currentSpeed,
                    maxSpeed * moveInput.magnitude,
                    acceleration * Time.fixedDeltaTime);
            }
            else
            {
                currentSpeed = Mathf.MoveTowards(
                    currentSpeed,
                    0f,
                    deceleration * Time.fixedDeltaTime);
            }

            Vector3 forwardVelocity = movementRotation * Vector3.forward * currentSpeed;
            Vector3 velocity = truckRigidbody.linearVelocity;
            truckRigidbody.linearVelocity = new Vector3(
                forwardVelocity.x,
                velocity.y,
                forwardVelocity.z);
        }

        private static Vector2 ReadMoveInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            float horizontal = 0f;
            float vertical = 0f;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                horizontal -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                horizontal += 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
            {
                vertical -= 1f;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                vertical += 1f;
            }

            return Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        }
    }
}
