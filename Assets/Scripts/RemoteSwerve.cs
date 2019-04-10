﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Newtonsoft.Json;

public class RemoteSwerve : MonoBehaviour {

    const string RemoteSwerveObjName = "test.RemoteSwerve";

    public WheelCollider lfWheel;
    public WheelCollider rfWheel;
    public WheelCollider lrWheel;
    public WheelCollider rrWheel;

    public float maxWheelSpeed;
    public float maxMotorSpeed = 18700f; // rev/min
    public float maxBrakeTorque;
    public float Kv = 1570.05f; // rev/volt-min
    public float Kt = 0.00530f; // Nm/A
    public float R = 0.0896f; // ohms

    private string objectName = "remoteSwerve";
    private SwerveStatus status = new SwerveStatus();
    private float gearing;

	// Use this for initialization
	void Start ()
    {
        gearing = maxWheelSpeed / maxMotorSpeed; // output over input
        Debug.Log("Gearing: " + gearing);
        float width = transform.localScale.x;
        float length = transform.localScale.z;
        object obj = RPC.Instance.InstantiateObject<object>(RemoteSwerveObjName, objectName,
            new string[] { "java.lang.Double", "java.lang.Double" },
            new object[] { width, length });
        Debug.Log("Instantiating remote object, success: " + (obj != null));
        StartCoroutine(CallRemote());
	}

    private float GetTorque(WheelCollider wheel, float volts)
    {
        // V = (T/Kt)*R + w/Kv
        // T = (V-w/Kv)*Kt/R
        float volts_abs = Math.Abs(volts);
        float rpm = Math.Abs(wheel.rpm / gearing);
        float back_emf = Math.Min(rpm / Kv, volts_abs);
        float torque_abs = (volts_abs - back_emf) * Kt / R;
        float torque = Math.Sign(volts) * torque_abs / gearing;
        Debug.LogFormat("Volts: {0}, rpm: {1}, out torque: {2}", volts, wheel.rpm, torque);
        return torque;
    }
	
	// Update is called once per frame
	void Update ()
    {
        SetWheel(lfWheel, status.lfPower, status.lfAngle);
        SetWheel(rfWheel, status.rfPower, status.rfAngle);
        SetWheel(lrWheel, status.lrPower, status.lrAngle);
        SetWheel(rrWheel, status.rrPower, status.rrAngle);
    }

    private void SetWheel(WheelCollider wheel, float power, float angle)
    {
        if(power != 0)
        {
            float volts = power * 12f;
            wheel.motorTorque = GetTorque(wheel, volts);
            wheel.brakeTorque = 0;
        }
        else
        {
            wheel.motorTorque = 0;
            wheel.brakeTorque = maxBrakeTorque;
        }
        wheel.steerAngle = angle;
        Vector3 rot = wheel.transform.localEulerAngles;
        Vector3 targetRot = new Vector3(rot.x, wheel.steerAngle, rot.z);
        wheel.transform.localEulerAngles = targetRot;
    }

    IEnumerator CallRemote()
    {
        while (true)
        {
            float x = Input.GetAxis("Horizontal");
            float y = Input.GetAxis("Vertical");
            float turn = Input.GetAxis("Turn");
            float heading = transform.eulerAngles.y;

            status = RPC.Instance.ExecuteMethod<SwerveStatus>(objectName, "getStatus",
                new string[] { "java.lang.Double",  "java.lang.Double", "java.lang.Double", "java.lang.Double" },
                new object[] { x, y, turn, heading });
            yield return new WaitForSeconds(0.005f);
        }
    }

    [Serializable]
    private class SwerveStatus
    {
        public float lfPower, lfAngle;
        public float rfPower, rfAngle;
        public float lrPower, lrAngle;
        public float rrPower, rrAngle;
    }
}
