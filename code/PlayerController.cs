using Sandbox;
using Sandbox.Citizen;
using System;
using System.Linq;

[Group("Hangout")]
[Title("Hangout - Player Controller")]
public sealed class PlayerController : Component
{
	[Property]
	public CharacterController CharacterController { get; set; }

	[Property] public float CrouchMoveSpeed { get; set; } = 64.0f;
	[Property] public float WalkMoveSpeed { get; set; } = 190.0f;
	[Property] public float RunMoveSpeed { get; set; } = 190.0f;
	[Property] public float SprintMoveSpeed { get; set; } = 320.0f;
	[Property] public float CameraDistance { get; set; } = 0f;
	public bool IsFirstPerson => CameraDistance == 0f;

	[Property] public CitizenAnimationHelper AnimationHelper { get; set; }

	public static PlayerController Local => NetworkManager.Instance.Players.FirstOrDefault(x => x.Network.IsOwner);

	[Sync] public ulong SteamId { get; set; }
	[Sync] public bool Crouching { get; set; }
	[Sync] public Vector3 LookForward { get; set; }

	public Angles EyeAngles;
	public Vector3 WishVelocity;
	public bool WishCrouch;
	public float EyeHeight = 64;
	Angles CamAngles = new Angles(0, 0, 0);
	bool isFreeCam = false;
	RealTimeUntil freeCamTime = 0f;

	protected override void OnUpdate()
	{
		if (!IsProxy)
		{
			MouseInput();

			Transform.Rotation = new Angles(0, EyeAngles.yaw, 0);
		}

		UpdateAnimation();

		var renderType = (!IsProxy && IsFirstPerson) ? ModelRenderer.ShadowRenderType.ShadowsOnly : ModelRenderer.ShadowRenderType.On;
		foreach (var modelRenderer in AnimationHelper.Components.GetAll<ModelRenderer>())
		{
			modelRenderer.RenderType = renderType;
		}
	}

	protected override void OnFixedUpdate()
	{
		if (IsProxy)
			return;

		CrouchingInput();
		MovementInput();
	}

	private void MouseInput()
	{
		bool updateEyes = true;
		if (!IsFirstPerson)
		{
			if (!isFreeCam && Input.Down("attack2"))
			{
				isFreeCam = true;
				CamAngles = EyeAngles;
			}
			else if (isFreeCam && !Input.Down("attack2"))
			{
				isFreeCam = false;
				freeCamTime = 1f;
			}

			if (isFreeCam)
			{
				updateEyes = false;
				CamAngles.pitch += Input.MouseDelta.y * 0.1f;
				CamAngles.yaw -= Input.MouseDelta.x * 0.1f;
				CamAngles.roll = 0;
				CamAngles.pitch = Math.Clamp(CamAngles.pitch, -89.9f, 89.9f);
			}
		}

		if (updateEyes)
		{
			EyeAngles += Input.AnalogLook;
			EyeAngles.pitch = EyeAngles.pitch.Clamp(-90, 90);
			EyeAngles.roll = 0.0f;
			if (freeCamTime > 0f)
				CamAngles = CamAngles.LerpTo(EyeAngles, RealTime.Delta * 25f);
			else
				CamAngles = EyeAngles;
		}

		LookForward = EyeAngles.ToRotation().Forward * 1024;

		// Zoom input
		CameraDistance = Math.Clamp(CameraDistance - Input.MouseWheel.y * 32f, 0f, 256f);
	}

	float CurrentMoveSpeed
	{
		get
		{
			if (Crouching) return CrouchMoveSpeed;
			if (Input.Down("run")) return SprintMoveSpeed;
			if (Input.Down("walk")) return WalkMoveSpeed;

			return RunMoveSpeed;
		}
	}

	RealTimeSince lastGrounded;
	RealTimeSince lastUngrounded;
	RealTimeSince lastJump;

	float GetFriction()
	{
		if (CharacterController.IsOnGround) return 6.0f;

		// air friction
		return 0.2f;
	}

	private void MovementInput()
	{
		if (CharacterController is null)
			return;

		var cc = CharacterController;

		Vector3 halfGravity = Scene.PhysicsWorld.Gravity * Time.Delta * 0.5f;

		WishVelocity = Input.AnalogMove;

		if (lastGrounded < 0.2f && lastJump > 0.3f && Input.Pressed("jump"))
		{
			lastJump = 0;
			cc.Punch(Vector3.Up * 300);
		}

		if (!WishVelocity.IsNearlyZero())
		{
			WishVelocity = new Angles(0, EyeAngles.yaw, 0).ToRotation() * WishVelocity;
			WishVelocity.z = 0;
			WishVelocity = WishVelocity.ClampLength(1);
			WishVelocity *= CurrentMoveSpeed;

			if (!cc.IsOnGround)
			{
				WishVelocity = WishVelocity.ClampLength(50);
			}
		}

		cc.ApplyFriction(GetFriction());

		if (cc.IsOnGround)
		{
			cc.Accelerate(WishVelocity);
			cc.Velocity = CharacterController.Velocity.WithZ(0);
		}
		else
		{
			cc.Velocity += halfGravity;
			cc.Accelerate(WishVelocity);

		}

		cc.Move();

		if (!cc.IsOnGround)
		{
			cc.Velocity += halfGravity;
		}
		else
		{
			cc.Velocity = cc.Velocity.WithZ(0);
		}

		if (cc.IsOnGround)
		{
			lastGrounded = 0;
		}
		else
		{
			lastUngrounded = 0;
		}
	}
	float DuckHeight = (64 - 36);

	bool CanUncrouch()
	{
		if (!Crouching) return true;
		if (lastUngrounded < 0.2f) return false;

		var tr = CharacterController.TraceDirection(Vector3.Up * DuckHeight);
		return !tr.Hit; // hit nothing - we can!
	}

	public void CrouchingInput()
	{
		WishCrouch = Input.Down("duck");

		if (WishCrouch == Crouching)
			return;

		// crouch
		if (WishCrouch)
		{
			CharacterController.Height = 36;
			Crouching = WishCrouch;

			// if we're not on the ground, slide up our bbox so when we crouch
			// the bottom shrinks, instead of the top, which will mean we can reach
			// places by crouch jumping that we couldn't.
			if (!CharacterController.IsOnGround)
			{
				CharacterController.MoveTo(Transform.Position += Vector3.Up * DuckHeight, false);
				Transform.ClearLerp();
				EyeHeight -= DuckHeight;
			}

			return;
		}

		// uncrouch
		if (!WishCrouch)
		{
			if (!CanUncrouch()) return;

			CharacterController.Height = 64;
			Crouching = WishCrouch;
			return;
		}


	}

	private void UpdateCamera()
	{
		if (IsProxy) return;

		var camera = Scene.GetAllComponents<CameraComponent>().Where(x => x.IsMainCamera).FirstOrDefault();
		if (camera is null) return;

		var targetEyeHeight = Crouching ? 28 : 64;
		EyeHeight = EyeHeight.LerpTo(targetEyeHeight, RealTime.Delta * 10.0f);

		var targetCameraPos = Transform.Position + new Vector3(0, 0, EyeHeight);

		if (CameraDistance > 0)
		{
			var tr = Scene.Trace.Ray(targetCameraPos, targetCameraPos + (CamAngles.Forward * -CameraDistance))
				.WithoutTags("player", "trigger")
				.Run();

			if (tr.Hit)
			{
				targetCameraPos = tr.HitPosition + tr.Normal * 2.0f;
			}
			else
			{
				targetCameraPos = tr.EndPosition;
			}
		}
		else
		{
			if (lastUngrounded > 0.2f)
			{
				targetCameraPos.z = camera.Transform.Position.z.LerpTo(targetCameraPos.z, RealTime.Delta * 25.0f);
			}
		}

		camera.Transform.Position = targetCameraPos;
		camera.Transform.Rotation = CamAngles;
		camera.FieldOfView = Preferences.FieldOfView;
	}

	protected override void OnPreRender()
	{
		UpdateCamera();
	}

	private void UpdateAnimation()
	{
		if (AnimationHelper is null) return;

		AnimationHelper.WithWishVelocity(WishVelocity);
		AnimationHelper.WithVelocity(CharacterController.Velocity);
		AnimationHelper.IsGrounded = CharacterController.IsOnGround;
		AnimationHelper.DuckLevel = Crouching ? 1.0f : 0.0f;

		AnimationHelper.WithLook(LookForward);
	}

}
