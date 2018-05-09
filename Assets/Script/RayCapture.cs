using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA.Input;
public class RayCapture : MonoBehaviour {

    GestureRecognizer recognizer;
    int cliker_times = 0;
    List<Vector3> dirVec = new List<Vector3>();
    List<Vector3> posVec = new List<Vector3>();
    async void aware()
    {
        recognizer = new GestureRecognizer();
        // Set up a GestureRecognizer to detect Select gestures.
        recognizer.Tapped += (args) =>
        {
            SendMessage("OnCilck", SendMessageOptions.DontRequireReceiver);
        };
        recognizer.StartCapturingGestures();
    } 
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    void OnClick()
    {
        cliker_times++;
        var objSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //objSphere.AddComponent(Rigidbody);
        objSphere.name = string.Format(@"Sphere_{0}", cliker_times);
        objSphere.GetComponent<Renderer>().material.color = Color.red;
        objSphere.transform.position = GameObject.Find("Cursor").transform.position;
        objSphere.transform.localScale =new Vector3(0.005f,0.005f,0.005f);
        Debug.Log(string.Format(@"Clicker_Times = {0}", cliker_times));
        if (cliker_times % 3 != 0)
        {
            Vector3 dir = Camera.main.transform.forward;
            Vector3 pos = Camera.main.transform.position;
            dirVec.Add(dir);
            posVec.Add(pos);
        }
        if (cliker_times % 3 == 0)
        {
            Vector3 Markerpos = ClosestPointBetweenRays(posVec[0], dirVec[0], dirVec[1], dirVec[1]);

            Debug.Log("RayPos = " + Markerpos.ToString("f4"));
            dirVec.Clear();
            posVec.Clear();
        }


        Debug.Log("CursorPos = " + GameObject.Find("Cursor").transform.position.ToString("f4"));
        

    }

    public static Vector3 ClosestPointBetweenRays(
    Vector3 point1, Vector3 normalizedDirection1,
    Vector3 point2, Vector3 normalizedDirection2)
    {
        float directionProjection = Vector3.Dot(normalizedDirection1, normalizedDirection2);
        if (directionProjection == 1)
        {
            return point1; // parallel lines
        }
        float projection1 = Vector3.Dot(point2 - point1, normalizedDirection1);
        float projection2 = Vector3.Dot(point2 - point1, normalizedDirection2);
        float distanceAlongLine1 = (projection1 - directionProjection * projection2) / (1 - directionProjection * directionProjection);
        float distanceAlongLine2 = (projection2 - directionProjection * projection1) / (directionProjection * directionProjection - 1);
        Vector3 pointOnLine1 = point1 + distanceAlongLine1 * normalizedDirection1;
        Vector3 pointOnLine2 = point2 + distanceAlongLine2 * normalizedDirection2;
        return Vector3.Lerp(pointOnLine2, pointOnLine1, 0.5f);
    }
}
