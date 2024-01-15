using System;
using System.IO.Compression;
using Sandbox;

[Title("Grabbable")]
[Icon("pan_tool", "red", "white")]
public sealed class Grabbable : Component
{
    Rigidbody rigidBody;

    float HoldDistance = 100f;

    protected override void OnUpdate()
    {
        if (!GameObject.Network.IsOwner) return;

        var player = PlayerController.Local;
        if (player is not null)
        {
            if (!Input.Down("Attack1") || Transform.Position.Distance(player.HeadPosition) > 250f)
            {
                StopGrabbing(player);
            }
            else if (Input.Down("attack2"))
            {
                if (Input.Down("forward"))
                {
                    HoldDistance += 50f * Time.Delta;
                }
                else if (Input.Down("backward"))
                {
                    HoldDistance -= 50f * Time.Delta;
                    if (HoldDistance < 25f) HoldDistance = 25f;
                }
            }
        }
    }

    protected override void OnFixedUpdate()
    {
        rigidBody ??= Components.Get<Rigidbody>();
        rigidBody.Gravity = GameObject.Network.OwnerId == Guid.Empty;

        if (!GameObject.Network.IsOwner) return;

        var player = PlayerController.Local;
        if (player is not null)
        {
            // Move towards a position in front of the holder's head
            var targetPos = player.HeadPosition + player.EyeAngles.Forward * HoldDistance;

            var delta = targetPos - Transform.Position;
            var distance = delta.Length;

            // if ( distance > 10f )
            // {
            var direction = delta.Normal;
            var speed = distance * 5f;

            if (speed > 800f) speed = 800f;

            var wishVelocity = direction * speed;

            if (rigidBody is not null)
            {
                rigidBody.Velocity = wishVelocity;
            }

            rigidBody.AngularVelocity = Vector3.Zero;
            rigidBody.AngularDamping = 0f;

            if (Input.Down("attack2"))
            {
                var mouseDelta = Input.MouseDelta;
                var pitchRotation = Rotation.FromAxis(player.EyeAngles.ToRotation().Right, mouseDelta.y * 0.1f);
                var yawRotation = Rotation.FromAxis(Vector3.Up, mouseDelta.x * 0.1f);
                var totalRotation = pitchRotation * yawRotation;
                Transform.Rotation = totalRotation * Transform.Rotation;
            }

            // }
            // else
            // {
            //     if ( rigidBody is not null )
            //     {
            //         rigidBody.Velocity = Vector3.Zero;
            //     }
            // }
        }
    }

    public void StartGrabbing(PlayerController player)
    {
        if (GameObject.Network.OwnerId != Guid.Empty) return;
        if (player.Grabbing.IsValid()) return;

        GameObject.Network.TakeOwnership();
        // GameObject.Tags.Add( "player" );

        HoldDistance = Transform.Position.Distance(player.HeadPosition);

        player.Grabbing = GameObject;
    }

    public void StopGrabbing(PlayerController player)
    {
        if (!GameObject.Network.IsOwner) return;

        Log.Info($"Dropping {GameObject.Network.OwnerId}");

        GameObject.Network.DropOwnership();
        // GameObject.Tags.Remove( "player" );
        player.Grabbing = null;
    }
}
