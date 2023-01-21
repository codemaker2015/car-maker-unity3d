using UnityEngine;
using System.Collections;
namespace KCC{

	//This script keeps all car's parameters.
[System.Serializable]
public class aWheel {
	public enum WheelLocation{FL,FR,RL,RR};
	[Header("Assign these vars")]
	public WheelLocation wheelLocation;
	public WheelCollider WheelCollider;
	public bool steering;
	public bool handbrake;
	public GameObject HubDummy;
	public GameObject ArmDummy;
	public Transform WCRotationHolder;	
	[Header("Read only")]
	public bool motor;
	public ParticleSystem SkidParticle;
	public Quaternion ArmDummyDefRot;
	public GameObject Hub;
	public GameObject BrakeDisk;
	public GameObject Absorber;
	public GameObject Wider;
	public GameObject Arm;
	public GameObject Wheel;
	public float BrakingEfficiency;
	public bool IsDefRotSet;
	public Vector3 DefRot;
	public Quaternion WCRotation;
	public Vector3 WCPosition;
	public float WiderOffset;
	public float Travel;
	public bool Grounded;
	public WheelHit wheelhit;
	public float DefaultSidewaysFriction;
}
	[System.Serializable]
	public class Vinyls{
		public string Name;
		public Texture texture;
	}

	[System.Serializable]
	public class engine{
		[Header("Assign these vars")]
		public GameObject DriveshaftLDummy;
		public GameObject DriveshaftRDummy;
		public GameObject DriveshaftRWDDummy;
		public Transform EngineDummy;
		[Header("All these are Read only")]
		public bool IsEngineWorking;
		public float MotorPower;
		public GameObject EngineBlock;
		public GameObject GearBox;
		public GameObject Camshaft;
		public GameObject Crankshaft;
		public GameObject Carburetor;
		public GameObject CylinderHead;
		public GameObject CylinderHeadCover;
		public GameObject IntakeManifold;
		public GameObject OilPan;
		public GameObject Pistons;
		public GameObject IgnitionSystem;
		public GameObject TimingBelt;
		public GameObject CamshaftBearingBridge;
		public GameObject DriveshaftL;
		public GameObject DriveshaftR;
		public GameObject DriveshftRWD;
		public Vector3 DriveshaftLDummyDefRot;
		public Vector3 DriveshaftRDummyDefRot;
		public Vector3 DriveshaftRWDDummyDefRot;
	}

	[System.Serializable]
	public class body{
		[Header ("Assign these vars")]
		public Transform SteeringWheelDummy;
		[Header("Read only")]
		public GameObject CarBody;
		public GameObject SteeringWheel;
		public GameObject BrakeLightLeft;
		public GameObject BrakeLightRight;
		public GameObject DriverSeat;
	}

public class ParametersKeeper : MonoBehaviour {

	public aWheel[] Wheels;
	public engine Engine;
	public body Body;
	public Vinyls[] vinyls;
	[Header("Assign these vars")]
	public string CarName;
	public GameObject BeamAxleDummy;
	public Transform CameraDriverPosition;
	

	[Header("Other read only params")]
	public GameObject BeamAxle;
	public float Speed;
	[HideInInspector]
	public aWheel 
		WheelFL,
		WheelFR,
		WheelRL,
		WheelRR;
	[HideInInspector]public bool WheelsTouchedGround;

		void Awake(){
			foreach (var wheel in Wheels) {
				if (wheel.wheelLocation == aWheel.WheelLocation.FL) WheelFL = wheel;
				if (wheel.wheelLocation == aWheel.WheelLocation.FR) WheelFR = wheel;
				if (wheel.wheelLocation == aWheel.WheelLocation.RL) WheelRL = wheel;
				if (wheel.wheelLocation == aWheel.WheelLocation.RR) WheelRR = wheel;
				if (wheel.ArmDummy != null) wheel.ArmDummyDefRot = wheel.ArmDummy.transform.localRotation;

				//Set default rotation of hubs
				if (!wheel.IsDefRotSet && wheel.Hub!=null) {
					wheel.DefRot =wheel.Hub.transform.localEulerAngles;
					wheel.IsDefRotSet = true;

				}
			}


			if (Engine.DriveshaftRWDDummy != null) Engine.DriveshaftRWDDummyDefRot = Engine.DriveshaftRWDDummy.transform.localEulerAngles;
			if (Engine.DriveshaftRDummy != null) Engine.DriveshaftRDummyDefRot = Engine.DriveshaftRDummy.transform.localEulerAngles;
			if (Engine.DriveshaftLDummy != null) Engine.DriveshaftLDummyDefRot = Engine.DriveshaftLDummy.transform.localEulerAngles;
		}



	void FixedUpdate () {
			if (GetComponent<Rigidbody> ()) Speed = GetComponent<Rigidbody> ().velocity.magnitude * 3.6f;
			else gameObject.AddComponent<Rigidbody> ();

		foreach (var item in Wheels) {
				if (item.WheelCollider != null) {
					item.WheelCollider.GetWorldPose (out item.WCPosition, out item.WCRotation);
					item.Grounded = item.WheelCollider.GetGroundHit (out item.wheelhit);
					if (item.Hub!=null && item.Hub.GetComponent<CarPart>().rotationAxis==CarPart.RotationAxis.X) item.Travel = (-item.WheelCollider.transform.InverseTransformPoint (item.wheelhit.point).x - item.WheelCollider.radius);
					if (item.Hub!=null && item.Hub.GetComponent<CarPart>().rotationAxis==CarPart.RotationAxis.Y) item.Travel = (-item.WheelCollider.transform.InverseTransformPoint (item.wheelhit.point).y - item.WheelCollider.radius);
					if (item.Hub!=null && item.Hub.GetComponent<CarPart>().rotationAxis==CarPart.RotationAxis.Z) item.Travel = (-item.WheelCollider.transform.InverseTransformPoint (item.wheelhit.point).z - item.WheelCollider.radius);
					if (item.Hub == null) item.Travel = 0;
				}
			}
				
	}

}

}