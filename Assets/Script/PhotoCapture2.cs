using System;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.WSA.WebCam;
using UnityEngine.XR.WSA.Input;
using System.Collections.Generic;
using System.IO;
#if WINDOWS_UWP
using System.Threading.Tasks;
#endif

public class PhotoCapture2 : MonoBehaviour {
    PhotoCapture photoCaptureObject = null;
    Matrix4x4 cameraToWorldMatrix = new Matrix4x4(), ProjectionMatrix = new Matrix4x4();
    CameraParameters cameraParameters = new CameraParameters();

    GestureRecognizer recognizer;
    bool flag_camisprocessing = false;
    int cliker_times = 0;

    List<Vector2> screenPosVec = new List<Vector2>();
    List<Matrix4x4> cameraToWorldMatrixVec = new List<Matrix4x4>();

    void Awake()
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
    void Start()
    {
        
    }

    void Update()
    {

    }
    void OnCilck()
    {
        if (!flag_camisprocessing)
        {
            cliker_times++;

            Debug.Log(string.Format(@"Clicker_Times = {0}", cliker_times));
            Locate_Camera_Process();
        }
        
    }
    void Locate_Camera_Process()
    {
        if (!flag_camisprocessing)
        {
            flag_camisprocessing = true;
            
            try
            {
                // Create a PhotoCapture object
                PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject)
                {
                    photoCaptureObject = captureObject;
                    Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
                    cameraParameters.hologramOpacity = 0.0f;
                    cameraParameters.cameraResolutionWidth = cameraResolution.width;
                    cameraParameters.cameraResolutionHeight = cameraResolution.height;
                    cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;
                   

                    // Activate the camera
                    photoCaptureObject.StartPhotoModeAsync(cameraParameters, delegate (PhotoCapture.PhotoCaptureResult result)
                    {
                        // Take a picture
                        if (result.success)
                        { 
                            photoCaptureObject.TakePhotoAsync(OnCapturedPhotoToMemory);
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                Debug.Log("something is wrong with locate_camera!");
            }
        }
    }
    void OnCapturedPhotoToMemory(PhotoCapture.PhotoCaptureResult result, PhotoCaptureFrame photoCaptureFrame)
    {
        // Copy the raw image data into the target texture
        //photoCaptureFrame.UploadImageDataToTexture(targetTexture);
        Texture2D targetTexture = new Texture2D(cameraParameters.cameraResolutionWidth, cameraParameters.cameraResolutionHeight);


        string filename = string.Format(@"CapturedImage{0}_n.jpg", Time.time);
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, filename);
        photoCaptureFrame.UploadImageDataToTexture(targetTexture);
        byte[] bytes = targetTexture.EncodeToPNG();
        if (bytes != null && bytes.Length > 0)
        {
            try
            {
                File.WriteAllBytes(filePath, bytes);
                Debug.Log("write " + filePath);
            }
            catch (Exception ex)
            {
                Debug.Log("Failed to write Image");

            }

        }
        Debug.Log("write " + filePath);
        photoCaptureFrame.TryGetCameraToWorldMatrix(out cameraToWorldMatrix);
        photoCaptureFrame.TryGetProjectionMatrix(out ProjectionMatrix);
        Debug.Log("cameraToWorldMatrix = ");
        Debug.Log(cameraToWorldMatrix.ToString("f4"));
        Debug.Log("ProjectionMatrix = ");
        Debug.Log(ProjectionMatrix.ToString("f4"));
        Debug.Log("camera Position = ");
        Debug.Log(Camera.main.cameraToWorldMatrix.ToString("f4"));
        Matrix4x4 mat = Camera.main.cameraToWorldMatrix;
        mat = mat.inverse * cameraToWorldMatrix;
        Debug.Log("cameraToLocateCamera = ");
        Debug.Log(mat.ToString("f4"));
        if (cliker_times % 3 != 0)
        {
            cameraToWorldMatrixVec.Add(cameraToWorldMatrix);
            Vector3 pos = GameObject.Find("Sphere").transform.position;
            Vector2 pix = Camera.main.WorldToScreenPoint(pos);//世界坐标(0,0,0)，一般可以用transform.
            screenPosVec.Add(pix);
        }
        if (cliker_times % 3 == 0)
        {
            cameraToWorldMatrix = cameraToWorldMatrixVec[0];
            List<Vector3> line1 = CaculatePixelToWorld(screenPosVec[0]);
            cameraToWorldMatrix = cameraToWorldMatrixVec[1];
            List<Vector3> line2 = CaculatePixelToWorld(screenPosVec[1]);
            Vector3 dir1 = line1[1] - line1[0];
            Vector3 dir2 = line2[1] - line2[0];
            Vector3 positon = ClosestPointBetweenRays(line1[0], dir1.normalized, line2[0], dir2.normalized);
            Debug.Log("positon = ");
            Debug.Log(positon.ToString("f4"));
            cameraToWorldMatrixVec.Clear();
            screenPosVec.Clear();
        }
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
       
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        // Shutdown the photo capture resource
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
        flag_camisprocessing = false;
    }
    void CaculateWorldToCameraPixel()
    {

    }

    List<Vector3> CaculatePixelToWorld(Vector2 PixelPos)
    {
        List<Vector3> line = new List<Vector3>();
        Vector2 ImagePosZeroToOne =new Vector2(PixelPos.x / cameraParameters.cameraResolutionWidth, 1.0f - (PixelPos.y / cameraParameters.cameraResolutionWidth));
        Vector2 ImagePosProjected = new Vector2(ImagePosZeroToOne.x* 2.0f-1.0f, ImagePosZeroToOne.y * 2.0f - 1.0f); // -1 to 1 space
        Vector3 CameraSpacePos = UnProjectVector(ProjectionMatrix, new Vector3(ImagePosProjected.x, ImagePosProjected.y, 1));
        //矩阵维数未对应
        Vector3 WorldSpaceRayPoint1 = cameraToWorldMatrix * new Vector4(0, 0, 0, 1); // camera location in world space
        Vector3 WorldSpaceRayPoint2 = cameraToWorldMatrix * CameraSpacePos; // ray point in world space
        line.Add(WorldSpaceRayPoint1);
        line.Add(WorldSpaceRayPoint2);
        return line;
    }

    public static Vector3 UnProjectVector(Matrix4x4 proj, Vector3 to)
    {
        Vector3 from = new Vector3(0, 0, 0);
        var axsX = proj.GetRow(0);
        var axsY = proj.GetRow(1);
        var axsZ = proj.GetRow(2);
        from.z = to.z / axsZ.z;
        from.y = (to.y - (from.z * axsY.z)) / axsY.y;
        from.x = (to.x - (from.z * axsX.z)) / axsX.x;
        return from;
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
