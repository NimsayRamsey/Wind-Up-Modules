using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using KModkit;

public class WindUpSimon : MonoBehaviour {

	public KMBombInfo Bomb;
	public KMAudio Audio;
	public KMBombModule Module;
	public KMGameInfo StateCheck;
	public KMColorblindMode Colorblind;

	public KMSelectable KeyHole;
	public KMSelectable[] TurnArrows;
	public GameObject[] TurnArrowsTransform;

	public GameObject Key;
	public Transform[] KeyRotation;

	public KMSelectable[] Buttons; //Blue, Yellow, Green, Red
	public GameObject[] ButtonRend;
	public TextMesh[] ButtonLabels;

	public Material[] StageMats;//B, Blit, Y, Ylit, G, Glit, R, Rlit
	public Renderer[] StageForms;//Blue, Yellow, Green, Red
	public Material LightMatExtra;//grey
	public Renderer LightForm;
	
	public GameObject ColorblindDisplay;
	public TextMesh ColorblindText;

	public bool debugMode;
	
	//-----------------------------------------------------//
	private int turnFrame = 0;
	private int direction = 0;

	private int viewStage = 0;
	private int[] submitSequence = new int[] {4, 4, 4, 4};
	private int[] targetSequence = new int[] {4, 4, 4, 4};
	private int[] shownSequence = new int[] {4, 4, 4, 4};
	private int submitPos = 0;

	private string[] colorLabels = new string[] { "B", "Y", "G", "R" };
	private bool colorFlash = false;
	private int colorTimer = 0;

	private string[,] glyphLabels = new string[,] {
		{"K", "(", "q", "/", "\"", ".", "M", "V", "A", "6", "J", "B", "Q", "o"}, // Set A
		{"e", "w", "x", "9", ";", "b", "C", "c", "z", "S", "Y", "F", "k", "n"}, // Set B
		{"G", "R", "h", "8", "!", ":", "L", "a", "U", "T", "W", "D", "X", "r"}, // Set C
		{"f", ")", "3", "1", "$", "H", "%", "?", "@", "y", "v", "E", "l", "s"}  // Set D
	};

	private int[] shuffleGrid = new int[] {
		1, 3, 0, 2,
		2, 0, 3, 1,
		3, 0, 1, 2,
		2, 1, 0, 3
	};

	//-----------------------------------------------------//
	//SHARED INFORMATION
	private bool HasKey = false;
	int windID = 0;

	private bool tpOverride = false;
	private bool cbActive;
	private bool moduleSolved = false;
	//-----------------------------------------------------//
	static int moduleIdCounter = 1;
	int moduleId;
	//-----------------------------------------------------//

	private void Awake () {
		moduleId = moduleIdCounter++;

		KeyHole.OnInteract += delegate () { PlaceKey(); return false; };
		foreach (KMSelectable NAME in TurnArrows) {
			KMSelectable pressedObject = NAME;
			NAME.OnInteract += delegate () { Turn(pressedObject); return false; };
		}
		
		foreach (KMSelectable NAME in Buttons) {
			KMSelectable pressedObject = NAME;
			NAME.OnInteract += delegate () { Press(pressedObject); return false; };
		}

		cbActive = Colorblind.ColorblindModeActive;
		if (cbActive) {ColorblindDisplay.SetActive(true);} else {ColorblindDisplay.SetActive(false);}
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
		/*Piece of crap Modkit pt2
			I do not understand why these two modules have decided to give me problems.
		*/
		yield return new WaitForSeconds(0.01f);
		TurnArrowsTransform[0].SetActive(false);
		TurnArrowsTransform[1].SetActive(false);
	}

	IEnumerator CheckKey () {
		yield return new WaitForSeconds(0.01f);
		if (MasterKey.PlaceKey(windID)) {
			HasKey = true;
			Key.SetActive(true);
			TurnArrowsTransform[0].SetActive(true);
			TurnArrowsTransform[1].SetActive(true);
			foreach (GameObject button in ButtonRend) { button.SetActive(false); }
			Debug.LogFormat("[Wind-Up Simon #{0}] Starting with key", moduleId);
		}
		//Debug.LogFormat("[Wind-Up Grommets #{0}] Max ID is {1}", moduleId, MasterKey.windIdCounter);
	}

	void InitSolution () {
		int[] sequenceOrder = new int[] {4, 4, 4, 4};
		for (int i = 0; i < 4; i++) {
			int T = UnityEngine.Random.Range(0, 4);
			while (sequenceOrder.Contains(T)) { T = UnityEngine.Random.Range(0, 4); }
			sequenceOrder[i] = T;
		}
		Debug.LogFormat("[Wind-Up Simon #{0}] Shown Order is {1}-{2}-{3}-{4}", moduleId, sequenceOrder[0]+1, sequenceOrder[1]+1, sequenceOrder[2]+1, sequenceOrder[3]+1);
		for (int i = 0; i < 4; i++) {
			targetSequence[sequenceOrder[i]] = UnityEngine.Random.Range(0, 4);
			shownSequence[i] = targetSequence[sequenceOrder[i]];
			//Debug.Log(shownSequence[i]);
			ButtonLabels[i].text = glyphLabels[sequenceOrder[i], UnityEngine.Random.Range(0, 14)];
			//Debug.Log(sequenceOrder[i] + " = " + sequenceOrder[i] + " = " + targetSequence[sequenceOrder[i]]);
		}
		Debug.LogFormat("[Wind-Up Simon #{0}] Shown Sequence is {1}-{2}-{3}-{4}", moduleId, colorLabels[targetSequence[sequenceOrder[0]]], colorLabels[targetSequence[sequenceOrder[1]]], colorLabels[targetSequence[sequenceOrder[2]]], colorLabels[targetSequence[sequenceOrder[3]]]);

		for (int i = 0; i < 4; i++) {
			targetSequence[i] = shuffleGrid[targetSequence[i]+(i*4)];
		}
		Debug.LogFormat("[Wind-Up Simon #{0}] Target Sequence is {1}-{2}-{3}-{4}", moduleId, colorLabels[targetSequence[0]], colorLabels[targetSequence[1]], colorLabels[targetSequence[2]], colorLabels[targetSequence[3]]);
	}
	
	void PlaceKey () {
		if (MasterKey.GlobalKeyHeld && !HasKey) {
			MasterKey.GlobalKeyHeld = false;
			HasKey = true;
			Key.SetActive(true);
			TurnArrowsTransform[0].SetActive(true);
			TurnArrowsTransform[1].SetActive(true);
			foreach (GameObject button in ButtonRend) { button.SetActive(false); }
			Audio.PlaySoundAtTransform("Key_In", transform);
		} else if (HasKey) {
			MasterKey.GlobalKeyHeld = true;
			HasKey = false;
			Key.SetActive(false);
			TurnArrowsTransform[0].SetActive(false);
			TurnArrowsTransform[1].SetActive(false);
			foreach (GameObject button in ButtonRend) { button.SetActive(true); }
			Audio.PlaySoundAtTransform("keys_01", transform);
		}
	}

	void Turn (KMSelectable Arrow) {
		int arrowNum = Array.IndexOf(TurnArrows, Arrow);
		if (!HasKey) { return; }
		if (turnFrame != 0) { return; }
		direction = arrowNum;
		if (direction == 0) { direction = -1; }
		turnFrame = 9;
		Audio.PlaySoundAtTransform("Key_Out", transform);
		submitPos = 0;
		Audio.PlaySoundAtTransform("Change Stage", transform);
		LightStage();
	}

	void Press(KMSelectable Button) {
		int buttonNum = Array.IndexOf(Buttons, Button);
		submitSequence[submitPos] = buttonNum;
		Audio.PlaySoundAtTransform("Enter Color", transform);
		submitPos++;
		if (submitPos == 4) {
			CheckSolve();
			submitPos = 0;
		}
	}

	void LightStage() {
		viewStage += direction;
		if (viewStage < 0) { viewStage = 3; } else if (viewStage > 3) { viewStage = 0; }
		for(int i = 0; i < 4; i++) {
			StageForms[i].material = StageMats[i*2];
			if (viewStage == i) { StageForms[i].material = StageMats[i*2+1]; }
		}
		
		LightForm.material = LightMatExtra;
		ColorblindText.text = "";
		colorFlash = false;
		colorTimer = 0;
	}

	void CheckSolve() {
		if (moduleSolved) { return; }
		Debug.LogFormat("[Wind-Up Simon #{0}] Submitted Sequence is {1}-{2}-{3}-{4}", moduleId, colorLabels[submitSequence[0]], colorLabels[submitSequence[1]], colorLabels[submitSequence[2]], colorLabels[submitSequence[3]]);
		for (int i = 0; i < 4; i++) {
			if (submitSequence[i] != targetSequence[i]) {
				Debug.LogFormat("[Wind-Up Simon #{0}] Module Striked", moduleId);
				Module.HandleStrike();
				return;
			}
		}
		Debug.LogFormat("[Wind-Up Simon #{0}] Module Passed", moduleId);
		moduleSolved = true;
		if (!tpOverride) { Module.HandlePass(); }
	}

	IEnumerator Animate () {
		while(true){
			if (turnFrame != 0) {
				KeyRotation[0].Rotate(0.0f, 0.0f, -10.0f*direction);
				KeyRotation[1].Rotate(0.0f, 10.0f*direction, 0.0f);
				turnFrame--;
			}

			if (!moduleSolved) {
				colorTimer++;
				if (!colorFlash && colorTimer == 40) {
					colorFlash = !colorFlash;
					LightForm.material = StageMats[shownSequence[viewStage]*2+1];//targetSequence[
					ColorblindText.text = colorLabels[shownSequence[viewStage]];

					colorTimer = 0;
				} else if (colorFlash && colorTimer == 20) {
					colorFlash = !colorFlash;
					LightForm.material = LightMatExtra;
					ColorblindText.text = "";

					colorTimer = 0;
				}
			} else {
				LightForm.material = LightMatExtra;
				ColorblindText.text = "";
			}

			yield return new WaitForSeconds(0.01f);
		}
	}
	
			// Twitch Plays Support

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} Grab -- Grab key (if applicable) || Check [1-4] -- checks 1-4. Starts at Blue/Up clockwise || Submit [B/Y/G/R]x4 -- inputs a sequence of 4 button presses. No more, no less.";
#pragma warning restore 414

	bool isValidPos(string n, int SET) {
		string[] valids = new string[] {};
		if (SET == 0) {
			valids = new string[] { "1", "2", "3", "4"};
		} else {
			valids = new string[] { "B", "Y", "G", "R"};
		}
		if (!valids.Contains(n)) { return false; }
		return true;
	}

	int tpPose(string C) {
		string[] charList = new string[] { "B", "Y", "G", "R"};
		return Array.IndexOf(charList, C);
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
			yield break;
		}

		if (split[0].EqualsIgnoreCase("CHECK")) {
			if (split.Length != 2) {
				yield return "sendtochaterror Too many words in command!";
				yield break;
			} else if (!isValidPos(split[1], 0)) {
				yield return "sendtochaterror " + split[1] + " is not valid";
				yield break;
			} else if (!MasterKey.GlobalKeyHeld && !HasKey) {
				yield return "sendtochaterror Key is located somewhere else";
				yield break;
			}
			if (!HasKey) { KeyHole.OnInteract(); }
			while (viewStage != Int32.Parse(split[1])-1) {
				TurnArrows[1].OnInteract();
				yield return new WaitForSeconds(0.2f);
			}
			KeyHole.OnInteract();
			yield break;
		}

		if (split[0].EqualsIgnoreCase("SUBMIT")) {
			//int numberClicks = 0;
			//int pos = 0;
			List<int> TPCODE = new List<int> {};
			if (split.Length != 5) {
				yield return "sendtochaterror Incorrect Length";
				yield break;
			} else {
				for (int i = 1; i < 5; i++) {
					if (!isValidPos(split[i], 1)) {
						yield return "sendtochaterror " + split[i] + " is not valid";
						yield break;
					} else {
						TPCODE.Add(tpPose(split[i]));
					}
				}
			}
			if (HasKey) { KeyHole.OnInteract(); }
			for (int i = 0; i < 4; i++) {
				Buttons[TPCODE[i]].OnInteract();
				yield return new WaitForSeconds(0.1f);
			}
			yield break;
		}
	}

	void TwitchHandleForcedSolve() { //Autosolver
		StartCoroutine(TPAutosolve());
	}
	
	IEnumerator TPAutosolve () {
		if (HasKey) { KeyHole.OnInteract(); }
		for (int i = 0; i < 4; i++) {
			Buttons[targetSequence[i]].OnInteract();
			yield return new WaitForSeconds(0.1f);
		}
		yield break;
	}
}
