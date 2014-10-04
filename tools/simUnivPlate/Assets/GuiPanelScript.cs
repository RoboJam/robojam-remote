using UnityEngine;
using System.Collections;

public class GuiPanelScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnGUI()
    {
        int offsY = 10;
        if (GUI.Button(new Rect(10, offsY, 100, 40), "Cut"))
        {
            var ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            var hitInfo = new RaycastHit();
            if(Physics.Raycast(ray,out hitInfo))
            {
                Debug.Log(hitInfo.collider.gameObject.ToString());

                var uniPlateObj = GetUniPlate_FindUpTree(hitInfo.collider.gameObject);
                if(uniPlateObj!=null)
                {
                    uniPlateObj.ExecuteCut();
                }
            }
        }
        offsY += 45;
        if (GUI.Button(new Rect(10, offsY, 100, 40), "Catch Center"))
        {
            var ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            var hitInfo = new RaycastHit();
            if(Physics.Raycast(ray,out hitInfo))
            {
                var uniPlateObj = GetUniPlate_FindUpTree(hitInfo.collider.gameObject);
                if (uniPlateObj != null)
                {
                    uniPlateObj.MoveToMainCamForward(50);
                }
            }
        }
    }

    UniPlate GetUniPlate_FindUpTree(GameObject gameObj)
    {
        for (; ; )
        {
            var uniPlateObj = gameObj.GetComponent<UniPlate>();
            if (uniPlateObj != null)
            {
                return uniPlateObj;
            }
            if (null == gameObj.transform.parent)
            {
                break;
            }
            gameObj = gameObj.transform.parent.gameObject;
        }
        return null;
    }

}
