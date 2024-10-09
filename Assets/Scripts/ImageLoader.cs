using SFB;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageLoader : MonoBehaviour
{
    [SerializeField] private RawImage displayImage; // �̹����� ǥ���� UI RawImage
    [SerializeField] private TesseractDemoScript _tesseract;

    public void LoadImage()
    {
        // �� �� �ִ� ���� Ȯ���� ����
        var extensions = new[] {
            new ExtensionFilter("Image Files", "png", "jpg", "jpeg", "bmp")
        };

        // ���� ���̾�α� ����
        var paths = StandaloneFileBrowser.OpenFilePanel("Open Image", "", extensions, false);

        if (paths.Length > 0 && !string.IsNullOrEmpty(paths[0]))
        {
            string path = paths[0];
            StartCoroutine(LoadImageCoroutine(path));
        }
    }

    private IEnumerator LoadImageCoroutine(string path)
    {
        // ������ �о����
        var www = new WWW("file:///" + path);
        yield return www;

        // �ؽ�ó ����
        Texture2D texture = www.texture;
        _tesseract.SetTexture(texture);
    }
}
