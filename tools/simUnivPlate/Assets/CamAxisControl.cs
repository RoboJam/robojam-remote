﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 各所からのコピペで作ったカメラ操作クラス
/// </summary>
public class CamAxisControl : MonoBehaviour
{
    public float RotateSpeed = 0.1f;
    public float UpDownSpeed = 0.01f;
 
	void Start ()
	{  
        /*
		if (this.focusObj == null)
			this.setupFocusObject("CameraFocusObject");
        
		Transform trans = this.transform;
		transform.parent = this.focusObj.transform;

		trans.LookAt(this.focus);
        */
        return;
	}
	
	void Update ()
	{
        this.keyEvent();
		this.mouseEvent();
        this.touchEvent();
	}

    void touchEvent()
    {
        int touchCount = Input.touches
             .Count(t => t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled);
        if (touchCount == 1)
        {
            Touch t = Input.touches.First();
            switch (t.phase)
            {
                case TouchPhase.Moved:

                    //移動量
                    float xDelta = t.deltaPosition.x * RotateSpeed;
                    float yDelta = t.deltaPosition.y * UpDownSpeed;

                    //左右回転
                    this.transform.Rotate(0, xDelta, 0, Space.World);
                    //上下移動
                    this.transform.position += new Vector3(0, -yDelta, 0);

                    break;
            }
        }
    }

    void keyEvent()
    {
        if(Input.GetKey(KeyCode.UpArrow)||
           Input.GetKey(KeyCode.DownArrow)||
           Input.GetKey(KeyCode.LeftArrow)||
           Input.GetKey(KeyCode.RightArrow))
        {
            Vector3 moveV = new Vector3();
            if(Input.GetKey(KeyCode.UpArrow)){
                if (Input.GetKey(KeyCode.LeftShift)){
                    moveV.y = 1f;
                }
                else{
                    moveV.z = 1f;
                }
            }
            if(Input.GetKey(KeyCode.DownArrow)){
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    moveV.y = -1f;
                }
                else
                {
                    moveV.z = -1f;
                }
            }
            if(Input.GetKey(KeyCode.LeftArrow)){
                if (Input.GetKey(KeyCode.LeftShift))
                {
                }
                else
                {
                    moveV.x = -1f;
                }
            }
            if(Input.GetKey(KeyCode.RightArrow)){
                if (Input.GetKey(KeyCode.LeftShift))
                {
                }
                else
                {
                    moveV.x = 1f;
                }
            }
            moveV = transform.localToWorldMatrix * moveV;
            if (Input.GetKey(KeyCode.LeftShift))
            {
                moveV.z = 0;
            }
            else
            {
                moveV.y = 0;
            }
            if (moveV.magnitude > Vector3.kEpsilon)
            {
                moveV.Normalize();
                transform.position += moveV * 0.2f;
            }
        }
    }

    #region mouseEvent() その他

    enum MouseButtonDown
    {
        MBD_LEFT = 0,
        MBD_RIGHT,
        MBD_MIDDLE,
    };

    private Vector3 oldPos;

    void mouseEvent()
	{
        Vector3 diff = Input.mousePosition - oldPos;
        if (diff.magnitude < 10.0f)//タッチで操作するとマウスがジャンプするので閾値を設けておきます
        {
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                if (Mathf.Abs(diff.x) > Vector3.kEpsilon)
                {
                    var q = Quaternion.AngleAxis(diff.x, Vector3.up);
                    this.transform.rotation = q * this.transform.rotation;
                }
                if (Mathf.Abs(diff.y) > Vector3.kEpsilon)
                {
                    var crossAxis = Vector3.Cross(Camera.main.transform.forward, Vector3.up);
                    if (Mathf.Abs(Vector3.Dot(Camera.main.transform.forward, Vector3.up)) > 0.9f)
                    {
                        crossAxis = Vector3.Cross(Camera.main.transform.up, Vector3.up);
                    }
                    var q = Quaternion.AngleAxis(diff.y, crossAxis);
                    this.transform.rotation = q * this.transform.rotation;
                }
            }
        }
        this.oldPos = Input.mousePosition;

		return;
	}

	public void mouseWheelEvent(float delta)
	{
		Vector3 focusToPosition = this.transform.position - this.focus;

		Vector3 post = focusToPosition * (1.0f + delta);

		if (post.magnitude > 0.01)
			this.transform.position = this.focus + post;

		return;
	}
    #endregion

    #region setupFocusObject(),
    [SerializeField]
    private Vector3 focus = Vector3.zero;
    [SerializeField]
    private GameObject focusObj = null;

    void setupFocusObject(string name)
    {
        GameObject obj = this.focusObj = new GameObject(name);
        obj.transform.position = this.focus;
        obj.transform.LookAt(this.transform.position);

        return;
    }
    #endregion

    #region cameraTranslate(),cameraRotate() カメラ操作補助

    void cameraTranslate(Vector3 vec)
	{
		Transform focusTrans = this.focusObj.transform;

		vec.x *= -1;

		focusTrans.Translate(Vector3.right * vec.x);
		focusTrans.Translate(Vector3.up * vec.y);

		this.focus = focusTrans.position;

		return;
	}

	public void cameraRotate(Vector3 eulerAngle)
	{
		Transform focusTrans = this.focusObj.transform;
		focusTrans.localEulerAngles = focusTrans.localEulerAngles + eulerAngle;
		this.transform.LookAt(this.focus);

		return;
    }

    #endregion
}
