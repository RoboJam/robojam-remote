using UnityEngine;
using System.Collections;
using Leap;

// Leap Motion hand script that detects pinches and grabs the
// closest rigidbody with a spring force if it's within a given range.
public class LeapMotionTest : MonoBehaviour
{
    private const float TRIGGER_DISTANCE_RATIO = 0.7f;

    public float forceSpringConstant = 100.0f;
    public float magnetDistance = 2.0f;

    private bool pinching_;
    private Collider grabbed_;

    private Controller leapController_;

    private Frame prevFrame_;

    enum ControlMode
    {
        None,
        Move,
        Rotate,
    };
    static ControlMode controlMode_ = ControlMode.None;

    void Start()
    {
        pinching_ = false;
        grabbed_ = null;
    }

    void OnPinch(Vector3 pinch_position)
    {
        pinching_ = true;

#if false
    // Check if we pinched a movable object and grab the closest one that's not part of the hand.
    Collider[] close_things = Physics.OverlapSphere(pinch_position, magnetDistance);
    Vector3 distance = new Vector3(magnetDistance, 0.0f, 0.0f);

    for (int j = 0; j < close_things.Length; ++j) {
      Vector3 new_distance = pinch_position - close_things[j].transform.position;
      if (close_things[j].rigidbody != null && new_distance.magnitude < distance.magnitude &&
          !close_things[j].transform.IsChildOf(transform)) {
        grabbed_ = close_things[j];
        distance = new_distance;
      }
    }
#endif
        Debug.Log("Pintch");
    }

    void OnRelease()
    {
        grabbed_ = null;
        pinching_ = false;
        Debug.Log("Pintch Release");
    }

    public static Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
        Quaternion q = new Quaternion();
        q.w = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] + m[1, 1] + m[2, 2])) / 2;
        q.x = Mathf.Sqrt(Mathf.Max(0, 1 + m[0, 0] - m[1, 1] - m[2, 2])) / 2;
        q.y = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] + m[1, 1] - m[2, 2])) / 2;
        q.z = Mathf.Sqrt(Mathf.Max(0, 1 - m[0, 0] - m[1, 1] + m[2, 2])) / 2;
        q.x *= Mathf.Sign(q.x * (m[2, 1] - m[1, 2]));
        q.y *= Mathf.Sign(q.y * (m[0, 2] - m[2, 0]));
        q.z *= Mathf.Sign(q.z * (m[1, 0] - m[0, 1]));
        return q;
    }

    void Update()
    {
        bool trigger_pinch = false;

        HandModel hand_model = GetComponent<HandModel>();
        if (hand_model == null)
            return;

        Hand leap_hand = hand_model.GetLeapHand();
        if (leap_hand == null)
            return;
        if (null==prevFrame_)
        {
            prevFrame_ = leap_hand.Frame;
            return;
        }

        //MyTest
        {
            if(leap_hand.IsRight)
            {
                var tgtObj = GameObject.Find("GameObject_meshTest");

                if (controlMode_ == ControlMode.Move)
                {
                    float pitch = -leap_hand.Direction.Pitch;
                    float yaw = leap_hand.Direction.Yaw;
                    float roll = leap_hand.PalmNormal.Roll;
                    var rQ = new Quaternion();
                    rQ.eulerAngles = new Vector3(pitch * Mathf.Rad2Deg, yaw * Mathf.Rad2Deg, roll * Mathf.Rad2Deg);

                    leap_hand.PalmVelocity.ToUnityScaled();

                    tgtObj.transform.position += leap_hand.PalmVelocity.ToUnityScaled();
                    //tgtObj.transform.localRotation = rQ;
                }                
            }
            else
            {
                if (leap_hand.PinchStrength > 0.5f)
                {
                    if (controlMode_ == ControlMode.None)
                    {
                        controlMode_ = ControlMode.Move;
                    }
                    else
                    {
                        var rotAngle = leap_hand.RotationAxis(prevFrame_);
                        Debug.Log(string.Format("{0}", rotAngle.ToString()));
                    }
                }
                else
                {
                    controlMode_ = ControlMode.None;
                }
            }
        }


        // Scale trigger distance by thumb proximal bone length.
        Vector leap_thumb_tip = leap_hand.Fingers[0].TipPosition;
        float proximal_length = leap_hand.Fingers[0].Bone(Bone.BoneType.TYPE_PROXIMAL).Length;
        float trigger_distance = proximal_length * TRIGGER_DISTANCE_RATIO;

        // Check thumb tip distance to joints on all other fingers.
        // If it's close enough, start pinching.
        for (int i = 1; i < HandModel.NUM_FINGERS && !trigger_pinch; ++i)
        {
            Finger finger = leap_hand.Fingers[i];

            for (int j = 0; j < FingerModel.NUM_BONES && !trigger_pinch; ++j)
            {
                Vector leap_joint_position = finger.Bone((Bone.BoneType)j).NextJoint;
                if (leap_joint_position.DistanceTo(leap_thumb_tip) < trigger_distance)
                    trigger_pinch = true;
            }
        }

        Vector3 pinch_position = hand_model.fingers[0].GetTipPosition();

        // Only change state if it's different.
        if (trigger_pinch && !pinching_)
            OnPinch(pinch_position);
        else if (!trigger_pinch && pinching_)
            OnRelease();

        // Accelerate what we are grabbing toward the pinch.
        if (grabbed_ != null)
        {
            Vector3 distance = pinch_position - grabbed_.transform.position;
            grabbed_.rigidbody.AddForce(forceSpringConstant * distance);
        }
    }
}
