using UnityEngine;
using System.Collections;

public class CamDirectionLight : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        this.transform.forward = Camera.main.transform.forward;
	}
}
