using UnityEngine;

namespace HunterAllen.Player
{
    public class PlayerCamera : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField]
        Transform _cameraTransform;

        [SerializeField]
        CapsuleCollider _playerBody;

        [Header("Parameters")]
        [SerializeField]
        float _sensitivity = 1f;
        public float Sensitivity { get => _sensitivity; set => _sensitivity = value; }

        [SerializeField]
        float _smoothTime = 0.05f;
        public float SmoothTime { get => _smoothTime; set => _smoothTime = value; }

        [SerializeField]
        float _headRoom = 0.1f;

        [HideInInspector] public bool InvertMouseY { get; set; }

        [HideInInspector] public float RotX;
        [HideInInspector] public float RotY;
        float _rotXLerped;
        float _rotYLerped;

        float _mouseX;
        float _mouseY;
        float _rotXVelocity;
        float _rotYVelocity;

        void Update()
        {
            _rotXLerped = Mathf.SmoothDamp(_rotXLerped, RotX, ref _rotXVelocity, _smoothTime);
            _rotYLerped = Mathf.SmoothDamp(_rotYLerped, RotY, ref _rotYVelocity, _smoothTime);
            _cameraTransform.rotation = Quaternion.Euler(_rotXLerped * Vector3.right + _rotYLerped * Vector3.up);
            _cameraTransform.localPosition = (_playerBody.height * 0.5f - _headRoom) * Vector3.up;
        }
        void FixedUpdate()
        {   
            _playerBody.transform.localRotation = Quaternion.Euler(_rotYLerped * Vector3.up);
        }
        
        public void ApplyLookInput(Vector2 input)
        {
            _mouseX = input.x;
            _mouseY = input.y * (InvertMouseY ? -1 : 1);

            RotX -= _mouseY * _sensitivity;
            RotY += _mouseX * _sensitivity;
            RotX = Mathf.Clamp(RotX, -89f, 89f);
        }
    }
}