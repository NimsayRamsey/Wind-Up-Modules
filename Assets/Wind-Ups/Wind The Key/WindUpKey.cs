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

	public bool debugMode;
	public Material[] BulbColors;
	public Renderer Bulb;

	//-----------------------------------------------------//
	private int heldFrame = 0;
	private bool held = false;

	private int maxTime = 88;//GetTime()
	private int targetTime = 585;

	private int debugStrikes = 0;
	private int moduleCount = 1;
	private int modCountDown = 0;
	private int solveCount = 0;

	private int blinkInterval = 0;
	private bool bulbOn = false;

	private List<string> Unsolved = new List<string> {};
	private bool lastRemaining = false;

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

		Debug.LogFormat("[Wind The Key #{0}] Solution Message", moduleId);
		moduleCount = Bomb.GetSolvableModuleNames().Count();
		InitModuleCount();
		StartCoroutine(Animate());
	}

	IEnumerator CheckKey () {
		yield return new WaitForSeconds(0.01f);
		if (MasterKey.PlaceKey(windID)) { HasKey = true; Key.SetActive(true); Debug.LogFormat("[Wind The Key #{0}] Starting with key", moduleId); }
	}

	void InitModuleCount () {
		if (moduleCount == 1) { InitSolution(); return; }
		modCountDown = UnityEngine.Random.Range(1, (moduleCount - (moduleCount/3)));
		timerDisplay.text = modCountDown.ToString();
	}
	
	void InitSolution () {
		maxTime = (int)Bomb.GetTime();
		maxTime = maxTime - (maxTime/10);
		if (maxTime < 90) { targetTime = maxTime; } else {
			targetTime = maxTime - (maxTime/4);//60*50
			//targetTime = maxTime - (maxTime/60*(UnityEngine.Random.Range(0, 54)));
		}
		string display = "";
		if (targetTime%60 < 10) { display = targetTime/60 + ":0" + targetTime%60; } else { display = targetTime/60 + ":" + targetTime%60; }
		timerDisplay.text = display;
		Debug.LogFormat("[Wind The Key #{0}] Phase 2 activated. Turn key at {1}", moduleId, display);
	}

	void Press () {
		held = true;
		//Debug.Log((int)Bomb.GetTime());
	}

	void Release () {
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
		}
		held = false;
		//heldFrame = 0;
	}

	IEnumerator Animate () {
		while(true){
			//Debug.Log(heldFrame);
			if (held && heldFrame < 15 && HasKey) { heldFrame += 1; }
			if (HasKey && held && heldFrame > 9 && heldFrame < 15) {
				KeyRotation.Rotate(0.0f, 0.0f, -8.0f);
				//KeyRotation.Rotate(0.0f, 0.0f, 0.0f);
				if (heldFrame == 10) { Audio.PlaySoundAtTransform("door_open_01", transform); }
				if (heldFrame == 14) { CheckSolve(); }
			} else if (!held && heldFrame > 9) {
				if (heldFrame == 15) { heldFrame--; }
				KeyRotation.Rotate(0.0f, 0.0f, 8.0f);
				//KeyRotation.Rotate(0.0f, -8.0f, 0.0f);
				heldFrame -= 1;
				if (heldFrame == 9) { heldFrame = 0; }
			} else if (!held && heldFrame < 10) { heldFrame = 0; }

			if ((int)Bomb.GetTime() < targetTime && !moduleSolved && modCountDown == 0) {
				if (debugStrikes == 0) { Debug.LogFormat("[Wind The Key #{0}] Missed the timer.", moduleId); }
				if (debugMode && debugStrikes >= 10) { break; }
				Module.HandleStrike();
				debugStrikes ++;
			}

			if (modCountDown == 0 && !moduleSolved && !lastRemaining) {
				if (checkRemaining()) {
					Bulb.material = BulbColors[1];
					timerDisplay.text = "00:00";
					bulbOn = true;
					lastRemaining = true;
					Debug.LogFormat("[Wind The Key #{0}] No other modules remaining. Soft disarm enabled. Turn key to disarm.", moduleId);
				} else {
					blinkInterval++;
					if (blinkInterval == 50) {
						if (bulbOn) { Bulb.material = BulbColors[0]; } else { Bulb.material = BulbColors[1]; }
						bulbOn = !bulbOn;
						blinkInterval = 0;
					}
				}
			}

			if (modCountDown > 0) {
				if (solveCount != Bomb.GetSolvedModuleNames().Count()) {
					solveCount = Bomb.GetSolvedModuleNames().Count();
					modCountDown -= 1;
					if (modCountDown == 0) {
						InitSolution();
						Audio.PlaySoundAtTransform("Key_Warning", transform);
					} else { timerDisplay.text = modCountDown.ToString(); }
				}
			}

			yield return new WaitForSeconds(0.01f);
		}
	}

	bool checkRemaining() {
		Unsolved = Bomb.GetSolvableModuleNames();
		List<string> Solved = Bomb.GetSolvedModuleNames();
		foreach (string item in Solved) {
			foreach (string check in Unsolved) {
				if (item == check) { Unsolved.Remove(check); break; }
			}
		}

		foreach (string item in Unsolved) {
			bool ignore = false;
			//if (Blacklist.Contains(item)) { ignore = true; }
			if (!ignore && item != "Wind the Key") { return false; }
		}
		return true;
	}

	void CheckSolve() {
		if (moduleSolved || modCountDown > 0) { return; }
		if ((int)Bomb.GetTime() == targetTime || lastRemaining) {
			Debug.LogFormat("[Wind The Key #{0}] Module Passed", moduleId);
			moduleSolved = true;
			Bulb.material = BulbColors[0];
			timerDisplay.text = "";
			if (!tpOverride) { Module.HandlePass(); }
		} else {
			Debug.LogFormat("[Wind The Key #{0}] Module Striked", moduleId);
			if (!tpOverride) { Module.HandleStrike(); }
		}
	}
}
