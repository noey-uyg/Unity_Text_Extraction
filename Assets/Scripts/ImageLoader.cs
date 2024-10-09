using SFB;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageLoader : MonoBehaviour
{
    [SerializeField] private RawImage displayImage; // 이미지를 표시할 UI RawImage
    [SerializeField] private TesseractDemoScript _tesseract;

    public void LoadImage()
    {
        // 열 수 있는 파일 확장자 설정
        var extensions = new[] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg", "bmp")
        };

        // 파일 다이얼로그 열기
        var paths = StandaloneFileBrowser.OpenFilePanel("Open Image", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string path = paths[0];
            StartCoroutine(LoadImageCoroutine(path));
        }
    }

    private IEnumerator LoadImageCoroutine(string path)
    {
        // 파일을 읽어오기
        var www = new WWW("file:///" + path);
        yield return www;

        // 텍스처 생성
        Texture2D texture = www.texture;
        _tesseract.SetTexture(texture);
    }
}
