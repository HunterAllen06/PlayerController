using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HunterAllen.Player
{
    public class PlayerMover : MonoBehaviour
    {
        [Header("Components")]
        public Rigidbody Rigidbody;
        public CapsuleCollider Collider;
        public Transform Orientation;
        public Transform CameraTransform;

        [Header("Acceleration")]
        public float AccelerationStrength = 1.5f;

        [Header("Speed")]
        public float DefaultSpeed = 1.5f;
        public float SprintSpeed = 3f;
        public float CrouchSpeedMultiplier = 0.5f;

        [Header("Spring")]
        public float SpringForce = 1f;
        public float SpringForceSprintMultiplier = 2f;
        public float SpringDamp = 1f;

        [Space]
        public float SpringHeight = 0.25f;
        public float SpringRaycastDistance = 0.5f;

        [Range(0.01f, 1f)]
        public float SpringRadius = 0.5f;
        
        [Range(0f, 1f)]
        public float DirectionBias = 0.2f;
        public float DirectionBiasSprintMultiplier = 2f;
        public LayerMask GroundLayer;
        public bool TagsAsBlacklist;
        public List<string> Tags;

        [Header("Steps and Slopes")]
        public float MaxStepHeight = 0.5f;

        [Range(1f, 89f)]
        public float MaxSlopeAngle = 40f;
        
        [Header("Crouching")]
        [Range(0.1f, 3f)]
        public float DefaultHeight = 1.5f;

        [Range(0.1f, 1f)]
        public float CrouchHeight = 0.8f;
        public float CrouchSmoothSpeed = 8f;

        public Vector2 MoveInput;

        public bool IsSprinting;
        public bool IsCrouching;
        
        public bool OverrideIsSprinting;
        public bool OverrideIsCrouching;
        
        float _maxSlopeDot;
        RaycastHit[] _groundHitResults = new RaycastHit[4];
        RaycastHit[] _headCheckResults = new RaycastHit[1];

        void Update()
        {
            HandleCrouchInput();
        }

        void FixedUpdate()
        {
            ApplySpringForce();
            ApplyMovementForce(MoveInput);
        }

        void ApplySpringForce()
        {
#if UNITY_6000_0_OR_NEWER
            Vector3 velocity = Rigidbody.linearVelocity;
#else
            Vector3 velocity = Rigidbody.velocity;
#endif
            Vector3 direction = velocity;
            direction.y = 0;
            direction = direction.magnitude > 0.2f ? direction : Vector3.zero;

            float multiplier = IsSprinting || OverrideIsSprinting ? DirectionBiasSprintMultiplier : 1f;

            Ray ray = new(Collider.transform.position - (Collider.height * 0.5f - Collider.radius) * Vector3.up + DirectionBias * multiplier * direction.normalized, Vector3.down);

            int hits = Physics.SphereCastNonAlloc(ray, Collider.radius * SpringRadius, _groundHitResults, SpringRaycastDistance, GroundLayer, QueryTriggerInteraction.Ignore);
            if (hits == 0) return;

            // Select hit closest to the feet
            // This shit EATS all the ram - 1.0 kb GC allocation :(
            // RaycastHit hit = _groundHitResults.OrderBy(x => (new Vector2(x.point.x, x.point.z) - new Vector2(Collider.transform.position.x, Collider.transform.position.z)).magnitude).ToArray()[0];
            
            // New method
            RaycastHit groundHit = default;
            float previousHorizontalDistance = SpringRaycastDistance;
            for (int i = 0; i < hits; i++)
            {
                var hit = _groundHitResults[i];

                if (hit.point == default || Tags.Contains(hit.collider.tag) == TagsAsBlacklist)
                {
                    continue;
                }
                if (groundHit.point == default)
                {
                    groundHit = _groundHitResults[i];
                    continue;
                }

                var colliderPosition = Collider.transform.position;
                var currentHorizontalDistance = (new Vector2(hit.point.x, hit.point.z) - new Vector2(colliderPosition.x, colliderPosition.z)).magnitude;
                var hitHeight = ray.origin.y - hit.point.y; // Collider.transform.position.y - Collider.height - SpringHeight;

                if (
                    hitHeight < MaxStepHeight + 0.05f && // Step height
                    Vector3.Dot(hit.normal, Vector3.up) > _maxSlopeDot && // Slope
                    currentHorizontalDistance < previousHorizontalDistance) // Closest to player horizontally
                    //hit.distance < groundHit.distance &&
                {
                    groundHit = hit;
                }
            }

            multiplier = IsSprinting || OverrideIsSprinting ? SpringForceSprintMultiplier : 1f;
            float force = (SpringHeight - groundHit.distance) * SpringForce * multiplier - (velocity.y * SpringDamp);
            Rigidbody.AddForce(force * Time.fixedDeltaTime * Vector3.up);
        }
        void ApplyMovementForce(Vector2 input)
        {
            float targetSpeed = IsSprinting || OverrideIsSprinting ? SprintSpeed : DefaultSpeed * (IsCrouching || OverrideIsCrouching ? CrouchSpeedMultiplier : 1f);

            Vector3 direction = input.y * Orientation.forward + input.x * Orientation.right;

#if UNITY_6000_0_OR_NEWER
            Vector3 velocity = Rigidbody.linearVelocity;
#else
            Vector3 velocity = Rigidbody.velocity;
#endif

            velocity.y = 0;
            Vector3 force = AccelerationStrength * (targetSpeed * direction - velocity);

            Rigidbody.AddForce(force);
        }

        void HandleCrouchInput()
        {
            if (!IsCrouching && !OverrideIsCrouching && !CheckHeadRoom()) return;

            float newHeight = IsCrouching || OverrideIsCrouching ? CrouchHeight : DefaultHeight;
            Collider.height = Mathf.Lerp(Collider.height, newHeight, 1f - Mathf.Exp(-Time.deltaTime * CrouchSmoothSpeed));
        }
        bool CheckHeadRoom() => Physics.SphereCastNonAlloc(Rigidbody.position + Collider.height * 0.55f * Vector3.up, Collider.radius * 0.99f, Vector3.up, _headCheckResults, DefaultHeight - Collider.height + 0.1f, GroundLayer, QueryTriggerInteraction.Ignore) == 0;

        public void SetMoveInput(Vector2 input) => MoveInput = input;
        public void SetSprint(bool isSprinting) => IsSprinting = isSprinting;
        public void SetCrouch(bool isCrouching) => IsCrouching = isCrouching;

        void OnValidate()
        {
            Collider.height = DefaultHeight;
            Vector3 newPos = Collider.transform.position;
            newPos.y = Collider.height * 0.5f + SpringHeight;
            Collider.transform.position = newPos;
            CameraTransform.localPosition = (DefaultHeight * 0.5f - 0.1f) * Vector3.up;
            _maxSlopeDot = 1f - (MaxSlopeAngle / 90f);
        }
    }
}