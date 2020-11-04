﻿using UnityEngine;
public class CalibrateUsingHands : MonoBehaviour
{
    //Usage: Add this script to the head position. Call SetHeadPositionUsingHands() when hands are positioned on the steering wheel. 
    //Returns: bool indicating succes.
    //Workings:
    //(1) Checks if both hands are visible
    //(2) Calculates the vector from average of two hands to the camera
    //(3) Sets the position of this object (i.e., the head position) to match this vector w.r.t. the steeringwheel inside the car

    public Transform driverView;
    //LeapMotion rigged hand prefabs
    public Leap.Unity.HandModel leftHand;
    public Leap.Unity.HandModel rightHand;

    //Wrist positions of the steering wheel:
    public Transform steeringWheel;
    private Transform centreWrists;

    private Vector3 handsToCam;
    private Vector3 handToHand;
    private Vector3 leftWristPos;
    private Vector3 rightWristPos;
    private Vector3 steeringWheelToCam;
    public bool SetPositionUsingHands()
    {
        if(driverView == null) { Debug.Log("Driver view is not set!"); }
        if(centreWrists == null) { centreWrists = steeringWheel.transform.Find("CentreWrists"); }
        //Some checks
        if (centreWrists == null) { Debug.Log("could not find predefined wrist position on steering wheel..."); return false; }

        if (leftHand.gameObject.activeSelf && rightHand.gameObject.activeSelf)
        {
            leftWristPos = leftHand.palm.position;
            rightWristPos = rightHand.palm.position;

            Vector3 posCentre = (rightWristPos + leftWristPos) / 2 + (steeringWheel.position - centreWrists.position);
            steeringWheelToCam = driverView.position - posCentre;

            //Set some other handy vectors
            handsToCam = driverView.position - (leftWristPos + rightWristPos) / 2;          
            handToHand = rightWristPos - leftWristPos;

            //Set steeringwheel position accordingly
            steeringWheel.position = transform.position - steeringWheelToCam;
            Debug.Log($"Succesfully calibrated headposition with hands on steering wheel, steeringWheelToCam: {steeringWheelToCam}...");
            return true;
        }
        else { Debug.Log("Could not set hand position..."); return false; }
    }

    public void SetLeftHand(){ if (leftHand.gameObject.activeSelf) { leftWristPos = leftHand.palm.position; } }
    public void SetRightHand() { if (rightHand.gameObject.activeSelf) { rightWristPos = rightHand.palm.position; } }
    public Vector3 GetHandsToCam() { return handsToCam; }
    public Vector3 GetSteeringWheelToCam(){return steeringWheelToCam; } 
    public Vector3 GetLeftHandPos() { return leftHand.palm.position; }
    public Vector3 GetRightHandPos() { return rightHand.palm.position; }
    public Vector3 GetRightToLeftHand() { return handToHand; }

}
