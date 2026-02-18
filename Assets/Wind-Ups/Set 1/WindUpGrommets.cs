using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using KModkit;

public class WindUpGrommets : MonoBehaviour {

	//-----------------------------------------------------//
	public KMBombInfo Bomb;
	public KMAudio Audio;
	public KMBombModule Module;
	public KMGameInfo StateCheck;

	public KMSelectable[] KeyHoles;
	public KMSelectable[] TurnArrows;
	public GameObject[] TurnArrowsTransform;
	public KMSelectable SubmitHole;
	public Material[] FrameMats;
	public Renderer[] FrameForms;

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
		SubmitHole.OnInteract += delegate () { Press(); return false; };
        SubmitHole.OnInteractEnded += delegate () { Release(); };

		//Bomb.OnBombSolved += MasterKey.ResetMaster;
		//Bomb.OnBombExploded += MasterKey.ResetMaster;
		StateCheck.OnStateChange += i => { MasterKey.ResetMaster(); };
	}

	void Start () {
		windID = MasterKey.ServeID(Bomb);

		for (int i = 0; i < 6; i++) { TurnArrowsTransform[i].SetActive(false); }
		for (int i = 0; i < 3; i++) {
			int j = UnityEngine.Random.Range(0, 4);
			dialVals[i] = j;
			KeyRotation[(i)*2].Rotate(0.0f, 0.0f, -30.0f*j);
			KeyRotation[(i)*2+1].Rotate(0.0f, 30.0f*j, 0.0f);
		}

		//if (windID+1 == MasterKey.windIdCounter) { HasKey = 4; MasterKey.GlobalKeyHeld = false; Key[3].SetActive(true); }
		StartCoroutine(CheckKey());
		//Debug.LogFormat("[Wind-Up Grommets #{0}] ID is {1}. First serial is {2}", moduleId, windID, MasterKey.firstSerial);

		InitSolution();
		StartCoroutine(Animate());
	}

	IEnumerator CheckKey () {
		yield return new WaitForSeconds(0.01f);
		if (MasterKey.PlaceKey(windID)) { HasKey = 4; Key[3].SetActive(true); Debug.LogFormat("[Wind-Up Grommets #{0}] Starting with key", moduleId); }
		//Debug.LogFormat("[Wind-Up Grommets #{0}] Max ID is {1}", moduleId, MasterKey.windIdCounter);
	}

	void InitSolution () {
		for (int i = 0; i < 4; i++) { frameVals[i] = UnityEngine.Random.Range(0, 3);}
		frameVals[4] = frameVals[3];
		if (UnityEngine.Random.Range(0, 2) == 1) { while (frameVals[4] == frameVals[3]) { frameVals[4] = UnityEngine.Random.Range(0, 3); } }

		if (debugMode) {
			frameVals[0] = 2;
			frameVals[1] = 2;
			frameVals[2] = 1;
			frameVals[3] = 1;
			frameVals[4] = 2;
		}

		for (int i = 0; i < 4; i++) {
			FrameForms[i*2].material = FrameMats[frameVals[i]];
			FrameForms[i*2+1].material = FrameMats[frameVals[i]];
			if (i == 3) { FrameForms[i*2+2].material = FrameMats[frameVals[4]]; }
		}
		if (conditionCheck(0)) { // All 3 turn pegs are the same
			if (conditionCheck(1)) {
				int j = 2;
				for (int i = 0; i < 3; i++) {
					solution[i] = frameVals[i]-j;
					j--;
				}
				Debug.LogFormat("[Wind-Up Grommets #{0}] Condition 1a met", moduleId);
			} else {
				for (int i = 0; i < 3; i++) {
					solution[i] = matInvert[frameVals[i]]+i;
				}
				Debug.LogFormat("[Wind-Up Grommets #{0}] Condition 1b met", moduleId);
			}
		} else if (conditionCheck(2)) {
			for (int i = 0; i < 3; i++) {
				if (frameVals[i] == 0) { solution[i] = frameVals[3]+frameVals[4]+1; } else { solution[i] = frameVals[i]+i+1; }
			}
			Debug.LogFormat("[Wind-Up Grommets #{0}] Condition 2 met", moduleId);
		} else if (conditionCheck(3)) {
			string serialNum = Bomb.GetSerialNumber();
			int CHECK = 0;
			for (int x = 0; x < 6; x++) {
				if (Regex.IsMatch(serialNum[x].ToString(), "[0-9]")) {
					//startingLayer += serialNum[x] - '0';
					if (x == 2) {
						CHECK += serialNum[x] - '0';
					} else if (x == 5) {
						CHECK += serialNum[x] - '0';
					}
				}
			}
			//Debug.Log(CHECK % 4 + 1);
			for (int i = 0; i < 3; i++) {
				if (frameVals[i] == 0) { solution[i] = Bomb.GetBatteryCount() % 4; }
				if (frameVals[i] == 1) { solution[i] = CHECK % 4; }
				if (frameVals[i] == 2) { solution[i] = Bomb.GetPortCount() % 4; }
			}
			Debug.LogFormat("[Wind-Up Grommets #{0}] Condition 3 met", moduleId);
			
		} else if (conditionCheck(4)) {
			for (int i = 0; i < 3; i++) {
				if (frameVals[i] == 0) { solution[i] = frameVals[i]+i+1; }
				if (frameVals[i] == 1) { solution[i] = frameVals[0]+frameVals[2]+1; }
				if (frameVals[i] == 2) { solution[i] = frameVals[i] + frameVals[4]+1; }
			}
			Debug.LogFormat("[Wind-Up Grommets #{0}] Condition 4 met", moduleId);
		} else {
			for (int i = 0; i < 3; i++) {
				solution[i] = frameVals[i]+frameVals[4]+1;
			}
			Debug.LogFormat("[Wind-Up Grommets #{0}] Condition 5 met", moduleId);
		}

		for (int i = 0; i < 3; i++) {
			if (solution[i] < 0) { solution[i] += 4; }
			if (solution[i] > 3) { solution[i] -= 4; }
		}
		Debug.LogFormat("[Wind-Up Grommets #{0}] Solution is {1}-{2}-{3}", moduleId, solution[0]+1, solution[1]+1, solution[2]+1);
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
		//Debug.Log(direction);
		//Debug.Log(dialVals[HasKey-1]);
		//if (HasKey == 0 || HasKey == 4) { return; }
		//dialVals[HasKey-1]+=1*direction;
		//Debug.Log(direction);
		if ((direction == -1 && dialVals[HasKey-1] != 0) || (direction == 1 && dialVals[HasKey-1] != 3)) {
			dialVals[HasKey-1] += 1*direction;
			turnFrame = 3;
			Audio.PlaySoundAtTransform("Key_Out", transform);
		} else {
			//Why is this here???
		}
	}

	void Press () {
		held = true;
	}

	void Release () {
		if (heldFrame < 10) {
			if (MasterKey.GlobalKeyHeld && HasKey == 0) {
				MasterKey.GlobalKeyHeld = false;
				HasKey = 4;
				Key[3].SetActive(true);
				Audio.PlaySoundAtTransform("Key_In", transform);
			} else if (HasKey == 4) {
				MasterKey.GlobalKeyHeld = true;
				HasKey = 0;
				Key[3].SetActive(false);
				Audio.PlaySoundAtTransform("keys_01", transform);
			}
		}
		held = false;
		//heldFrame = 0;
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

			yield return new WaitForSeconds(0.01f);
		}
	}

	bool conditionCheck (int condition) {
		int[] CHECK = new int[] {0, 0, 0};
		if (condition == 0) {
			if (frameVals[0] == frameVals[1] && frameVals[1] == frameVals[2]) { return true; }

		} else if (condition == 1) {
			if (frameVals[3] == frameVals[4]) { return true; }

		} else if (condition == 2) {
			for (int i = 0; i < 5; i++) {
				if (frameVals[i] == 0) { CHECK[0]++; }
			}
			if (CHECK[0] >= 3) { return true; }

		} else if (condition == 3) {
			for (int i = 0; i < 5; i++) {
				CHECK[frameVals[i]]++;
			}
			if (CHECK[0] == 0 || CHECK[1] == 0 || CHECK[2] == 0 ) { return true; }

		} else if (condition == 4) {
			if (frameVals[0] != frameVals[1] && frameVals[0] != frameVals[2] && frameVals[1] != frameVals[2]) { return true; }

		} else if (condition == 5) { // Unused // Submit peg and washer are different
			if (frameVals[3] != frameVals[4]) { return true; }

		}
		return false;
	}

	void CheckSolve () {
		Debug.LogFormat("[Wind-Up Grommets #{0}] Submitted {1}-{2}-{3}", moduleId, dialVals[0]+1, dialVals[1]+1, dialVals[2]+1);
		if (dialVals[0] == solution[0] && dialVals[1] == solution[1] && dialVals[2] == solution[2]) {
			Debug.LogFormat("[Wind-Up Grommets #{0}] Module Passed", moduleId);
			moduleSolved = true;
			if (!tpOverride) { Module.HandlePass(); }
		} else {
			Debug.LogFormat("[Wind-Up Grommets #{0}] Module Striked", moduleId);
			if (!tpOverride) { Module.HandleStrike(); }
		}
	}
	
			// Twitch Plays Support

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} Grab -- Grab key (if applicable) || Set [1/2/3] [1-4] -- Set the dials || Submit -- submits the combination";
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
		if (HasKey == 4) {
			SubmitHole.OnInteract();
			SubmitHole.OnInteractEnded();
			yield return new WaitForSeconds(0.1f);
		}
		KeyHoles[HOLE].OnInteract();
		yield return new WaitForSeconds(0.1f);
		yield break;
	}

	void TwitchEndCheck() {
		SubmitHole.OnInteract();
		SubmitHole.OnInteractEnded();
		if (moduleSolved) { Module.HandlePass(); } else { Module.HandleStrike(); }
		tpOverride = false;
	}

	IEnumerator ProcessTwitchCommand (string command) {
		yield return null;

		string[] split = command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

		if (split[0].EqualsIgnoreCase("GRAB")) {
			if (split.Length != 1) {
				yield return "sendtochaterror Too many words in command!";
				yield break;
			} else if (HasKey != 4) {
				yield return "sendtochaterror Key is located somewhere else or was already grabbed";
				yield break;
			}
			SubmitHole.OnInteract();
			SubmitHole.OnInteractEnded();
			yield break;
		}

		if (split[0].EqualsIgnoreCase("SUBMIT")) {
			if (split.Length != 1) {
				yield return "sendtochaterror Too many words in command!";
				yield break;
			} else if (!MasterKey.GlobalKeyHeld && HasKey == 0) {
				yield return "sendtochaterror Key is located somewhere else";
				yield break;
			}
			tpOverride = true;
			if (HasKey == 0) { SubmitHole.OnInteract(); SubmitHole.OnInteractEnded(); yield return new WaitForSeconds(0.1f); }
			SubmitHole.OnInteract();
			yield return new WaitForSeconds(0.3f);			
			SubmitHole.OnInteractEnded();
			yield return new WaitForSeconds(0.1f);
			TwitchEndCheck();
			yield break;
		}

		if (split[0].EqualsIgnoreCase("SET")) {
			//int numberClicks = 0;
			//int pos = 0;
			if (split.Length != 3) {
				yield return "sendtochaterror Too many words in command!";
				yield break;
			} else if (!isValidPos(split[1], 1)) {
				yield return "sendtochaterror " + split[1] + " is not valid";
				yield break;
			} else if (!isValidPos(split[2], 0)) {
				yield return "sendtochaterror " + split[2] + " is not valid";
				yield break;
			} else if (!MasterKey.GlobalKeyHeld && HasKey == 0) {
				yield return "sendtochaterror Key is located somewhere else";
				yield break;
			}
			int TPKEY = Int32.Parse(split[1]) - 1;
			int TPTURN = Int32.Parse(split[2]) - 1;
			
			StartCoroutine(TwitchPlaceKey(TPKEY));
			yield return new WaitForSeconds(0.3f);
			while (dialVals[TPKEY] != TPTURN) {
				if (dialVals[TPKEY] > TPTURN) {
					TurnArrows[TPKEY*2].OnInteract();
				} else if (dialVals[TPKEY] < TPTURN) {
					TurnArrows[TPKEY*2+1].OnInteract();
				}
				yield return new WaitForSeconds(0.3f);
			}
			KeyHoles[TPKEY].OnInteract();
			yield break;
		}
	}

	void TwitchHandleForcedSolve() { //Autosolver
		tpOverride = true;
		StartCoroutine(TPAutosolve());
	}
	
	IEnumerator TPAutosolve () {
		while (!MasterKey.GlobalKeyHeld && HasKey == 0) { yield return new WaitForSeconds(0.1f); }
		if (HasKey == 4) { SubmitHole.OnInteract(); SubmitHole.OnInteractEnded(); yield return new WaitForSeconds(0.1f); }
		for (int i = 0; i < 3; i++) {
			KeyHoles[i].OnInteract();
			yield return new WaitForSeconds(0.1f);

			while (dialVals[i] != solution[i]) {
				if (dialVals[i] > solution[i]) {
					TurnArrows[i*2].OnInteract();
				} else if (dialVals[i] < solution[i]) {
					TurnArrows[i*2+1].OnInteract();
				}
				yield return new WaitForSeconds(0.3f);
			}
			KeyHoles[i].OnInteract();
		}

		SubmitHole.OnInteract();
		SubmitHole.OnInteractEnded();
		yield return new WaitForSeconds(0.1f);
		SubmitHole.OnInteract();
		yield return new WaitForSeconds(0.3f);			
		SubmitHole.OnInteractEnded();
		yield return new WaitForSeconds(0.1f);

		SubmitHole.OnInteract();
		SubmitHole.OnInteractEnded();
		Module.HandlePass();
		yield break;
	}
}
