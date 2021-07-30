using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XRay;

public abstract class XRayModuleBase : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;

    public GameObject[] ScanLights;
    public KMSelectable[] Buttons;
    public Material[] ScannerColors;

    protected bool _isSolved = false;
    private Coroutine _coroutine = null;

    void Start()
    {
        for (int i = 0; i < Buttons.Length; i++)
            setButtonHandler(i);
        StartModule();
    }

    protected abstract void StartModule();

    protected void MarkSolved()
    {
        Audio.PlaySoundAtTransform("X-RaySolve", transform);
        Module.HandlePass();
        _isSolved = true;
        StopLights();
    }

    protected abstract void handleButton(int i);

    private void setButtonHandler(int i)
    {
        Buttons[i].OnInteract = delegate
        {
            Buttons[i].AddInteractionPunch();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttons[i].transform);

            if (!_isSolved)
                handleButton(i);
            return false;
        };
    }

    protected void StartLights(SymbolInfo[] icons, ScanningMode mode, ScannerColor color, float delayBetweenRepeats = 0)
    {
        StopLights();
        _coroutine = StartCoroutine(RunLights(icons, mode, color, delayBetweenRepeats));
    }

    protected void StopLights()
    {
        if (_coroutine != null)
            StopCoroutine(_coroutine);

        foreach (var scanLight in ScanLights)
            scanLight.SetActive(false);
    }

    private IEnumerator RunLights(SymbolInfo[] icons, ScanningMode mode, ScannerColor color, float delayBetweenRepeats = 0)
    {
        const int _iconWidth = 180;
        const int _iconHeight = 180;

        const int _ulongsPerScanline = (_iconWidth + 63) / 64;
        const float _pixelWidth = 0.138f / _iconWidth;
        const float _secondsPerIcon = 4f;

        foreach (var scanLight in ScanLights)
            scanLight.GetComponent<MeshRenderer>().sharedMaterial = ScannerColors[(int) color];

        var prevScanline = -1;
        while (true)
        {
            int curScanline;
            switch (mode)
            {
                case ScanningMode.TopToBottom:
                    curScanline = Mathf.FloorToInt((Time.time % (icons.Length * _secondsPerIcon + delayBetweenRepeats)) * (_iconHeight / _secondsPerIcon));
                    break;

                case ScanningMode.BottomToTop:
                    curScanline = Mathf.FloorToInt((icons.Length * _secondsPerIcon - Time.time % (icons.Length * _secondsPerIcon + delayBetweenRepeats)) * (_iconHeight / _secondsPerIcon));
                    break;

                default:    // alternating
                    var e = Time.time % (2 * icons.Length * _secondsPerIcon + 2 * delayBetweenRepeats);
                    curScanline = e > icons.Length * _secondsPerIcon
                        ? Mathf.FloorToInt((2 * icons.Length * _secondsPerIcon - e - delayBetweenRepeats) * (_iconHeight / _secondsPerIcon))
                        : Mathf.FloorToInt((e - delayBetweenRepeats) * (_iconHeight / _secondsPerIcon));
                    break;
            }

            if (curScanline < icons.Length * _iconHeight && curScanline > 0 && curScanline != prevScanline)
            {
                var icon = RawBits.Icons[icons[curScanline / _iconHeight].Index];
                var scanlineStart = (icons[curScanline / _iconHeight].Flipped ? _iconHeight - 1 - (curScanline % _iconHeight) : curScanline % _iconHeight) * _ulongsPerScanline;
                var lightIx = 0;
                int? startX = null;
                for (int x = 0; x <= _iconWidth; x++)
                {
                    var curBit = x < _iconWidth && (icon[scanlineStart + x / 64] & (1UL << (x % 64))) != 0;
                    if (curBit && startX == null)
                        startX = x;
                    else if (!curBit && startX != null)
                    {
                        ScanLights[lightIx].SetActive(true);
                        ScanLights[lightIx].transform.localScale = new Vector3((x - startX.Value) * _pixelWidth, 1, 1);
                        ScanLights[lightIx].transform.localPosition = new Vector3((x + startX.Value - _iconWidth) * .5f * _pixelWidth, 0, 0);
                        lightIx++;
                        startX = null;
                    }
                }
                for (int i = lightIx; i < ScanLights.Length; i++)
                    ScanLights[i].SetActive(false);
            }

            prevScanline = curScanline;
            yield return null;
        }
    }

    protected static readonly Dictionary<string, int> _twitchButtonMap = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase)
    {
        { "tl", 1 }, { "t", 2 }, { "tm", 2 }, { "tc", 2 }, { "tr", 2 }, { "bl", 3 }, { "b", 4 }, { "bm", 4 }, { "bc", 4 }, { "br", 5 }
    };
}
