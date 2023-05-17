using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using KModkit;

public class WindUpMaze : MonoBehaviour {

	//-----------------------------------------------------//
	public KMBombInfo Bomb;
	public KMAudio Audio;
	public KMBombModule Module;
	public KMGameInfo StateCheck;

	public KMSelectable MoveButton;
	public KMSelectable[] KeyHoles;
	public KMSelectable turnKey;
	public KMSelectable modeKey;
	public KMSelectable[] TurnArrows;
	public KMSelectable[] TurnArrows2;
	public GameObject[] TurnArrowsTransform;
	//public KMSelectable SubmitHole;

	public GameObject[] Keys;
	public Transform[] KeyRotation;

	public KMSelectable ModeButton;
	public Material[] BulbColors; //Black, Red, White
	public Renderer[] Bulbs;

	public TextMesh[] tempCoords;

	public bool debugMode;
	public int debugMaze;

	//-----------------------------------------------------//
	private bool moduleSolved = false;
	private int heldFrame = 0;
	private bool held = false;

	private int[,] mainCoords = new int[,] { {0, 0}, {0, 0} };
	private int[] coords = new int[] {0, 0};
	private int facing = 0;
	private int[,] faceOrder = new int[,] {{0, -1}, {1, 0}, {0, 1}, {-1, 0}};
	private string[] faceName = new string[] {"UP", "RIGHT", "DOWN", "LEFT"};
	private int turnFrame = 0;
	private int turnFrame2 = 0;
	private int direction = 0;

	private int[] hintLights = new int[] {0, 0};
	private int[,] hintGrid = new int[,] {
		{0, 0, 0, 0, 0, 0, 0, 0},
		{0, 0, 0, 0, 0, 0, 0, 0}
	};
	private int hintMode = 0;

	private int chosenMaze = 11;
	private string[] mazeNames = new string[] { "SND", "IND", "FRK", "FRQ", "NSA", "MSA", "TRN", "CLR", "SIG", "BOB", "CAR", "NLL" };
	private bool[,,] Mazes = new bool[,,] { // Up, Left, Down, Right // 
		{    // SND
			{false, false, true, false}, {false, false, false, true}, {false, true, true, true}, {false, true, true, false},
			{true, false, true, false}, {false, false, true, true}, {true, true, true, false}, {true, false, false, false},
			{true, false, true, true}, {true, true, false, false}, {true, false, true, false}, {false, false, true, false},
			{true, false, false, true}, {false, true, false, false}, {true, false, false, true}, {true, true, false, false}
		}, { // IND
			{false, false, true, false}, {false, false, true, true}, {false, true, true, false}, {false, false, true, false},
			{true, false, true, true}, {true, true, false, false}, {true, false, false, true}, {true, true, true, false},
			{true, false, true, true}, {false, true, true, false}, {false, false, true, true}, {true, true, true, false},
			{true, false, false, false}, {true, false, false, true}, {true, true, false, false}, {true, false, false, false}
		}, { // FRK
			{false, false, true, false}, {false, false, false, true}, {false, true, true, true}, {false, true, false, false},
			{true, false, true, true}, {false, true, true, false}, {true, false, true, true}, {false, true, true, false},
			{true, false, false, false}, {true, false, true, true}, {true, true, true, false}, {true, false, false, false},
			{false, false, false, true}, {true, true, false, false}, {true, false, false, true}, {false, true, false, false}
		}, { // FRQ
			{false, false, true, false}, {false, false, true, true}, {false, true, true, false}, {false, false, true, false},
			{true, false, true, true}, {true, true, false, false}, {true, false, true, true}, {true, true, true, false},
			{true, false, true, false}, {false, false, true, true}, {true, true, false, false}, {true, false, true, false},
			{true, false, false, true}, {true, true, false, false}, {false, false, false, true}, {true, true, false, false}
		}, { // NSA
			{false, false, true, true}, {false, true, false, false}, {false, false, false, true}, {false, true, true, false},
			{true, false, false, true}, {false, true, true, true}, {false, true, true, true}, {true, true, false, false},
			{false, false, true, true}, {true, true, false, true}, {true, true, false, true}, {false, true, true, false},
			{true, false, false, true}, {false, true, false, false}, {false, false, false, true}, {true, true, false, false}
		}, { // MSA
			{false, false, true, false}, {false, false, true, true}, {false, true, false, false}, {false, false, true, false},
			{true, false, true, false}, {true, false, false, true}, {false, true, true, true}, {true, true, false, false},
			{true, false, false, true}, {false, true, true, true}, {true, true, true, true}, {false, true, false, false},
			{false, false, false, true}, {true, true, false, false}, {true, false, false, true}, {false, true, false, false}
		}, { // TRN
			{false, false, true, true}, {false, true, true, false}, {false, false, true, true}, {false, true, true, false},
			{true, false, true, false}, {true, false, true, true}, {true, true, true, false}, {true, false, true, false},
			{true, false, true, false}, {true, false, true, true}, {true, true, true, false}, {true, false, true, false},
			{true, false, false, true}, {true, true, false, true}, {true, true, false, true}, {true, true, false, false}
		}, { // CLR
			{false, false, true, true}, {false, true, false, true}, {false, true, false, true}, {false, true, true, false},
			{true, false, true, true}, {false, true, false, true}, {false, true, true, false}, {true, false, false, false},
			{true, false, false, false}, {false, false, true, false}, {true, false, true, true}, {false, true, true, false},
			{false, false, false, true}, {true, true, false, false}, {true, false, false, false}, {true, false, false, false}
		}, { // SIG
			{false, false, true, false}, {false, false, true, true}, {false, true, true, true}, {false, true, true, false},
			{true, false, false, true}, {true, true, true, false}, {true, false, true, false}, {true, false, false, false},
			{false, false, true, false}, {true, false, true, false}, {true, false, true, true}, {false, true, true, false},
			{true, false, false, true}, {true, true, false, true}, {true, true, false, false}, {true, false, false, false}
		}, { // BOB
			{false, false, false, true}, {false, true, false, true}, {false, true, true, false}, {false, false, true, false},
			{false, false, true, true}, {false, true, true, true}, {true, true, false, false}, {true, false, true, false},
			{true, false, true, false}, {true, false, false, true}, {false, true, true, true}, {true, true, false, false},
			{true, false, false, false}, {false, false, false, true}, {true, true, false, true}, {false, true, false, false}
		}, { // CAR
			{false, false, true, true}, {false, true, true, true}, {false, true, true, true}, {false, true, true, false},
			{true, false, true, false}, {true, false, false, false}, {true, false, false, false}, {true, false, true, false},
			{true, false, true, false}, {false, false, true, false}, {false, false, true, false}, {true, false, true, false},
			{true, false, false, true}, {true, true, false, true}, {true, true, false, true}, {true, true, false, false}
		}, { // UNUSED
			{false, false, false, true}, {false, true, true, false}, {false, false, false, true}, {false, true, true, false},
			{false, false, true, false}, {true, false, true, true}, {false, true, true, true}, {true, true, false, false},
			{true, false, false, true}, {true, true, true, false}, {true, false, true, false}, {false, false, true, false},
			{false, false, false, true}, {true, true, false, false}, {true, false, false, true}, {true, true, false, false}
		}
	};

	//-----------------------------------------------------//
	//SHARED INFORMATION
	private int HasKey = 0;
	int windID = 0;

	//-----------------------------------------------------//
	static int moduleIdCounter = 1;
	int moduleId;
	//-----------------------------------------------------//

	private void Awake () {
		moduleId = moduleIdCounter++;

		foreach (KMSelectable NAME in KeyHoles) {
			KMSelectable pressedObject = NAME;
			NAME.OnInteract += delegate () { Press(pressedObject); return false; };
    		NAME.OnInteractEnded += delegate () { Release(pressedObject); };
		}
		foreach (KMSelectable NAME in TurnArrows) {
			KMSelectable pressedObject = NAME;
			NAME.OnInteract += delegate () { Turn(pressedObject); return false; };
		}
		foreach (KMSelectable NAME in TurnArrows2) {
			KMSelectable pressedObject = NAME;
			NAME.OnInteract += delegate () { Turn2(pressedObject); return false; };
		}
		turnKey.OnInteract += delegate () { PlaceKey(); return false; };
		modeKey.OnInteract += delegate () { PlaceMode(); return false; };
		MoveButton.OnInteract += delegate () { Move(); return false; };
		ModeButton.OnInteract += delegate () { Mode(); return false; };

		StateCheck.OnStateChange += i => { MasterKey.ResetMaster(); };
	}

	void Start () {
		windID = MasterKey.ServeID(Bomb);

		for (int i = 0; i < 4; i++) { TurnArrowsTransform[i].SetActive(false); }

		StartCoroutine(CheckKey());

		InitSolution();
		Debug.LogFormat("[Wind-Up Maze #{0}] Maze is {1}. Starting at {2}, {3}. Solution is on {4}, {5}", moduleId, mazeNames[chosenMaze], coords[0], coords[1], mainCoords[1, 0], mainCoords[1, 1]);
		StartCoroutine(Animate());
	}

	IEnumerator CheckKey () {
		yield return new WaitForSeconds(0.01f);
		if (MasterKey.PlaceKey(windID)) { HasKey = 1; Keys[0].SetActive(true); Debug.LogFormat("[Wind-Up Maze #{0}] Starting with key", moduleId); }
	}
	
	void InitSolution () {
		List<string> indON = Bomb.GetOnIndicators().ToList();
		List<string> indOFF = Bomb.GetOffIndicators().ToList();

		if (indON.Count != 0) {
			foreach (string IND in indON) { if (Array.IndexOf(mazeNames, IND) < chosenMaze) { chosenMaze = Array.IndexOf(mazeNames, IND); } }
		} else if (indOFF.Count != 0) {
			foreach (string IND in indOFF) { if (Array.IndexOf(mazeNames, IND) < chosenMaze) { chosenMaze = Array.IndexOf(mazeNames, IND); } }
		}

		// DEBUG //
		if (debugMode) { chosenMaze = debugMaze; }
		// DEBUG //

		setCoords();
		shiftCoords();

		coords[0] = mainCoords[0, 0];
		coords[1] = mainCoords[0, 1];

		tempCoords[0].text = mainCoords[0, 0] + ", " + mainCoords[0, 1];
		tempCoords[1].text = mainCoords[1, 0] + ", " + mainCoords[1, 1];
	}

	void setCoords() {
		for (int i = 0; i < 2; i++){
			mainCoords[i, 0] = UnityEngine.Random.Range(0, 4);
		if (mainCoords[i, 0] == 1 || mainCoords[i, 0] == 2) {
			mainCoords[i, 1] = UnityEngine.Random.Range(1, 3);
		} else {
			mainCoords[i, 1] = 3*UnityEngine.Random.Range(0, 2);
		}
		hintLights[i] = mainCoords[i, 0];
		if (mainCoords[i, 1] > 1) { hintLights[i] = hintLights[i]+4; }
		hintGrid[i, hintLights[i]] = 2;
		}

		for (int i = 0; i < 8; i++) {
			for (int j = 0; j < 2; j++) {
				if (hintGrid[j, i] == 0) { hintGrid[j, i] = UnityEngine.Random.Range(0, 2); }
			}
		}
		for (int i = 0; i < 2; i++) {
			Debug.LogFormat("[Wind-Up Maze #{0}] Mode {1} grid:\n{2} - - {5}\n- {3} {4} -\n- {7} {8} -\n{6} - - {9}", moduleId, i, hintGrid[i, 0], hintGrid[i, 1], hintGrid[i, 2], hintGrid[i, 3], hintGrid[i, 4], hintGrid[i, 5], hintGrid[i, 6], hintGrid[i, 7]);
		}
	}

	void shiftCoords() {
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
		Debug.Log(Ser3rd);//Bomb.GetBatteryCount(1)
		for (int i = 0; i < 8; i++) {
			for (int j = 0; j < 2; j++) {
				if (hintGrid[j, i] == 1) {
					if (i == 0) {
						mainCoords[j, 0] += Bomb.GetBatteryCount(1);
					} else if (i == 1) {
						mainCoords[j, 0] += Bomb.GetSerialNumberNumbers().Count();
					} else if (i == 2) {
						mainCoords[j, 1] += Bomb.GetSerialNumberLetters().Count();
					} else if (i == 3) {
						mainCoords[j, 1] += Bomb.GetBatteryCount(2);
					} else if (i == 4) {
						mainCoords[j, 1] += Bomb.GetOnIndicators().Count();
					} else if (i == 5) {
						mainCoords[j, 1] += Ser3rd;
					} else if (i == 6) {
						mainCoords[j, 0] += Ser6th;
					} else {
						mainCoords[j, 0] += Bomb.GetOffIndicators().Count();
					}
					if (mainCoords[j, 0] > 3) { mainCoords[j, 0] = mainCoords[j, 0]%4; }
					if (mainCoords[j, 1] > 3) { mainCoords[j, 1] = mainCoords[j, 1]%4; }
				}
			}
		}
	}

	//----INIT END----//

	void Mode() {
		if (hintMode == 1) { hintMode = 0; } else { hintMode = 1; }
	}

	void Move () {
		Audio.PlaySoundAtTransform("click5", transform);
		if (moduleSolved) { return; }
		int mDir = (facing + 1) % 2;
		//Debug.Log(chosenMaze);
		//Debug.Log(coords[0] + (coords[1] * 4));
		//Debug.Log(facing % 2);
		//Debug.Log((facing % 2)+1 + faceOrder[facing, mDir] );
		bool wall = Mazes[chosenMaze, (coords[0] + (coords[1] * 4)), ((facing % 2)+1 + faceOrder[facing, mDir])];
		//Debug.Log(wall);
		if (!wall) {
			Debug.LogFormat("[Wind-Up Maze #{0}] Hit a wall at {1}, {2} facing {3}. Striking", moduleId, coords[0], coords[1], faceName[facing]);
			Module.HandleStrike();
			return;
		} else { coords[0] += faceOrder[facing, 0]; coords[1] += faceOrder[facing, 1]; }
		Debug.LogFormat("[Wind-Up Maze #{0}] Moved to {1}, {2}", moduleId, coords[0], coords[1]);
	}

	void PlaceKey () {
		if (MasterKey.GlobalKeyHeld && HasKey == 0) {
			MasterKey.GlobalKeyHeld = false;
			HasKey = 3;
			Keys[2].SetActive(true);
			TurnArrowsTransform[0].SetActive(true);
			TurnArrowsTransform[1].SetActive(true);
			Audio.PlaySoundAtTransform("Key_In", transform);
		} else if (HasKey == 3) {
			MasterKey.GlobalKeyHeld = true;
			HasKey = 0;
			Keys[2].SetActive(false);
			TurnArrowsTransform[0].SetActive(false);
			TurnArrowsTransform[1].SetActive(false);
			Audio.PlaySoundAtTransform("keys_01", transform);
		}
	}

	void PlaceMode () {
		if (MasterKey.GlobalKeyHeld && HasKey == 0) {
			MasterKey.GlobalKeyHeld = false;
			HasKey = 4;
			Keys[3].SetActive(true);
			if (hintMode == 1) { TurnArrowsTransform[2].SetActive(true); }
			if (hintMode == 0) { TurnArrowsTransform[3].SetActive(true); }
			Audio.PlaySoundAtTransform("Key_In", transform);
		} else if (HasKey == 4) {
			MasterKey.GlobalKeyHeld = true;
			HasKey = 0;
			Keys[3].SetActive(false);
			TurnArrowsTransform[2].SetActive(false);
			TurnArrowsTransform[3].SetActive(false);
			Audio.PlaySoundAtTransform("keys_01", transform);
		}
	}

	void Turn2 (KMSelectable Arrow) {
		int arrow = Array.IndexOf(TurnArrows2, Arrow);
		if (HasKey != 4) { return; }
		if (turnFrame2 != 0) { return; }
		direction = (arrow % 2)-1;
		if (direction == 0) { direction = 1; }
		//Debug.Log(direction);
		//Debug.Log(dialVals[HasKey-1]);
		//if (HasKey == 0 || HasKey == 4) { return; }
		//dialVals[HasKey-1]+=1*direction;
		//Debug.Log(direction);
		if (hintMode == 0) {
			hintMode = 1;
			TurnArrowsTransform[2].SetActive(true);
			TurnArrowsTransform[3].SetActive(false);
		} else {
			hintMode = 0;
			TurnArrowsTransform[2].SetActive(false);
			TurnArrowsTransform[3].SetActive(true);
		}

		turnFrame2 = 6;
		Audio.PlaySoundAtTransform("Key_Out", transform);
	}

	void Turn (KMSelectable Arrow) {
		int arrow = Array.IndexOf(TurnArrows, Arrow);
		if (HasKey != 3) { return; }
		if (turnFrame != 0) { return; }
		Audio.PlaySoundAtTransform("Key_Out", transform);
		direction = (arrow % 2)-1;
		if (direction == 0) { direction = 1; }
		//Debug.Log(direction);
		//Debug.Log(Values[HasKey-1]);
		//if (HasKey == 0 || HasKey == 4) { return; }
		//Values[HasKey-1]+=1*direction;
		//Debug.Log(direction);
		//Values[HasKey-1] += 1*direction;
		facing += direction;
		if (facing == -1) { facing = 3; } else if (facing == 4) { facing = 0; }
		//Debug.LogFormat("[Wind-Up Maze #{0}] ", moduleId, coords[0], coords[1]);
		turnFrame = 10;
	}

	void Press (KMSelectable Keyhole) {
		int keyNum = Array.IndexOf(KeyHoles, Keyhole);
		if (keyNum == HasKey-1) {held = true;}
	}

	void Release (KMSelectable Keyhole) {
		int keyNum = Array.IndexOf(KeyHoles, Keyhole);
		if (heldFrame < 10) {
			if (MasterKey.GlobalKeyHeld && HasKey == 0) {
				MasterKey.GlobalKeyHeld = false;
				HasKey = keyNum+1;
				Keys[keyNum].SetActive(true);
				Audio.PlaySoundAtTransform("Key_In", transform);
			} else if (HasKey == keyNum+1) {
				MasterKey.GlobalKeyHeld = true;
				HasKey = 0;
				Keys[keyNum].SetActive(false);
				Audio.PlaySoundAtTransform("keys_01", transform);
			}
		}
		held = false;
		//heldFrame = 0;
	}

	IEnumerator Animate () {
		while(true){
			printHints();
			//Debug.Log(heldFrame);
			int keySlot = HasKey-1;
			if (held && heldFrame < 15 && (HasKey == 1 || HasKey == 2)) { heldFrame += 1; }
			if ((HasKey == 1 || HasKey == 2) && held && heldFrame > 9 && heldFrame < 15) {
				KeyRotation[keySlot*2].Rotate(0.0f, 0.0f, -8.0f);
				KeyRotation[keySlot*2+1].Rotate(0.0f, 8.0f, 0.0f);
				if (heldFrame == 10) { Audio.PlaySoundAtTransform("door_open_01", transform); }
				if (heldFrame == 14) { CheckSolve(); }
			} else if (!held && heldFrame > 9) {
				if (heldFrame == 15) { heldFrame--; }
				KeyRotation[keySlot*2].Rotate(0.0f, 0.0f, 8.0f);
				KeyRotation[keySlot*2+1].Rotate(0.0f, -8.0f, 0.0f);
				heldFrame -= 1;
				if (heldFrame == 9) { heldFrame = 0; }
			} else if (!held && heldFrame < 10) { heldFrame = 0; }
			
			if (turnFrame > 5) {
				KeyRotation[4].Rotate(0.0f, 0.0f, -8.0f*direction);
				KeyRotation[5].Rotate(0.0f, 8.0f*direction, 0.0f);
				turnFrame--;
			} else if (turnFrame > 0) {
				KeyRotation[4].Rotate(0.0f, 0.0f, 8.0f*direction);
				KeyRotation[5].Rotate(0.0f, -8.0f*direction, 0.0f);
				turnFrame--;
			}

			if (turnFrame2 != 0) {
				KeyRotation[6].Rotate(0.0f, 0.0f, -15.0f*direction);
				KeyRotation[7].Rotate(0.0f, 15.0f*direction, 0.0f);
				turnFrame2--;
			}

			yield return new WaitForSeconds(0.01f);
		}
	}

	void printHints() {
		tempCoords[1].text = hintMode.ToString();
		for (int i = 0; i < 8; i++) {
			if (!moduleSolved) { Bulbs[i].material = BulbColors[hintGrid[hintMode, i]]; } else { Bulbs[i].material = BulbColors[0]; }
		}
	}

	void CheckSolve () {
		if (moduleSolved) { return; }
		if (HasKey == 1) {
			coords[0] = mainCoords[0, 0];
			coords[1] = mainCoords[0, 1];
			facing = 0;
			Debug.LogFormat("[Wind-Up Maze #{0}] Resetting to {1}, {2} facing {3}", moduleId, coords[0], coords[1], faceName[facing]);
			return;
		}
		Debug.LogFormat("[Wind-Up Maze #{0}] Submitted {1}, {2}", moduleId, coords[0], coords[1]);
		if (coords[0] == mainCoords[1, 0] && coords[1] == mainCoords[1, 1]) { Debug.LogFormat("[Wind-Up Maze #{0}] Correct", moduleId); moduleSolved = true; Module.HandlePass(); } else { coords = new int[] {mainCoords[0, 0], mainCoords[0, 1]}; facing = 0; Debug.LogFormat("[Wind-Up Maze #{0}] Wrong. Striking and resetting to {1}, {2} facing {3}", moduleId, coords[0], coords[1], faceName[facing]); Module.HandleStrike(); }
	}
	
	void Update () {
		//if (needyActive) {  }
	}
}
