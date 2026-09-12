using System.Collections;
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

        void Start()
        {
            _maxSlopeDot = 1f - (MaxSlopeAngle / 90f);
        }
        
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

            Vector3 rayOrigin = Collider.transform.position - 0.5f * Collider.height * Vector3.up;
            Vector3 rayOriginWithRadiusOffset = rayOrigin + (Collider.radius * Vector3.up);
            Ray ray = new(rayOrigin, Vector3.down);
            Ray rayWithDirectionBias = new(rayOriginWithRadiusOffset + DirectionBias * multiplier * direction.normalized, Vector3.down);

            int hits = Physics.SphereCastNonAlloc(rayWithDirectionBias, Collider.radius * SpringRadius, _groundHitResults, SpringRaycastDistance, GroundLayer, QueryTriggerInteraction.Ignore);
            if (hits == 0) return;

            RaycastHit groundHit;
            bool onGround = Physics.Raycast(ray, out groundHit, SpringRaycastDistance, GroundLayer, QueryTriggerInteraction.Ignore);

            Debug.DrawRay(ray.origin, SpringRaycastDistance * ray.direction, onGround ? Color.green : Color.red);
            Debug.DrawRay(rayWithDirectionBias.origin - Collider.radius * Vector3.up, SpringRaycastDistance * rayWithDirectionBias.direction, direction.magnitude == 0 ? Color.green : Color.yellow);

            var colliderPosition = Collider.transform.position;
            var feetPosition = ray.origin - SpringHeight * Vector3.up;
            float previousHorizontalDistance = SpringRaycastDistance;
            float previousHitHeight = SpringHeight - SpringRaycastDistance;
            
            // Select hit closest to the feet
            for (int i = 0; i < hits; i++)
            {
                var hit = _groundHitResults[i];
                var currentHorizontalDistance = (new Vector2(hit.point.x, hit.point.z) - new Vector2(colliderPosition.x, colliderPosition.z)).magnitude;
                var hitHeight = hit.point.y - feetPosition.y;

                if (hit.point == default || Tags.Contains(hit.collider.tag) == TagsAsBlacklist)
                {
                    StartCoroutine(DrawPoint(hit.point, 1));
                    continue;
                }
                StartCoroutine(DrawPoint(hit.point, 2));
                if (groundHit.point == default && hitHeight < MaxStepHeight + 0.05f)
                {
                    // Because of the direction bias setting, I'd like to do a separate raycast for the
                    // default grouind hit instead of the first hit in the array
                    groundHit = hit;
                    continue;
                }

                bool isStepAboveFeet = hitHeight >= 0;
                bool isLessThanStepHeight = hitHeight < MaxStepHeight + 0.05f;
                bool isLessThanMaxSlope = Vector3.Dot(hit.normal, Vector3.up) > _maxSlopeDot;
                bool isClosestHorizontally = currentHorizontalDistance < previousHorizontalDistance;
                bool isHighestVertically = hitHeight > previousHitHeight;
                // need to figure out conditions for step up vs step down

                if (isLessThanStepHeight && isLessThanMaxSlope && isHighestVertically) // && isClosestHorizontally)
                {
                    groundHit = hit;
                    // previousHorizontalDistance = currentHorizontalDistance;
                    previousHitHeight = hitHeight;
                }
            }

            if (groundHit.point == default) return;
            StartCoroutine(DrawPoint(groundHit.point, 3));

            // var distance = SpringHeight - groundHit.distance;
            var distance = SpringHeight - (ray.origin.y - groundHit.point.y);
            float velocityDot = Vector3.Dot(velocity, Vector3.up);
            multiplier = IsSprinting || OverrideIsSprinting ? SpringForceSprintMultiplier : 1f;
            float force = (distance) * SpringForce * multiplier - (velocityDot * SpringDamp) + Rigidbody.mass * Physics.gravity.magnitude;
            Rigidbody.AddForce(force * Vector3.up, ForceMode.Force);
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
            if (Application.isPlaying) return;

            Collider.height = DefaultHeight;
            Vector3 newPos = Collider.transform.position;
            newPos.y = Collider.height * 0.5f + SpringHeight;
            Collider.transform.position = newPos;
            CameraTransform.localPosition = (DefaultHeight * 0.5f - 0.1f) * Vector3.up;
            _maxSlopeDot = 1f - (MaxSlopeAngle / 90f);
        }

        IEnumerator DrawPoint(Vector3 pos, int index)
        {
            Color c = index switch
            {
                1 => Color.red,
                2 => Color.yellow,
                3 => Color.green,
                _ => Color.beige,
            };

            float scale = index switch
            {
                1 => 0.3f,
                2 => 0.2f,
                3 => 0.1f,
                _ => 0.1f,
            };
            var backBottomLeft = pos - scale * 0.5f * Vector3.one;
            var backBottomRight = backBottomLeft + scale * Vector3.right;
            var frontTopRight = pos + scale * 0.5f * Vector3.one;
            var frontTopLeft = frontTopRight - scale * Vector3.right;
            var backTopLeft = frontTopLeft - scale * Vector3.forward;
            var backTopRight = frontTopRight - scale * Vector3.forward;
            var frontBottomRight = backBottomRight + scale * Vector3.forward;
            var frontBottomLeft = backBottomLeft + scale * Vector3.forward;

            float time = index switch
            {
                1 => 2f,
                2 => 0.5f,
                _ => 0.2f,
            };
            float t = time;

            while (time > 0f)
            {
                c = Color.Lerp(c, c - Color.black, t - time);
                Debug.DrawLine(backBottomLeft, backBottomRight, c);
                Debug.DrawLine(backBottomLeft, backTopLeft, c);
                Debug.DrawLine(backTopRight, backTopLeft, c);
                Debug.DrawLine(backTopRight, backBottomRight, c);

                Debug.DrawLine(frontBottomLeft, frontBottomRight, c);
                Debug.DrawLine(frontBottomLeft, frontTopLeft, c);
                Debug.DrawLine(frontTopRight, frontTopLeft, c);
                Debug.DrawLine(frontTopRight, frontBottomRight, c);

                Debug.DrawLine(backBottomLeft, frontBottomLeft, c);
                Debug.DrawLine(backTopLeft, frontTopLeft, c);
                Debug.DrawLine(backBottomRight, frontBottomRight, c);
                Debug.DrawLine(backTopRight, frontTopRight, c);

                time -= Time.deltaTime;
                yield return null;
            }
        }
    }
}