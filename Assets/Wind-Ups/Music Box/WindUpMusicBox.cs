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

	private int ChosenSong = 0;
	private string[] SongNames = new string[] {
		"Titanic", "Beyond The Sea",      // 1
		"Rainbow", "Puff",                // 2
		"Snow", "Santa",                  // 3
		"Small World", "Mickey Mouse",    // 4
		"Hidden Palace", "Rickroll",      // 5
		"Mice on Venus", "Living Mice",   // 6
		"Rock a Bye", "Clair de lune",    // 7 Take Me Out to the Ball Game
		"His Theme", "MerryGoRound",      // 8
		"Lullaby", "Sunshine",            // 9
		"Fur Elise", "Boba Fett",         // 10
		"Chihiro", "Christmas",           // 11
		"Song of Storms", "Wind Fish",    // 12
		"FNAF", "Great Fairy",            // 13
		"Dearly Beloved", "Mountain",     // 14
		"Luma", "Bluebird",               // 15
		"Rush E", "Salut d'Amour",        // 16
		"Kirby", "Memories Returned",     // 17
		"Toad House", "Hatsune Miku",     // 18
		"File Select", "Two Birds",       // 19
		"Swan Lake", "Rainbow Connection" // 20
	};

	private int[,] SolveStrokes = new int[,] {
		{3, 3, 2, 1}, {2, 1, 0, 3},      // 1
		{0, 3, 2, 0}, {3, 3, 1, 2},      // 2
		{3, 2, 2, 1}, {0, 3, 2, 1},      // 3
		{3, 2, 1, 1}, {0, 1, 2, 2},      // 4
		{1, 2, 1, 3}, {1, 2, 3, 2},      // 5
		{3, 1, 0, 2}, {0, 1, 3, 2},      // 6
		{0, 2, 1, 3}, {0, 1, 2, 1},      // 7
		{1, 2, 1, 0}, {2, 1, 3, 2},      // 8
		{1, 1, 0, 2}, {0, 1, 3, 3},      // 9
		{3, 3, 3, 2}, {2, 1, 2, 2},      // 10
		{2, 1, 0, 2}, {2, 0, 1, 0},      // 11
		{3, 2, 3, 2}, {0, 3, 2, 3},      // 12
		{3, 1, 2, 0}, {3, 2, 1, 0},      // 13
		{3, 1, 2, 1}, {2, 1, 2, 1},      // 14
		{0, 1, 2, 3}, {0, 3, 1, 2},      // 15
		{1, 2, 0, 2}, {2, 2, 1, 0},      // 16
		{1, 0, 0, 1}, {0, 1, 0, 2},      // 17
		{2, 2, 1, 2}, {0, 0, 3, 2},      // 18
		{2, 3, 3, 1}, {1, 1, 3, 2},      // 19
		{3, 2, 3, 3}, {2, 1, 2, 3}       // 20
	};

	private int step = 0;
	private int[] submit = new int[] {0, 0, 0, 0};

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
		Debug.LogFormat("[Wind-Up Music Box #{0}] Chosen song is {1}", moduleId, SongNames[ChosenSong]);
		StartCoroutine(Animate());
	}

	IEnumerator CheckKey () {
		yield return new WaitForSeconds(0.01f);
		if (MasterKey.PlaceKey(windID)) { HasKey = true; Key.SetActive(true); Debug.LogFormat("[Wind-Up Music Box #{0}] Starting with key", moduleId); }
		//Debug.LogFormat("[Wind-Up Lockpick #{0}] Max ID is {1}", moduleId, MasterKey.windIdCounter);
	}

	void InitSolution () {
		ChosenSong = UnityEngine.Random.Range(0, SongNames.Length);
		if (debugMode) { ChosenSong = debugSong; }
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
		string[] buttonSounds = new string[] {"Note 1", "Note 2", "Note 3", "Note 4"};
		submit[step] = buttonNum;
		step++;
		Audio.PlaySoundAtTransform(buttonSounds[buttonNum], transform);
		if (step == 4) { CheckSolve(); step = 0; }
		
		//Module.HandlePass();
	}

	IEnumerator Animate () {
		while(true){
			if (WindTimer == 0) { holdFlop = false; } else if (WindTimer == 1000) { holdFlop = true; }//!held && 

			if (holdFlop && WindTimer != 0) {
				WindTimer--;
				KeyRotation[0].Rotate(0.0f, 0.0f, 0.35f);
				KeyRotation[1].Rotate(0.0f, -0.35f, 0.0f);
				KeyRotation[2].Rotate(0.0f, 0.35f, 0.0f);
				if (tickTimer == 230) { tickTimer = 0; }
				if (WindTimer == 999) { Audio.PlaySoundAtTransform(SongNames[ChosenSong], transform); step = 0; }
			} else {
				if (HasKey && held && !holdFlop) { heldFrame += 1; }//Debug.Log(heldFrame);
				if (HasKey && held && heldFrame >= 10 && WindTimer < 1000) {
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

	void CheckSolve () {
		if (SolveStrokes[ChosenSong, 0] == 4) { submit = new int[] { 4, 4, 4, 4 }; }
		//Debug.LogFormat("[Wind-Up Lockpick #{0}] Submitted {1}-{2}-{3}", moduleId, dialVals[0]+1, dialVals[1]+1, dialVals[2]+1);
		if (submit[0] == SolveStrokes[ChosenSong, 0] && submit[1] == SolveStrokes[ChosenSong, 1] && submit[2] == SolveStrokes[ChosenSong, 2] && submit[3] == SolveStrokes[ChosenSong, 3]) { Debug.LogFormat("[Wind-Up Music Box #{0}] Module Passed", moduleId); Module.HandlePass(); } else { Debug.LogFormat("[Wind-Up Music Box #{0}] Module Striked", moduleId); Module.HandleStrike(); }//Audio.PlaySoundAtTransform("Music Wrong", transform);
	}
	
	void Update () {
		//if (needyActive) {  }
	}
}
