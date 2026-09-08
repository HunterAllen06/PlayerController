<details>
<summary>Disclaimer</summary>
This repo primarily exists for personal use, and so projects I'm working on that have multiple programmers can share these utility/helper classes. Again, please note that these tools are built for my own projects; <b><ins>this means that they could change in functionality at any time</ins></b>. If you plan on using them long term, I strongly suggest sticking to one version/installing a packing and sticking to it, or paying very close attention to each update/commit. Feel free to use these in your own projects or base your own code off of mine, no credit needed; just don't claim it as your own.
</details>

# PlayerController
**System is a heavy W.I.P!**
Simple player controller with built in stair/slope support.

## PlayerController.cs
Has functions for providing input to the PlayerMover and PlayerCamera, but doesn't apply input on its own. You will need to create a class to provide input to PlayerController.

## PlayerMover.cs
Can move the player and handle ground/stair/slope detection. Automatically sets Collider height in OnValidate() according to height/raycast settings. Input is provided by the Player Controller.

## PlayerCamera.cs
Rotates the player camera and player body in Update and FixedUpdate. Input is provided by the Player Controller.
