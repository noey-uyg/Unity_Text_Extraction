using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

// Page Segmentation Mode (PSM) Enum
public enum PageSegMode
{
    PSM_OSD_ONLY = 0,
    PSM_AUTO_OSD = 1,
    PSM_AUTO_ONLY = 2,
    PSM_AUTO = 3,
    PSM_SINGLE_COLUMN = 4,
    PSM_SINGLE_BLOCK_VERT_TEXT = 5,
    PSM_SINGLE_BLOCK = 6,
    PSM_SINGLE_LINE = 7,
    PSM_SINGLE_WORD = 8,
    PSM_CIRCLE_WORD = 9,
    PSM_SINGLE_CHAR = 10,
    PSM_SPARSE_TEXT = 11,
    PSM_SPARSE_TEXT_OSD = 12,
    PSM_RAW_LINE = 13,
    PSM_COUNT = 14
}

public class TesseractWrapper
{
#if UNITY_EDITOR
    private const string TesseractDllName = "tesseract";
    private const string LeptonicaDllName = "tesseract";
#elif UNITY_ANDROID
    private const string TesseractDllName = "libtesseract.so";
    private const string LeptonicaDllName = "liblept.so";
#else
    private const string TesseractDllName = "tesseract";
    private const string LeptonicaDllName = "tesseract";
#endif

    private IntPtr _tessHandle;
    private Texture2D _highlightedTexture;
    private string _errorMsg;
    private const float MinimumConfidence = 60;

    [DllImport(TesseractDllName)]
    private static extern IntPtr TessVersion();

    [DllImport(TesseractDllName)]
    private static extern IntPtr TessBaseAPICreate();

    [DllImport(TesseractDllName)]
    private static extern int TessBaseAPIInit3(IntPtr handle, string dataPath, string language);

    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPIDelete(IntPtr handle);

    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPISetImage(IntPtr handle, IntPtr imagedata, int width, int height,
        int bytes_per_pixel, int bytes_per_line);

    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPISetImage2(IntPtr handle, IntPtr pix);

    [DllImport(TesseractDllName)]
    private static extern int TessBaseAPIRecognize(IntPtr handle, IntPtr monitor);

    [DllImport(TesseractDllName)]
    private static extern IntPtr TessBaseAPIGetUTF8Text(IntPtr handle);

    [DllImport(TesseractDllName)]
    private static extern void TessDeleteText(IntPtr text);

    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPIEnd(IntPtr handle);

    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPIClear(IntPtr handle);

    [DllImport(TesseractDllName)]
    private static extern IntPtr TessBaseAPIGetWords(IntPtr handle, IntPtr pixa);
    
    [DllImport(TesseractDllName)]
    private static extern IntPtr TessBaseAPIAllWordConfidences(IntPtr handle);

    [DllImport(TesseractDllName)]
    private static extern void TessBaseAPISetPageSegMode(IntPtr handle, int mode);

    [DllImport(TesseractDllName)]
    private static extern IntPtr TessBaseAPIGetHOCRText(IntPtr handle, int page);

    public TesseractWrapper()
    {
        _tessHandle = IntPtr.Zero;
    }

    public string Version()
    {
        IntPtr strPtr = TessVersion();
        string tessVersion = Marshal.PtrToStringAnsi(strPtr);
        return tessVersion;
    }

    public string GetErrorMessage()
    {
        return _errorMsg;
    }

    public bool Init(string lang, string dataPath, PageSegMode psm = PageSegMode.PSM_AUTO)
    {
        if (!_tessHandle.Equals(IntPtr.Zero))
            Close();

        try
        {
            _tessHandle = TessBaseAPICreate();
            if (_tessHandle.Equals(IntPtr.Zero))
            {
                _errorMsg = "TessAPICreate failed";
                return false;
            }

            if (string.IsNullOrWhiteSpace(dataPath))
            {
                _errorMsg = "Invalid DataPath";
                return false;
            }

            int init = TessBaseAPIInit3(_tessHandle, dataPath, lang);
            if (init != 0)
            {
                Close();
                _errorMsg = "TessAPIInit failed. Output: " + init;
                return false;
            }

            TessBaseAPISetPageSegMode(_tessHandle, (int)psm);
        }
        catch (Exception ex)
        {
            _errorMsg = ex + " -- " + ex.Message;
            return false;
        }

        return true;
    }

    public string Recognize(Texture2D texture)
    {
        if (_tessHandle.Equals(IntPtr.Zero))
            return null;

        _highlightedTexture = texture;

        // 이미지 전처리 단계
        //Texture2D grayTexture = ConvertToGrayscale(_highlightedTexture);
        //Texture2D binaryTexture = BinarizeImage(grayTexture);
        //Texture2D cleanTexture = RemoveNoise(binaryTexture);
        //Texture2D resizedTexture = AdjustResolution(cleanTexture);

        int width = _highlightedTexture.width;
        int height = _highlightedTexture.height;
        Color32[] colors = _highlightedTexture.GetPixels32();
        int count = width * height;
        int bytesPerPixel = 4;
        byte[] dataBytes = new byte[count * bytesPerPixel];
        int bytePtr = 0;

        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                int colorIdx = y * width + x;
                dataBytes[bytePtr++] = colors[colorIdx].r;
                dataBytes[bytePtr++] = colors[colorIdx].g;
                dataBytes[bytePtr++] = colors[colorIdx].b;
                dataBytes[bytePtr++] = colors[colorIdx].a;
            }
        }

        IntPtr imagePtr = Marshal.AllocHGlobal(count * bytesPerPixel);
        Marshal.Copy(dataBytes, 0, imagePtr, count * bytesPerPixel);

        TessBaseAPISetImage(_tessHandle, imagePtr, width, height, bytesPerPixel, width * bytesPerPixel);

        if (TessBaseAPIRecognize(_tessHandle, IntPtr.Zero) != 0)
        {
            Marshal.FreeHGlobal(imagePtr);
            return null;
        }

        IntPtr stringPtr = TessBaseAPIGetUTF8Text(_tessHandle);
        Marshal.FreeHGlobal(imagePtr);
        if (stringPtr.Equals(IntPtr.Zero))
            return null;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        string recognizedText = Marshal.PtrToStringAnsi(stringPtr);
#else
    string recognizedText = Marshal.PtrToStringAuto(stringPtr);
#endif
        recognizedText = recognizedText.Trim();

        TessBaseAPIClear(_tessHandle);
        TessDeleteText(stringPtr);

        return recognizedText;
    }

    #region 전처리
    // 그레이스케일 변환 함수
    private Texture2D ConvertToGrayscale(Texture2D original)
    {
        Texture2D grayTexture = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);
        for (int y = 0; y < original.height; y++)
        {
            for (int x = 0; x < original.width; x++)
            {
                Color pixel = original.GetPixel(x, y);
                float gray = pixel.r * 0.299f + pixel.g * 0.587f + pixel.b * 0.114f; // 가중 평균
                grayTexture.SetPixel(x, y, new Color(gray, gray, gray, 1.0f)); // Alpha를 1로 설정
            }
        }
        grayTexture.Apply();
        return grayTexture;
    }

    // 이진화 함수 (Otsu's Thresholding)
    private Texture2D BinarizeImage(Texture2D original)
    {
        Texture2D binaryTexture = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);
        float threshold = 0.5f; // 임계값을 0.5로 고정
        for (int y = 0; y < original.height; y++)
        {
            for (int x = 0; x < original.width; x++)
            {
                Color pixel = original.GetPixel(x, y);
                float binaryValue = (pixel.r < threshold) ? 0 : 1; // 0과 1로 변환
                binaryTexture.SetPixel(x, y, new Color(binaryValue, binaryValue, binaryValue, 1.0f)); // Alpha를 1로 설정
            }
        }
        binaryTexture.Apply();
        return binaryTexture;
    }

    // Otsu's Thresholding 알고리즘 구현
    private float OtsuThreshold(Texture2D original)
    {
        int[] histogram = new int[256];
        // 히스토그램 계산
        for (int y = 0; y < original.height; y++)
        {
            for (int x = 0; x < original.width; x++)
            {
                Color pixel = original.GetPixel(x, y);
                int gray = Mathf.RoundToInt(pixel.r * 255);
                gray = Mathf.Clamp(gray, 0, 255);
                histogram[gray]++;
            }
        }

        int total = original.width * original.height;
        float sum = 0;
        for (int t = 0; t < 256; t++)
            sum += t * histogram[t];

        float sumB = 0;
        int wB = 0;
        int wF = 0;

        float varMax = 0;
        float threshold = 0;

        for (int t = 0; t < 256; t++)
        {
            wB += histogram[t];
            if (wB == 0) continue;
            wF = total - wB;
            if (wF == 0) break;

            sumB += t * histogram[t];

            float mB = sumB / wB;
            float mF = (sum - sumB) / wF;

            float varBetween = (float)wB * (float)wF * (mB - mF) * (mB - mF);

            if (varBetween > varMax)
            {
                varMax = varBetween;
                threshold = t;
            }
        }

        return threshold / 255f;
    }

    // 노이즈 제거 함수 (Median Filter)
    private Texture2D RemoveNoise(Texture2D original)
    {
        Texture2D cleanTexture = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);
        for (int y = 1; y < original.height - 1; y++)
        {
            for (int x = 1; x < original.width - 1; x++)
            {
                List<float> neighbors = new List<float>();
                for (int ky = -1; ky <= 1; ky++)
                {
                    for (int kx = -1; kx <= 1; kx++)
                    {
                        Color pixel = original.GetPixel(x + kx, y + ky);
                        neighbors.Add(pixel.r); // 그레이스케일이므로 r, g, b 동일
                    }
                }
                neighbors.Sort();
                float median = neighbors[4]; // 중앙값
                cleanTexture.SetPixel(x, y, new Color(median, median, median, 1.0f)); // Alpha를 1로 설정
            }
        }
        cleanTexture.Apply();
        return cleanTexture;
    }

    // 해상도 조정 함수 (Dynamically adjust based on image size)
    private Texture2D ResizeImage(Texture2D original, int targetWidth, int targetHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight, 0);
        RenderTexture.active = rt;
        Graphics.Blit(original, rt);
        Texture2D resized = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
        resized.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
        resized.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return resized;
    }

    // 텍스트 크기에 따라 해상도 조정
    private Texture2D AdjustResolution(Texture2D original)
    {
        int targetWidth = original.width;
        int targetHeight = original.height;

        // 최적의 해상도를 결정
        if (original.width < 800 || original.height < 800)
        {
            targetWidth = original.width * 2;
            targetHeight = original.height * 2;
        }
        else if (original.width > 1600 || original.height > 1600)
        {
            targetWidth = original.width / 2;
            targetHeight = original.height / 2;
        }

        Texture2D resizedTexture = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        for (int y = 0; y < targetHeight; y++)
        {
            for (int x = 0; x < targetWidth; x++)
            {
                float u = (float)x / targetWidth;
                float v = (float)y / targetHeight;
                resizedTexture.SetPixel(x, y, original.GetPixel((int)(u * original.width), (int)(v * original.height)));
            }
        }
        resizedTexture.Apply();
        return resizedTexture;
    }

    private Texture2D PreprocessImage(Texture2D inputTexture)
    {
        // Resize texture (if necessary)
        int targetWidth = 300; // Adjust based on your requirement
        int targetHeight = (int)(inputTexture.height * (float)targetWidth / inputTexture.width);
        Texture2D resizedTexture = new Texture2D(targetWidth, targetHeight);
        Graphics.ConvertTexture(inputTexture, resizedTexture);

        // Convert to grayscale
        Color32[] pixels = resizedTexture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            byte gray = (byte)(0.299f * pixels[i].r + 0.587f * pixels[i].g + 0.114f * pixels[i].b);
            pixels[i] = new Color32(gray, gray, gray, pixels[i].a);
        }
        resizedTexture.SetPixels32(pixels);
        resizedTexture.Apply();

        // Apply binarization
        for (int y = 0; y < resizedTexture.height; y++)
        {
            for (int x = 0; x < resizedTexture.width; x++)
            {
                Color32 pixel = resizedTexture.GetPixel(x, y);
                // Threshold to make it binary
                byte binary = pixel.r > 128 ? (byte)255 : (byte)0; // Adjust threshold
                resizedTexture.SetPixel(x, y, new Color32(binary, binary, binary, 255));
            }
        }
        resizedTexture.Apply();

        return resizedTexture;
    }
    #endregion

    #region hOCR
    public string GetHOCRText(int page = 0)
    {
        if (_tessHandle.Equals(IntPtr.Zero))
            return null;

        IntPtr hocrPtr = TessBaseAPIGetHOCRText(_tessHandle, page);
        if (hocrPtr.Equals(IntPtr.Zero))
            return null;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        string hocrText = Marshal.PtrToStringAnsi(hocrPtr);
#else
    string hocrText = Marshal.PtrToStringAuto(hocrPtr);
#endif

        TessDeleteText(hocrPtr);
        return hocrText;
    }

    private string ParseHOCRAndInsertLineBreaks(string hocrText)
    {
        // <span class='ocr_line'> 태그 내의 텍스트를 추출
        var regex = new Regex(@"<span class='ocr_line[^>]*>(.*?)<\/span>", RegexOptions.Singleline);
        var matches = regex.Matches(hocrText);
        StringBuilder result = new StringBuilder();

        foreach (Match match in matches)
        {
            if (match.Groups.Count > 1)
            {
                string lineText = match.Groups[1].Value;
                // HTML 태그 제거
                lineText = Regex.Replace(lineText, "<[^>]+>", "");
                result.AppendLine(lineText.Trim());
            }
        }

        return result.ToString();
    }

    #endregion

    private void DrawLines(Texture2D texture, Rect boundingRect, Color color, int thickness = 3)
    {
        int x1 = (int) boundingRect.x;
        int x2 = (int) (boundingRect.x + boundingRect.width);
        int y1 = (int) boundingRect.y;
        int y2 = (int) (boundingRect.y + boundingRect.height);

        for (int x = x1; x <= x2; x++)
        {
            for (int i = 0; i < thickness; i++)
            {
                texture.SetPixel(x, y1 + i, color);
                texture.SetPixel(x, y2 - i, color);
            }
        }

        for (int y = y1; y <= y2; y++)
        {
            for (int i = 0; i < thickness; i++)
            {
                texture.SetPixel(x1 + i, y, color);
                texture.SetPixel(x2 - i, y, color);
            }
        }

        texture.Apply();
    }

    public Texture2D GetHighlightedTexture()
    {
        return _highlightedTexture;
    }

    public void Close()
    {
        if (_tessHandle.Equals(IntPtr.Zero))
            return;
        TessBaseAPIEnd(_tessHandle);
        TessBaseAPIDelete(_tessHandle);
        _tessHandle = IntPtr.Zero;
    }
}