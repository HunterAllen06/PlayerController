# PlayerController
**System is a heavy W.I.P!**
Simple player controller with built in stair/slope support.

## PlayerController.cs
Has functions for providing input to the PlayerMover and PlayerCamera, but doesn't apply input on its own. You will need to create a class to provide input to PlayerController.

## PlayerMover.cs
Can move the player and handle ground/stair/slope detection. Automatically sets Collider height in OnValidate() according to height/raycast settings. Input is provided by the Player Controller.

## PlayerCamera.cs
Rotates the player camera and player body in Update and FixedUpdate. Input is provided by the Player Controller.
