# Wind-Up Documentation
## A brief note...
Before trying to make your own modules, it's recommended to play around with the ones in this mod first to get an idea of the mechanic and its various uses

I also highly recommend you set the original upload as a dependancy, as to avoid duplicate MasterKey.cs scripts causing errors

## Required Parts
There are 2 files important for keys to function

### Key Prefab


### MasterKey.cs

// Setting up //
	private bool HasKey = false;
	int windID = 0;

These are required for nearly all the key functions, as well as placing and grabbing

// REQUIRED FUNCTIONS //
	void Start () {
		windID = MasterKey.ServeID(Bomb);
	...
This grabs the wind up's ID in relation to where to place the key on startup

	IEnumerator CheckKey () {
		yield return new WaitForSeconds(0.01f);
		if (MasterKey.PlaceKey(windID)) { HasKey = true; Key.SetActive(true); Debug.LogFormat("[Wind The Key #{0}] Starting with key", moduleId); }
	}
This uses the previous function to place the key


### Using the Key
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
These are the fundementals of passing the key around
1. Checks to verify both the global and local variables are synced
2. Sets the global hold
3. Shows/Hides the key transform
4. Sets the local hold
5. Plays the place/pickup sound

From there, you can use the key in whatever creative ways you like

## Controller Support






public KMSelectable[] KeyHoles;
public KMSelectable[] TurnArrows;
public GameObject[] TurnArrowsTransform;

public GameObject[] Key;
public Transform[] KeyRotation;