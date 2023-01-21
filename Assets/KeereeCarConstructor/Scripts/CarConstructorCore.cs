using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Text;

// Main car construction script.

namespace KCC
{
	public class CarConstructorCore : MonoBehaviour
	{
		public GameObject 
			Car,				//Selected Car GameObject. Assigned from OnGUI() function, while ChooseCar is true.
			ShopGO,				//GameObject - parent of all part for sale having CarPart script.
			GarageGO,			//GameObject - parent of all bought parts in garage.
			CarsContainer;		//GameObject - parent of all cars.

		public Transform 
			GaragePoint,		//Center position of garage. In ConstructionModeInitialize() car is put at this position.
			RaceTrackPoint,		//Position where car will be put in RaceModeInitialize().
			PreviewPartDummy;	//Position in front of preview camera, where preview part will be put. Used in OnGUI().

		public Camera PreviewCamera;				//Preview camera. Make the background - solid color, and CullingMask layer - previewPart layer (Create it if missing).
		public enum GameMode { Garage, Driving} ;	//Game mode

		private ParametersKeeper ParamsKeeper;
		private AnimatedCarParts AnimCarParts;
		private aWheel tempWheel;					
		private RaycastHit hit;							//Used in Raycasting() for calculating which part of car is under mouse position.
		private Vector2 scrollPosition = Vector2.zero;	//Used in OnGUI() for parts list.
		private CamControl CameraController;			
		[HideInInspector] public  GameMode gameMode;
		[HideInInspector] public Rigidbody car_rigidbody;

		private CarPart[] carparts;										//Parts containing CarPart script on selected Car.
		private CarPart[] PartsInShop;									//Children of ShopGO containing CarPart script.
		private List<CarPart> InactiveCarParts = new List<CarPart> ();	//Parts in garage that are inactive (unmounted).
		private List<CarPart> EngineBlocks = new List<CarPart> ();		//Engine blocks under ShopGO.

		private Vector3
			RotationVector,			//Rotation direction for openable parts depending on CarPart.rotationAxis and CarPart.opendirection. Used in OpenClosePart();
			PreviewPartDummyDefPos;	//Default position of preview part dummy.

		private string[] PartGroupNames = new string[]{ "Body", "Engine", "Suspension" };
		private String 
			PartTypeToBeMounted,	//In checkEngine(), checkBody() and checkSuspension() part the Car misses is written in this variable.
			EngineSelected,			//Used in OnGUI() for showing parts of only selected engine.
			TextToShow;				//Text for being shown while ShowTip() function.

		private float 
			timeGone,				//Used in ShowTip() as timer.
			timeToShow,				//Used in ShowTip(). Limits the tips show time.
			Timer;					//Used in OpenClosePart for limiting doors spinning time.

		private int 
			Money = 8500,
			PartGroupIndex,			//Used in OnGUI(). Index of selected part group : Body, Suspension, Engine.
			car_mass,				//Car mass calculated in RecalculateMass().
			ClickAction, 			//Action on click on car part. 0-Mount/unmount,1-tune/open/close door, 2-paint.
			FontSize,				//Used in ShowTip().
			boxwidth,				//Used in ShowTip().
			ColorSelected,			//Selected color for paint. Changes in OnGUI().
			PartIndex;				//Index of part selected in parts list in show. Used for creating preview part.

		[HideInInspector] 
		public bool 
			RaceModeInitialized,
			ConstructionModeInitialized,
			BodyShow,					//Used by buttons in the upper left bar, OnGUI().
			SuspensionShow,				//Used by buttons in the upper left bar, OnGUI().
			EngineShow,					//Used by buttons in the upper left bar, OnGUI().
			ShowTipText,				//Text to be shown in ShowTip() function.
			isEngineOk,					//Returns true if in checkEngine() every necessary engine part is mounted.
			isSuspensionOk,				//Returns true if in checkSuspension() every necessary suspension part is mounted.
			isBodyOk,					//Returns true if in checkBody() every necessary body part is mounted.
			RotatePart,					//Used in OpenClosePart(). True, when door/hood/trunk is opening.
			Shop,						//OnGUI shows shop menu when true.
			AreNecessaryVarsAssigned,	
			ChooseCar;					//OnGUI shows car choose menu when true.

		[HideInInspector]
		public GameObject 
			objectHit,				//GameObject raycast ray meets.
			PartToRotate,			//Used in OpenClosePart(). This part is opening.
			PreviewPart,			//Part shown in menu shop as preview.
			RootGO,
			Body, 					//Body GameObject of selected Car.
			Suspension,				//Suspension GameObject of selected Car.
			Engine,					//Engine GameObject of selected Car.
			Parent;	

		[HideInInspector] public bool[] bodyparttypeindex; 		//Boolean variables that define if a part of any type is mounted. Works with enums BodyPartType/SuspensionPartType/EnginePartType from CarPart script.
		[HideInInspector] public bool[,] suspensiontypeindex; 	//For example: BumperFront in BodyPartType is at 3th position. If front bumper (tuned of stock) is mounted, then
		[HideInInspector] public bool[] engineparttypeindex; 	// bodyparttypeindex[3] is true.

		[Header ("Sounds")]
		public AudioSource unMountSound;
		public AudioSource MountSound;
		public AudioSource ErrorSound;
		public AudioSource DoorCloseSound;
		public AudioSource DoorOpenSound;
		public AudioSource SkidSound;
		public AudioSource Buy;
		public AudioSource Spray;

		[Header ("Icons")]
		public Texture BodyIcon;
		public Texture SuspIcon;
		public Texture EngineIcon;
		public Texture MountIcon;
		public Texture TuneIcon;
		public Texture PaintIcon;
		public Texture2D MountCursor;
		public Texture2D TuneCursor;
		public Texture2D PaintCursor;

		[Header ("Paint")]
		public Color[] BodyColors;
		public Texture[] BodyColorsIcons;
		public Material 
			CarPaintMaterial,
			RimPaintMaterial;

		[Header ("GUI")]
		public GUIStyle BoxStyleForParts;
		public GUIStyle LabelStyleForDescription;
		public GUIStyle BoxStyleForBackground;
		private GUIStyle 
			CustomButton,
			CustomBoxStyle1,
			CustomLabelStyle;


		void Start ()
		{
			CheckMainVariables ();
			if (!AreNecessaryVarsAssigned)
				return;

			//Creating array of all parts in shop
			PartsInShop = ShopGO.GetComponentsInChildren <CarPart> (true);

			//Setting default position of all parts in shop
			foreach (var item in PartsInShop) {
				GetCarPart (item.gameObject).defRot = item.transform.localEulerAngles;
				GetCarPart (item.gameObject).defWorldRot = item.transform.rotation;
				GetCarPart (item.gameObject).DefLocalPos = item.transform.localPosition;
				if (item.engineparttype == CarPart.EnginePartType.EngineBlock) EngineBlocks.Add (item);
				item.gameObject.SetActive (false);
			}

			//Setting default position of all parts on cars
			foreach (var item in CarsContainer.GetComponentsInChildren<CarPart>(true)) {
				item.defRot = item.transform.localEulerAngles;
				item.defWorldRot = item.transform.rotation;
				item.DefLocalPos = item.transform.localPosition;
			}

			BodyShow = true;
			SuspensionShow = true;
			EngineShow = true;

			RaceModeInitialized = false;
			ConstructionModeInitialized = false;

			if (GetComponent<CamControl> ()) CameraController = GetComponent<CamControl> ();

			PreviewPartDummyDefPos = PreviewPartDummy.transform.position;

			bodyparttypeindex = new bool[(int)CarPart.BodyPartType.NumberOfBodyPartTypes];
			suspensiontypeindex = new bool[(int)CarPart.SuspensionPartType.NumberOfSuspensionPartTypes, 4];
			engineparttypeindex = new bool[(int)CarPart.EnginePartType.NumberOfEnginePartTypes];

			DeactiveateCars ();

			if (Car != null) ConstructionModeInitialize ();
		}


		void CheckMainVariables ()
		{
			AreNecessaryVarsAssigned = true;

			if (PreviewCamera == null) {
				Debug.LogError ("Assign preview camera to CarConstructionCore");
				AreNecessaryVarsAssigned = false;
			}
			if (PreviewPartDummy == null) {
				Debug.LogError ("Assign preview part dummy to CarConstructionCore");
				AreNecessaryVarsAssigned = false;
			}
			if (CarsContainer == null) {
				Debug.LogError ("Assign cars container gameobject to CarConstructionCore");
				AreNecessaryVarsAssigned = false;
			}
			if (ShopGO == null) {
				Debug.LogError ("Assign shop gameobject to CarConstructionCore");
				AreNecessaryVarsAssigned = false;
			}
			if (GarageGO == null) {
				Debug.LogError ("Assign garage gameobject to CarConstructionCore");
				AreNecessaryVarsAssigned = false;
			}

			if (!AreNecessaryVarsAssigned)
				Debug.LogError ("The CarConstructionCore script is not configured correctly and doesn't work");
		}


		//Putting all parts at their default positions, defined in Start().
		void PutPartsAtTheirPlaces ()
		{
			carparts = Car.GetComponentsInChildren<CarPart> (true);

			foreach (var item in ParamsKeeper.Wheels) {
				if (item.Wider != null) item.WiderOffset = item.Wider.gameObject.GetComponent<CarPart> ().WiderOffset;
				else item.WiderOffset = 0;
			}
				
			foreach (var item in carparts) {
				tempWheel = null;
				foreach (var wheel in ParamsKeeper.Wheels) {
					if (wheel.wheelLocation == aWheel.WheelLocation.FL && item.location == CarPart.Location.FrontLeft) tempWheel = wheel;
					if (wheel.wheelLocation == aWheel.WheelLocation.FR && item.location == CarPart.Location.FrontRight) tempWheel = wheel;
					if (wheel.wheelLocation == aWheel.WheelLocation.RL && item.location == CarPart.Location.RearLeft) tempWheel = wheel;
					if (wheel.wheelLocation == aWheel.WheelLocation.RR && item.location == CarPart.Location.RearRight) tempWheel = wheel;
				}
					
				if (item.parttype == CarPart.PartType.SuspensionPart) { 
					
					if (item.suspensionparttype == CarPart.SuspensionPartType.Hub) {
						item.transform.position = tempWheel.HubDummy.transform.position;
						item.transform.rotation = tempWheel.HubDummy.transform.rotation;
						item.transform.parent = Suspension.transform;
						tempWheel.Hub = item.gameObject;
					}

					if (item.suspensionparttype == CarPart.SuspensionPartType.Absorber) {
						item.transform.parent = tempWheel.Hub.transform;
						item.transform.localPosition = item.DefLocalPos;
						item.transform.localEulerAngles = item.defRot;
					}

					if (item.suspensionparttype == CarPart.SuspensionPartType.Caliper) {
						item.transform.parent = tempWheel.Hub.transform;
						item.transform.localPosition = item.DefLocalPos;
						item.transform.localEulerAngles = item.defRot;
					}

					if (item.suspensionparttype == CarPart.SuspensionPartType.BrakeDisk) {
						item.transform.parent = tempWheel.Hub.transform;
						item.transform.localPosition = item.DefLocalPos;
						item.transform.localEulerAngles = item.defRot;
					}
						
					if (item.suspensionparttype == CarPart.SuspensionPartType.Wider) {
						item.transform.parent = tempWheel.BrakeDisk.transform;
						item.transform.localPosition = item.DefLocalPos;
						item.transform.localEulerAngles = item.defRot;
					}

					if (item.suspensionparttype == CarPart.SuspensionPartType.BeamAxle) {
						item.transform.parent = ParamsKeeper.BeamAxleDummy.transform;
						item.transform.localPosition = item.DefLocalPos;
						item.transform.localEulerAngles = item.defRot;
					}

					if (item.suspensionparttype == CarPart.SuspensionPartType.Wheel) {
						if (tempWheel.BrakeDisk != null) item.transform.parent = tempWheel.BrakeDisk.transform;
						if (tempWheel.BrakeDisk != null) item.transform.position = tempWheel.BrakeDisk.transform.position;
						Vector3 Tmp = item.transform.localPosition;
						Tmp.x = item.WheelColliderOffset + tempWheel.WiderOffset / 100;
						item.transform.localPosition = Tmp;
						if (tempWheel.BrakeDisk != null) item.transform.rotation = tempWheel.BrakeDisk.transform.rotation;

					}

					if (item.suspensionparttype == CarPart.SuspensionPartType.Arm) {
						item.transform.parent = tempWheel.Hub.transform;
						item.transform.localPosition = item.DefLocalPos;
						item.transform.localEulerAngles = item.defRot;

						Vector3 Tmp1 = item.transform.position;
						Tmp1.y = tempWheel.ArmDummy.transform.position.y;
						item.transform.position = Tmp1;
						tempWheel.ArmDummy.transform.localRotation = tempWheel.ArmDummyDefRot;
						item.transform.parent = tempWheel.ArmDummy.transform;
					}
						
				}

				if (item.parttype == CarPart.PartType.BodyPart) {
					if (item.bodyparttype == CarPart.BodyPartType.SteeringWheel && ParamsKeeper.Body.SteeringWheelDummy != null) {
						item.transform.parent = ParamsKeeper.Body.SteeringWheelDummy.transform;
						item.transform.localPosition = item.DefLocalPos;
						item.transform.localEulerAngles = item.defRot;
					}

				}


				if (item.parttype == CarPart.PartType.EnginePart) {
					if (item.engineparttype == CarPart.EnginePartType.DriveshaftL && ParamsKeeper.Engine.DriveshaftLDummy != null) {
						item.transform.parent = ParamsKeeper.Engine.EngineBlock.transform;
						item.transform.localPosition = item.DefLocalPos;
						item.transform.localEulerAngles = item.defRot;
						ParamsKeeper.Engine.DriveshaftLDummy.transform.localEulerAngles = ParamsKeeper.Engine.DriveshaftLDummyDefRot;
						ParamsKeeper.Engine.DriveshaftLDummy.transform.position = item.transform.position;
						item.transform.parent = ParamsKeeper.Engine.DriveshaftLDummy.transform;
					}
					if (item.engineparttype == CarPart.EnginePartType.DriveshaftR && ParamsKeeper.Engine.DriveshaftRDummy != null) {
						item.transform.parent = ParamsKeeper.Engine.EngineBlock.transform;
						item.transform.localPosition = item.DefLocalPos;
						item.transform.localEulerAngles = item.defRot;
						ParamsKeeper.Engine.DriveshaftRDummy.transform.localEulerAngles = ParamsKeeper.Engine.DriveshaftRDummyDefRot;
						ParamsKeeper.Engine.DriveshaftRDummy.transform.position = item.transform.position;
						item.transform.parent = ParamsKeeper.Engine.DriveshaftRDummy.transform;
					}
					if (item.engineparttype == CarPart.EnginePartType.DriveshaftRWD && ParamsKeeper.Engine.DriveshaftRWDDummy != null) {
						item.transform.parent = ParamsKeeper.Engine.EngineBlock.transform;
						item.transform.localPosition = item.DefLocalPos;
						item.transform.localEulerAngles = item.defRot;
						ParamsKeeper.Engine.DriveshaftRWDDummy.transform.localEulerAngles = ParamsKeeper.Engine.DriveshaftRWDDummyDefRot;
						ParamsKeeper.Engine.DriveshaftRWDDummy.transform.position = item.transform.position;
						item.transform.parent = ParamsKeeper.Engine.DriveshaftRWDDummy.transform;
					}
					if (item.engineparttype == CarPart.EnginePartType.EngineBlock && ParamsKeeper.Engine.EngineDummy != null) {
						item.transform.parent = Engine.transform;
						item.transform.position = ParamsKeeper.Engine.EngineDummy.transform.position;
						item.transform.rotation = ParamsKeeper.Engine.EngineDummy.transform.rotation;
					}
				}

			}
		}


		void MoveInactivePartsToGarage ()
		{
			foreach (var item in carparts)
				if (!item.gameObject.activeSelf)
					item.transform.parent = GarageGO.transform;
		}



		void RecalculateMass ()
		{
			car_mass = 0;
			carparts = Car.GetComponentsInChildren<CarPart> ();
			for (int i = 0; i < carparts.Length; i++)
				if (carparts [i].havingmass && carparts [i].gameObject.activeSelf)
					car_mass = car_mass + carparts [i].mass;
			car_rigidbody.mass = car_mass;
		}


		public CarPart GetCarPart (GameObject GO)
		{
			CarPart carpart;
			carpart = GO.gameObject.GetComponent<CarPart> ();
			return carpart;
		}


		void UnMountPart (CarPart GO, bool WithSound, bool ChangePartTypeIndex)
		{

			if (GO.parttype == CarPart.PartType.BodyPart) RootGO = Body;
			if (GO.parttype == CarPart.PartType.EnginePart) RootGO = Engine;
			if (GO.parttype == CarPart.PartType.SuspensionPart) RootGO = Suspension;

			if (WithSound)
				//Unmount part only if no other part depends on this one.
				foreach (var item in RootGO.GetComponentsInChildren<CarPart>())
					foreach (var DependingPart in item.DependsOn)
						if (DependingPart!=null && GetCarPart (DependingPart).showedname == GO.showedname) {
							ShowTip ("Unmount " + item.showedname + " first!", 3, 24, (item.showedname.Length + 16) * 12);
							if (ErrorSound != null) ErrorSound.Play ();
							else Debug.LogWarning ("Error sound of car construction core is not assigned!");
							return;
						}

			//If the part is engine block, unmount it if only all other engine parts are unmounted.
			if (GO.parttype == CarPart.PartType.EnginePart && GO.engineparttype == CarPart.EnginePartType.EngineBlock)
			if (ParamsKeeper.Engine.EngineBlock.transform.childCount > 1) {
				ShowTip ("Unmount all engine parts before unmounting engine block!", 3, 24, 58 * 12);
				if (ErrorSound != null) ErrorSound.Play ();
				else Debug.LogWarning ("Error sound of car construction core is not assigned!");
				return;
			}

			if (WithSound) {
				if (unMountSound != null) unMountSound.Play ();
				else Debug.LogWarning ("Unmount sound of car construction core is not assigned!");
			}

			InactiveCarParts.Add (GO);					//Adding part to list in garage.
			GO.IsOpen = false;							//Closing part (if it's door, trunk, hood).
			GO.gameObject.SetActive (false);			//Deactivating it.
			RecalculateMass ();							//Calculating mass without this part.
			GO.transform.parent = GarageGO.transform;	//Moving part to garage.
			if (ChangePartTypeIndex) {					//Making type of this part unmounted.
				if (GO.parttype == CarPart.PartType.BodyPart) bodyparttypeindex [(int)GO.bodyparttype] = false;
				if (GO.parttype == CarPart.PartType.SuspensionPart) suspensiontypeindex [(int)GO.suspensionparttype, (int)GO.location] = false;
				if (GO.parttype == CarPart.PartType.EnginePart) engineparttypeindex [(int)GO.engineparttype] = false;
			}

			checkEngine ();
			checkSuspension ();
			checkBody ();
		}




		void ShowTip (string Text, int TimeToShow, int fontSize, int BoxWidth)
		{
			ShowTipText = true;
			TextToShow = Text;
			FontSize = fontSize;
			boxwidth = BoxWidth;
			timeGone = 0;
			timeToShow = TimeToShow;
		}


	

		void MountPart (CarPart GO, bool WithSound, int ItemToRemove)
		{
			if (GO.parttype == CarPart.PartType.BodyPart) RootGO = Body;
			if (GO.parttype == CarPart.PartType.EnginePart) RootGO = Engine;
			if (GO.parttype == CarPart.PartType.SuspensionPart) RootGO = Suspension;

			bool InstallPart = false;
			bool[] InstalledPart = new bool[GO.DependsOn.Length];
			string MissingPart = "";
			Parent = null;

			//If the part depends on several parts and DependsOnAll is true, then install only if all of them are. If DependsOnAll is false, then install if any of them is mounted.
			for (int i = 0; i < GO.DependsOn.Length; i++)
				foreach (var Part in RootGO.GetComponentsInChildren<CarPart>()) {
					if (Part.showedname == GetCarPart (GO.DependsOn [i]).showedname && !GO.DependsOnAll) {
						InstallPart = true;
						Parent = Part.gameObject;
					}
					if (GO.DependsOnAll && Part.showedname == GetCarPart (GO.DependsOn [i]).showedname) InstalledPart [i] = true;
				}

			if (GO.DependsOnAll) {
				InstallPart = true;
				for (int i = 0; i < InstalledPart.Length; i++)
					if (!InstalledPart [i]) {
						InstallPart = false;
						if (GO.DependsOn.Length - 1 > i) MissingPart += GetCarPart (GO.DependsOn [i]).showedname + ", ";
						if (GO.DependsOn.Length - 1 == i) MissingPart += GetCarPart (GO.DependsOn [i]).showedname;
					}
			} else {
				MissingPart = "";
				for (int i = 0; i < GO.DependsOn.Length; i++)
					if (GO.DependsOn.Length - 1 > i)
						MissingPart += GetCarPart (GO.DependsOn [i]).showedname + " or ";
					else
						MissingPart += GetCarPart (GO.DependsOn [i]).showedname;
			}

			bool Mount = true;

			//Forbidding mounting if a part with such name is already mounted.
			foreach (var item in Car.GetComponentsInChildren<CarPart>())
				if (item.showedname == GO.showedname) {
					Mount = false;
					ShowTip (GO.showedname+ " is already mounted on car!", 3, 24, (GO.showedname.Length + 28) * 12);
					if (ErrorSound != null) ErrorSound.Play ();
					else Debug.LogWarning ("Error sound of car construction core is not assigned!");
				}


			if (!InstallPart && GO.Dependent) {
				if (ErrorSound != null) ErrorSound.Play ();
				else Debug.LogWarning ("Error sound of car construction core is not assigned!");
				ShowTip ("Mount " + MissingPart + " first!", 3, 24, (MissingPart.Length + 13) * 12);
			} else {
				//
				switch (GO.parttype) {
				case CarPart.PartType.BodyPart:
					if (!bodyparttypeindex [(int)InactiveCarParts [ItemToRemove].bodyparttype]) {
						if (Parent != null) GO.transform.parent = Parent.transform;
						else GO.transform.parent = Body.transform;
						GO.transform.localPosition = GO.DefLocalPos;
						GO.transform.localEulerAngles = GO.defRot;
						if ((int)InactiveCarParts [ItemToRemove].bodyparttype != (int)CarPart.BodyPartType.Other)
							bodyparttypeindex [(int)InactiveCarParts [ItemToRemove].bodyparttype] = true;
					} else {
						Mount = false;
						ShowTip (InactiveCarParts [ItemToRemove].bodyparttype.ToString () + " is already mounted on car!", 3, 24, (28 + InactiveCarParts [ItemToRemove].bodyparttype.ToString ().Length) * 12);
					}
					break;

				case CarPart.PartType.SuspensionPart:
					if (!suspensiontypeindex [(int)InactiveCarParts [ItemToRemove].suspensionparttype, (int)InactiveCarParts [ItemToRemove].location]) {
						GO.transform.parent = RootGO.transform;
						if ((int)InactiveCarParts [ItemToRemove].suspensionparttype != (int)CarPart.SuspensionPartType.Other)
							suspensiontypeindex [(int)InactiveCarParts [ItemToRemove].suspensionparttype, (int)InactiveCarParts [ItemToRemove].location] = true;
					} else {
						Mount = false;
						ShowTip (InactiveCarParts [ItemToRemove].location.ToString () + " " + InactiveCarParts [ItemToRemove].suspensionparttype.ToString () + " is already mounted on car!", 3, 24, (29 + InactiveCarParts [ItemToRemove].location.ToString ().Length + InactiveCarParts [ItemToRemove].suspensionparttype.ToString ().Length) * 12);
					}
					break;

				case CarPart.PartType.EnginePart:
					if (!engineparttypeindex [(int)InactiveCarParts [ItemToRemove].engineparttype]) {
						if (GO.engineparttype == CarPart.EnginePartType.EngineBlock || engineparttypeindex [(int)CarPart.EnginePartType.EngineBlock] && GetCarPart (ParamsKeeper.Engine.EngineBlock).EngineType == GO.EngineType) {

							if (GO.engineparttype == CarPart.EnginePartType.EngineBlock) {
								ParamsKeeper.Engine.EngineBlock = GO.gameObject;
								GO.transform.position = ParamsKeeper.Engine.EngineDummy.position;
								GO.transform.SetParent (Engine.transform);
								GO.transform.rotation = ParamsKeeper.Engine.EngineDummy.rotation;
							} else {
								GO.transform.SetParent (ParamsKeeper.Engine.EngineBlock.transform);
								GO.transform.localPosition = GO.DefLocalPos;
								GO.transform.localEulerAngles = GO.defRot;
							}
							if ((int)InactiveCarParts [ItemToRemove].engineparttype != (int)CarPart.EnginePartType.Other)
								engineparttypeindex [(int)InactiveCarParts [ItemToRemove].engineparttype] = true;
							
						}
					} else {
						Mount = false;
						ShowTip (InactiveCarParts [ItemToRemove].engineparttype.ToString () + " is already mounted on car!", 3, 24, (29 + InactiveCarParts [ItemToRemove].engineparttype.ToString ().Length) * 12);
					}

					if (ParamsKeeper.Engine.EngineBlock != null && GetCarPart (ParamsKeeper.Engine.EngineBlock).EngineType != GO.EngineType) {
						ShowTip ("This part doesn't fit this engine!", 3, 14, 250);
						Mount = false;
					}
					break;
				}
				
				if (Mount) {
					GO.gameObject.SetActive (true);								//Activating the part.
					if (WithSound)												//Playing mount sound.
					if (MountSound != null) MountSound.Play ();
					else Debug.LogWarning ("Mount sound of car construction core is not assigned!");
					InactiveCarParts.Remove (InactiveCarParts [ItemToRemove]);	//Removing part from garage parts list.
					RecalculateMass ();											//Calculating mass with this part.
					AddMC (GO.gameObject);										//Adding mesh collider to this part.
					checkSuspension ();
					checkEngine ();
					checkBody ();
					PutPartsAtTheirPlaces ();
				} else {
					if (ErrorSound != null) ErrorSound.Play ();
					else Debug.LogWarning ("Error sound of car construction core is not assigned!");
				}
			}
		}



		//Creating parts list from GarageGO children.
		void CreateList ()
		{
			InactiveCarParts.Clear ();
			foreach (var item in GarageGO.GetComponentsInChildren<CarPart>(true))
				InactiveCarParts.Add (item);
		}



		//Defining what part types are mounted, and unmounting parts whose types are already mounted.
		void CheckSameTypePart ()
		{

			System.Array.Clear (bodyparttypeindex, 0, bodyparttypeindex.Length);
			System.Array.Clear (engineparttypeindex, 0, engineparttypeindex.Length);
			System.Array.Clear (suspensiontypeindex, 0, suspensiontypeindex.Length);

			for (int i = 0; i < carparts.Length; i++)
				switch (carparts [i].parttype) {
				case CarPart.PartType.BodyPart:
					if (bodyparttypeindex [(int)carparts [i].bodyparttype] && carparts [i].bodyparttype != CarPart.BodyPartType.Other)
						UnMountPart (carparts [i], false, false);
					if (carparts [i].gameObject.activeInHierarchy && carparts [i].bodyparttype != CarPart.BodyPartType.Other)
						bodyparttypeindex [(int)carparts [i].bodyparttype] = true;
					break;

				case  CarPart.PartType.EnginePart:
					if (engineparttypeindex [(int)carparts [i].engineparttype] && (int)carparts [i].engineparttype > 0)
						UnMountPart (carparts [i], false, false);
					if (carparts [i].gameObject.activeInHierarchy && carparts [i].engineparttype != CarPart.EnginePartType.Other)
						engineparttypeindex [(int)carparts [i].engineparttype] = true;
					break;

				case CarPart.PartType.SuspensionPart:
					if (suspensiontypeindex [(int)carparts [i].suspensionparttype, (int)carparts [i].location] && (int)carparts [i].suspensionparttype > 0)
						UnMountPart (carparts [i], false, false);
					else if (carparts [i].gameObject.activeInHierarchy && carparts [i].suspensionparttype != CarPart.SuspensionPartType.Other)
						suspensiontypeindex [(int)carparts [i].suspensionparttype, (int)carparts [i].location] = true; 
					break;
				}
		}




		void BuyPart (CarPart Part)
		{
			if (Money < Part.Price) {
				ShowTip ("You don't have enough money!", 3, 24, 29 * 12);
				if (ErrorSound != null) ErrorSound.Play ();
				else Debug.LogWarning ("Error sound of car construction core is not assigned!");
				return;
			}

			GameObject TempGO = Instantiate (Part.gameObject);
			TempGO.layer = 0;
			CarPart[] TempCP = TempGO.GetComponentsInChildren<CarPart> (true);
			foreach (var item in TempCP)
				if (item.transform != TempGO.transform)
					Destroy (item.gameObject);
			
			InactiveCarParts.Add (GetCarPart (TempGO));
			TempGO.transform.SetParent (GarageGO.transform);
			TempGO.SetActive (false);
			Money -= Part.Price;
			if (Buy!=null) Buy.Play ();
			else Debug.LogWarning ("Buy sound of car construction core is not assigned!");
			ShowTip (GetCarPart (TempGO).showedname + " added to garage", 3, 14, (18 + GetCarPart (TempGO).showedname.Length) * 7);
		}



		void MakePreviewPart ()
		{
			Destroy (PreviewPart);
			PreviewPart = Instantiate (PartsInShop [PartIndex].gameObject);
			PreviewPart.gameObject.SetActive (true);
			AddMC (PreviewPart);
			PreviewPartDummy.transform.position = PreviewPartDummyDefPos;
			PreviewPart.transform.position = PreviewPartDummy.transform.position;
			PreviewPart.transform.rotation = GetCarPart (PreviewPart).defWorldRot;
			PreviewPartDummy.transform.position = PreviewPart.GetComponent<MeshCollider> ().bounds.center;
			PreviewPart.transform.SetParent (PreviewPartDummy.transform);
			PreviewPart.gameObject.layer = 8;
			GUI.Label (new Rect (Screen.width * 0.4f, Screen.height * 0.6f, Screen.width * 0.6f, Screen.height * 0.4f), GetCarPart (PreviewPart).Description);

		}

		void DeactiveateCars ()
		{
			foreach (var item in CarsContainer.GetComponentsInChildren<ParametersKeeper>(true))
				item.gameObject.SetActive (false);
		}


//-----------------------------------------------O N G U I---------------------------------O N G U I----------------------------------------------------O N G U I------------------------------
		void OnGUI ()
		{
			if (!AreNecessaryVarsAssigned)
				return;
			
			if (ShowTipText && timeGone < timeToShow) {
				timeGone += Time.deltaTime;
				CustomBoxStyle1.alignment = TextAnchor.MiddleCenter;
				CustomBoxStyle1.fontSize = FontSize;
				CustomBoxStyle1.fontStyle = FontStyle.Bold;
				GUILayout.BeginArea (new Rect (Screen.width / 2 - boxwidth / 2, 150, boxwidth, 100));
				GUILayout.Box (TextToShow);
				GUILayout.EndArea ();
				CustomBoxStyle1.alignment = TextAnchor.UpperCenter;
			}

			CustomButton = "button";
			CustomBoxStyle1 = "box";
			CustomLabelStyle = "label";
			CustomButton.alignment = TextAnchor.MiddleCenter;
			CustomLabelStyle.normal.textColor = Color.white;
			CustomLabelStyle.fontStyle = FontStyle.Bold;

			//Car choose menu
			if (Car == null || ChooseCar) {
				GUI.Box (new Rect (Screen.width / 2 - 100, 50, 200, 200), "Choose car");
				ParametersKeeper[] Cars = CarsContainer.GetComponentsInChildren<ParametersKeeper> (true);
				for (int i = 0; i < Cars.Length; i++)
					if (GUI.Button (new Rect (Screen.width / 2 - 90, 90 + i * 30, 180, 25), Cars [i].CarName)) {
						if (ParamsKeeper != null)
							ParamsKeeper.WheelsTouchedGround = false;
						DeactiveateCars ();
						Car = null;
						Cars [i].gameObject.SetActive (true);
						Car = Cars [i].gameObject;
						ChooseCar = false;
						ConstructionModeInitialize ();
					}
				return;
			}

			//Shop menu
			if (Shop) {
				GUI.Box (new Rect (0, 0, Screen.width, Screen.height), "Money: " + Money.ToString (), BoxStyleForBackground);

				if (GUI.Button (new Rect (Screen.width - 170, 20, 150, 30), "GARAGE")) {
					Shop = false;
					CameraController.enabled = true;
					PreviewCamera.gameObject.SetActive (false);
					Destroy (PreviewPart);
				}

				float butX = Screen.width * 0.7f - 20, butY = 0;
				PartGroupIndex = GUI.SelectionGrid (new Rect (Screen.width * 0.25f + 40, 20, 150, 100), PartGroupIndex, PartGroupNames, 1);
				if (PreviewPart != null)
					GUI.Label (new Rect (Screen.width * 0.3f, Screen.height * 0.7f, Screen.width * 0.6f, Screen.height * 0.3f), GetCarPart (PreviewPart).Description, LabelStyleForDescription);

				int PartsToShow = new int ();
				for (int i = 0; i < PartsInShop.Length; i++)
					if ((int)PartsInShop [i].parttype == PartGroupIndex && (PartsInShop[i].BodyType==ParamsKeeper.CarName || PartsInShop[i].SuspensionType==ParamsKeeper.CarName || PartsInShop[i].EngineType==ParamsKeeper.CarName))
						PartsToShow += 1;
				
				scrollPosition = GUI.BeginScrollView (new Rect (20, 20, Screen.width * 0.25f, Screen.height - 40), scrollPosition, new Rect (0, 0, 220, 23 * (PartsToShow + 1)));


				for (int i = 0; i < PartsInShop.Length; i++) {
					
					if (PartGroupIndex == 1 && (int)PartsInShop [i].parttype == 1 && PartsInShop[i].EngineType ==ParamsKeeper.CarName) {
						GUI.Box (new Rect (0, butY * 23, Screen.width * 0.25f, 22), new GUIContent ("  " + PartsInShop [i].showedname.ToString (), i.ToString ()), BoxStyleForParts);
						GUI.Label (new Rect (Screen.width * 0.2f, butY * 23, 40, 20), PartsInShop [i].Price.ToString () + "$");
						butY += 1;
					}
					if (PartGroupIndex == 0 && (int)PartsInShop [i].parttype == 0 && PartsInShop [i].BodyType == ParamsKeeper.CarName) {
						GUI.Box (new Rect (0, butY * 23, Screen.width * 0.25f, 22), new GUIContent ("  " + PartsInShop [i].showedname.ToString (), i.ToString ()), BoxStyleForParts);
						GUI.Label (new Rect (Screen.width * 0.2f, butY * 23, 40, 20), PartsInShop [i].Price.ToString () + "$");
						butY += 1;
					}

					if (PartGroupIndex == 2 && (int)PartsInShop [i].parttype == 2 && PartsInShop [i].SuspensionType == ParamsKeeper.CarName) {
						GUI.Box (new Rect (0, butY * 23, Screen.width * 0.25f, 22), new GUIContent ("  " + PartsInShop [i].showedname.ToString (), i.ToString ()), BoxStyleForParts);
						GUI.Label (new Rect (Screen.width * 0.2f, butY * 23, 40, 20), PartsInShop [i].Price.ToString () + "$");
						butY += 1;
					}

					if (GUI.tooltip != "")
						PartIndex = System.Int32.Parse (GUI.tooltip);

					if (PreviewPart != null)
					if (GetCarPart (PreviewPart).showedname != GetCarPart (PartsInShop [PartIndex].gameObject).showedname) MakePreviewPart ();	
					if (PreviewPart == null && GUI.tooltip != "") MakePreviewPart ();					
						
				}


				if (Event.current.type == EventType.MouseDown && PreviewPart != null)
					BuyPart (GetCarPart (PreviewPart));

				GUI.EndScrollView ();
			}


			int ButtonY = 0, ButtonX = 20;
			CustomBoxStyle1.alignment = TextAnchor.UpperCenter;
			CustomBoxStyle1.fontSize = 24;

			if (gameMode == GameMode.Garage && !Shop) {
				
				//Race button
				if (GUI.Button (new Rect (Screen.width - 170, 10, 150, 30), "RACE!"))
					RaceModeInitialize ();
				
				//Engine check box
				GUI.Box (new Rect (Screen.width - 220, 150, 210, 120), "Engine check");

				//Shop button
				if (GUI.Button (new Rect (Screen.width - 170, 50, 150, 30), "SHOP")) {
					Shop = true;
					CameraController.enabled = false;
					PreviewCamera.gameObject.SetActive (true);
				}

				//Car choose button
				if (GUI.Button (new Rect (Screen.width - 170, 90, 150, 30), "Choose another car"))
					ChooseCar = true;
				
				//Displaying if systems are ready or not.
				String isSystemOk;
				int LabelHeight = 180;

				//Engine:
				if (isEngineOk) {
					CustomLabelStyle.normal.textColor = Color.green;
					isSystemOk = "Ready";
				} else {
					CustomLabelStyle.normal.textColor = Color.red;
					isSystemOk = "not ready";
				}

				GUI.Label (new Rect (Screen.width - 200, LabelHeight, 190, 20), "Engine: " + isSystemOk);
				LabelHeight = LabelHeight + 20;

				//Suspension:
				if (isSuspensionOk) {
					CustomLabelStyle.normal.textColor = Color.green;
					isSystemOk = "OK";
				} else {
					CustomLabelStyle.normal.textColor = Color.red;
					isSystemOk = "Wrong";
				}

				GUI.Label (new Rect (Screen.width - 200, LabelHeight, 190, 20), "Suspension: " + isSystemOk);
				LabelHeight = LabelHeight + 20;

				//Current motor power:
				CustomLabelStyle.normal.textColor = Color.white;
				if (isEngineOk) isSystemOk = ((int)(ParamsKeeper.Engine.MotorPower / 4)).ToString ();
				else isSystemOk = "-";
				GUI.Label (new Rect (Screen.width - 200, LabelHeight, 190, 20), "Current engine power: " + isSystemOk + " hp");
				LabelHeight = LabelHeight + 20;

				//Mass:
				GUI.Label (new Rect (Screen.width - 200, LabelHeight, 190, 20), "Car mass: " + car_mass.ToString () + " kg");
				LabelHeight = LabelHeight + 20;

				//Action on click on car part:
				ClickAction = GUI.SelectionGrid (new Rect (Screen.width / 2 - 75, 15, 150, 50), ClickAction, new Texture[] {
					MountIcon,
					TuneIcon,
					PaintIcon
				}, 3);
				CustomButton.alignment = TextAnchor.MiddleLeft;
				CustomButton.fontStyle = FontStyle.Bold;

				//Displaying a box near cursor with part name:
				CustomBoxStyle1.fontSize = 12;
				if (objectHit != null && GetCarPart (objectHit))
					GUI.Box (new Rect (Input.mousePosition.x + 30, -Input.mousePosition.y + Screen.height, GetCarPart (objectHit).showedname.Length * 8, 20), GetCarPart (objectHit).showedname);
			
				//Drawing vinyls icons and applying selected vinyls to parts:
				if (ClickAction == 2) {
					ColorSelected = GUI.SelectionGrid (new Rect (Screen.width / 2 - BodyColors.Length * 15, 80, BodyColors.Length * 30, 30), ColorSelected, BodyColorsIcons, BodyColorsIcons.Length);
					for (int i = 0; i < ParamsKeeper.vinyls.Length; i++) {
						if (GUI.Button (new Rect (Screen.width / 2 - ParamsKeeper.vinyls.Length * 30 + i * 60, 120, 55, 30), ParamsKeeper.vinyls [i].Name))
							foreach (var item in carparts)
								for (int t = 0; t < item.GetComponent<Renderer> ().materials.Length; t++)
									if (item.GetComponent<Renderer> ().materials [t].name.ToString () == CarPaintMaterial.name.ToString () + " (Instance)")
										item.GetComponent<Renderer> ().materials [t].mainTexture = ParamsKeeper.vinyls [i].texture;
					}
				}
					
				//Buttons showing body, suspension and engine:
				BodyShow = GUI.Toggle (new Rect (30, 15, 50, 50), BodyShow, BodyIcon, "button");
				SuspensionShow = GUI.Toggle (new Rect (90, 15, 50, 50), SuspensionShow, SuspIcon, "button");
				EngineShow = GUI.Toggle (new Rect (150, 15, 50, 50), EngineShow, EngineIcon, "button");

				//DRAWING PARTS LIST:
				for (int i = 0; i < InactiveCarParts.Count; i++) {
					if (InactiveCarParts [i].parttype == CarPart.PartType.BodyPart && BodyShow && InactiveCarParts [i].BodyType == ParamsKeeper.CarName ||
					    InactiveCarParts [i].parttype == CarPart.PartType.SuspensionPart && SuspensionShow && InactiveCarParts [i].SuspensionType == ParamsKeeper.CarName ||
					    InactiveCarParts [i].parttype == CarPart.PartType.EnginePart && EngineShow) {
						
						//Defining second row:
						if ((ButtonY * 22 + 75) > Screen.height - 10) {
							ButtonY = 0;
							ButtonX += 220;
						}
								
						//Button color: mountable-green, unmountable-red:
						GUI.backgroundColor = Color.green;
						if (bodyparttypeindex [(int)InactiveCarParts [i].bodyparttype] ||
						     suspensiontypeindex [(int)InactiveCarParts [i].suspensionparttype, (int)InactiveCarParts [i].location] ||
						     engineparttypeindex [(int)InactiveCarParts [i].engineparttype])
							GUI.backgroundColor = Color.red;

						bool InstallPart = false;
						bool[] InstalledPart = new bool[InactiveCarParts [i].DependsOn.Length];
							
						for (int r = 0; r < InactiveCarParts [i].DependsOn.Length; r++) {
							if (Body!=null)
							foreach (var Part in Body.GetComponentsInChildren<CarPart>()) {
								if (InactiveCarParts[i].DependsOn[r]!=null && Part.showedname == GetCarPart (InactiveCarParts [i].DependsOn [r]).showedname && !InactiveCarParts [i].DependsOnAll)
									InstallPart = true;
								if (InactiveCarParts [i].DependsOnAll && Part.showedname == GetCarPart (InactiveCarParts [i].DependsOn [r]).showedname)
									InstalledPart [r] = true;
							}
							if (Suspension!=null)
							foreach (var Part in Suspension.GetComponentsInChildren<CarPart>()) {
								if (Part.showedname == GetCarPart (InactiveCarParts [i].DependsOn [r]).showedname && !InactiveCarParts [i].DependsOnAll)
									InstallPart = true;
								if (InactiveCarParts [i].DependsOnAll && Part.showedname == GetCarPart (InactiveCarParts [i].DependsOn [r]).showedname)
									InstalledPart [r] = true;
							}
							if (Engine!=null)
							foreach (var Part in Engine.GetComponentsInChildren<CarPart>()) {
								if (Part.showedname == GetCarPart (InactiveCarParts [i].DependsOn [r]).showedname && !InactiveCarParts [i].DependsOnAll)
									InstallPart = true;
								if (InactiveCarParts [i].DependsOnAll && Part.showedname == GetCarPart (InactiveCarParts [i].DependsOn [r]).showedname)
									InstalledPart [r] = true;
							}
						}

						if (InactiveCarParts [i].DependsOnAll) {
							InstallPart = true;
							for (int r = 0; r < InstalledPart.Length; r++)
								if (!InstalledPart [r])
									InstallPart = false;
						}

						foreach (var item in Car.GetComponentsInChildren<CarPart>())
							if (item.showedname == InactiveCarParts [i].showedname)
								GUI.backgroundColor = Color.red;

						if (!InstallPart && InactiveCarParts [i].Dependent)
							GUI.backgroundColor = Color.red;

						String ButtonName = "";
						if (InactiveCarParts [i].parttype == CarPart.PartType.BodyPart) ButtonName = InactiveCarParts [i].BodyType.ToString () + " " + InactiveCarParts [i].showedname.ToString ();
						if (InactiveCarParts [i].parttype == CarPart.PartType.SuspensionPart) ButtonName = InactiveCarParts [i].SuspensionType.ToString () + " " + InactiveCarParts [i].showedname.ToString ();
						if (InactiveCarParts [i].parttype == CarPart.PartType.EnginePart) ButtonName = InactiveCarParts [i].EngineType.ToString () + " " + InactiveCarParts [i].showedname.ToString ();
						
						//The button itself:
						if (GUI.Button (new Rect (ButtonX, ButtonY * 22 + 75, 200, 20), ButtonName)) MountPart (InactiveCarParts [i], true, i);
						ButtonY += 1;
					}
						
				}

			}


			if (gameMode == GameMode.Driving) {
				// Back to garage button
				if (GUI.Button (new Rect (Screen.width - 170, 10, 150, 30), "Back to garage"))
					gameMode = GameMode.Garage;	
				GUI.Label (new Rect (15, 20, 190, 40), "Current engine power: " + ((int)(ParamsKeeper.Engine.MotorPower / 4)).ToString () + " hp");
			}
		}

//-----------------------------------------------/ONGUI----------------------------------------------------------------------------------------------------------------------------------------------------------------------


		void ChangeRaceCamera ()
		{
			if (CameraController.camtype == CamControl.CamType.Follow) {
				CameraController.camtype = CamControl.CamType.BodyCam;
				return;
			}
				
			if (CameraController.camtype == CamControl.CamType.BodyCam) {
				CameraController.camtype = CamControl.CamType.Driver;
				return;
			}

			if (CameraController.camtype == CamControl.CamType.Driver) {
				CameraController.camtype = CamControl.CamType.Follow;
				return;
			}

		}
			
		void checkEngine ()
		{
			isEngineOk = true;

			PartsInShop = ShopGO.GetComponentsInChildren <CarPart> (true);
			carparts = Car.transform.GetComponentsInChildren<CarPart> (true);

			if (Car.transform.Find ("Engine")) Engine = Car.transform.Find ("Engine").gameObject;
			else return;

			ParamsKeeper.Engine.MotorPower = 0;
			if (AnimCarParts != null) AnimCarParts.SpinningParts.Clear ();

			foreach (var item in Engine.GetComponentsInChildren<CarPart>()) {
				if (AnimCarParts != null && item.spinning) AnimCarParts.SpinningParts.Add (item);
				if (item.AddPower && item.gameObject.activeSelf) ParamsKeeper.Engine.MotorPower += item.AddingPower;

				if (item.engineparttype == CarPart.EnginePartType.GearBox) ParamsKeeper.Engine.GearBox = item.gameObject;
				if (item.engineparttype == CarPart.EnginePartType.EngineBlock) ParamsKeeper.Engine.EngineBlock = item.gameObject;
				if (item.engineparttype == CarPart.EnginePartType.Camshaft) ParamsKeeper.Engine.Camshaft = item.gameObject;
				if (item.engineparttype == CarPart.EnginePartType.Carburetor) ParamsKeeper.Engine.Carburetor = item.gameObject;
				if (item.engineparttype == CarPart.EnginePartType.Crankshaft) ParamsKeeper.Engine.Crankshaft = item.gameObject;
				if (item.engineparttype == CarPart.EnginePartType.CylinderHead) ParamsKeeper.Engine.CylinderHead = item.gameObject;
				if (item.engineparttype == CarPart.EnginePartType.CylinderHeadCover) ParamsKeeper.Engine.CylinderHeadCover = item.gameObject;
				if (item.engineparttype == CarPart.EnginePartType.IntakeManifold) ParamsKeeper.Engine.IntakeManifold = item.gameObject;
				if (item.engineparttype == CarPart.EnginePartType.OilPan) ParamsKeeper.Engine.OilPan = item.gameObject;
				if (item.engineparttype == CarPart.EnginePartType.Pistons) ParamsKeeper.Engine.Pistons = item.gameObject;
				if (item.engineparttype == CarPart.EnginePartType.IgnitionSystem) ParamsKeeper.Engine.IgnitionSystem = item.gameObject;
				if (item.engineparttype == CarPart.EnginePartType.TimingBelt) ParamsKeeper.Engine.TimingBelt = item.gameObject;
				if (item.engineparttype == CarPart.EnginePartType.CamshaftBearingBridge) ParamsKeeper.Engine.CamshaftBearingBridge = item.gameObject;
			}

			if (ParamsKeeper.Engine.GearBox != null && !ParamsKeeper.Engine.GearBox.activeSelf) ParamsKeeper.Engine.GearBox = null;
			if (ParamsKeeper.Engine.EngineBlock != null && !ParamsKeeper.Engine.EngineBlock.activeSelf) ParamsKeeper.Engine.EngineBlock = null;
			if (ParamsKeeper.Engine.Camshaft != null && !ParamsKeeper.Engine.Camshaft.activeSelf) ParamsKeeper.Engine.Camshaft = null;
			if (ParamsKeeper.Engine.Carburetor != null && !ParamsKeeper.Engine.Carburetor.activeSelf) ParamsKeeper.Engine.Carburetor = null;
			if (ParamsKeeper.Engine.Crankshaft != null && !ParamsKeeper.Engine.Crankshaft.activeSelf) ParamsKeeper.Engine.Crankshaft = null;
			if (ParamsKeeper.Engine.CylinderHead != null && !ParamsKeeper.Engine.CylinderHead.activeSelf) ParamsKeeper.Engine.CylinderHead = null;
			if (ParamsKeeper.Engine.CylinderHeadCover != null && !ParamsKeeper.Engine.CylinderHeadCover.activeSelf)	ParamsKeeper.Engine.CylinderHeadCover = null;
			if (ParamsKeeper.Engine.IntakeManifold != null && !ParamsKeeper.Engine.IntakeManifold.activeSelf) ParamsKeeper.Engine.IntakeManifold = null;
			if (ParamsKeeper.Engine.OilPan != null && !ParamsKeeper.Engine.OilPan.activeSelf) ParamsKeeper.Engine.OilPan = null;
			if (ParamsKeeper.Engine.Pistons != null && !ParamsKeeper.Engine.Pistons.activeSelf) ParamsKeeper.Engine.Pistons = null;
			if (ParamsKeeper.Engine.IgnitionSystem != null && !ParamsKeeper.Engine.IgnitionSystem.activeSelf) ParamsKeeper.Engine.IgnitionSystem = null;
			if (ParamsKeeper.Engine.TimingBelt != null && !ParamsKeeper.Engine.TimingBelt.activeSelf) ParamsKeeper.Engine.TimingBelt = null;
			if (ParamsKeeper.Engine.CamshaftBearingBridge != null && !ParamsKeeper.Engine.CamshaftBearingBridge.activeSelf)	ParamsKeeper.Engine.CamshaftBearingBridge = null;


			if (ParamsKeeper.Engine.EngineBlock != null) {
				if (GetCarPart (ParamsKeeper.Engine.EngineBlock).DriveType == CarPart.Drive.FWD)
					foreach (var wheel in ParamsKeeper.Wheels)
						if (wheel.wheelLocation == aWheel.WheelLocation.FL || wheel.wheelLocation == aWheel.WheelLocation.FR)
							wheel.motor = true;
				if (GetCarPart (ParamsKeeper.Engine.EngineBlock).DriveType == CarPart.Drive.RWD)
					foreach (var wheel in ParamsKeeper.Wheels)
						if (wheel.wheelLocation == aWheel.WheelLocation.RL || wheel.wheelLocation == aWheel.WheelLocation.RR)
							wheel.motor = true;
				if (GetCarPart (ParamsKeeper.Engine.EngineBlock).DriveType == CarPart.Drive.AWD)
					foreach (var wheel in ParamsKeeper.Wheels)
						wheel.motor = true;

			}
				
			if (ParamsKeeper.Engine.Camshaft == null) {
				isEngineOk = false;
				PartTypeToBeMounted = CarPart.EnginePartType.Camshaft.ToString ();
			}

			if (ParamsKeeper.Engine.Carburetor == null) {
				isEngineOk = false;
				PartTypeToBeMounted = CarPart.EnginePartType.Carburetor.ToString ();
			}

			if (ParamsKeeper.Engine.Crankshaft == null) {
				isEngineOk = false;
				PartTypeToBeMounted = CarPart.EnginePartType.Crankshaft.ToString ();
			}

			if (ParamsKeeper.Engine.CylinderHead == null) {
				isEngineOk = false;
				PartTypeToBeMounted = CarPart.EnginePartType.CylinderHead.ToString ();
			}

			if (ParamsKeeper.Engine.CylinderHeadCover == null) {
				isEngineOk = false;
				PartTypeToBeMounted = CarPart.EnginePartType.CylinderHeadCover.ToString ();
			}

			if (ParamsKeeper.Engine.GearBox == null) {
				isEngineOk = false;
				PartTypeToBeMounted = CarPart.EnginePartType.GearBox.ToString ();
			}

			if (ParamsKeeper.Engine.IntakeManifold == null) {
				isEngineOk = false;
				PartTypeToBeMounted = CarPart.EnginePartType.IntakeManifold.ToString ();
			}

			if (ParamsKeeper.Engine.OilPan == null) {
				isEngineOk = false;
				PartTypeToBeMounted = CarPart.EnginePartType.OilPan.ToString ();
			}

			if (ParamsKeeper.Engine.Pistons == null) {
				isEngineOk = false;
				PartTypeToBeMounted = CarPart.EnginePartType.Pistons.ToString ();
			}

			if (ParamsKeeper.Engine.IgnitionSystem == null) {
				isEngineOk = false;
				PartTypeToBeMounted = CarPart.EnginePartType.IgnitionSystem.ToString ();
			}

			if (ParamsKeeper.Engine.TimingBelt == null) {
				isEngineOk = false;
				PartTypeToBeMounted = CarPart.EnginePartType.TimingBelt.ToString ();
			}

			if (ParamsKeeper.Engine.CamshaftBearingBridge == null) {
				isEngineOk = false;
				PartTypeToBeMounted = CarPart.EnginePartType.CamshaftBearingBridge.ToString ();
			}



		}


		void checkSuspension ()
		{
			isSuspensionOk = true;

			foreach (var item in Car.GetComponentsInChildren<CarPart> ()) {
				tempWheel = null;
				foreach (var wheel in ParamsKeeper.Wheels) {
					if (wheel.wheelLocation == aWheel.WheelLocation.FL && item.location == CarPart.Location.FrontLeft) tempWheel = wheel;
					if (wheel.wheelLocation == aWheel.WheelLocation.FR && item.location == CarPart.Location.FrontRight) tempWheel = wheel;
					if (wheel.wheelLocation == aWheel.WheelLocation.RL && item.location == CarPart.Location.RearLeft) tempWheel = wheel;
					if (wheel.wheelLocation == aWheel.WheelLocation.RR && item.location == CarPart.Location.RearRight) tempWheel = wheel;
				}
				if (tempWheel != null) {
					if (item.suspensionparttype == CarPart.SuspensionPartType.Absorber) tempWheel.Absorber = item.gameObject;
					if (item.suspensionparttype == CarPart.SuspensionPartType.BrakeDisk) tempWheel.BrakeDisk = item.gameObject;
					if (item.suspensionparttype == CarPart.SuspensionPartType.Arm) tempWheel.Arm = item.gameObject;
					if (item.suspensionparttype == CarPart.SuspensionPartType.Hub) tempWheel.Hub = item.gameObject;
					if (item.suspensionparttype == CarPart.SuspensionPartType.Wider) tempWheel.Wider = item.gameObject;
					if (item.suspensionparttype == CarPart.SuspensionPartType.Wheel) tempWheel.Wheel = item.gameObject;
					if (item.suspensionparttype == CarPart.SuspensionPartType.BrakeDisk) tempWheel.BrakingEfficiency = item.BrakingEfficiency;
					if (item.suspensionparttype == CarPart.SuspensionPartType.Caliper) tempWheel.BrakingEfficiency += item.BrakingEfficiency;

					if (tempWheel.Absorber != null) tempWheel.WheelCollider.suspensionDistance = GetCarPart (tempWheel.Absorber).SpringHeight / 100f;
					if (tempWheel.Absorber != null) tempWheel.WheelCollider.suspensionSpring = new JointSpring () {
							damper = (float)GetCarPart (tempWheel.Absorber).AbsorberDamping,
							spring = (float)GetCarPart (tempWheel.Absorber).SpringRate,
							targetPosition = 0.5f
						};
						
					if (tempWheel.Absorber != null && !tempWheel.Absorber.activeSelf) tempWheel.Absorber = null;
					if (tempWheel.BrakeDisk != null && !tempWheel.BrakeDisk.activeSelf) tempWheel.BrakeDisk = null;
					if (tempWheel.Hub != null && !tempWheel.Hub.activeSelf) tempWheel.Hub = null;
					if (tempWheel.Wider != null && !tempWheel.Wider.activeSelf) tempWheel.Wider = null;
					if (tempWheel.Arm != null && !tempWheel.Arm.activeSelf) tempWheel.Arm = null;
					if (tempWheel.Wheel != null && !tempWheel.Wheel.activeSelf) tempWheel.Wheel = null;
				}
				if (item.suspensionparttype == CarPart.SuspensionPartType.BeamAxle) ParamsKeeper.BeamAxle = item.gameObject;
			}

			foreach (var item in ParamsKeeper.Wheels)
				if (item.Absorber == null || item.Hub == null || item.Wheel == null || item.BrakeDisk == null)
					isSuspensionOk = false;
			
			if (ParamsKeeper.BeamAxle == null)
				isSuspensionOk = false;

		}


		void checkBody ()
		{
			isBodyOk = true;
			if (ParamsKeeper.Body.SteeringWheel == null) {
				isBodyOk = false;
				PartTypeToBeMounted = CarPart.BodyPartType.SteeringWheel.ToString ();
			}
			if (ParamsKeeper.Body.DriverSeat == null) {
				isBodyOk = false;
				PartTypeToBeMounted = CarPart.BodyPartType.DriverSeat.ToString ();
			}
			Body = Car.transform.Find ("Body").gameObject;

			foreach (var item in Body.transform.GetComponentsInChildren<CarPart>()) {
				if (item.bodyparttype == CarPart.BodyPartType.CarBody) ParamsKeeper.Body.CarBody = item.gameObject;
				if (item.bodyparttype == CarPart.BodyPartType.SteeringWheel) ParamsKeeper.Body.SteeringWheel = item.gameObject;
				if (item.bodyparttype == CarPart.BodyPartType.BrakelightLeft) ParamsKeeper.Body.BrakeLightLeft = item.gameObject;
				if (item.bodyparttype == CarPart.BodyPartType.BrakelightRight) ParamsKeeper.Body.BrakeLightRight = item.gameObject;
				if (item.bodyparttype == CarPart.BodyPartType.DriverSeat) ParamsKeeper.Body.DriverSeat = item.gameObject;
			}


			if (ParamsKeeper.Body.SteeringWheel != null && !ParamsKeeper.Body.SteeringWheel.activeSelf) ParamsKeeper.Body.SteeringWheel = null;
			if (ParamsKeeper.Body.BrakeLightLeft != null && !ParamsKeeper.Body.BrakeLightLeft.activeSelf) ParamsKeeper.Body.BrakeLightLeft = null;
			if (ParamsKeeper.Body.BrakeLightRight != null && !ParamsKeeper.Body.BrakeLightRight.activeSelf) ParamsKeeper.Body.BrakeLightRight = null;
			if (ParamsKeeper.Body.DriverSeat != null && !ParamsKeeper.Body.DriverSeat.activeSelf) ParamsKeeper.Body.DriverSeat = null;

		}




		void RaceModeInitialize ()
		{
			RaceModeInitialized = true;
			ConstructionModeInitialized = false;

			CloseOpenedParts ();

			checkEngine ();
			if (!isEngineOk) {
				ShowTip ("The engine needs " + PartTypeToBeMounted + " to be installed!", 5, 24, (PartTypeToBeMounted.Length + 36) * 12);
				gameMode = GameMode.Garage;
				return;
			}
			checkSuspension ();
			if (!isSuspensionOk) {
				ShowTip ("Suspension is missing some of its elements!", 5, 24, 44 * 12);
				gameMode = GameMode.Garage;
				return;
			}
			checkBody ();
			if (!isBodyOk) {
				ShowTip ("Car needs " + PartTypeToBeMounted + "!", 5, 24, (PartTypeToBeMounted.Length + 36) * 12);
				gameMode = GameMode.Garage;
				return;
			}

			if (CameraController != null) ChangeCamera (CamControl.CamType.Follow);

			//Removing mesh colliders from parts.
			foreach (var item in carparts)
				RemoveMC (item.gameObject);


			if (RaceTrackPoint != null) {
				Car.transform.position = RaceTrackPoint.position;
				Car.transform.rotation = RaceTrackPoint.rotation;
			} else Debug.LogWarning ("Race track point transform of car construction core script in not assigned!");

			foreach (var wheel in ParamsKeeper.Wheels)
				if (wheel.WheelCollider != null) wheel.WheelCollider.brakeTorque = 0;

			car_rigidbody.constraints = RigidbodyConstraints.None;

			ParamsKeeper.Engine.IsEngineWorking = true;

			gameMode = GameMode.Driving;

		}




		void ConstructionModeInitialize ()
		{
			carparts = Car.transform.GetComponentsInChildren<CarPart> (true);

			if (Car.transform.Find("Body")) Body = Car.transform.Find ("Body").gameObject;
			if (Car.transform.Find("Suspension")) Suspension = Car.transform.Find ("Suspension").gameObject;
			if (Car.transform.Find("Engine")) Engine = Car.transform.Find ("Engine").gameObject;

			AnimCarParts = Car.GetComponent<AnimatedCarParts> ();
			ParamsKeeper = Car.GetComponent<ParametersKeeper> ();
			car_rigidbody = Car.GetComponent<Rigidbody> ();

			checkEngine ();
			checkSuspension ();
			checkBody ();
			RecalculateMass ();
			CheckSameTypePart ();
			MoveInactivePartsToGarage ();
			CreateList ();

			car_rigidbody.constraints = RigidbodyConstraints.FreezeAll;
			car_rigidbody.constraints = RigidbodyConstraints.None;
			car_rigidbody.velocity = Vector3.zero;

			CloseOpenedParts ();

			foreach (var item in carparts)
				if (item.unMountable)
					AddMC (item.gameObject);
					
			if (GaragePoint != null) {
				Car.transform.position = GaragePoint.position;
				Car.transform.rotation = GaragePoint.rotation;
			} else {
				Car.transform.position = Vector3.zero;
				Car.transform.rotation = Quaternion.identity;
				Debug.LogWarning ("Garage point transform of car construction core script is not assigned! Car set to (0,0,0).");
			}
				
			if (CameraController != null) ChangeCamera (CamControl.CamType.BodyCam);
			else Debug.LogError ("Camera controller is not assigned to main camera!");

			ConstructionModeInitialized = true;
			RaceModeInitialized = false;
			PutPartsAtTheirPlaces ();
			ParamsKeeper.Engine.IsEngineWorking = false;

		}

		//Adding convex mesh collider to parts.
		void AddMC (GameObject item)
		{
			if (!item.GetComponent<MeshCollider> ()) {
				item.AddComponent<MeshCollider> (); 
				MeshCollider MC = item.GetComponent <MeshCollider> ();
				MC.convex = true;
			}
		}


		//Removing mesh colliders.
		void RemoveMC (GameObject item)
		{
			if (item.GetComponent<MeshCollider> ())
				Destroy (item.GetComponent<MeshCollider> ());
		}



		void OpenClosePart (GameObject GO)
		{
			if (RotatePart) return;
			if (GetCarPart (GO).rotationAxis == CarPart.RotationAxis.X && GetCarPart (GO).opendirection == CarPart.OpenDirection.Positive) RotationVector = new Vector3 (1, 0, 0);
			if (GetCarPart (GO).rotationAxis == CarPart.RotationAxis.X && GetCarPart (GO).opendirection == CarPart.OpenDirection.Negative) RotationVector = new Vector3 (-1, 0, 0);
			if (GetCarPart (GO).rotationAxis == CarPart.RotationAxis.Y && GetCarPart (GO).opendirection == CarPart.OpenDirection.Positive) RotationVector = new Vector3 (0, 1, 0);
			if (GetCarPart (GO).rotationAxis == CarPart.RotationAxis.Y && GetCarPart (GO).opendirection == CarPart.OpenDirection.Negative) RotationVector = new Vector3 (0, -1, 0);
			if (GetCarPart (GO).rotationAxis == CarPart.RotationAxis.Z && GetCarPart (GO).opendirection == CarPart.OpenDirection.Positive) RotationVector = new Vector3 (0, 0, 1);
			if (GetCarPart (GO).rotationAxis == CarPart.RotationAxis.Z && GetCarPart (GO).opendirection == CarPart.OpenDirection.Negative) RotationVector = new Vector3 (0, 0, -1);
			RotatePart = true;
			PartToRotate = GO;
			Timer = 0;
			if (!GetCarPart (GO).IsOpen)
			if (DoorOpenSound != null) DoorOpenSound.Play ();
			else Debug.LogWarning ("Door open sound of car construction core script is not assigned!");
		}


		void CloseOpenedParts ()
		{
			carparts = Car.GetComponentsInChildren<CarPart> ();
			foreach (var item in carparts)
				if (item.IsOpen) {
					item.gameObject.transform.localEulerAngles = item.defRot;
					item.IsOpen = false;
				}
			RotatePart = false;
		}




		void Hightlight (GameObject ObjectHit)
		{
			for (int i = 0; i < ObjectHit.GetComponent<Renderer> ().materials.Length; i++)
				ObjectHit.GetComponent<Renderer> ().materials [i].SetColor ("_EmissionColor", Color.gray);
		}


		void UnHightlight (GameObject ObjectHit)
		{
			for (int i = 0; i < ObjectHit.GetComponent<Renderer> ().materials.Length; i++)
				ObjectHit.GetComponent<Renderer> ().materials [i].SetColor ("_EmissionColor", Color.black);
		}


		void SetCursor (Texture2D Icon)
		{
			Cursor.SetCursor (Icon, Vector2.zero, CursorMode.ForceSoftware);
		}


		void PaintPart (GameObject GO)
		{
			for (int i = 0; i < GO.GetComponent<Renderer> ().materials.Length; i++)
				if (GO.GetComponent<Renderer> ().materials [i].name.ToString () == CarPaintMaterial.name.ToString () + " (Instance)" ||
				    GO.GetComponent<Renderer> ().materials [i].name.ToString () == CarPaintMaterial.name.ToString () ||
				    GO.GetComponent<Renderer> ().materials [i].name.ToString () == RimPaintMaterial.name.ToString () + " (Instance)" ||
				    GO.GetComponent<Renderer> ().materials [i].name.ToString () == RimPaintMaterial.name.ToString ())
					GO.GetComponent<Renderer> ().materials [i].color = BodyColors [ColorSelected];
			if (Spray != null) Spray.Play ();
			else Debug.LogWarning ("Spray sound of car constructor core is not assigned!");

		}


		void ChangeCamera (CamControl.CamType CamTypeToSet)
		{
			if (CameraController != null) CameraController.camtype = CamTypeToSet;
			else Debug.LogError ("Camera controller is not assigned to main camera!");

			if (CamTypeToSet == CamControl.CamType.BodyCam) {
				if (BodyShow && Body!=null)
					foreach (var item in Body.GetComponentsInChildren<CarPart>())
						item.gameObject.layer = 0;
				if (SuspensionShow && Suspension!=null)
					foreach (var item in Suspension.GetComponentsInChildren<CarPart>())
						item.gameObject.layer = 0;
				if (EngineShow && Engine!=null)
					foreach (var item in Engine.GetComponentsInChildren<CarPart>())
						item.gameObject.layer = 0;
			}

			//Putting parts to Ignore Raycast layer
			if (CamTypeToSet == CamControl.CamType.EngineCam) {
				foreach (var item in Body.GetComponentsInChildren<CarPart>())
					item.gameObject.layer = 2;
				foreach (var item in Suspension.GetComponentsInChildren<CarPart>())
					item.gameObject.layer = 2;
			}

			//Putting parts to Ignore Raycast layer
			if (CamTypeToSet == CamControl.CamType.WheelFLCam ||
			    CamTypeToSet == CamControl.CamType.WheelFRCam ||
			    CamTypeToSet == CamControl.CamType.WheelRLCam ||
			    CamTypeToSet == CamControl.CamType.WheelRRCam) {
				foreach (var item in Body.GetComponentsInChildren<CarPart>())
					item.gameObject.layer = 2;
				foreach (var item in Engine.GetComponentsInChildren<CarPart>())
					item.gameObject.layer = 2;
			}
		}
//--------------------------------------------------------------------------------------R A Y C A S T I N G----------------------------------------------------------------------------
		void RayCasting ()
		{
			if (CameraController == null) return;
			
			Ray ray = GetComponent<Camera> ().ScreenPointToRay (Input.mousePosition);
			if (objectHit == null) SetCursor (null);

			if (Physics.Raycast (ray, out hit)) {
				if (objectHit != null && objectHit != hit.collider.gameObject && GetCarPart (objectHit)) UnHightlight (objectHit);
				
				objectHit = hit.collider.gameObject;
					

				if (GetCarPart (objectHit) && (GetCarPart(objectHit).unMountable || GetCarPart(objectHit).paintable)) {
					
					CarPart objectHitCarPart = GetCarPart (objectHit);
					Hightlight (objectHit);
					if (Input.GetKeyDown ("mouse 0")) {

						if (objectHitCarPart.parttype == CarPart.PartType.BodyPart ||
						   objectHitCarPart.parttype == CarPart.PartType.EnginePart && CameraController.camtype == CamControl.CamType.EngineCam ||
						   objectHitCarPart.parttype == CarPart.PartType.SuspensionPart && CameraController.camtype != CamControl.CamType.EngineCam && CameraController.camtype != CamControl.CamType.BodyCam)

						if (objectHitCarPart.unMountable && ClickAction == 0) UnMountPart (objectHitCarPart, true, true);

						if (GetCarPart (objectHit).openable && ClickAction == 1) OpenClosePart (objectHit);
						if (GetCarPart (objectHit).paintable && ClickAction == 2) PaintPart (objectHit);

						if (objectHitCarPart.parttype == CarPart.PartType.SuspensionPart && CameraController.camtype == CamControl.CamType.BodyCam && ClickAction != 2) {
							if (objectHitCarPart.location == CarPart.Location.FrontLeft) ChangeCamera (CamControl.CamType.WheelFLCam);
							if (objectHitCarPart.location == CarPart.Location.FrontRight) ChangeCamera (CamControl.CamType.WheelFRCam);
							if (objectHitCarPart.location == CarPart.Location.RearLeft) ChangeCamera (CamControl.CamType.WheelRLCam);
							if (objectHitCarPart.location == CarPart.Location.RearRight) ChangeCamera (CamControl.CamType.WheelRRCam);
						}

						if (objectHitCarPart.parttype == CarPart.PartType.EnginePart && CameraController.camtype != CamControl.CamType.EngineCam)
							ChangeCamera (CamControl.CamType.EngineCam);
					}

					if (ClickAction == 0 && objectHitCarPart.unMountable) SetCursor (MountCursor);
					if (ClickAction == 0 && !objectHitCarPart.unMountable) SetCursor (null);
					if (ClickAction == 1 && objectHitCarPart.openable) SetCursor (TuneCursor);
					if (ClickAction == 2 && objectHitCarPart.paintable) SetCursor (PaintCursor);
				} else SetCursor (null);
			} else {
				if (objectHit != null && GetCarPart (objectHit)) UnHightlight (objectHit);
				objectHit = null;
			}
		}

//------------------------------------------------------- / R A Y C A S T I N G------------------------------------------------------------------------------------------------------------------




		void HideShowRoots ()
		{
			if (Body == null || Engine == null || Suspension == null)
				return;

			foreach (var item in Body.GetComponentsInChildren<MeshRenderer>()) {
				if (!BodyShow && item.enabled) {
					item.enabled = false;
					item.gameObject.layer = 2;
				}
			
				if (BodyShow && !item.enabled) {
					item.enabled = true;
					item.gameObject.layer = 0;
				}
			}
			foreach (var item in Suspension.GetComponentsInChildren<MeshRenderer>()) {
				if (!SuspensionShow && item.enabled) {
					item.enabled = false;
					item.gameObject.layer = 2;
				}
				if (SuspensionShow && !item.enabled) {
					item.enabled = true;
					item.gameObject.layer = 0;
				}
			}

			foreach (var item in Engine.GetComponentsInChildren<MeshRenderer>()) {
				if (!EngineShow && item.enabled) {
					item.enabled = false;
					item.gameObject.layer = 2;
				}

				if (EngineShow && !item.enabled) {
					item.enabled = true;
					item.gameObject.layer = 0;
				}
			}

		}


		void OpenCloseParts ()
		{
			
			if (PartToRotate == null)
				return; 

			Vector3 Angle = PartToRotate.transform.localEulerAngles;

			if (RotatePart && !GetCarPart (PartToRotate).IsOpen) PartToRotate.transform.Rotate (RotationVector, Space.Self);
			if (RotatePart && GetCarPart (PartToRotate).IsOpen) PartToRotate.transform.Rotate (-RotationVector);


			if (!GetCarPart (PartToRotate).IsOpen)
			if (RotatePart && Timer > 1) {
				GetCarPart (PartToRotate).IsOpen = true;
				RotatePart = false;
			}

			if (GetCarPart (PartToRotate).IsOpen)
			if (RotatePart && Timer > 1) {
				GetCarPart (PartToRotate).IsOpen = false;
				PartToRotate.transform.localEulerAngles = GetCarPart (PartToRotate).defRot;
				RotatePart = false;
				if (DoorCloseSound != null) DoorCloseSound.Play ();
				else Debug.LogWarning ("Door close sound of car construction core script is not assigned!");
			}

		}



		void Update ()
		{

			Timer += Time.deltaTime;

			if (!AreNecessaryVarsAssigned || Car == null || ParamsKeeper==null)
				return;
			
			if (ParamsKeeper.Body.CarBody != null)
				if (ClickAction == 2) ParamsKeeper.Body.CarBody.layer = 0;
				else ParamsKeeper.Body.CarBody.layer=2;

			if (Input.GetAxis ("Mouse ScrollWheel") < 0)
				ChangeCamera (CamControl.CamType.BodyCam);

			PreviewCamera.transform.LookAt (PreviewPartDummy.transform);
			PreviewPartDummy.transform.Rotate (new Vector3 (0, 1, 0));

			if (gameMode == GameMode.Garage) {
				if (!Shop && !ChooseCar) RayCasting ();
				HideShowRoots ();
				OpenCloseParts ();
				if (!ConstructionModeInitialized) ConstructionModeInitialize ();
				foreach (var wheel in ParamsKeeper.Wheels)
					if (wheel.WheelCollider != null) wheel.WheelCollider.brakeTorque = 10000;
			}


			if (gameMode == GameMode.Driving) {
				if (!RaceModeInitialized) RaceModeInitialize ();
				if (Input.GetKeyDown ("c")) ChangeRaceCamera ();
			}

		}



	}
		
}
	
