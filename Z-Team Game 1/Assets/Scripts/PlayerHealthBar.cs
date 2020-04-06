using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthBar : MonoBehaviour
{
    public float ratio;
    public bool hide = false;

    private GameObject fillObject;

    private float maxWidth;
    private float height;

    private Color green;
    private Color red;

    private void Awake()
    {
        Transform outlineTransform = transform.Find("Outline");
        fillObject = transform.Find("Fill").gameObject;

        maxWidth = outlineTransform.GetComponent<RectTransform>().rect.width - (fillObject.GetComponent<RectTransform>().anchoredPosition.x * 2);

        height = fillObject.GetComponent<RectTransform>().sizeDelta.y;

        fillObject.GetComponent<RectTransform>().sizeDelta = new Vector2(maxWidth, height);

        green = new Color(0.18f, 0.65f, 0.31f, 0.8f);
        red = new Color(0.68f, 0.14f, 0.14f, 0.8f);

        fillObject.GetComponent<Image>().color = green;

        if (hide)
        {
            gameObject.SetActive(false);
        }
    }

    public void UpdateDisplay(int current, int max)
    {
        ratio = (float)current / (float)max;

        if (ratio < 0) ratio = 0;
        else if (ratio > 1) ratio = 1;

        fillObject.GetComponent<RectTransform>().sizeDelta = new Vector2(ratio * maxWidth, height);

        UpdateColor();

        if (hide && ratio == 1)
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
        }
    }

    private void UpdateColor()
    {
        if (ratio < 0.3)
        {
            fillObject.GetComponent<Image>().color = red;
        }
        else
        {
            fillObject.GetComponent<Image>().color = green;
        }
    }
}
