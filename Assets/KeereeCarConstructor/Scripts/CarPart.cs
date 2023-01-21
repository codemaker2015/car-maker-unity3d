#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.Collections;

// Script that defines every car part's parameters

namespace KCC
{
	public class CarPart : MonoBehaviour
	{
		public int 
			SpringHeight,
			AbsorberDamping,
			SpringRate,
			BrakingEfficiency,
			mass,
			AddingPower,
			WiderOffset,
			Price;
		public bool 
			unMountable,
			havingmass,
			openable,
			paintable,
			spinning,
			AddPower,
			IsOpen,
			Dependent,
			DependsOnAll;
		public GameObject[] DependsOn;
		public float[] GearRatios;
		public float 
			TopGear,
			WheelColliderOffset;
		public Vector3 
			defRot,
			RelativePosition,
			DefLocalPos;
		public string 
			Description,
			showedname,
			EngineType,
			SuspensionType,
			BodyType;
		public Quaternion defWorldRot;

		public enum PartType
		{
			BodyPart,
			EnginePart,
			SuspensionPart,
		}

		public enum BodyPartType
		{
			Other,
			Hood,
			TrunkDoor,
			BumperFront,
			BumperRear,
			Grill,
			HeadlightLeft,
			HeadlightRight,
			BrakelightLeft,
			BrakelightRight,
			MirrorLeft,
			MirrorRight,
			SteeringWheel,
			DriverSeat,
			Muffler,
			CarBody,
			RoofRack,
			NumberOfBodyPartTypes
		}

		public enum RotationAxis
		{
			X,
			Y,
			Z
		}

		public enum SuspensionPartType
		{
			Other,
			BrakeDisk,
			Wider,
			Absorber,
			Wheel,
			Hub,
			Arm,
			BeamAxle,
			Caliper,
			NumberOfSuspensionPartTypes
		}

		public enum EnginePartType
		{
			Other,
			EngineBlock,
			CylinderHead,
			CylinderHeadCover,
			Camshaft,
			AirFilter,
			Pistons,
			Crankshaft,
			Carburetor,
			IntakeManifold,
			TimingBelt,
			IgnitionSystem,
			CamshaftBearingBridge,
			OilPan,
			GearBox,
			Compressor,
			DriveshaftL,
			DriveshaftR,
			DriveshaftRWD,
			NumberOfEnginePartTypes
		}
			

		public enum Location
		{
			FrontLeft,
			FrontRight,
			RearLeft,
			RearRight
		}

		public enum OpenDirection
		{
			Positive,
			Negative
		}

		public enum Drive
		{
			FWD,
			RWD,
			AWD
		}

		public PartType parttype;
		public BodyPartType bodyparttype;
		public SuspensionPartType suspensionparttype;
		public EnginePartType engineparttype;
		public Location location;
		public OpenDirection opendirection;
		public Drive DriveType;
		public RotationAxis rotationAxis;
		public AudioSource EngineSound;
	}
	


	#if UNITY_EDITOR
	[CustomEditor (typeof(CarPart)), CanEditMultipleObjects]
	public class PartTypeEditor : Editor
	{
		public SerializedProperty 
			massProp,
			unMountableProp,
			NecessarityProp,
			havingmassProp,
			PriceProp,
			DescriptionProp,
			DependentProp,
			DependsOnAllProp,
			DependsOnProp,
			AddPowerProp,
			AddingPowerProp,
			openableProp,
			paintableProp,
			spinningProp,
			parttypeProp,
			locationProp,
			opendirectionProp,
			drivetypeProp,
			carpaintgroupProp,
			suspensionparttypeProp,
			bodyparttypeProp,
			engineparttypeProp,
			BrakingEfficiencyProp,
			SpringRateProp,
			AbsorberDampingProp,
			SpringHeightProp,
			showednameProp,
			WheelColliderOffsetProp,
			rotationAxisProp,
			EngineTypeProp,
			SuspensionTypeProp,
			BodyTypeProp,
			WiderOffsetProp,
			GearRatiosProp,
			TopGearProp,
			EngineSoundProp;

		void OnEnable ()
		{
			massProp = serializedObject.FindProperty ("mass");
			unMountableProp = serializedObject.FindProperty ("unMountable");
			havingmassProp = serializedObject.FindProperty ("havingmass");
			PriceProp = serializedObject.FindProperty ("Price");
			DescriptionProp = serializedObject.FindProperty ("Description");
			DependentProp = serializedObject.FindProperty ("Dependent");
			DependsOnAllProp = serializedObject.FindProperty ("DependsOnAll");
			DependsOnProp = serializedObject.FindProperty ("DependsOn");
			AddPowerProp = serializedObject.FindProperty ("AddPower");
			AddingPowerProp = serializedObject.FindProperty ("AddingPower");
			openableProp = serializedObject.FindProperty ("openable");
			paintableProp = serializedObject.FindProperty ("paintable");
			spinningProp = serializedObject.FindProperty ("spinning");
			parttypeProp = serializedObject.FindProperty ("parttype");
			locationProp = serializedObject.FindProperty ("location");
			opendirectionProp = serializedObject.FindProperty ("opendirection");
			drivetypeProp = serializedObject.FindProperty ("DriveType");
			suspensionparttypeProp = serializedObject.FindProperty ("suspensionparttype");
			bodyparttypeProp = serializedObject.FindProperty ("bodyparttype");
			engineparttypeProp = serializedObject.FindProperty ("engineparttype");
			BrakingEfficiencyProp = serializedObject.FindProperty ("BrakingEfficiency");
			SpringRateProp = serializedObject.FindProperty ("SpringRate");
			rotationAxisProp = serializedObject.FindProperty ("rotationAxis");
			AbsorberDampingProp = serializedObject.FindProperty ("AbsorberDamping");
			SpringHeightProp = serializedObject.FindProperty ("SpringHeight");
			showednameProp = serializedObject.FindProperty ("showedname");
			WheelColliderOffsetProp = serializedObject.FindProperty ("WheelColliderOffset");
			EngineTypeProp = serializedObject.FindProperty ("EngineType");
			SuspensionTypeProp = serializedObject.FindProperty ("SuspensionType");
			BodyTypeProp = serializedObject.FindProperty ("BodyType");
			WiderOffsetProp = serializedObject.FindProperty ("WiderOffset");
			GearRatiosProp = serializedObject.FindProperty ("GearRatios");
			TopGearProp = serializedObject.FindProperty ("TopGear");
			EngineSoundProp = serializedObject.FindProperty ("EngineSound");

		}

	
		public override void OnInspectorGUI ()
		{
			serializedObject.Update ();
			EditorGUILayout.HelpBox ("Common settings", MessageType.None);
			EditorGUILayout.PropertyField (showednameProp, new GUIContent ("Name showed in game:")); 
			EditorGUILayout.PropertyField (unMountableProp, new GUIContent ("Unmountableness"));	
			EditorGUILayout.PropertyField (havingmassProp, new GUIContent ("Having mass"));
			EditorGUILayout.PropertyField (paintableProp, new GUIContent ("Paintable"));
			if (havingmassProp.boolValue)
				EditorGUILayout.PropertyField (massProp, new GUIContent ("Mass:"));
			EditorGUILayout.PropertyField (PriceProp, new GUIContent ("Price:"));
			EditorGUILayout.PropertyField (DescriptionProp, new GUIContent ("Description:"));
			EditorGUILayout.PropertyField(DependentProp,new GUIContent("Dependent"));
			if (DependentProp.boolValue){
			EditorGUILayout.PropertyField (DependsOnProp, true);
			EditorGUILayout.PropertyField(DependsOnAllProp,new GUIContent("Depends on all"));
				}
			EditorGUILayout.HelpBox ("Special settings", MessageType.None);
			EditorGUILayout.PropertyField (parttypeProp);
			CarPart.PartType PT = (CarPart.PartType)parttypeProp.enumValueIndex;
		
			switch (PT) 
			{
	
			case CarPart.PartType.BodyPart:
				EditorGUILayout.PropertyField (BodyTypeProp, new GUIContent ("Body type: "));
				EditorGUILayout.PropertyField (bodyparttypeProp);
				EditorGUILayout.PropertyField (openableProp, new GUIContent ("Openable"));
				if (openableProp.boolValue) {
					EditorGUILayout.PropertyField (rotationAxisProp, new GUIContent ("Open axle: "));
					EditorGUILayout.PropertyField (opendirectionProp, new GUIContent ("opendirection"));
				}
				break;
			case CarPart.PartType.EnginePart:
				EditorGUILayout.PropertyField (engineparttypeProp);
				EditorGUILayout.PropertyField (EngineTypeProp);
				EditorGUILayout.PropertyField (spinningProp);
				EditorGUILayout.PropertyField (AddPowerProp);
				if (AddPowerProp.boolValue)
					EditorGUILayout.PropertyField (AddingPowerProp);
				if (spinningProp.boolValue)
					EditorGUILayout.PropertyField (rotationAxisProp, new GUIContent ("Spinning axle: "));
				if (engineparttypeProp.intValue == (int)CarPart.EnginePartType.GearBox) {
					EditorGUILayout.PropertyField (GearRatiosProp, true);
					EditorGUILayout.PropertyField (TopGearProp, new GUIContent ("Top Gear: "));
				}
				if (engineparttypeProp.intValue == (int)CarPart.EnginePartType.EngineBlock) {
					EditorGUILayout.PropertyField (EngineSoundProp);
					EditorGUILayout.PropertyField (drivetypeProp, new GUIContent ("Drive type: "));
				}
				break;
			case CarPart.PartType.SuspensionPart:
				EditorGUILayout.PropertyField(SuspensionTypeProp, new GUIContent("Suspension type: "));
				EditorGUILayout.PropertyField (suspensionparttypeProp, new GUIContent ("Suspension part type:"));
				EditorGUILayout.PropertyField (locationProp, new GUIContent ("Location:"));
				if (suspensionparttypeProp.intValue==(int) CarPart.SuspensionPartType.Caliper)
					EditorGUILayout.IntSlider (BrakingEfficiencyProp, 100, 1000, new GUIContent ("Braking efficiency:"));
				if (suspensionparttypeProp.intValue==(int)CarPart.SuspensionPartType.Hub)
					EditorGUILayout.PropertyField(rotationAxisProp,new GUIContent("Hub turning axis: "));
				if (suspensionparttypeProp.intValue==(int) CarPart.SuspensionPartType.BrakeDisk)
					EditorGUILayout.IntSlider (BrakingEfficiencyProp, 100, 1000, new GUIContent ("Braking efficiency:"));
				if (suspensionparttypeProp.intValue==(int) CarPart.SuspensionPartType.Wider)
					EditorGUILayout.IntSlider (WiderOffsetProp, -20, 20, new GUIContent ("Wider offset:"));
				if (suspensionparttypeProp.intValue == (int)CarPart.SuspensionPartType.Absorber) {
					EditorGUILayout.IntSlider (SpringHeightProp, 0, 50, new GUIContent ("Spring height:"));
					EditorGUILayout.IntSlider (AbsorberDampingProp, 100, 10000, new GUIContent ("Absorber Damping:"));
					EditorGUILayout.IntSlider (SpringRateProp, 10000, 35000, new GUIContent ("Spring rate:"));
				}
				if (suspensionparttypeProp.intValue == (int)CarPart.SuspensionPartType.Wheel)
					EditorGUILayout.PropertyField (WheelColliderOffsetProp, new GUIContent ("Visual wheel offset:"));
				break;
			

			}



			serializedObject.ApplyModifiedProperties ();

		}

	}
	#endif
}