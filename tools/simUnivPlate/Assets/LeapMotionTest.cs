using UnityEngine;
using System.Collections;
using Leap;

public class LeapMotionTest : MonoBehaviour {
    Controller controller;

	// Use this for initialization
	void Start () {
        controller = new Controller();
	}

	// Update is called once per frame
	void Update () {
       var frame = controller.Frame();
	
	}
}
