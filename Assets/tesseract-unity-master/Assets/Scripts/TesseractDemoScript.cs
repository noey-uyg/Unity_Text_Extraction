using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TesseractDemoScript : MonoBehaviour
{
    [SerializeField] private Texture2D imageToRecognize;
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField] private RawImage outputImage;
    private TesseractDriver _tesseractDriver;
    private string _text = "";
    private Texture2D _texture;

    public void SetTexture(Texture2D tex)
    {
        Texture2D texture = ConvertToGrayScale(tex);
        texture.SetPixels32(tex.GetPixels32());
        texture.Apply();

        _tesseractDriver = new TesseractDriver();
        Recoginze(texture);
    }

    private Texture2D ConvertToGrayScale(Texture2D originalTex)
    {
        Texture2D grayTexture = new Texture2D(originalTex.width, originalTex.height);
        for(int y=0;y<originalTex.height;y++)
        {
            for(int x= 0;x<originalTex.width;x++)
            {
                Color pixel = originalTex.GetPixel(x, y);
                float gray = (pixel.r + pixel.g + pixel.b) / 3f;
                grayTexture.SetPixel(x,y,new Color(gray,gray,gray));
            }
        }

        grayTexture.Apply();

        return grayTexture;
    }

    private void Recoginze(Texture2D outputTexture)
    {
        _texture = outputTexture;
        ClearTextDisplay();

        _tesseractDriver.Setup(OnSetupCompleteRecognize);
    }

    private void OnSetupCompleteRecognize()
    {
        AddToTextDisplay(_tesseractDriver.Recognize(_texture));
        AddToTextDisplay(_tesseractDriver.GetErrorMessage(), true);
        SetImageDisplay();
    }

    private void ClearTextDisplay()
    {
        _text = "";
    }

    private void AddToTextDisplay(string text, bool isError = false)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        ClearTextDisplay();
        _text += (string.IsNullOrWhiteSpace(displayText.text) ? "" : "\n") + text;
        
        if (isError)
            Debug.LogError(text);
        else
            Debug.Log(text);
    }

    private void LateUpdate()
    {
        displayText.text = _text;
    }

    private void SetImageDisplay()
    {
        RectTransform rectTransform = outputImage.GetComponent<RectTransform>();
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            rectTransform.rect.width * _tesseractDriver.GetHighlightedTexture().height / _tesseractDriver.GetHighlightedTexture().width);
        outputImage.texture = _tesseractDriver.GetHighlightedTexture();
    }
}