using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;

// Camera controller script

namespace KCC
{
	
	public class CamControl : MonoBehaviour
	{

		private float 
			DistanceCam1,
			DistanceCam,
			rotX,
			rotY,
			VelY,
			VelZ,
			Dist1,
			WantedRot,
			RotAngle;

		[HideInInspector]  public Transform target;
		private CarConstructorCore CarConstruct;
		private ParametersKeeper ParamsKeeper;
		public enum CamType {BodyCam,WheelFLCam,WheelFRCam,WheelRLCam,WheelRRCam,EngineCam,Follow,Driver};
		[HideInInspector]  public CamType camtype;

		public float
			DistanceFollow = 4,
			HeightFollow = 1.5f,
			MouseSpeed = 4,
			MouseScrollSpeed = 2,
			WheelViewCameraDistance = 1,
			BodyMinDistance = 3,
			BodyMaxDistance = 4,
			EngineCamDistance = 1.2f;
			

		void Start ()
		{
			if (GetComponent<CarConstructorCore> ()) 
				CarConstruct = GetComponent<CarConstructorCore> ();
			rotX = -150;
			rotY = 10;
		}



		void FixedUpdate ()
		{
			if (CarConstruct == null) return;
			if (CarConstruct.Car!=null && CarConstruct.Car.GetComponent<ParametersKeeper> ()) ParamsKeeper = CarConstruct.Car.GetComponent<ParametersKeeper> ();

			if (rotX > 360)  rotX -= 360;
			if (rotX < 0) rotX += 360;
			rotY = Mathf.Clamp (rotY, 5, 70);

			if (Input.GetButton ("Fire2")) {
				rotX += Input.GetAxis ("Mouse X") * MouseSpeed;
				rotY -= Input.GetAxis ("Mouse Y") * MouseSpeed;
			}


			switch (camtype) {
			case CamType.BodyCam:
				DistanceCam -= Input.GetAxis ("Mouse ScrollWheel") * MouseScrollSpeed;
				DistanceCam = Mathf.Clamp (DistanceCam, BodyMinDistance, BodyMaxDistance);
				if (CarConstruct.Car != null) target = CarConstruct.Car.transform;
				else target = CarConstruct.GaragePoint;
					DoFreeCam ();
				break;
				case CamType.EngineCam:
					DistanceCam = EngineCamDistance;
					target = ParamsKeeper.Engine.EngineDummy;
					DoFreeCam ();
				break;
				case CamType.WheelFLCam:
					DistanceCam = WheelViewCameraDistance;
					target = ParamsKeeper.WheelFL.HubDummy.transform;
					DoFreeCam ();
					break;
				case CamType.WheelFRCam:
					DistanceCam = WheelViewCameraDistance;
					target = ParamsKeeper.WheelFR.HubDummy.transform;
					DoFreeCam ();
				break;
				case CamType.WheelRLCam:
					DistanceCam = WheelViewCameraDistance;
					target = ParamsKeeper.WheelRL.HubDummy.transform;
					DoFreeCam ();
				break;
				case CamType.WheelRRCam:
					DistanceCam = WheelViewCameraDistance;
					target = ParamsKeeper.WheelRR.HubDummy.transform;
					DoFreeCam ();
				break;
				case CamType.Follow:
					transform.parent = null;
					WantedRot = target.eulerAngles.y;
					RotAngle = Mathf.SmoothDampAngle (RotAngle, WantedRot, ref VelY, 0.3f);
					Dist1 = Mathf.SmoothDampAngle (Dist1, DistanceFollow + (ParamsKeeper.Speed * 0.01f), ref VelZ, 0); 
					transform.LookAt (target.position);
					transform.position = target.position + Quaternion.Euler (0, RotAngle, 0) * new Vector3 (0, HeightFollow, -Dist1);
				break;
				case CamType.Driver:
					transform.position = ParamsKeeper.CameraDriverPosition.transform.position;
					transform.rotation = ParamsKeeper.CameraDriverPosition.transform.rotation;
					transform.parent = CarConstruct.Car.transform;
				break;
			}
		}


		void DoFreeCam(){
			DistanceCam1 = Mathf.Lerp (DistanceCam1, DistanceCam, 7 * Time.deltaTime);
			transform.rotation = Quaternion.Slerp (transform.rotation, Quaternion.Euler (rotY, rotX, 0), Time.deltaTime * 3);
			transform.position = target.position + transform.rotation * new Vector3 (0.0f, 0.0f, -DistanceCam1);
			transform.parent = null;
		}


	}

}
