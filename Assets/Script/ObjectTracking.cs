using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
public class ObjectTracking : MonoBehaviour
{
    public string MarkerName;
    // Use this for initialization
    void Start()
    {
        Assert.IsFalse(string.IsNullOrEmpty(MarkerName));
    }

    // Update is called once per frame
    void Update()
    {
        try
        {
            gameObject.GetComponentInChildren<MeshRenderer>().enabled = true;
            GameObject camMarker = GameObject.Find("Main Camera");

            Vector3 probePos = MarkerManager.MarkerPositions[MarkerName];
            Quaternion probeOrien = MarkerManager.MarkerOrientations[MarkerName];
            Vector3 camPos = MarkerManager.MarkerPositions["Hololens"];
            Quaternion camOrien = MarkerManager.MarkerOrientations["Hololens"];

            Vector3 relPos = Quaternion.Inverse(camOrien) * (probePos - camPos);
            Quaternion relOrien = Quaternion.Inverse(camOrien) * probeOrien;

            Vector3 worldPos = camMarker.transform.rotation * relPos + camMarker.transform.position;
            Quaternion worldOrien = camMarker.transform.rotation * relOrien;
            transform.SetPositionAndRotation(worldPos, worldOrien);

        }
        catch (KeyNotFoundException ex)
        {
            gameObject.GetComponentInChildren<MeshRenderer>().enabled = false;
        }
    }
}