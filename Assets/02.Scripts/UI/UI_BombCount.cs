using System;
using UnityEngine;
using UnityEngine.UI;

public class UI_BombCount : MonoBehaviour
{
    [SerializeField] private Text _maxBombCount;
    [SerializeField] private Text _currentBombCount;
    [SerializeField] private PlayerBombFire _bombFire;

    private void Awake()
    {
        _bombFire.OnUsed += UseBomb;
        _maxBombCount.text = _bombFire.MaxBombCount.ToString();
    }

    private void UseBomb(int count)
    {
        _currentBombCount.text = count.ToString();
    }
}