using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using KModkit;

public class WindUpCombination : MonoBehaviour {

	//-----------------------------------------------------//
	public KMBombInfo Bomb;
	public KMAudio Audio;
	public KMBombModule Module;
	public KMGameInfo StateCheck;

	public KMSelectable[] KeyHoles;
	public KMSelectable[] TurnArrows;
	public GameObject[] TurnArrowsTransform;
	public Material[] LedMats;
	public Renderer[] LEDs;

	public GameObject[] Key;
	public Transform[] KeyRotation;

	public bool debugMode;

	//-----------------------------------------------------//
	private int heldFrame = 0;
	private bool held = false;

	private int[] dialVals = new int[] {0, 0, 0};
	private int[] solution = new int[] {0, 0, 0};
	private int turnFrame = 0;
	private int direction = 0;

	private int[] frameVals = new int[] {0, 0, 0, 0, 0};
	private int[] matInvert = new int[] {3, 2, 1};

	//-----------------------------------------------------//
	//INVALID LIBRARY
	private int[,] strikeCoords = new int[,] {
		{1, 0, 0}, {2, 0, 0}, {0, 0, 1}, {1, 0, 2}, {2, 0, 2}, {1, 0, 3}, {3, 0, 3},
		{3, 1, 0}, {1, 1, 1}, {3, 1, 1}, {1, 1, 2}, {0, 1, 3}, {3, 1, 3}, {1, 2, 0},
		{2, 2, 0}, {0, 2, 1}, {3, 2, 1}, {0, 2, 2}, {2, 2, 2}, {0, 2, 3}, {2, 2, 3},
		{2, 3, 0}, {0, 3, 1}, {1, 3, 1}, {1, 3, 2}, {2, 3, 2}, {3, 3, 3}
	};

	private int[,] startCoords = new int[,] {
		{0, 0, 0}, {0, 0, 3}, {2, 3, 3}, {1, 2, 1}, {3, 2, 0}, {3, 2, 3}, {1, 3, 0}, {2, 1, 2},
		{3, 0, 0}, {1, 0, 1}, {0, 3, 2}, {2, 0, 3} //Finish Coords ONLY
	};

	//-----------------------------------------------------//
	//SHARED INFORMATION
	private int HasKey = 0;
	int windID = 0;

	private bool tpOverride = false;
	private bool moduleSolved = false;
	//-----------------------------------------------------//
	static int moduleIdCounter = 1;
	int moduleId;
	//-----------------------------------------------------//

	private void Awake () {
		moduleId = moduleIdCounter++;

		foreach (KMSelectable NAME in KeyHoles) {
			KMSelectable pressedObject = NAME;
			NAME.OnInteract += delegate () { PlaceKey(pressedObject); return false; };
		}
		foreach (KMSelectable NAME in TurnArrows) {
			KMSelectable pressedObject = NAME;
			NAME.OnInteract += delegate () { Turn(pressedObject); return false; };
		}

		//Bomb.OnBombSolved += MasterKey.ResetMaster;
		//Bomb.OnBombExploded += MasterKey.ResetMaster;
		StateCheck.OnStateChange += i => { MasterKey.ResetMaster(); };
	}

	void Start () {
		windID = MasterKey.ServeID(Bomb);

		StartCoroutine(GoFuckYourself());
		StartCoroutine(CheckKey());
		
		InitSolution();
		StartCoroutine(Animate());
	}

	IEnumerator GoFuckYourself() {
		/*Piece of crap Modkit pt3
			Delays the transform hiding so the module doesn't shit itself and die
		*/
		yield return new WaitForSeconds(0.01f);
		for (int i = 0; i < 6; i++) { TurnArrowsTransform[i].SetActive(false); }
	}

	IEnumerator CheckKey () {
		yield return new WaitForSeconds(0.01f);
		if (MasterKey.PlaceKey(windID)) {
			HasKey = 2; Key[1].SetActive(true);
			TurnArrowsTransform[2].SetActive(true);
			TurnArrowsTransform[3].SetActive(true);
			Debug.LogFormat("[Wind-Up Combination #{0}] Starting with key", moduleId);
		}
	}

	void InitSolution () {
		int S = UnityEngine.Random.Range(0, 8); //Start Position
		for (int i = 0; i < 3; i++) {
			dialVals[i] = startCoords[S, i];
			KeyRotation[(i)*2].Rotate(0.0f, 0.0f, -30.0f*dialVals[i]);
			KeyRotation[(i)*2+1].Rotate(0.0f, 30.0f*dialVals[i], 0.0f);
		}
		Debug.LogFormat("[Wind-Up Combination #{0}] Starting values are {1}-{2}-{3}", moduleId, dialVals[0]+1, dialVals[1]+1, dialVals[2]+1);
		
		int F = UnityEngine.Random.Range(0, 12); //Finish Position
		while (S == F) {
			F = UnityEngine.Random.Range(0, 12);
			if (S == 7 && F == 11) { F = UnityEngine.Random.Range(0, 12); }
		}
		for (int i = 0; i < 3; i++) {
			solution[i] = startCoords[F, i];
			LEDs[solution[i]+(4*i)].material = LedMats[1];
		}
		Debug.LogFormat("[Wind-Up Combination #{0}] Target values are {1}-{2}-{3}", moduleId, solution[0]+1, solution[1]+1, solution[2]+1);
		
	}

	void PlaceKey (KMSelectable Keyhole) {
		int keyNum = Array.IndexOf(KeyHoles, Keyhole);
		if (MasterKey.GlobalKeyHeld && HasKey == 0) {
			MasterKey.GlobalKeyHeld = false;
			HasKey = keyNum+1;
			Key[keyNum].SetActive(true);
			TurnArrowsTransform[keyNum*2].SetActive(true);
			TurnArrowsTransform[keyNum*2+1].SetActive(true);
			Audio.PlaySoundAtTransform("Key_In", transform);
		} else if (HasKey == keyNum+1) {
			MasterKey.GlobalKeyHeld = true;
			HasKey = 0;
			Key[keyNum].SetActive(false);
			TurnArrowsTransform[keyNum*2].SetActive(false);
			TurnArrowsTransform[keyNum*2+1].SetActive(false);
			Audio.PlaySoundAtTransform("keys_01", transform);
		}
	}

	void Turn (KMSelectable Arrow) {
		int arrow = Array.IndexOf(TurnArrows, Arrow);
		if (HasKey-1 != arrow/2) { return; }
		if (turnFrame != 0) { return; }

		direction = (arrow % 2)-1;
		if (direction == 0) { direction = 1; }
		if ((direction == -1 && dialVals[HasKey-1] != 0) || (direction == 1 && dialVals[HasKey-1] != 3)) {
			
			if (!moduleSolved) { if (!CheckValid()) { Arrow.AddInteractionPunch(); return; } }

			dialVals[HasKey-1] += 1*direction;
			turnFrame = 3;
			Audio.PlaySoundAtTransform("Key_Out", transform);
		} else {
			//Why is this here???
		}
	}

	bool CheckValid () {
		int[] checkMod = new int[] {dialVals[0], dialVals[1], dialVals[2]};
		checkMod[HasKey-1] += 1*direction;
		//Debug.Log(strikeCoords.Length);
		for (int i = 0; i < 27; i++) {
			//Debug.Log(i);
			if (checkMod[0] == strikeCoords[i, 0] && checkMod[1] == strikeCoords[i, 1] && checkMod[2] == strikeCoords[i, 2]) {
				Debug.LogFormat("[Wind-Up Combination #{0}] Invalid move from {1}-{2}-{3} to {4}-{5}-{6}", moduleId, dialVals[0]+1, dialVals[1]+1, dialVals[2]+1, checkMod[0]+1, checkMod[1]+1, checkMod[2]+1);
				if (!tpOverride) { Module.HandleStrike(); }
				return false;
			}
		}
		return true;
	}

	IEnumerator Animate () {
		while(true){
			//Debug.Log(heldFrame);
			if (held && heldFrame < 15 && HasKey == 4) { heldFrame += 1; }
			if (HasKey == 4 && held && heldFrame > 9 && heldFrame < 15) {
				KeyRotation[6].Rotate(0.0f, 0.0f, -8.0f);
				KeyRotation[7].Rotate(0.0f, 8.0f, 0.0f);
				if (heldFrame == 10) { Audio.PlaySoundAtTransform("door_open_01", transform); }
				if (heldFrame == 14) { CheckSolve(); }
			} else if (!held && heldFrame > 9) {
				if (heldFrame == 15) { heldFrame--; }
				KeyRotation[6].Rotate(0.0f, 0.0f, 8.0f);
				KeyRotation[7].Rotate(0.0f, -8.0f, 0.0f);
				heldFrame -= 1;
				if (heldFrame == 9) { heldFrame = 0; }
			} else if (!held && heldFrame < 10) { heldFrame = 0; }
			
			if (turnFrame != 0) {
				KeyRotation[(HasKey-1)*2].Rotate(0.0f, 0.0f, -10.0f*direction);
				KeyRotation[(HasKey-1)*2+1].Rotate(0.0f, 10.0f*direction, 0.0f);
				turnFrame--;
			}
			if (turnFrame == 1) {
				CheckSolve();
			}

			yield return new WaitForSeconds(0.01f);
		}
	}

	void CheckSolve () {
		if (moduleSolved) { return; }
		if (dialVals[0] == solution[0] && dialVals[1] == solution[1] && dialVals[2] == solution[2]) {
			Debug.LogFormat("[Wind-Up Combination #{0}] Module Passed", moduleId);
			moduleSolved = true;
			if (!tpOverride) { Module.HandlePass(); }
		} else {
			Debug.LogFormat("[Wind-Up Combination #{0}] Moved to {1}-{2}-{3}", moduleId, dialVals[0]+1, dialVals[1]+1, dialVals[2]+1);
		}
	}
	
			// Twitch Plays Support

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} Grab -- Grab key (if applicable) || Set [1/2/3] [1-4] -- Set the dials";
#pragma warning restore 414

	bool isValidPos(string n, int SET) {
		string[] valids = new string[] {};
		if (SET == 0) {
			valids = new string[] { "1", "2", "3", "4"};
		} else {
			valids = new string[] { "1", "2", "3"};
		}
		if (!valids.Contains(n)) { return false; }
		return true;
	}

	IEnumerator TwitchPlaceKey(int HOLE) {
		KeyHoles[HOLE].OnInteract();
		yield return new WaitForSeconds(0.1f);
		yield break;
	}

	void TwitchEndCheck() {
		if (moduleSolved) { Module.HandlePass(); } else { Module.HandleStrike(); }
		tpOverride = false;
	}

	IEnumerator ProcessTwitchCommand (string command) {
		yield return null;

		string[] split = command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
	}

	void TwitchHandleForcedSolve() { //Autosolver
		tpOverride = true;
		StartCoroutine(TPAutosolve());
	}
	
	IEnumerator TPAutosolve () {
		while (!MasterKey.GlobalKeyHeld && HasKey == 0) { yield return new WaitForSeconds(0.1f); }
		
		Module.HandlePass();
		yield break;
	}
}
