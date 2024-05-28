using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCvSharp;
using UnityEngine.UI;
using Gather.Data;

public class Test_Opencv1 : MonoBehaviour
{
    #region public members
    public Texture2D m_texture;

    public RawImage m_image_origin;
    public RawImage m_image_gray;
    public RawImage m_Image_binarization;
    public RawImage m_image_mask;
    public RawImage m_image_backgroundTransparent;

    public double v_thresh = 180;
    public double v_maxval = 255;
    #endregion

    private void Start()
    {
        #region load texture
        Mat origin = OpenCvSharp.Unity.TextureToMat(this.m_texture);
        m_image_origin.texture = OpenCvSharp.Unity.MatToTexture(origin);
        #endregion

        #region  Gray scale image
        Mat grayMat = new Mat();
        Cv2.CvtColor(origin, grayMat, ColorConversionCodes.BGR2GRAY);
        m_image_gray.texture = OpenCvSharp.Unity.MatToTexture(grayMat);
        #endregion

        #region Find Edge
        Mat thresh = new Mat();
        Cv2.Threshold(grayMat, thresh, v_thresh, v_maxval, ThresholdTypes.BinaryInv);
        m_Image_binarization.texture = OpenCvSharp.Unity.MatToTexture(thresh);
        #endregion

        #region Create Mask
        Mat Mask = OpenCvSharp.Unity.TextureToMat(OpenCvSharp.Unity.MatToTexture(grayMat));
        Point[][] contours; HierarchyIndex[] hierarchy;
        Cv2.FindContours(thresh, out contours, out hierarchy, RetrievalModes.Tree, ContourApproximationModes.ApproxNone, null);
        for (int i = 0; i < contours.Length; i++)
        {
            Cv2.DrawContours(Mask, new Point[][] { contours[i] }, 0, new Scalar(0, 0, 0), -1);
        }
        Mask = Mask.CvtColor(ColorConversionCodes.BGR2GRAY);
        Cv2.Threshold(Mask, Mask, v_thresh, v_maxval, ThresholdTypes.Binary);
        m_image_mask.texture = OpenCvSharp.Unity.MatToTexture(Mask);
        #endregion

        #region TransparentBackground
        Mat transparent = origin.CvtColor(ColorConversionCodes.BGR2BGRA);
        unsafe
        {
            byte* b_transparent = transparent.DataPointer;
            byte* b_mask = Mask.DataPointer;
            float pixelCount = transparent.Height * transparent.Width;

            for (int i = 0; i < pixelCount; i++)
            {
                if (b_mask[0] == 255)
                {
                    b_transparent[0] = 0;
                    b_transparent[1] = 0;
                    b_transparent[2] = 0;
                    b_transparent[3] = 0;
                }
                b_transparent = b_transparent + 4;
                b_mask = b_mask + 1;
            }
        }
        m_image_backgroundTransparent.texture = OpenCvSharp.Unity.MatToTexture(transparent);
        #endregion
    }

}
