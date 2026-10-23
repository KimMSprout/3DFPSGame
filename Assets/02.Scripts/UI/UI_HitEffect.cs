using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UI_HitEffect : MonoBehaviour
{
    [SerializeField] private float _flashTime = 0.2f;

    private Image _image;

    private void Awake()
    {
        _image = GetComponentInChildren<Image>(true);
        _image.enabled = false;
    }

    public void Flash()
    {
        StartCoroutine(Flash_Coroutine());
    }

    private IEnumerator Flash_Coroutine()
    {
        _image.enabled = true;

        yield return new WaitForSeconds(_flashTime);

        _image.enabled = false;
    }
}