using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using KModkit;

public class WindUpMusicBox : MonoBehaviour {

	//-----------------------------------------------------//
	public KMBombInfo Bomb;
	public KMAudio Audio;
	public KMBombModule Module;
	public KMGameInfo StateCheck;

	public KMSelectable KeyHole;
	public KMSelectable[] Buttons;

	public GameObject Key;
	public Transform[] KeyRotation;

	public bool debugMode;
	public int debugSong;

	//-----------------------------------------------------//
	private int heldFrame = 0;
	private bool held = false;
	private bool holdFlop = false;

	private int WindTimer = 0;
	private int tickTimer = 230;

	private string[] buttonSounds = new string[] {"Note 1", "Note 2", "Note 3", "Note 4"};

	private int[] stored = new int[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
	private List<List<int>> keys = new List<List<int>> {
		new List<int> {6, 6, 6, 6, 6, 6}, new List<int> {6, 6, 6, 6, 6, 6}
	};
	private int playback = 0;
	private int[] shuffled = new int[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
	private int step = 0;
	private int[] submit = new int[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};

	private int[] noteCount = new int[] {0, 0, 0, 0};

	private string[] logSym = new string[] {"♩", "♪", "♫", "♬"};

	private string[] alphabet = new string[] {//The Alphabet
		"A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"
	  // 1    2    3    4    5    6    7    8    9    10   11   12   13   14   15   16   17   18   19   20   21   22   23   24   25   26
	};

	private bool moduleSolved = false;

	//-----------------------------------------------------//
	//SHARED INFORMATION
	private bool HasKey = false;
	int windID = 0;

	//-----------------------------------------------------//
	static int moduleIdCounter = 1;
	int moduleId;
	//-----------------------------------------------------//

	private void Awake () {
		moduleId = moduleIdCounter++;

		foreach (KMSelectable NAME in Buttons) {
			KMSelectable pressedObject = NAME;
			NAME.OnInteract += delegate () { PressButton(pressedObject); return false; };
		}
		KeyHole.OnInteract += delegate () { Press(); return false; };
        KeyHole.OnInteractEnded += delegate () { Release(); };

		StateCheck.OnStateChange += i => { MasterKey.ResetMaster(); };
	}

	void Start () {
		windID = MasterKey.ServeID(Bomb);
		StartCoroutine(CheckKey());

		//Audio.PlaySoundAtTransform("Gamecube", transform);
		InitSolution();
		//Debug.LogFormat("[Wind-Up Music Box #{0}] Chosen song is {1}", moduleId, SongNames[ChosenSong]);
		StartCoroutine(Animate());
	}

	IEnumerator CheckKey () {
		yield return new WaitForSeconds(0.01f);
		if (MasterKey.PlaceKey(windID)) { HasKey = true; Key.SetActive(true); Debug.LogFormat("[Wind-Up Music Box #{0}] Starting with key", moduleId); }
		//Debug.LogFormat("[Wind-Up Lockpick #{0}] Max ID is {1}", moduleId, MasterKey.windIdCounter);
	}

	void InitSolution () {
		for (int i = 0; i < 12; i++){
			stored[i] = UnityEngine.Random.Range(0, 4);
			shuffled[i] = stored[i];
			noteCount[stored[i]]++;
		}
		GenerateKeys();
		SwapMatches();

		//if (debugMode) { ChosenSong = debugSong; }
		LogResult(0);
		LogResult(1);
		LogResult(2);
	}

	void LogResult(int ID) {
		if (ID == 0) {
			Debug.LogFormat("[Wind-Up Music Box #{0}] Encrypted sheet is {1}-{2}-{3}-{4}-{5}-{6}  {7}-{8}-{9}-{10}-{11}-{12}", moduleId, logSym[shuffled[0]], logSym[shuffled[1]], logSym[shuffled[2]], logSym[shuffled[3]], logSym[shuffled[4]], logSym[shuffled[5]], logSym[shuffled[6]], logSym[shuffled[7]], logSym[shuffled[8]], logSym[shuffled[9]], logSym[shuffled[10]], logSym[shuffled[11]]);
		} else if (ID == 1) {
			Debug.LogFormat("[Wind-Up Music Box #{0}] Keys are {1}-{2}-{3}-{4}-{5}-{6}  {7}-{8}-{9}-{10}-{11}-{12}", moduleId, keys[0] [0], keys[0] [1], keys[0] [2], keys[0] [3], keys[0] [4], keys[0] [5], keys[1] [0], keys[1] [1], keys[1] [2], keys[1] [3], keys[1] [4], keys[1] [5]);
		} else if (ID == 2) {
			Debug.LogFormat("[Wind-Up Music Box #{0}] Solution sheet is {1}-{2}-{3}-{4}-{5}-{6}  {7}-{8}-{9}-{10}-{11}-{12}", moduleId, logSym[stored[0]], logSym[stored[1]], logSym[stored[2]], logSym[stored[3]], logSym[stored[4]], logSym[stored[5]], logSym[stored[6]], logSym[stored[7]], logSym[stored[8]], logSym[stored[9]], logSym[stored[10]], logSym[stored[11]]);
		} else {
			Debug.LogFormat("[Wind-Up Music Box #{0}] Submitted {1}-{2}-{3}-{4}-{5}-{6}  {7}-{8}-{9}-{10}-{11}-{12}", moduleId, logSym[submit[0]], logSym[submit[1]], logSym[submit[2]], logSym[submit[3]], logSym[submit[4]], logSym[submit[5]], logSym[submit[6]], logSym[submit[7]], logSym[submit[8]], logSym[submit[9]], logSym[submit[10]], logSym[submit[11]]);
		}
	}

	void GenerateKeys() {
		int T = 0;
		for (int i = 0; i < 6; i++){
			T = ReturnSerialPos(i);
			CycleKey(T, 0, i);
		}

		keys[1][0] = noteCount[0] % 6;
		CycleKey(VowelCount(), 1, 1);
		CycleKey(noteCount[1] % 6, 1, 2);
		CycleKey(Bomb.GetPortCount() % 6, 1, 3);
		CycleKey(noteCount[3] % 6, 1, 4);
		CycleKey(noteCount[2] % 6, 1, 5);
	}

	void CycleKey (int IN, int i, int j) {
		if (keys[i].Contains(IN)){
			IN++;
			if (IN > 5) { IN = 0; }
			while (keys[i].Contains(IN)) {
				IN++;
				if (IN > 5) { IN = 0; }
			}
		}
		keys[i][j] = IN;
	}

	int VowelCount() {
		int OUT = 0;
		List<string> indicators = Bomb.GetIndicators().ToList();
		foreach(string label in indicators) {
			OUT += label.Count(t => t == 'A');
			OUT += label.Count(t => t == 'E');
			OUT += label.Count(t => t == 'I');
			OUT += label.Count(t => t == 'O');
			OUT += label.Count(t => t == 'U');
		}
		OUT = OUT % 6;
		return OUT;
	}

	int ReturnSerialPos (int ID) {
		int OUT = 0;
		string serialNum = Bomb.GetSerialNumber();
		if (Regex.IsMatch(serialNum[ID].ToString(), "[0-9]")) {//serialNum[y] matches numCheck
			OUT = ((serialNum[ID] - '0'));
		} else {
			OUT = (Array.IndexOf(alphabet, serialNum[ID].ToString()) + 1);
		}
		OUT = OUT % 6;
		return OUT;
	}

	void SwapMatches() {
		int HOLD = 0;
		int SWAP = 6;
		for (int i = 0; i < 6; i++) {
			HOLD = shuffled[i];
			SWAP = keys[1].IndexOf(keys[0][i]) + 6;
			shuffled[i] = shuffled[SWAP];
			shuffled[SWAP] = HOLD;
		}
	}

	void Press () {
		held = true;
	}

	void Release () {
		if (heldFrame < 10) {
			if (MasterKey.GlobalKeyHeld && !HasKey) {
				MasterKey.GlobalKeyHeld = false;
				HasKey = true;
				Key.SetActive(true);
				Audio.PlaySoundAtTransform("Key_In", transform);
			} else if (HasKey) {
				MasterKey.GlobalKeyHeld = true;
				HasKey = false;
				Key.SetActive(false);
				Audio.PlaySoundAtTransform("keys_01", transform);
			}
		}
		held = false;
		heldFrame = 0;
	}

	void PressButton (KMSelectable button) {
		int buttonNum = Array.IndexOf(Buttons, button);
		if (holdFlop) { return; }
		submit[step] = buttonNum;
		PlayNote(buttonNum);
		step++;
		if (step == 12) { CheckSolve(); step = 0; }
		//Module.HandlePass();
	}

	IEnumerator Animate () {
		while(true){
			if (WindTimer == 0) { holdFlop = false; } else if (WindTimer == 1020) { holdFlop = true; playback = 0; }//!held && 

			if (holdFlop && WindTimer != 0) {
				KeyRotation[0].Rotate(0.0f, 0.0f, 0.7f);
				KeyRotation[1].Rotate(0.0f, -0.7f, 0.0f);
				KeyRotation[2].Rotate(0.0f, 0.35f, 0.0f);
				if (WindTimer % 84 == 0) { PlayNote(shuffled[playback]); step = 0; playback++; }
				WindTimer-=2;
				if (tickTimer == 230) { tickTimer = 0; }
			} else {
				if (HasKey && held && !holdFlop) { heldFrame += 1; }//Debug.Log(heldFrame);
				if (HasKey && held && heldFrame >= 10 && WindTimer < 1020) {
					KeyRotation[0].Rotate(0.0f, 0.0f, -1.75f);
					KeyRotation[1].Rotate(0.0f, 1.75f, 0.0f);
					WindTimer+=5;
					if (heldFrame == 40) { heldFrame = 10; }
					if (heldFrame == 10) { Audio.PlaySoundAtTransform("wind-up2_ALT", transform); }
				}
			}
			if (tickTimer == 0) { Audio.PlaySoundAtTransform("ticking", transform); }
			if (tickTimer < 230) { tickTimer++; }
			//Debug.Log(WindTimer);
			yield return new WaitForSeconds(0.01f);
		}
	}

	void PlayNote(int ID) {
		Audio.PlaySoundAtTransform(buttonSounds[ID], transform);
	}

	void CheckSolve () {
		if (moduleSolved) { return; }
		LogResult(3);
		for (int i = 0; i < 12; i++){
			if (submit[i] != stored[i]) {
				Debug.LogFormat("[Wind-Up Music Box #{0}] Incorrect Sequence. Module Striked", moduleId);
				Module.HandleStrike();
				return;
			}
		}
		Debug.LogFormat("[Wind-Up Music Box #{0}] Sequence Accepted", moduleId);
		moduleSolved = true;
		Module.HandlePass();
	}
	
			// Twitch Plays Support

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} Grab -- Grab key (if applicable) || Play -- Tests notes then winds key to play music box || Submit [1-4]x12 -- inputs a sequence of 12 button presses. No more, no less.";
#pragma warning restore 414

	bool isValidPos(string n) {
		string[] valids = new string[] { "1", "2", "3", "4"};
		if (!valids.Contains(n)) { return false; }
		return true;
	}

	void TwitchToggleKey() {
		KeyHole.OnInteract();
		KeyHole.OnInteractEnded();
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
			TwitchToggleKey();
			yield break;
		}

		if (split[0].EqualsIgnoreCase("PLAY")) {
			if (split.Length != 1) {
				yield return "sendtochaterror Too many words in command!";
				yield break;
			} else if (!MasterKey.GlobalKeyHeld && !HasKey) {
				yield return "sendtochaterror Key is located somewhere else";
				yield break;
			} else if (holdFlop) {
				yield return "sendtochaterror Music Box is currently playing";
				yield break;
			}

			if (!HasKey) { TwitchToggleKey(); yield return new WaitForSeconds(0.4f); }
			for (int i = 0; i < 4; i++) {
				Buttons[i].OnInteract();
				yield return new WaitForSeconds(0.5f);
			}
			KeyHole.OnInteract();
			while (!holdFlop) { yield return new WaitForSeconds(0.1f); }
			KeyHole.OnInteractEnded();
			yield return new WaitForSeconds(0.1f);
			TwitchToggleKey();
			yield break;
		}

		if (split[0].EqualsIgnoreCase("SUBMIT")) {
			//int numberClicks = 0;
			//int pos = 0;
			List<int> TPCODE = new List<int> {};
			if (split.Length != 13) {
				yield return "sendtochaterror Incorrect Length";
				yield break;
			} else if (holdFlop) {
				yield return "sendtochaterror Music Box is currently playing";
				yield break;
			} else {
				for (int i = 1; i < 13; i++) {
					if (!isValidPos(split[i])) {
						yield return "sendtochaterror " + split[i] + " is not valid";
						yield break;
					} else {
						TPCODE.Add(Int32.Parse(split[i])-1);
					}
				}
			}
			if (HasKey) { TwitchToggleKey(); }
			for (int i = 0; i < 12; i++) {
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
		if (HasKey) { TwitchToggleKey(); yield return new WaitForSeconds(0.1f); }
		for (int i = 0; i < 12; i++) {
			Buttons[stored[i]].OnInteract();
			yield return new WaitForSeconds(0.1f);
		}
		yield break;
	}
}
