using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KModkit;

public static class MasterKey {
	//-----------------------------------------------------//
	public static bool GlobalKeyHeld = true;
	public static int windIdCounter = 1;
	public static string firstSerial = "";

	public static int ServeID (KMBombInfo Bomb) {
		//Debug.Log(Bomb.GetSerialNumber());
		if (firstSerial == "") { firstSerial = Bomb.GetSerialNumber(); }
		//Debug.Log(firstSerial);
		if (firstSerial != Bomb.GetSerialNumber()) { return 0; }
		return windIdCounter++;
	}

	public static bool PlaceKey (int ID) {
		if (ID+1 == windIdCounter) { GlobalKeyHeld = false; return true; }
		return false;
	}

	public static void ResetMaster () {
		GlobalKeyHeld = true;
		windIdCounter = 1;
		firstSerial = "";
	}

	//-----------------------------------------------------//
}
