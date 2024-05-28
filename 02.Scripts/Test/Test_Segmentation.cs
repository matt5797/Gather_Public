using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mediapipe.SelfieSegmentation;
using UnityEngine.UI;
using OpenCvSharp;
using OpenCvSharp.Util;

public class Test_Segmentation : MonoBehaviour
{
    public int webCamNum;
    WebCamTexture webCamTexture;
    [SerializeField] Vector2 webCamResolution = new Vector2(1920, 1080);

    [SerializeField] Shader shader;
    [SerializeField] Texture backGroundTexture;
    [SerializeField] SelfieSegmentationResource resource;

    MeshRenderer meshRenderer;

    SelfieSegmentation segmentation;

    [SerializeField] RawImage rawImage1;
    [SerializeField] RawImage rawImage2;
    [SerializeField] RawImage rawImage3;

    public RenderTexture renderTexture;
    public RenderTexture renderTexture2;

    // Start is called before the first frame update
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();

        segmentation = new SelfieSegmentation(resource);

        StartCoroutine(CaptureVideoStart());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void LateUpdate()
    {
        if (webCamTexture != null)
        {
            //inputImageUI.texture = webCamInput.inputImageTexture;

            // Predict segmentation by neural network model.
            segmentation.ProcessImage(webCamTexture);

            meshRenderer.material.SetTexture("_BaseMap", webCamTexture);
            meshRenderer.material.SetTexture("_WebcamInput", webCamTexture);
            meshRenderer.material.SetTexture("_SegmentMask", segmentation.texture);

            rawImage1.texture = webCamTexture;
            rawImage2.texture = segmentation.texture;
        }
    }

    public Texture2D Multiply(Texture2D baseTexture, Texture2D maskTexture)
    {
        Mat res = new Mat();
        Mat mat = OpenCvSharp.Unity.TextureToMat(baseTexture);
        Mat mat2 = OpenCvSharp.Unity.TextureToMat(maskTexture);

        Cv2.Multiply(mat, mat2, res);

        return OpenCvSharp.Unity.MatToTexture(res);
    }

    Texture2D toTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(512, 512, TextureFormat.RGB24, false);
        // ReadPixels looks at the active RenderTexture.
        RenderTexture.active = rTex;
        tex.ReadPixels(new UnityEngine.Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }

    private IEnumerator CaptureVideoStart()
    {
        if (WebCamTexture.devices.Length == 0)
        {
            Debug.LogFormat("WebCam device not found");
            yield break;
        }

        yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            Debug.LogFormat("authorization for using the device is denied");
            yield break;
        }
        
        webCamTexture = new WebCamTexture(WebCamTexture.devices[webCamNum].name, (int)webCamResolution.x, (int)webCamResolution.y);
        webCamTexture.Play();
        yield return new WaitUntil(() => webCamTexture.didUpdateThisFrame);
        
        yield break;
    }

    void CaptureVideoStop()
    {
        webCamTexture?.Stop();
        webCamTexture = null;
    }

    public void SaveTextureToPNGFile(Texture texture, string directoryPath, string fileName)
    {
        int width = texture.width;
        int height = texture.height;
        
        
    }
}
