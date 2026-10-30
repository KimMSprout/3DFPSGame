using UnityEngine;
using UnityEngine.UI;

public class UI_GunMagazine : MonoBehaviour
{
    [SerializeField] private Text _maxMagazineText;
    [SerializeField] private Text _currentMagazineText;
    [SerializeField] private PlayerGunFire _gunFire;

    private void Awake()
    {
        _gunFire.OnAmmoChanged += RefreshAmmo;
    }

    private void Start()
    {
        // PlayerGunFire.Awake에서 탄약이 초기화되므로 Start에서 초기값을 표시한다.
        RefreshAmmo(_gunFire.CurrentMagazine, _gunFire.MaxMagazine);
    }

    private void OnDestroy()
    {
        if (_gunFire != null)
        {
            _gunFire.OnAmmoChanged -= RefreshAmmo;
        }
    }

    private void RefreshAmmo(int currentMagazine, int maxMagazine)
    {
        _currentMagazineText.text = currentMagazine.ToString();
        _maxMagazineText.text = maxMagazine.ToString();
    }
}
