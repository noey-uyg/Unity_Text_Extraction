using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TesseractDemoScript : MonoBehaviour
{
    [SerializeField] private Texture2D imageToRecognize;
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField] private RawImage outputImage;
    [SerializeField] private PageSegMode _pageSegMode;
    
    private TesseractDriver _tesseractDriver;
    private string _text = "";
    private Texture2D _texture;

    private Color _originColor;

    private void Start()
    {
        _originColor = outputImage.color;
        Color color = new Color(255, 255, 255, 0f);
        outputImage.color = color;
    }

    public void SetTexture(Texture2D tex)
    {
        Texture2D texture = tex;
        texture.SetPixels32(tex.GetPixels32());
        texture.Apply();

        _tesseractDriver = new TesseractDriver();
        Recoginze(texture);
    }

    private void Recoginze(Texture2D outputTexture)
    {
        _texture = outputTexture;
        ClearTextDisplay();

        _tesseractDriver.Setup(OnSetupCompleteRecognize, _pageSegMode);
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
        _text = text;
        
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
        outputImage.color = _originColor;
    }
}