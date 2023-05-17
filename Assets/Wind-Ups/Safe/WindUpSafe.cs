using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using KModkit;

public class WindUpSafe : MonoBehaviour {

	//-----------------------------------------------------//
	public KMBombInfo Bomb;
	public KMAudio Audio;
	public KMBombModule Module;
	public KMGameInfo StateCheck;

	public KMSelectable KeyHole;
	public KMSelectable[] TurnArrows;
	public GameObject[] TurnArrowsTransform;
	public Material[] lightMats; //Red, RedLit, Green, GreenLit
	public Renderer[] lightMesh;

	//public Transform SafeDial;

	public GameObject Key;
	public Transform[] KeyRotation;

	public TextMesh tempCombo;

	public bool debugMode;

	//-----------------------------------------------------//
	private int dialVal = 0;
	private int[] solution = new int[] {11, 7, 6};
	private int turnFrame = 0;
	private int direction = 0;

	private int[] submit = new int[] {0, 0, 0};
	private int currentDirection = 1;
	private int currentStep = 0;
	private bool startCombo = false;
	private bool discard = false;

	//-----------------------------------------------------//
	//SHARED INFORMATION
	private bool HasKey = false;
	int windID = 0;

	//-----------------------------------------------------//
	static int moduleIdCounter = 1;
	int moduleId;
	private bool modulePassed = false;
	//-----------------------------------------------------//

	private void Awake () {
		moduleId = moduleIdCounter++;

		foreach (KMSelectable NAME in TurnArrows) {
			KMSelectable pressedObject = NAME;
			NAME.OnInteract += delegate () { Turn(pressedObject); return false; };
		}
		KeyHole.OnInteract += delegate () { PlaceKey(); return false; };
        
		//Bomb.OnBombSolved += MasterKey.ResetMaster;
		//Bomb.OnBombExploded += MasterKey.ResetMaster;
		StateCheck.OnStateChange += i => { MasterKey.ResetMaster(); };
	}

	void Start () {
		windID = MasterKey.ServeID(Bomb);

		TurnArrowsTransform[0].SetActive(false);
		TurnArrowsTransform[1].SetActive(false);
		StartCoroutine(CheckKey());
		

		InitSolution();
		StartCoroutine(Animate());
	}

	IEnumerator CheckKey () {
		yield return new WaitForSeconds(0.01f);
		if (MasterKey.PlaceKey(windID)) { HasKey = true; Key.SetActive(true); TurnArrowsTransform[0].SetActive(true); TurnArrowsTransform[1].SetActive(true); Debug.LogFormat("[Wind-Up Safe #{0}] Starting with key", moduleId); lightMesh[0].material = lightMats[1]; }
	}

	void InitSolution () {
		for (int i = 0; i < 3; i++) {
			solution[i] = UnityEngine.Random.Range(0, 12);
		}
		tempCombo.text = solution[0] + "-" + solution[1] + "-" + solution[2];
		Debug.LogFormat("[Wind-Up Safe #{0}] Combination is {1}-{2}-{3}", moduleId, solution[0], solution[1], solution[2]);
		//Debug.LogFormat("[Wind-Up Lockpick #{0}] Solution is {1}-{2}-{3}", moduleId, solution[0]+1, solution[1]+1, solution[2]+1);
	}

	void PlaceKey () {
		if (turnFrame != 0) { return; }
		if (MasterKey.GlobalKeyHeld && !HasKey) {
			MasterKey.GlobalKeyHeld = false;
			HasKey = true;
			Key.SetActive(true);
			Audio.PlaySoundAtTransform("Key_In", transform);
			TurnArrowsTransform[0].SetActive(true);
			TurnArrowsTransform[1].SetActive(true);
			lightMesh[0].material = lightMats[1];
		} else if (HasKey) {
			MasterKey.GlobalKeyHeld = true;
			HasKey = false;
			Key.SetActive(false);
			Audio.PlaySoundAtTransform("keys_01", transform);
			TurnArrowsTransform[0].SetActive(false);
			TurnArrowsTransform[1].SetActive(false);
			lightMesh[0].material = lightMats[0];
			lightMesh[1].material = lightMats[2];
			CheckSolve();
			if (dialVal < 6) { direction = -1; turnFrame = dialVal*3; } else if (dialVal > 6) { direction = 1; turnFrame = (12-dialVal)*3; } else {
				if (UnityEngine.Random.Range(0, 2) == 1) { direction = -1; } else { direction = 1; }
				turnFrame = 18;
			}
			submit = new int[] {0, 0, 0};
			currentDirection = 1;
			currentStep = 0;
			if (dialVal == 0) { return; }
			Audio.PlaySoundAtTransform("Zip_1", transform);
			dialVal = 0;
		}
	}

	void Turn (KMSelectable Arrow) {
		int bugSTORE = dialVal;
		if (!HasKey) { return; }
		int arrow = Array.IndexOf(TurnArrows, Arrow);
		//Debug.Log(arrow);
		if (turnFrame != 0) { return; }
		direction = (arrow % 2)-1;
		if (direction == 0) { direction = 1; }
		dialVal += 1*direction;
		if (dialVal == 12) { dialVal = 0; } else if (dialVal == -1) { dialVal = 11; }
		turnFrame = 3;
		Audio.PlaySoundAtTransform("metal_hit_05", transform);
		//Debug.Log(dialVal);

		if(direction != currentDirection && !startCombo) { currentStep = 2; } else { startCombo = true; }

		if (direction != currentDirection && currentStep != 3) { currentStep += 1; currentDirection = direction; if (currentStep != 3) { Debug.LogFormat("[Wind-Up Safe #{0}] Step {1} stored as {2}", moduleId, currentStep, bugSTORE); } }
		if (currentStep == 3) { submit = new int[] {0, 0, 0}; if (!discard) { Debug.LogFormat("[Wind-Up Safe #{0}] Combination discarded", moduleId); discard = true; lightMesh[0].material = lightMats[0]; lightMesh[1].material = lightMats[2]; } }
		if (currentStep != 3) { submit[currentStep] = dialVal; }
		if (currentStep == 2) { lightMesh[1].material = lightMats[3]; }
	}

	IEnumerator Animate () {
		while(true){
			if (turnFrame != 0) {
				KeyRotation[0].Rotate(0.0f, 0.0f, 10.0f*direction);
				KeyRotation[1].Rotate(0.0f, -10.0f*direction, 0.0f);
				KeyRotation[2].Rotate(0.0f, -10.0f*direction, 0.0f);
				turnFrame--;
			}

			yield return new WaitForSeconds(0.01f);
		}
	}

	bool conditionCheck (int condition) {
		
		return false;
	}

	void CheckSolve () {
		discard = false;
		startCombo = false;
		if (currentStep == 2) { Debug.LogFormat("[Wind-Up Safe #{0}] Step 3 stored as {1}", moduleId, dialVal); } else if (currentStep != 3 && !(currentStep == 0 && dialVal == 0)) { Debug.LogFormat("[Wind-Up Safe #{0}] Combination discarded", moduleId); }
		if (currentStep == 2 && submit[0] == solution[0] && submit[1] == solution[1] && submit[2] == solution[2]) { Debug.LogFormat("[Wind-Up Safe #{0}] Combination accepted", moduleId); Module.HandlePass(); modulePassed = true; } else if (currentStep == 2 && !modulePassed) { Debug.LogFormat("[Wind-Up Safe #{0}] Combination incorrect", moduleId); Module.HandleStrike(); }
	}
	
	void Update () {
		//if (needyActive) {  }
	}
}
