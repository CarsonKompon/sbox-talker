using Sandbox;

public class DevCam : Component
{
	public static DevCam Instance { get; private set; }
	public static bool On { get; private set; } = false;

	[Property] GameObject DevCamOverlay { get; set; }

	Vector3 TargetPos;
	Angles EyeAngles;

	bool PivotEnabled;
	Vector3 PivotPos;
	float PivotDist;

	float MoveSpeed;
	float BaseMoveSpeed = 300.0f;
	float LerpAmount = 4f;
	float FovOverride = 0;
	float OldFov = 0;

	CameraComponent Camera;

	protected override void OnStart()
	{
		Camera = Components.Get<CameraComponent>();
		Instance = this;
		On = false;
	}

	protected override void OnUpdate()
	{
		if (Input.Pressed("view")) SetOnOff(!On);

		if (!On) return;

		var input = Input.AnalogMove;
		MoveSpeed = 1;

		if (Input.Down("run")) MoveSpeed = 5;
		if (Input.Down("duck")) MoveSpeed = 0.2f;

		if (Input.Pressed("walk"))
		{
			var tr = Scene.Trace.Ray(Transform.Position, Transform.Position + Transform.Rotation.Forward * 4096).Run();
			if (tr.Hit)
			{
				PivotEnabled = true;
				PivotPos = tr.EndPosition;
				PivotDist = Transform.Position.Distance(PivotPos);
			}
		}

		if (Input.Down("attack2"))
		{
			FovOverride += Input.AnalogLook.pitch * (FovOverride / 30f);
			FovOverride = FovOverride.Clamp(5, 150);
			Camera.FieldOfView = FovOverride;
			Input.AnalogLook = default;
		}

		if (Input.Pressed("score"))
		{
			DevCamOverlay.Enabled = !DevCamOverlay.Enabled;
		}

		EyeAngles += Input.AnalogLook * (FovOverride / 80f);
		EyeAngles.roll = 0;

		PivotEnabled = PivotEnabled && Input.Down("walk");

		if (PivotEnabled)
		{
			input.x += Input.MouseWheel.y * 10.0f;

			PivotDist -= input.x * RealTime.Delta * 100f * (PivotDist / 50f);
			PivotDist = PivotDist.Clamp(10, 1000);

			Transform.Rotation = Rotation.Slerp(Transform.Rotation, EyeAngles.ToRotation(), RealTime.Delta * LerpAmount);

			TargetPos = PivotPos + Transform.Rotation.Backward * PivotDist;
			Transform.Position = TargetPos;
		}
		else
		{

			BaseMoveSpeed += Input.MouseWheel.y * 10.0f;
			BaseMoveSpeed = BaseMoveSpeed.Clamp(10, 1000);

			var mv = input.Normal * BaseMoveSpeed * RealTime.Delta * Transform.Rotation * MoveSpeed;

			TargetPos += mv;

			Transform.Position = Transform.Position.LerpTo(TargetPos, RealTime.Delta * LerpAmount);
			Transform.Rotation = Rotation.Slerp(Transform.Rotation, EyeAngles.ToRotation(), RealTime.Delta * LerpAmount);
		}
	}

	public void SetOnOff(bool isOn)
	{
		if (isOn)
		{
			OldFov = Camera.FieldOfView;
			FovOverride = OldFov;
			TargetPos = Camera.Transform.Position;
			EyeAngles = Camera.Transform.Rotation.Angles();

			On = true;
		}
		else
		{
			Camera.FieldOfView = OldFov;

			On = false;
		}
	}

}