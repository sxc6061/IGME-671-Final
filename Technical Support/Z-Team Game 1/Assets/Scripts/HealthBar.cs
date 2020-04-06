using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    public bool hide = false;

    private GameObject fillObject;

    private float ratio;
    private float maxWidth;
    private float height;

    private Color green;
    private Color red;

    public void Init()
    {
        fillObject = transform.Find("Fill").gameObject;

        maxWidth = fillObject.transform.localScale.x;

        height = fillObject.transform.localScale.x;

        fillObject.transform.localScale = new Vector3(maxWidth, height, 1);

        green = new Color(0.18f, 0.65f, 0.31f, 0.8f);
        red = new Color(0.68f, 0.14f, 0.14f, 0.8f);

        fillObject.GetComponent<SpriteRenderer>().color = green;

        if (hide)
        {
            gameObject.SetActive(false);
        }
    }

    public void UpdateDisplay(int current, int max, Color? colorOverride = null)
    {
        ratio = (float)current / (float)max;

        if (ratio < 0) ratio = 0;
        else if (ratio > 1) ratio = 1;

        fillObject.transform.localScale = new Vector3(maxWidth * ratio, height, 1);

        if (colorOverride == null)
            UpdateColor();
        else
            fillObject.GetComponent<SpriteRenderer>().color = colorOverride.Value;

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
            fillObject.GetComponent<SpriteRenderer>().color = red;
        }
        else
        {
            fillObject.GetComponent<SpriteRenderer>().color = green;
        }
    }

    //quit game
    public void QuitGame()
    {
        Application.Quit();
    }
}
