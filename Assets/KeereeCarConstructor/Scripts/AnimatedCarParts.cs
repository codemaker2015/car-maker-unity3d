using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace KCC{
public class AnimatedCarParts : MonoBehaviour {

		//This script moves EVERYTHING in car: spinning engine parts, turning visual wheels, dynamic suspension, steering wheel spinning, etc.



	private ParametersKeeper ParamsKeeper;
		private Vector3 HubHeight,Temp;
		[HideInInspector]
		public List<CarPart> SpinningParts;
		private CarController CarControl;

		public bool 
		HubTravelUpDown=true,
		HubTurningDueToSteering=true,
		BrakeDiskRotation=true,
		AmrLookingAtHub=true,
		AbsorberCompression=true,
		BeamAxlePosition=true,
		DriveshaftLookAtHub=true,
		SpinningEngineParts=true,
		SteeringWheelSpinning=true;

	void Start () {
		ParamsKeeper = GetComponent<ParametersKeeper> ();
		CarControl = GetComponent<CarController> ();
	}
	

	void FixedUpdate () {

			foreach (var item in ParamsKeeper.Wheels){

			
				if (item.Grounded)
					ParamsKeeper.WheelsTouchedGround = true;

				if (item.Hub != null) {
					
			//Hub travel up and down
					if (HubTravelUpDown && ParamsKeeper.WheelsTouchedGround) {
						HubHeight = item.Hub.transform.localPosition;
						if (item.Hub.GetComponent<CarPart> ().rotationAxis == CarPart.RotationAxis.X) HubHeight.y = item.Hub.transform.parent.transform.InverseTransformPoint (item.WCPosition).x;
						if (item.Hub.GetComponent<CarPart> ().rotationAxis == CarPart.RotationAxis.Y) HubHeight.y = item.Hub.transform.parent.transform.InverseTransformPoint (item.WCPosition).y;
						if (item.Hub.GetComponent<CarPart> ().rotationAxis == CarPart.RotationAxis.Z) HubHeight.z = item.Hub.transform.parent.transform.InverseTransformPoint (item.WCPosition).z;// Dummy.localPosition.y;
						item.Hub.transform.localPosition = HubHeight;
					}

			//Hub turn due to steering
					if (item.steering && HubTurningDueToSteering) {
						Temp = item.Hub.transform.localEulerAngles;
						if (item.Hub.GetComponent<CarPart>().rotationAxis== CarPart.RotationAxis.X) Temp.x=item.DefRot.x + item.WheelCollider.steerAngle;
						if (item.Hub.GetComponent<CarPart>().rotationAxis== CarPart.RotationAxis.Y) Temp.y=item.DefRot.y + item.WheelCollider.steerAngle;
						if (item.Hub.GetComponent<CarPart>().rotationAxis== CarPart.RotationAxis.Z) Temp.z=item.DefRot.z + item.WheelCollider.steerAngle;
						item.Hub.transform.localEulerAngles = Temp;
					}

			//Arm look at hub
					if (item.ArmDummy!=null && AmrLookingAtHub)
						item.ArmDummy.transform.LookAt (item.Hub.transform);

				}
					
			//Brake disk rotation
				if (item.BrakeDisk != null && BrakeDiskRotation)
					//item.BrakeDisk.transform.eulerAngles = item.WCRotation.eulerAngles;
					item.BrakeDisk.transform.eulerAngles=item.WCRotationHolder.eulerAngles;

			
				
			//Absorber compression
				if (item.Absorber != null && AbsorberCompression) {
					Vector3 TempLocalScale = item.Absorber.transform.localScale;
					TempLocalScale.z = 1 - (item.WheelCollider.suspensionDistance*0.4f - item.Travel + 0.02f)*2;
					TempLocalScale.z = Mathf.Clamp (TempLocalScale.z, 0.8f, 1.2f);
					item.Absorber.transform.localScale = TempLocalScale;
				}

			

			//Beam axle position
				if (ParamsKeeper.WheelRL != null && ParamsKeeper.WheelRR != null && ParamsKeeper.WheelRR.Hub!=null  && ParamsKeeper.WheelRL.Hub!=null && ParamsKeeper.BeamAxleDummy!=null && BeamAxlePosition) {
					ParamsKeeper.BeamAxleDummy.transform.position = ParamsKeeper.WheelRR.Hub.transform.position;
					ParamsKeeper.BeamAxleDummy.transform.LookAt (ParamsKeeper.WheelRL.Hub.transform); // Сменить на Hub
				}
			

			//Driveshaft Look at hub
				if (DriveshaftLookAtHub) {
					if (ParamsKeeper.Engine.DriveshaftLDummy != null)
					if (item.BrakeDisk != null && item.wheelLocation == aWheel.WheelLocation.FL)
						ParamsKeeper.Engine.DriveshaftLDummy.transform.LookAt (item.BrakeDisk.transform);

					if (ParamsKeeper.Engine.DriveshaftRDummy != null)
					if (item.BrakeDisk != null && item.wheelLocation == aWheel.WheelLocation.FR)
						ParamsKeeper.Engine.DriveshaftRDummy.transform.LookAt (item.BrakeDisk.transform);

					if (ParamsKeeper.BeamAxle!=null && ParamsKeeper.Engine.DriveshaftRWDDummy!=null) ParamsKeeper.Engine.DriveshaftRWDDummy.transform.LookAt (ParamsKeeper.BeamAxle.transform);



				}
		}


			//Spinning engine parts
			if (ParamsKeeper.Engine.IsEngineWorking && SpinningEngineParts)
				foreach (var part in SpinningParts) {
					Vector3 RotationVector=new Vector3(0,0,0);
					if (part.rotationAxis == CarPart.RotationAxis.X) RotationVector = new Vector3 (100, 0, 0);
					if (part.rotationAxis == CarPart.RotationAxis.Y) RotationVector = new Vector3 (0, 100, 0);
					if (part.rotationAxis == CarPart.RotationAxis.Z) RotationVector = new Vector3 (0, 0, 100);
					part.transform.Rotate (RotationVector, Space.Self);
				}
			
			//Steering wheel spinning
			if (ParamsKeeper.Body.SteeringWheelDummy != null && SteeringWheelSpinning) {
				Vector3 Tmp = ParamsKeeper.Body.SteeringWheelDummy.transform.localEulerAngles;
				Tmp.z = ParamsKeeper.WheelFL.WheelCollider.steerAngle * 5;
				ParamsKeeper.Body.SteeringWheelDummy.transform.localEulerAngles = Tmp;
			}



				
	}
}

}