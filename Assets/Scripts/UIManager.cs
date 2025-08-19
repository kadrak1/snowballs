
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public TextMeshProUGUI ammoText;
    public Image handsIcon;
    public Sprite emptyIcon;
    public Sprite snowIcon;
    public Sprite snowballIcon;
    public Slider moldSlider;

    public void UpdateAmmoText(int currentAmmo, int maxAmmo)
    {
        if (ammoText != null)
        {
            ammoText.text = "Snowballs: " + currentAmmo + " / " + maxAmmo;
        }
    }

	public void UpdateHandsStatus(string handsStatus, int moldProgress, int moldRequired)
	{
		if (ammoText != null)
		{
			ammoText.text = "Hands: " + handsStatus + " | Mold: " + moldProgress + " / " + moldRequired;
		}

		if (handsIcon != null)
		{
			handsIcon.sprite = GetIconForStatus(handsStatus);
			handsIcon.enabled = handsIcon.sprite != null;
		}

		if (moldSlider != null)
		{
			moldSlider.maxValue = moldRequired;
			moldSlider.value = moldProgress;
		}
	}

	Sprite GetIconForStatus(string handsStatus)
	{
		switch (handsStatus)
		{
			case "Empty":
				return emptyIcon;
			case "Snow":
				return snowIcon;
			case "Snowball":
				return snowballIcon;
		}
		return null;
	}
}
