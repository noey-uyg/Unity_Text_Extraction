using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TesseractDemoScript : MonoBehaviour
{
    [SerializeField] private Texture2D imageToRecognize;
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField] private RawImage outputImage;
    [SerializeField] private RectTransform _imageObject;
    [SerializeField] private RectTransform _textObject;
    private TesseractDriver _tesseractDriver;
    private string _text = "";
    private Texture2D _texture;

    private void Start()
    {
        _imageObject.sizeDelta = new Vector2(ScreenManager.Instance.ScreenWidth / 2f, _imageObject.sizeDelta.y);
        _textObject.sizeDelta = new Vector2(ScreenManager.Instance.ScreenHeight / 2f, _textObject.sizeDelta.y);
        Texture2D texture = new Texture2D(imageToRecognize.width, imageToRecognize.height, TextureFormat.ARGB32, false);
        texture.SetPixels32(imageToRecognize.GetPixels32());
        texture.Apply();

        _tesseractDriver = new TesseractDriver();
        Recoginze(texture);
    }

    private void Recoginze(Texture2D outputTexture)
    {
        _texture = outputTexture;
        ClearTextDisplay();
        AddToTextDisplay(_tesseractDriver.CheckTessVersion());
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