using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using KModkit;

public class WindUpKey : MonoBehaviour {

	//-----------------------------------------------------//
	public KMBombInfo Bomb;
	public KMAudio Audio;
	public KMBombModule Module;
	public KMGameInfo StateCheck;

	public KMSelectable KeyHole;

	public GameObject Key;
	public Transform KeyRotation;

	public TextMesh timerDisplay;

	public GameObject[] morseBulbs;
	public Transform ParentBulb; //Testing something...

	public bool debugMode;
	public int debugDisplay;
	public int debugFlash = 36;

	//-----------------------------------------------------//
	private int heldFrame = 0;
	private bool held = false;

	private int blinkInterval = 0;
	private bool bulbOn = false;
	private bool firstTime = true;

	private int displayNum = 0;

	private int requiredSeconds = 0;
	private int requiredDigit = 0;
	private bool timedRelease = false;
	private bool instantRelease = false;
	private int releaseTimer = 0;
	private int startHold;
	private int endHold;

	private string[] INDs = new string[] {};

	//-----------------------------------------------------//

	private int flashMorseIndex = 0;
	private List<List<int>> morseTable = new List<List<int>> {
		new List<int> {1, 1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0}, //0
		new List<int> {1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0}, //1
		new List<int> {1, 0, 1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0}, //2
		new List<int> {1, 0, 1, 0, 1, 0, 1, 1, 0, 1, 1, 0}, //3
		new List<int> {1, 0, 1, 0, 1, 0, 1, 0, 1, 1, 0}, //4
		new List<int> {1, 0, 1, 0, 1, 0, 1, 0, 1, 0}, //5
		new List<int> {1, 1, 0, 1, 0, 1, 0, 1, 0, 1, 0}, //6
		new List<int> {1, 1, 0, 1, 1, 0, 1, 0, 1, 0, 1, 0}, //7
		new List<int> {1, 1, 0, 1, 1, 0, 1, 1, 0, 1, 0, 1, 0}, //8
		new List<int> {1, 1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0, 1, 0}, //9
		new List<int> {1, 0, 1, 1, 0}, //A
		new List<int> {1, 1, 0, 1, 0, 1, 0, 1, 0}, //B
		new List<int> {1, 1, 0, 1, 0, 1, 1, 0, 1, 0}, //C
		new List<int> {1, 1, 0, 1, 0, 1, 0}, //D
		new List<int> {1, 0}, //E
		new List<int> {1, 0, 1, 0, 1, 1, 0, 1, 0}, //F
		new List<int> {1, 1, 0, 1, 1, 0, 1, 0}, //G
		new List<int> {1, 0, 1, 0, 1, 0, 1, 0}, //H
		new List<int> {1, 0, 1, 0}, //I
		new List<int> {1, 0, 1, 1, 0, 1, 1, 0, 1, 1, 0}, //J
		new List<int> {1, 1, 0, 1, 0, 1, 1, 0}, //K
		new List<int> {1, 0, 1, 1, 0, 1, 0, 1, 0}, //L
		new List<int> {1, 1, 0, 1, 1, 0}, //M
		new List<int> {1, 1, 0, 1, 0}, //N
		new List<int> {1, 1, 0, 1, 1, 0, 1, 1, 0}, //O
		new List<int> {1, 0, 1, 1, 0, 1, 1, 0, 1, 0}, //P
		new List<int> {1, 1, 0, 1, 1, 0, 1, 0, 1, 1, 0}, //Q
		new List<int> {1, 0, 1, 1, 0, 1, 0}, //R
		new List<int> {1, 0, 1, 0, 1, 0}, //S
		new List<int> {1, 1, 0}, //T
		new List<int> {1, 0, 1, 0, 1, 1, 0}, //U
		new List<int> {1, 0, 1, 0, 1, 0, 1, 1, 0}, //V
		new List<int> {1, 0, 1, 1, 0, 1, 1, 0}, //W
		new List<int> {1, 1, 0, 1, 0, 1, 0, 1, 1, 0}, //X
		new List<int> {1, 1, 0, 1, 0, 1, 1, 0, 1, 1, 0}, //Y
		new List<int> {1, 1, 0, 1, 1, 0, 1, 0, 1, 0}, //Z
	}; // Always add 3 0s to the end of the sequence
	private string[] debugChar = new string[] {
		"0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
		"A", "B", "C", "D", "E", "F", "G", "H",
		"I", "J", "K", "L", "M", "N", "O", "P",
		"Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"
	};
	private int flashMorseChar = 0;

	//-----------------------------------------------------//
	//SHARED INFORMATION
	private bool HasKey = false;
	int windID = 0;

	private bool tpOverride = false;
	private bool moduleSolved = false;
	//-----------------------------------------------------//
	static int moduleIdCounter = 1;
	int moduleId;
	//-----------------------------------------------------//

	private void Awake () {
		moduleId = moduleIdCounter++;

		KeyHole.OnInteract += delegate () { Press(); return false; };
    	KeyHole.OnInteractEnded += delegate () { Release(); };
		//KeyHole.OnInteract += delegate () { PlaceKey(); return false; };

		StateCheck.OnStateChange += i => { MasterKey.ResetMaster(); };
	}

	void Start () {
		windID = MasterKey.ServeID(Bomb);

		StartCoroutine(CheckKey());

		InitSolution();
		StartCoroutine(Animate());
		//BulbSetup();
		StartCoroutine(InitLight());
	}

	IEnumerator InitLight () {
		yield return new WaitForSeconds(0.02f);
		BulbSetup();
	}

	IEnumerator CheckKey () {
		yield return new WaitForSeconds(0.01f);
		if (MasterKey.PlaceKey(windID)) { HasKey = true; Key.SetActive(true); Debug.LogFormat("[Wind The Key #{0}] Starting with key", moduleId); }
	}
	
	void InitSolution () {
		INDs = Bomb.GetIndicators().ToArray();
		displayNum = UnityEngine.Random.Range(0, 1000);
		//DEBUG OVERWRITE//
		if (debugMode) { displayNum = debugDisplay; }

		timerDisplay.text = displayNum.ToString();
		Debug.LogFormat("[Wind The Key #{0}] Display shows {1}", moduleId, displayNum);
		requiredSeconds = Bomb.GetBatteryCount() + INDs.Length;
		ConditionCheck();
		ShuffleLight();
		if (timedRelease) {
			Debug.LogFormat("[Wind The Key #{0}] Hold for {1} timer unit(s)", moduleId, requiredSeconds);
		} else if (instantRelease) {
			Debug.LogFormat("[Wind The Key #{0}] Release within 1 second", moduleId);
		} else {
			Debug.LogFormat("[Wind The Key #{0}] Release on a {1}", moduleId, requiredDigit);
		}
	}

	void ConditionCheck() {
		int matchRule = 0;
		if (displayNum > 9 && displayNum < 100) {
			matchRule = 1;
		} else if (INDs.Contains("CLR") && Bomb.GetBatteryCount() > 2) {
			instantRelease = true;
			matchRule = 2;
		} else if (Bomb.GetBatteryCount() > Bomb.GetPortCount() && Bomb.GetPortCount() > 0) {
			timedRelease = true;
			matchRule = 3;
		} else if (CheckAdd()) {
			instantRelease = true;
			matchRule = 4;
		} else if (CheckSerial()) {
			matchRule = 5;
		} else if (CheckPrime()) {
			instantRelease = true;
			matchRule = 6;
		} else {
			timedRelease = true;
			matchRule = 7;
		}
		if (timedRelease && Bomb.GetBatteryCount() + INDs.Length == 0) { timedRelease = false; instantRelease = true; }
		Debug.LogFormat("[Wind The Key #{0}] Rule {1} met", moduleId, matchRule);
	}

	bool CheckAdd() {
		int A = displayNum % 10;
		int B = displayNum % 100 / 10;
		int C = displayNum / 100;
		//Debug.Log(A + " + " + B + " + " + C);
		if (A + B + C >= 20 && Bomb.GetOnIndicators().ToArray().Length > Bomb.GetOffIndicators().ToArray().Length ) { return true; } else { return false; }
	}

	bool CheckSerial() {
		int[] SERIAL = Bomb.GetSerialNumberNumbers().ToArray();
		string displayHOLD = displayNum.ToString();
		foreach (int NUM in SERIAL) {
			if (displayHOLD.Contains(NUM.ToString())) { return true; }
		}
		return false;
	}

	bool CheckPrime() {
		int prime = 0;
		for (int i = 1; i <= displayNum; i++) {
			if (displayNum % i == 0) {
				prime++;
			}
		}
		if (prime == 2) { return true; } else { return false; }
	}

	void ShuffleLight() {
		flashMorseIndex = UnityEngine.Random.Range(0, 36);

		if (debugMode && debugFlash < 36) { flashMorseIndex = debugFlash; }

		if (flashMorseIndex < 10) {
			requiredDigit = flashMorseIndex;
		} else if (flashMorseIndex < 18) {
			requiredDigit = 2;
		} else if (flashMorseIndex < 26) {
			requiredDigit = 7;
		} else {
			requiredDigit = 9;
		}

		if (!timedRelease && !instantRelease) { Debug.LogFormat("[Wind The Key #{0}] Shuffled light. New character is {1}", moduleId, debugChar[flashMorseIndex]); }
	}

	void Press () {
		held = true;
		startHold = (int)Bomb.GetTime();
		//Debug.Log((int)Bomb.GetTime());
	}

	void Release () {
		//Debug.Log("Debug // Released at " + heldFrame);
		if (heldFrame < 10) {
			if (MasterKey.GlobalKeyHeld && !HasKey) {
				MasterKey.GlobalKeyHeld = false;
				Key.SetActive(true);
				HasKey = !HasKey;
				Audio.PlaySoundAtTransform("Key_In", transform);
			} else if (HasKey) {
				MasterKey.GlobalKeyHeld = true;
				Key.SetActive(false);
				HasKey = !HasKey;
				Audio.PlaySoundAtTransform("keys_01", transform);
			}
		} else if (HasKey) {
			endHold = (int)Bomb.GetTime();
			CheckSolve();
			Audio.PlaySoundAtTransform("door_open_01", transform); //Key_Out
		}
		startHold = 0;
		endHold = 0;
		flashMorseChar = 0;
		//Bulb.material = BulbColors[0];//Replace
		BulbState(false);
		held = false;
		releaseTimer = 0;
	}

	IEnumerator Animate () {
		while(true){
			//Debug.Log(heldFrame);
			if (held && heldFrame < 15 && HasKey) { heldFrame += 1; }
			if (HasKey && held && heldFrame > 9 && heldFrame < 15) {
				KeyRotation.Rotate(0.0f, 0.0f, -8.0f);
				//KeyRotation.Rotate(0.0f, 0.0f, 0.0f);
				if (heldFrame == 10) {
					Audio.PlaySoundAtTransform("door_open_01", transform);
					if (!moduleSolved) { Debug.LogFormat("[Wind The Key #{0}] Held at {1}", moduleId, Bomb.GetFormattedTime()); }
				}
			} else if (!held && heldFrame > 9) {
				if (heldFrame == 15) { heldFrame--; }
				KeyRotation.Rotate(0.0f, 0.0f, 8.0f);
				//KeyRotation.Rotate(0.0f, -8.0f, 0.0f);
				heldFrame -= 1;
				if (heldFrame == 9) { heldFrame = 0; }
			} else if (!held && heldFrame < 10) { heldFrame = 0; }

			if (!moduleSolved && held && heldFrame >= 10 && HasKey) {
				releaseTimer++;
				if (blinkInterval == 0) {
					bulbOn = morseTable[flashMorseIndex][flashMorseChar] == 1;
					//if (bulbOn) { Bulb.material = BulbColors[1]; } else { Bulb.material = BulbColors[0]; }
					BulbState(bulbOn);
					flashMorseChar++;
					if (flashMorseChar == morseTable[flashMorseIndex].Count) { blinkInterval = 80; flashMorseChar = 0; } else { blinkInterval = 20; }
					//Debug.Log(flashMorseIndex + ", " + flashMorseChar + ", " + morseTable[flashMorseIndex][flashMorseChar] + ", " + bulbOn);
				}
				blinkInterval--;
			}

			yield return new WaitForSeconds(0.01f);
		}
	}

	void BulbSetup() {
		//MorseBulbData.InactiveLight = morseBulb.GetComponent<StatusLight>().InactiveLight; //BackupBulbs[0];
		//MorseBulbData.StrikeLight = morseBulb.GetComponent<StatusLight>().StrikeLight;
		string lightStrikeName = "Component_LED_STRIKE_mesh";
		if (Application.isEditor) { lightStrikeName = "Component_LED_STRIKE"; }

		Debug.Log(morseBulbs[1] == null);
		Debug.Log(FindDeep(ParentBulb, "Component_LED_STRIKE") == null);
		morseBulbs[0].GetComponent<MeshFilter>().mesh = FindDeep(ParentBulb, "Component_LED_OFF").gameObject.GetComponent<MeshFilter>().mesh;
		Debug.Log("Checkpoint A");
		morseBulbs[0].GetComponent<Renderer>().material = FindDeep(ParentBulb, "Component_LED_OFF").gameObject.GetComponent<Renderer>().material;
		Debug.Log("Checkpoint B");
		firstTime = false;
		morseBulbs[1].GetComponent<MeshFilter>().mesh = FindDeep(ParentBulb, lightStrikeName).gameObject.GetComponent<MeshFilter>().mesh;
		firstTime = true;
		Debug.Log("Checkpoint C");
		morseBulbs[1].GetComponent<Renderer>().material = FindDeep(ParentBulb, lightStrikeName).gameObject.GetComponent<Renderer>().material;
		Debug.Log("Checkpoint D");
		if (!Application.isEditor) {
			morseBulbs[2] = FindDeep(ParentBulb, "LightGlow").gameObject;
			Debug.Log("Checkpoint E");
		}
		BulbState(false);
	}

	Transform FindDeep (Transform PARENT, string CHILD) {
		var result = PARENT.Find(CHILD);
		if (!firstTime) { Debug.Log("DEBUG // Scanning " + PARENT.name); } //Search for light gameobjects in case of discrepencies between modkit and game
		if (result != null)
			return result;
		foreach (Transform child in PARENT) {
			result = FindDeep(child, CHILD);
			if (result != null)
				return result;
		}
		//Debug.Log("Go Fuck Yourself");
		return null;
	}

	void BulbState(bool STATE) {
		morseBulbs[0].SetActive(!STATE);
		morseBulbs[1].SetActive(STATE);
		if (!Application.isEditor) { morseBulbs[2].SetActive(STATE); }
		//REMINDER: Grab LightGlow, child of Component_LED_STRIKE
	}

	/*public StatusLight MorseBulb {
		get {
			if (!MorseBulbData) {
				MorseBulbData = gameObject.AddComponent<StatusLight>();
				//morseBulb.PassLight = /* /;
			}
			return MorseBulbData;
		}
	}*/

	void CheckSolve() {
		if (moduleSolved) { return; }
		if (debugMode) {
			debugFlash++;
			ShuffleLight();
			if (debugFlash == 35) { debugMode = false; }
			return;
		}

		Debug.LogFormat("[Wind The Key #{0}] Released at {1}", moduleId, Bomb.GetFormattedTime());
		//Debug.Log(timedRelease + ", " + instantRelease);
		if ((timedRelease && checkTimed(endHold, requiredSeconds)) || (instantRelease && releaseTimer < 60) || (!timedRelease && !instantRelease && checkMatch(requiredDigit.ToString()))) {
			Debug.LogFormat("[Wind The Key #{0}] Module Passed", moduleId);
			moduleSolved = true;
			//Bulb.material = BulbColors[0];
			BulbState(false);
			timerDisplay.text = "";
			if (!tpOverride) { Module.HandlePass(); }
		} else {
			Debug.LogFormat("[Wind The Key #{0}] Module Striked", moduleId);
			ShuffleLight();
			if (!tpOverride) { Module.HandleStrike(); }
		}
	}

	bool checkMatch(string CHECK) {
		string time = Bomb.GetFormattedTime();
		if (time.Contains(CHECK)) { return true; } else { return false; }
	}

	bool checkTimed(int B, int CHECK) {
		if (B <= startHold) {
			return startHold - B == CHECK;
		} else {
			return B - startHold == CHECK;
		}
	}

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} Grab -- Grab key (if applicable) || Hold [seconds] -- Turn the key for # seconds || Hold to [digit] -- Holds key until # appears in timer";
#pragma warning restore 414

	bool isValidPos(string n) {
		foreach (char c in n) {
			if (c < '0' || c > '9') { return false; }
		}
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
			KeyHole.OnInteractEnded();
			yield break;
		}

		if (split[0].EqualsIgnoreCase("HOLD")) {
			//int numberClicks = 0;
			//int pos = 0;
			if (split.Length != 2) {
				// HOLD TO
				if (split.Length != 3) {
					yield return "sendtochaterror Too many words in command!";
					yield break;
				} else if (!split[1].EqualsIgnoreCase("TO")) {
					yield return "sendtochaterror Wrong command!";
					yield break;
				} else if (!isValidPos(split[2])) {
					yield return "sendtochaterror " + split[2] + " is not valid";
					yield break;
				} else if (!MasterKey.GlobalKeyHeld && !HasKey) {
					yield return "sendtochaterror Key is located somewhere else";
					yield break;
				}
				if (!HasKey) { KeyHole.OnInteract(); KeyHole.OnInteractEnded(); yield return new WaitForSeconds(0.1f); }
				KeyHole.OnInteract();
				yield return new WaitForSeconds(0.3f);
				while (!checkMatch(split[2])) { yield return new WaitForSeconds(0.1f); }
				tpOverride = true;
				KeyHole.OnInteractEnded();
				yield return new WaitForSeconds(0.1f);
				TwitchEndCheck();
				yield break;
				
			//HOLD
			} else if (!isValidPos(split[1])) {
				yield return "sendtochaterror " + split[1] + " is not valid";
				yield break;
			} else if (!MasterKey.GlobalKeyHeld && !HasKey) {
				yield return "sendtochaterror Key is located somewhere else";
				yield break;
			}
			if (!HasKey) { KeyHole.OnInteract(); KeyHole.OnInteractEnded(); yield return new WaitForSeconds(0.1f); }
			KeyHole.OnInteract();
			if (split[1].EqualsIgnoreCase("0")) {
				yield return new WaitForSeconds(0.3f);
			} else {
				while (!checkTimed((int)Bomb.GetTime(), Int32.Parse(split[1]))) { yield return new WaitForSeconds(0.1f); }
			}
			tpOverride = true;
			KeyHole.OnInteractEnded();
			yield return new WaitForSeconds(0.1f);
			TwitchEndCheck();
			yield break;
		}
	}

	void TwitchEndCheck() {
		KeyHole.OnInteract();
		KeyHole.OnInteractEnded();
		if (moduleSolved) { Module.HandlePass(); } else { Module.HandleStrike(); }
		tpOverride = false;
	}

	void TwitchHandleForcedSolve() { //Autosolver
		tpOverride = true;
		StartCoroutine(TPAutosolve());
	}
	
	IEnumerator TPAutosolve () {
		while (!MasterKey.GlobalKeyHeld && !HasKey) { yield return new WaitForSeconds(0.1f); }
		if (!HasKey) { KeyHole.OnInteract(); KeyHole.OnInteractEnded(); yield return new WaitForSeconds(0.1f); }
		KeyHole.OnInteract();
		if (instantRelease) {
			yield return new WaitForSeconds(0.3f);
		} else if (timedRelease) {
			yield return new WaitForSeconds(0.1f);
			while (!checkTimed((int)Bomb.GetTime(), requiredSeconds)) { yield return new WaitForSeconds(0.1f); }
		} else {
			while (!checkMatch(requiredDigit.ToString())) { yield return new WaitForSeconds(0.1f); }
		}
		KeyHole.OnInteractEnded();
		yield return new WaitForSeconds(0.1f);

		KeyHole.OnInteract();
		KeyHole.OnInteractEnded();
		Module.HandlePass();
		yield break;
	}
}
