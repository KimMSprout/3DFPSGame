using System;
using UnityEngine;

public class UI_Minimap : MonoBehaviour
{
    [SerializeField] private MinimapCamera _minimapCamera;

    public void PlusButtonClicked()
    {
        _minimapCamera.ZoomIn();
    }

    public void MinusButtonClicked()
    {
        _minimapCamera.ZoomOut();
    }
}