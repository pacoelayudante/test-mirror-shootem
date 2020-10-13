using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ColorControl : MonoBehaviour
{
    Dictionary<SpriteRenderer, Color> initialColors = new Dictionary<SpriteRenderer, Color>();
    public bool startWithFadeFrom = true;
    public float fadeTimeDefault = 0.5f;
    public Color colorFadeDefault = Color.black;
    public float flashTimeDefault = 0.2f;
    public Color flashColorDefault = Color.red;

    private void Awake()
    {
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
        {
            initialColors.Add(sr, sr.color);
        }
    }
    void Start()
    {
        if (startWithFadeFrom)
        {
            ColorFade(fadeFrom: true);
        }
    }

    public void Multiply(Color c)
    {
        foreach (var combo in initialColors)
        {
            if (combo.Key) combo.Key.color = combo.Value * c;
        }
    }

    public void Clear()
    {
        foreach (var combo in initialColors)
        {
            if (combo.Key) combo.Key.color = combo.Value;
        }
    }

    public void ColorFlash() => ColorFlash(flashColorDefault, flashTimeDefault);
    public void ColorFlash(Color colorToFlash, float flashTime, int repeatAmount = 1, float downTime = 0f) =>
        StartCoroutine(ColorFlashCorut(colorToFlash, flashTime, repeatAmount, downTime));

    IEnumerator ColorFlashCorut(Color colorToFlash, float flashTime, int repeatAmount = 1, float downTime = 0f)
    {
        for (int i = 0; i < repeatAmount; i++)
        {
            Multiply(colorToFlash);
            yield return new WaitForSeconds(flashTime);
            Clear();
            yield return new WaitForSeconds(downTime <= 0f ? flashTime : downTime);
        }
    }

    public void ColorFade(bool fadeFrom) => ColorFade(fadeFrom, colorFadeDefault, fadeTimeDefault);
    public void ColorFade(bool fadeFrom, Color colorToFade, float fadeTime) =>
        StartCoroutine(ColorFadeCorut(fadeFrom, colorToFade, fadeTime));

    IEnumerator ColorFadeCorut(bool fadeFrom, Color colorToFade, float fadeTime)
    {
        float t = 0f;

        while (t < fadeTime)
        {
            var val = t/fadeTime;
            if (fadeFrom) val = 1f-val;
            Multiply( Color.Lerp( Color.white, colorToFade, val) );
            yield return null;
            t += Time.deltaTime;
        }
        if (fadeFrom) Clear();
    }
}
