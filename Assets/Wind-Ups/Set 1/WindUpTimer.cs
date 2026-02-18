using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using KModkit;
//using MasterKey;

public class WindUpTimer : MonoBehaviour {

	//-----------------------------------------------------//
	public KMBombInfo Bomb;
	public KMAudio Audio;
	public KMNeedyModule Needy;
	public KMGameInfo StateCheck;

	public KMSelectable KeyHole;
	public GameObject Key;
	public Transform[] KeyRotation;

	//public TextMesh TempTimer;
	//-----------------------------------------------------//
	private bool needyActive = false;

	private int heldFrame = 0;
	private bool held = false;

	private int WindTimer = 2496;
	private int tickTimer = 0;

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

		GetComponent<KMNeedyModule>().OnNeedyActivation += NeedyStart;
		GetComponent<KMNeedyModule>().OnTimerExpired += OnTimerExpired;
		GetComponent<KMNeedyModule>().OnNeedyDeactivation += NeedyOff;

		StateCheck.OnStateChange += i => { MasterKey.ResetMaster(); };

		KeyHole.OnInteract += delegate () { Press(); return false; };
        KeyHole.OnInteractEnded += delegate () { Release(); };
	}

	void Start () {
		windID = MasterKey.ServeID(Bomb);
		//Debug.Log(windID);
		//Debug.Log(MasterKey.windIdCounter);
		StartCoroutine(CheckKey());
		//Debug.LogFormat("[Wind-Up Timer #{0}] ID is {1}. First serial is {2}", moduleId, windID, MasterKey.firstSerial);
	}

	IEnumerator CheckKey () {
		yield return new WaitForSeconds(0.01f);
		if (MasterKey.PlaceKey(windID)) { HasKey = true; Key.SetActive(true); Debug.LogFormat("[Wind-Up Timer #{0}] Starting with key", moduleId); }
	}

	void NeedyStart () {
		needyActive = true;
		WindTimer = 2499;
		Audio.PlaySoundAtTransform("bell_1", transform);
		StartCoroutine(TickDown());
	}

	void OnTimerExpired () {
		Needy.HandleStrike();
        Needy.HandlePass();
		needyActive = false;
	}

	void NeedyOff () {
		//Needy.HandlePass();
		needyActive = false;
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

	IEnumerator TickDown () {
		while(needyActive){
			if ((!held || !HasKey) && WindTimer != 99) {
				WindTimer--;
				KeyRotation[0].Rotate(0.0f, 0.0f, -0.15f);
				KeyRotation[1].Rotate(0.0f, 0.15f, 0.0f);
				if (tickTimer == 230) { tickTimer = 0; }
				if (WindTimer == 699) { Audio.PlaySoundAtTransform("bell_1", transform); }
			} else {
				if (held) { heldFrame += 1; }//Debug.Log(heldFrame);
				if (HasKey && held && heldFrame >= 10 && WindTimer < 2496) {
					KeyRotation[0].Rotate(0.0f, 0.0f, 0.6f);
					KeyRotation[1].Rotate(0.0f, -0.6f, 0.0f);
					WindTimer+=4;
					if (heldFrame == 40) { heldFrame = 10; }
					if (heldFrame == 10) { Audio.PlaySoundAtTransform("wind-up2_ALT", transform); }
				}
			}
			if (tickTimer == 0) { Audio.PlaySoundAtTransform("ticking", transform); }
			if (tickTimer < 230) { tickTimer++; }
			//TempTimer.text = WindTimer.ToString();
			if (WindTimer != 99) { Needy.SetNeedyTimeRemaining(WindTimer/100); } else { OnTimerExpired(); }
			//Debug.Log(WindTimer);
			yield return new WaitForSeconds(0.01f);
		}
	}
	
	void Update () {
		//if (needyActive) {  }
	}
		// Twitch Plays Support

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} Grab -- Grab key (if applicable) || Hold [Half/Full] -- Wind the module half or completely ";
#pragma warning restore 414

	bool isValidPos(string n) {
		string[] valids = { "HALF", "FULL"};
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
			KeyHole.OnInteractEnded();
			yield break;
		}

		if (split[0].EqualsIgnoreCase("HOLD")) {
			//int numberClicks = 0;
			//int pos = 0;
			if (split.Length != 2) {
				yield return "sendtochaterror Too many words in command!";
				yield break;
			} else if (!isValidPos(split[1])) {
				yield return "sendtochaterror " + split[1] + " is not valid";
				yield break;
			} else if ((!MasterKey.GlobalKeyHeld && !HasKey) || !needyActive) {
				yield return "sendtochaterror Key is located somewhere else or Needy is inactive";
				yield break;
			}
			if (HasKey) { KeyHole.OnInteract(); KeyHole.OnInteractEnded(); yield return new WaitForSeconds(0.1f); }
			if (split[1].EqualsIgnoreCase("HALF")) {
				KeyHole.OnInteract();
				KeyHole.OnInteractEnded();
				yield return new WaitForSeconds(0.1f);
				KeyHole.OnInteract();
				int FREEZE = WindTimer;
				while (WindTimer < 2496 && WindTimer < (FREEZE + 1255)) { yield return new WaitForSeconds(0.1f); }
				KeyHole.OnInteractEnded();
				yield return new WaitForSeconds(0.1f);
				KeyHole.OnInteract();
				KeyHole.OnInteractEnded();
			} else if (split[1].EqualsIgnoreCase("FULL")) {
				KeyHole.OnInteract();
				KeyHole.OnInteractEnded();
				yield return new WaitForSeconds(0.1f);
				KeyHole.OnInteract();
				while (WindTimer < 2496) { yield return new WaitForSeconds(0.1f); }
				KeyHole.OnInteractEnded();
				yield return new WaitForSeconds(0.1f);
				KeyHole.OnInteract();
				KeyHole.OnInteractEnded();
			}
			yield break;
		}
	}

	void TwitchHandleForcedSolve() { //Autosolver
		StartCoroutine(DealWithNeedy());
	}
	
	IEnumerator DealWithNeedy () {
		if (HasKey) { KeyHole.OnInteract(); KeyHole.OnInteractEnded(); yield return new WaitForSeconds(0.1f); }
		while (true) {
			while(WindTimer > 699 || !MasterKey.GlobalKeyHeld || !needyActive){ yield return null; }
			KeyHole.OnInteract();
			KeyHole.OnInteractEnded();
			yield return new WaitForSeconds(0.1f);
			KeyHole.OnInteract();
			while (WindTimer < 2496) { yield return new WaitForSeconds(0.1f); }
			KeyHole.OnInteractEnded();
			yield return new WaitForSeconds(0.1f);
			KeyHole.OnInteract();
			KeyHole.OnInteractEnded();
		}
	}

}