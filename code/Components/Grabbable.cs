using System;
using System.Buffers;
using System.IO.Compression;
using System.Linq;
using Sandbox;

[Title("Grabbable")]
[Icon("pan_tool", "red", "white")]
public sealed class Grabbable : Component
{
    Rigidbody rigidBody;

    float HoldDistance = 100f;

    [Sync] Vector3 startingPos { get; set; } = Vector3.Zero;
    [Sync] public bool Frozen { get; set; } = true;

    protected override void OnStart()
    {
        startingPos = Transform.Position;
    }

    [Broadcast]
    public void Init()
    {
        if (!IsProxy) Frozen = false;
        InitAsync();
    }

    async void InitAsync()
    {
        var ident = GameObject.Name;
        var package = await Package.FetchAsync(ident, false);
        await package.MountAsync();

        var model = Model.Load(package.GetMeta("PrimaryAsset", ""));
        var modelRenderer = Components.Get<ModelRenderer>();
        if (modelRenderer is not null)
            modelRenderer.Model = model;
        var modelCollider = Components.Get<ModelCollider>();
        if (modelCollider is not null)
            modelCollider.Model = model;
    }

    protected override void OnUpdate()
    {
        if (rigidBody is not null)
        {
            if (!IsProxy && rigidBody.Velocity.z > 99999)
            {
                rigidBody.Velocity = Vector3.Zero;
                Transform.Position = startingPos;
            }
            rigidBody.Enabled = !Frozen;
        }

        var holder = NetworkManager.Instance.Players.Where(x => x.Network.OwnerId == GameObject.Network.OwnerId).FirstOrDefault();
        if (holder is not null)
        {
            using (Gizmo.Scope("physlol"))
            {
                Gizmo.Draw.Color = Color.Cyan;
                Gizmo.Draw.LineThickness = 4f;
                Gizmo.Draw.Line(holder.RightHand.Transform.Position, Transform.Position);
            }
        }

        if (!GameObject.Network.IsOwner) return;

        var player = PlayerController.Local;
        if (player is not null)
        {
            if (Input.Pressed("attack2"))
            {
                Frozen = true;
                CreateFreezeParticle();
                StopGrabbing(player);
            }
            else if (!Input.Down("Attack1") || Transform.Position.Distance(player.HeadPosition) > 1100f)
            {
                StopGrabbing(player);
            }
            else if (Input.Down("use"))
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

            if (Input.Down("use"))
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
        Frozen = false;

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

    [Broadcast]
    void CreateFreezeParticle()
    {
        var transform = Transform.World;
        transform.Position = Transform.Position;
        var obj = NetworkManager.Instance.FreezePrefab.Clone(transform);

    }
}
