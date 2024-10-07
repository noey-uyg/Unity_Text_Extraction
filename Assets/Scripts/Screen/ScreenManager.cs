using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenManager : MonoBehaviour
{
    private static ScreenManager _instance;

    [SerializeField] private Canvas _mainCanvas;
    [SerializeField] private RectTransform _mainCanvasRect;
    [SerializeField] private CanvasScaler _mainCanvasScaler;

    public static ScreenManager Instance { get { return _instance; } }
    public float ScreenWidth { get { return _mainCanvasRect.sizeDelta.x; } }
    public float ScreenHeight { get { return _mainCanvasRect.sizeDelta.y; } }

    private void Awake()
    {
        _instance = this;

        if(_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }
}
