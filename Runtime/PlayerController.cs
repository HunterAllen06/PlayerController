using System;
using UnityEngine;

namespace HunterAllen.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField]
        public PlayerMover PlayerMover;

        [SerializeField]
        public PlayerCamera PlayerCamera;

        public virtual void ProvideMoveInput(Vector2 input)
        {
            PlayerMover.SetMoveInput(input);
        }
        public virtual void ProvideLookInput(Vector2 input)
        {
            PlayerCamera.ApplyLookInput(input);
        }
        public virtual void ProvideSprintInput(bool input)
        {
            PlayerMover.SetSprint(input);
        }
        public virtual void ProvideCrouchInput(bool input)
        {
            PlayerMover.SetCrouch(input);
        }
    }
}