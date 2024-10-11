using System;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TesseractDemoScript : MonoBehaviour
{
    [SerializeField] private Texture2D imageToRecognize;
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField] private RawImage outputImage;
    [SerializeField] private PageSegMode _pageSegMode;
    [SerializeField] private bool _bIsSave;
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

        ExportTextToFile(text);

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

    public void ExportTextToFile(string text, string filePath = null)
    {
        if (!_bIsSave) return;

        try
        {
            // filePath가 비어있으면 바탕화면 경로 설정
            if (string.IsNullOrEmpty(filePath))
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                filePath = Path.Combine(desktopPath, "TextFile.txt");
            }

            // 텍스트 파일로 저장
            File.WriteAllText(filePath, text);
            Debug.Log($"텍스트가 {filePath}에 저장되었습니다.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"파일 저장 오류: {ex.Message}");
        }
    }
}