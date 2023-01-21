using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;

namespace KCC
{
	
//Simple car controller.


public class CarController : MonoBehaviour {
	private Rigidbody RB;
	private ParametersKeeper ParamsKeeper;
	private float[] GearRatios;
	private WheelFrictionCurve TempFriction;

	[HideInInspector]
	public float 
		SteeringAngle,
		TorqToWheel,
		WheelRPM,
		TopGear,
		TravelFL,
		TravelFR,
		TravelRL,
		TravelRR,
		AntirollforceFront,
		AntirollforceRear;

	[HideInInspector]
	public bool 
		isBraking,
		isHandBraking,
		WCSubstepsApplyed,
		RevsMoreThanDecrease,
		isRedlined;
	public Texture2D 
		Speedometer,
		Tahometer,
		Pointer;
	
	[Header("Particles")]
	public ParticleSystem ExhaustParticle;
	public ParticleSystem SkidParticles;

	[Header("Driving parameters")]
	public float MaxSteeringAngle=25;
	public float AirResistanceCoefficient=0.01f;
	public float HandbrakeSidewaysFriction=0.8f;
	public float AntirollBarForce=1000;

	[Header("Gearbox")]
	public float IdleRpm=800;
	public float MaxRpm=6000;
	public float IncreaseGearRpm=5000;
	public float  DecreaseGearRpm=2000;
	
	[HideInInspector] public int  CurrentGear;
	[HideInInspector]  public float CurrentRPM;
	
	[Header("Lights")]
	public GameObject LeftStoplight;
	public GameObject RightStoplight;
	public GameObject LeftReverselight;
	public GameObject RightReverselight;
	
	private AudioSource EngineSound;
	public AudioSource skidSound;

	void Start ()
		{
			ParamsKeeper = gameObject.GetComponent<ParametersKeeper> ();
			RB = gameObject.GetComponent<Rigidbody> ();
			foreach (var item in ParamsKeeper.Wheels) 
				item.DefaultSidewaysFriction = item.WheelCollider.sidewaysFriction.stiffness;
		}
			
		void Lights(){
			if (LeftStoplight == null || RightStoplight == null || LeftReverselight == null || RightReverselight == null) return;
			if (isBraking) 
			{
				if (ParamsKeeper.Body.BrakeLightLeft!=null && ParamsKeeper.Body.BrakeLightLeft.GetComponent<MeshRenderer>().enabled) LeftStoplight.SetActive (true);
				if (ParamsKeeper.Body.BrakeLightRight!=null && ParamsKeeper.Body.BrakeLightRight.GetComponent<MeshRenderer>().enabled) RightStoplight.SetActive (true);
			} else {
				LeftStoplight.SetActive (false);
				RightStoplight.SetActive (false);
			}

			if (WheelRPM < 0) {
				if (ParamsKeeper.Body.BrakeLightLeft!=null && ParamsKeeper.Body.BrakeLightLeft.GetComponent<MeshRenderer>().enabled) LeftReverselight.SetActive (true);
				if (ParamsKeeper.Body.BrakeLightRight!=null && ParamsKeeper.Body.BrakeLightRight.GetComponent<MeshRenderer>().enabled) RightReverselight.SetActive (true);
			} else {
				LeftReverselight.SetActive (false);
				RightReverselight.SetActive (false);
			}

			if (!ParamsKeeper.Engine.IsEngineWorking) {
				if (LeftStoplight.activeSelf) LeftStoplight.SetActive (false);
				if (RightStoplight.activeSelf) RightStoplight.SetActive (false);
				if (LeftReverselight.activeSelf) LeftReverselight.SetActive (false);
				if (RightReverselight.activeSelf) RightReverselight.SetActive (false);
			}
		}




		void Controls()
		{
			TorqToWheel = Input.GetAxis ("Vertical") * ParamsKeeper.Engine.MotorPower;

			if (Mathf.Sign (Input.GetAxis ("Vertical")) != Mathf.Sign (WheelRPM)) isBraking = true;
			else isBraking = false;

			if ((int)ParamsKeeper.Speed == 0) isBraking = false;

			if (WheelRPM < 0 && Input.GetAxis ("Vertical") == 0) isBraking = false;

			if (Input.GetKey ("space")) isHandBraking = true;
			else isHandBraking=false;

			SteeringAngle = (Input.GetAxis ("Horizontal")) * MaxSteeringAngle;
		}



		void GearsShifting(){
			
			if (ParamsKeeper.Engine.GearBox != null && ParamsKeeper.Engine.GearBox.GetComponent<CarPart>().GearRatios.Length>1) {
				GearRatios = ParamsKeeper.Engine.GearBox.GetComponent<CarPart> ().GearRatios;
				TopGear = ParamsKeeper.Engine.GearBox.GetComponent<CarPart> ().TopGear;
			} else {
				Debug.LogWarning ("Gearbox not found. Gear ratios are set to default values.");
				GearRatios = new float[]{ 3.6f, 1.9f, 1.4f, 0.9f, 0.6f };
				TopGear = 4;
			}

			WheelRPM=0;
			for (int i = 0; i < ParamsKeeper.Wheels.Length; i++)
				if (ParamsKeeper.Wheels [i].motor) {
					if (WheelRPM == 0) WheelRPM = ParamsKeeper.Wheels [i].WheelCollider.rpm;
					WheelRPM = Mathf.Min (WheelRPM, ParamsKeeper.Wheels [i].WheelCollider.rpm);
				}
					
			if (Mathf.Abs(WheelRPM) > ((MaxRpm - IdleRpm) / (GearRatios [CurrentGear - 1] * TopGear))) isRedlined = true;
			else isRedlined = false;
			CurrentRPM = Mathf.Clamp (CurrentRPM, IdleRpm, MaxRpm);
			CurrentRPM = IdleRpm + (Mathf.Abs(WheelRPM)  * GearRatios [CurrentGear - 1] * TopGear);

			if (Mathf.Sign(WheelRPM) == 1) {
				
				if (CurrentRPM > DecreaseGearRpm) RevsMoreThanDecrease = true;

				bool DontShift=false;

				if (CurrentRPM > IncreaseGearRpm && CurrentGear < GearRatios.Length) {
					foreach (var item in ParamsKeeper.Wheels)
						if (item.motor)
							if (Mathf.Abs (item.wheelhit.forwardSlip) > 0.5f * item.DefaultSidewaysFriction) DontShift = true;
					if (!DontShift) {
						CurrentGear += 1;
						RB.AddForceAtPosition (Vector3.down * 200,ParamsKeeper.Engine.EngineDummy.transform.position, ForceMode.Impulse);
					}
				}
					
				if (CurrentRPM < DecreaseGearRpm && RevsMoreThanDecrease && CurrentGear > 1) {
					CurrentGear -= 1;
					RevsMoreThanDecrease = false;
				}

			} else CurrentGear = 1;
			
		}


		void OnGUI() {
			if (!ParamsKeeper.Engine.IsEngineWorking) return;
			float speedfactor,  rotationangle,  revsfactor;

			if (Speedometer != null) {
				GUI.DrawTexture (new Rect (Screen.width - 250, Screen.height - 250, 250, 250), Speedometer);
				if (ParamsKeeper != null) speedfactor = ParamsKeeper.Speed / 180;
				else speedfactor = 0;
				rotationangle = Mathf.Lerp (-45, 225, speedfactor);
				GUIUtility.RotateAroundPivot (rotationangle, new Vector2 (Screen.width - 125, Screen.height - 125));
				GUI.DrawTexture (new Rect (Screen.width - 250, Screen.height - 250, 250, 250), Pointer);
				GUIUtility.RotateAroundPivot (-rotationangle, new Vector2 (Screen.width - 125, Screen.height - 125));
			}

			if (Tahometer != null) {
				GUI.DrawTexture (new Rect (Screen.width - 450, Screen.height - 220, 200, 200), Tahometer);
				revsfactor = CurrentRPM / MaxRpm;
				rotationangle = Mathf.Lerp (-45, 140, revsfactor);
				GUIUtility.RotateAroundPivot (rotationangle, new Vector2 (Screen.width - 350, Screen.height - 120));
				GUI.DrawTexture (new Rect (Screen.width - 450, Screen.height - 220, 200, 200), Pointer);
				GUIUtility.RotateAroundPivot (-rotationangle, new Vector2 (Screen.width - 350, Screen.height - 120));
			}
			GUI.Label (new Rect (Screen.width - 370, Screen.height - 100, 50, 30), "Gear: " + CurrentGear.ToString());

		}


		//Necessary for correct WheelCollider.rpm calculation.
		void WCSubstepsApply (){
			foreach (var item in ParamsKeeper.Wheels) item.WheelCollider.ConfigureVehicleSubsteps (50, 50, 50);
			WCSubstepsApplyed = true;
		}


	
		void AntirollBar(){

			AntirollforceFront = (TravelFL - TravelFR) * AntirollBarForce;
			AntirollforceRear = (TravelRL - TravelRR) * AntirollBarForce;

			foreach (var wheel in ParamsKeeper.Wheels) {

				if (wheel.wheelLocation==aWheel.WheelLocation.FL) {
					TravelFL = wheel.Travel;
					if (wheel.Grounded) RB.AddForceAtPosition (wheel.WheelCollider.transform.up * -AntirollforceFront, wheel.WheelCollider.transform.position);
				}
				if (wheel.wheelLocation==aWheel.WheelLocation.FR) {
					TravelFR = wheel.Travel;
					if (wheel.Grounded) RB.AddForceAtPosition (wheel.WheelCollider.transform.up * AntirollforceFront, wheel.WheelCollider.transform.position);
				}
				if (wheel.wheelLocation==aWheel.WheelLocation.RL) {
					TravelRL = wheel.Travel;
					if (wheel.Grounded) RB.AddForceAtPosition (wheel.WheelCollider.transform.up * -AntirollforceRear, wheel.WheelCollider.transform.position);
				}
				if (wheel.wheelLocation==aWheel.WheelLocation.RR) {
					TravelRR = wheel.Travel;
					if (wheel.Grounded) RB.AddForceAtPosition (wheel.WheelCollider.transform.up * AntirollforceRear, wheel.WheelCollider.transform.position);
				}
				
			}
		}



		void Skidding() {
			bool SkidSound = false;

			foreach (var item in ParamsKeeper.Wheels)
			if ((Mathf.Abs (item.wheelhit.sidewaysSlip) > 0.5f * item.DefaultSidewaysFriction || Mathf.Abs (item.wheelhit.forwardSlip) > 0.5f * item.DefaultSidewaysFriction)) {
					if (SkidParticles!=null && item.SkidParticle == null) item.SkidParticle = Instantiate (SkidParticles);
					if (item.SkidParticle!=null) item.SkidParticle.transform.position = item.wheelhit.point;
					if (item.SkidParticle!=null) item.SkidParticle.Play ();
					SkidSound = true;
			} else if (item.SkidParticle != null) { 
				item.SkidParticle.Stop ();
				Destroy (item.SkidParticle.gameObject, 2);
			}
			
			if (skidSound != null) {
				if (!skidSound.isPlaying && SkidSound) skidSound.Play ();
				if (skidSound.isPlaying && !SkidSound) skidSound.Stop ();
			}

		}


	void FixedUpdate (){

			Lights ();
			if (EngineSound == null)
			if (ParamsKeeper.Engine.EngineBlock != null)
			if (ParamsKeeper.Engine.EngineBlock.GetComponent<CarPart> ().EngineSound != null)
				EngineSound = ParamsKeeper.Engine.EngineBlock.GetComponent<CarPart> ().EngineSound;



			if (ParamsKeeper.Engine.IsEngineWorking)
			{
				GearsShifting ();
				Controls ();
				AntirollBar ();
				Skidding ();
				if (!WCSubstepsApplyed) WCSubstepsApply ();

				if (EngineSound != null) {
					if (!EngineSound.isPlaying)
						EngineSound.Play ();
					EngineSound.pitch = Mathf.Lerp (0.5f, 2, CurrentRPM / MaxRpm);
				}

				if (ExhaustParticle!=null && !ExhaustParticle.isPlaying) ExhaustParticle.Play (true);

				//Air resistance
				if (WheelRPM>0) RB.AddRelativeForce (new Vector3 (0, 0, -AirResistanceCoefficient*ParamsKeeper.Speed*ParamsKeeper.Speed));
				if (WheelRPM<0) RB.AddRelativeForce (new Vector3 (0, 0, AirResistanceCoefficient*ParamsKeeper.Speed*ParamsKeeper.Speed));

			}
				
			if (!ParamsKeeper.Engine.IsEngineWorking) 
			{
				if (EngineSound!=null && EngineSound.isPlaying) EngineSound.Stop ();
				if (ExhaustParticle!=null && ExhaustParticle.isPlaying) ExhaustParticle.Stop ();

				WCSubstepsApplyed = false;

				isBraking = true;
				TorqToWheel = 0;
				CurrentGear = 1;
				CurrentRPM = 0;
				WheelRPM = 0;
				SteeringAngle = 0;
			}
				
			foreach (var item in ParamsKeeper.Wheels)
			{
				
				//Motor torque
				if (!isRedlined && item.motor)  item.WheelCollider.motorTorque=Mathf.Lerp(TorqToWheel*0.2f,TorqToWheel,CurrentRPM/MaxRpm);

				//Transfering wheelcollider rotation to wheelcollider rotation holder
				if (item.WCRotationHolder!=null) item.WCRotationHolder.transform.rotation = item.WCRotation;

				//Redline limit
				if (isRedlined && Mathf.Abs (item.WheelCollider.motorTorque) > 0) item.WheelCollider.motorTorque -= 50 * Mathf.Sign(WheelRPM);

				//Braking & handbraking
				if (isBraking) item.WheelCollider.brakeTorque = item.BrakingEfficiency;
				if (!isHandBraking && !isBraking)  item.WheelCollider.brakeTorque = 0;

				if (item.handbrake && isHandBraking) {
					item.WheelCollider.brakeTorque = item.BrakingEfficiency*10;
					TempFriction = item.WheelCollider.sidewaysFriction;
					TempFriction.stiffness = HandbrakeSidewaysFriction;
					item.WheelCollider.sidewaysFriction = TempFriction;
				}
				if (item.handbrake && !isHandBraking) {
					TempFriction = item.WheelCollider.sidewaysFriction;
					TempFriction.stiffness = item.DefaultSidewaysFriction;
					item.WheelCollider.sidewaysFriction = TempFriction;
				}

				//Steering
				if (item.steering)  item.WheelCollider.steerAngle = SteeringAngle;

		}


	}
	


}
}