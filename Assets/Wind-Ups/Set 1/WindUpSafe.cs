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
	public int[] debugCode;

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

	
	int[] solveCache = new int[] {0, 0, 0};
	private List<string> PortCache = new List<string> {};

	//-----------------------------------------------------//
	//SHARED INFORMATION
	private bool HasKey = false;
	int windID = 0;

	int tpDirection = 1; // Right = 0 // Left = 1 // But why???
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
		PortCache = Bomb.GetPorts().ToList();

		for (int i = 0; i < 3; i++) {
			solution[i] = UnityEngine.Random.Range(0, 12);
			if (debugMode) { solution[i] = debugCode[i]; }
		}
		tempCombo.text = solution[0] + "-" + solution[1] + "-" + solution[2];
		Debug.LogFormat("[Wind-Up Safe #{0}] Note reads {1}-{2}-{3}", moduleId, solution[0], solution[1], solution[2]);
		if (PortCache.Count() == 0) {
			PortCache = new List<string> {"DVI", "Parallel", "PS2", "RJ45", "Serial", "StereoRCA"};
			Debug.LogFormat("[Wind-Up Safe #{0}] No Ports! Using all ciphers...", moduleId);
		}
		SolutionCipher();
		Debug.LogFormat("[Wind-Up Safe #{0}] Final Combination is {1}-{2}-{3}", moduleId, solution[0], solution[1], solution[2]);
		//Debug.LogFormat("[Wind-Up Lockpick #{0}] Solution is {1}-{2}-{3}", moduleId, solution[0]+1, solution[1]+1, solution[2]+1);
	}

	void SolutionCipher () {
		/*	[X] DVI ------- XOR
			[X] Parallel -- Shift
			[X] PS2 ------- Wheel
			[ ] RJ45 ------ Grid
			[X] Serial ---- M-Shift
			[ ] StereoRCA - Doubles
		*/	
		for (int i = 0; i < 3; i++) { solveCache[i] = solution[i]; }
		if (PortCache.Contains("DVI")) {
			bool[,] decimalToBinary = new bool[,] {
				{false, false, false, false}, {false, false, false, true}, {false, false, true, false}, {false, false, true, true}, //0-1-2-3
				{false, true, false, false}, {false, true, false, true}, {false, true, true, false}, {false, true, true, true},     //4-5-6-7
				{true, false, false, false}, {true, false, false, true}, {true, false, true, false}, {true, false, true, true},     //8-9-10-11
				{true, true, false, false}, {true, true, false, true}, {true, true, true, false}, {true, true, true, true},         //12-13-14-15
			};
			bool[,] solveCacheBool = new bool[,] { {false, false, false, false}, {false, false, false, false}, {false, false, false, false} };
			bool[,] cacheCacheBool = new bool[,] { {false, false, false, false}, {false, false, false, false}, {false, false, false, false} };

			for (int i = 0; i < 3; i++) {
				for (int j = 0; j < 4; j++) {
					solveCacheBool[i, j] = decimalToBinary[solveCache[i], j];
				}
			}
			for (int i = 0; i < 4; i++) {
				if (solveCacheBool[0, i] != solveCacheBool[1, i]) { cacheCacheBool[0, i] = true; } else { cacheCacheBool[0, i] = false; }
				if (solveCacheBool[1, i] != solveCacheBool[2, i]) { cacheCacheBool[1, i] = true; } else { cacheCacheBool[1, i] = false; }
				if (solveCacheBool[2, i] != solveCacheBool[0, i]) { cacheCacheBool[2, i] = true; } else { cacheCacheBool[2, i] = false; }
			}
			for (int i = 0; i < 3; i++) {
				solveCache[i] = 0;
				if (cacheCacheBool[i, 0]) { solveCache[i] += 8; }
				if (cacheCacheBool[i, 1]) { solveCache[i] += 4; }
				if (cacheCacheBool[i, 2]) { solveCache[i] += 2; }
				if (cacheCacheBool[i, 3]) { solveCache[i] += 1; }
			}
			SetSolution("DVI");
		}
		if (PortCache.Contains("Parallel")) {
			int shift = Bomb.GetBatteryCount();
			if (shift == 0) { shift = 6; }
			for (int i = 0; i < 3; i++) { solveCache[i] = solveCache[i] + shift; }
			SetSolution("Parallel");
		}
		if (PortCache.Contains("PS2")) {
			solveCache[0] = solveCache[0] + solution[1] - solution[2];
			solveCache[1] = solveCache[1] + solution[2] - solution[0];
			solveCache[2] = solveCache[2] + solution[0] - solution[1];
			SetSolution("PS2");
		}
		if (PortCache.Contains("RJ45")) {
			int[] gridCph = new int[] {
				7, 2, 4,
				3, 5, 11,
				10, 6, 8,
				1, 9, 0
			};
			solveCache[0] = gridCph[(solution[1]%3) + ((solution[2]%4)*3)];
			solveCache[1] = gridCph[(solution[2]%3) + ((solution[0]%4)*3)];
			solveCache[2] = gridCph[(solution[0]%3) + ((solution[1]%4)*3)];
			SetSolution("RJ45");
		}
		if (PortCache.Contains("Serial")) {
			int chunk = Bomb.GetPortCount();
			if (chunk == 0) { chunk = 9; }
			//while (chunk > 3) { chunk = chunk - 4; }
			for (int i = 0; i < 3; i++) {
				int dir = -1;
				for (int j = 0; j < chunk; j++) {
					solveCache[i] = solveCache[i] + (1*dir);
					if (solveCache[i] < solution[i] - 3) { solveCache[i] = solveCache[i] + 2; dir = 1; }
					if (solveCache[i] > solution[i]) { solveCache[i] = solveCache[i] - 2; dir = -1; }
				}
				
			}
			SetSolution("Serial");
		}
		if (PortCache.Contains("StereoRCA")) {
			string serialNum = Bomb.GetSerialNumber();
			int Ser3rd = 0;
			int Ser6th = 0;
			for (int x = 0; x < 6; x++) {
				if (Regex.IsMatch(serialNum[x].ToString(), "[0-9]")) {
					//startingLayer += serialNum[x] - '0';
					if (x == 2) {
						Ser3rd = serialNum[x] - '0';
					} else if (x == 5) {
						Ser6th = serialNum[x] - '0';
					}
				}
			}
			solveCache[0] = (solution[1] + Ser3rd) - (solution[2] + Ser6th);
			solveCache[1] = (solution[2] + Ser3rd) - (solution[0] + Ser6th);
			solveCache[2] = (solution[0] + Ser3rd) - (solution[1] + Ser6th);

			SetSolution("StereoRCA");
		}
	}

	void SetSolution (string cipher) {
		Debug.Log("Cache = " + solveCache[0] + "-" + solveCache[1] + "-" + solveCache[2]);
		for (int i = 0; i < 3; i++) {
			while (solveCache[i] > 11 || solveCache[i] < 0) {
				if (solveCache[i] < 0) { solveCache[i] = 12+solveCache[i]; } else if (solveCache[i] > 11) { solveCache[i] = solveCache[i]-12; }
			}
			solution[i] = solveCache[i];
		}
		Debug.LogFormat("[Wind-Up Safe #{0}] {1} port found // New combination is {2}-{3}-{4}", moduleId, cipher, solution[0], solution[1], solution[2]);
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
			if (modulePassed) { lightMesh[1].material = lightMats[3]; } else { lightMesh[0].material = lightMats[1]; }
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
		if (modulePassed) { return; }
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

	void CheckSolve () {
		if (modulePassed) { return; }
		discard = false;
		startCombo = false;
		if (currentStep == 2) { Debug.LogFormat("[Wind-Up Safe #{0}] Step 3 stored as {1}", moduleId, dialVal); } else if (currentStep != 3 && currentStep != 0) { Debug.LogFormat("[Wind-Up Safe #{0}] Combination discarded", moduleId); }
		if (currentStep == 2 && submit[0] == solution[0] && submit[1] == solution[1] && submit[2] == solution[2]) { Debug.LogFormat("[Wind-Up Safe #{0}] Combination accepted", moduleId); Module.HandlePass(); modulePassed = true; } else if (currentStep == 2) { Debug.LogFormat("[Wind-Up Safe #{0}] Combination incorrect", moduleId); Module.HandleStrike(); }
	}

			// Twitch Plays Support

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} Grab -- Grab key (if applicable) || Turn [0-11] -- Turn to [num]";
#pragma warning restore 414

	bool isValidPos(string n) {
		string[] valids = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11"};
		if (!valids.Contains(n)) { return false; }
		return true;
	}

	IEnumerator ProcessTwitchCommand (string command) {
		yield return null;

		string[] split = command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

		if (split[0].EqualsIgnoreCase("GRAB")) {
			if (split.Length != 1) {
				yield return "sendtochaterror Too many words in command!";
				yield break;
			} else if (!HasKey) {
				yield return "sendtochaterror Key is located somewhere else or was already grabbed";
				yield break;
			}
			KeyHole.OnInteract();
			tpDirection = 1;
			yield break;
		}

		if (split[0].EqualsIgnoreCase("TURN")) {
			//int numberClicks = 0;
			//int pos = 0;
			if (split.Length != 2) {
				yield return "sendtochaterror Too many words in command!";
				yield break;
			} else if (!isValidPos(split[1])) {
				yield return "sendtochaterror " + split[1] + " is not valid";
				yield break;
			} else if (!MasterKey.GlobalKeyHeld && !HasKey) {
				yield return "sendtochaterror Key is located somewhere else";
				yield break;
			}
			int TPTURN = Int32.Parse(split[1]);
			if (!HasKey) { KeyHole.OnInteract(); }
			TurnArrows[tpDirection].OnInteract();
			yield return new WaitForSeconds(0.1f);
			while (dialVal != TPTURN) {
				TurnArrows[tpDirection].OnInteract();
				yield return new WaitForSeconds(0.1f);
			}
			tpDirection += 1;
			if (tpDirection == 2) { tpDirection = 0; }
			yield break;
		}
	}

	void TwitchHandleForcedSolve() { //Autosolver
		StartCoroutine(TPAutosolve());
	}
	
	IEnumerator TPAutosolve () {
		while (!MasterKey.GlobalKeyHeld && !HasKey) { yield return new WaitForSeconds(0.1f); }
		if (HasKey) { KeyHole.OnInteract(); yield return new WaitForSeconds(0.1f); }
		KeyHole.OnInteract();
		tpDirection = 1;
		for (int i = 0; i < 3; i++) {
			TurnArrows[tpDirection].OnInteract();
			yield return new WaitForSeconds(0.1f);
			while (dialVal != solution[i]) {
				TurnArrows[tpDirection].OnInteract();
				yield return new WaitForSeconds(0.1f);
			}

			tpDirection ++;
			if (tpDirection == 2) { tpDirection = 0; }
		}
		KeyHole.OnInteract();
		yield break;
	}
}
