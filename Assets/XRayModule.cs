using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XRay;

using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of X-Rays
/// Created by Timwi
/// </summary>
public class XRayModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;

    public GameObject[] ScanLights;
    public Texture[] ButtonLabels;
    public MeshRenderer[] ButtonLabelObjs;
    public KMSelectable[] Buttons;

    private const int _iconWidth = 180;
    private const int _iconHeight = 180;

    private static int[] _table = { 30, 28, 26, 1, 25, 11, 8, 29, 9, 12, 3, 11, 15, 13, 28, 19, 14, 31, 0, 15, 13, 31, 18, 9, 8, 11, 2, 9, 23, 22, 17, 14, 0, 23, 32, 20, 30, 27, 25, 4, 26, 16, 9, 24, 23, 6, 4, 28, 12, 15, 21, 10, 21, 22, 29, 17, 26, 27, 22, 32, 0, 7, 15, 17, 14, 23, 2, 11, 27, 27, 23, 18, 13, 25, 9, 11, 19, 19, 4, 14, 16, 32, 5, 12, 3, 10, 0, 5, 32, 25, 30, 28, 3, 19, 6, 22, 18, 10, 24, 20, 6, 13, 16, 16, 7, 5, 30, 18, 29, 31, 1, 31, 21, 3, 1, 17, 20, 20, 5, 32, 17, 2, 1, 29, 7, 15, 16, 19, 24, 4, 7, 22, 26, 5, 4, 27, 6, 12, 14, 6, 10, 31, 18, 21 };

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _isSolved;
    private Coroutine _coroutine;

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        _isSolved = false;
        Initialize();
    }

    private void Initialize()
    {
        var col = Rnd.Range(0, 12);
        var row = Rnd.Range(0, 12);
        // This makes sure that we don’t go off the edge of the table
        var dir = Enumerable.Range(0, 9).Where(dr => !(col == 0 && dr % 3 == 0) && !(col == 11 && dr % 3 == 2) && !(row == 0 && dr / 3 == 0) && !(row == 11 && dr / 3 == 2)).PickRandom();
        var solutionIcon = _table[(row + dir / 3 - 1) * 12 + col + (dir % 3 - 1)];
        var decoyIcon = _table[(row + dir / 3 - 1) * 12 + (col ^ 1) + (dir % 3 - 1)];
        var buttonLabelIxs = Enumerable.Range(0, 33).Where(i => i != solutionIcon && i != decoyIcon).ToList().Shuffle();
        if (solutionIcon == decoyIcon)
            buttonLabelIxs = buttonLabelIxs.Take(Buttons.Length - 1).Concat(new[] { solutionIcon }).ToList().Shuffle();
        else
            buttonLabelIxs = buttonLabelIxs.Take(Buttons.Length - 2).Concat(new[] { solutionIcon, decoyIcon }).ToList().Shuffle();
        var solutionIx = buttonLabelIxs.IndexOf(solutionIcon);
        for (int i = 0; i < Buttons.Length; i++)
        {
            ButtonLabelObjs[i].material.mainTexture = ButtonLabels[buttonLabelIxs[i]];
            setButtonHandler(i, solutionIx);
        }

        Debug.LogFormat("[X-Ray #{0}] Column {1}, Row {2}: symbol there is {3}.", _moduleId, col + 1, row + 1, _table[row * 12 + col]);
        Debug.LogFormat("[X-Ray #{0}] {1} = {2}. Solution symbol is {3}.", _moduleId, dir + 1, "Move up-left,Move up,Move up-right,Move left,Stay put,Move right,Move down-left,Move down,Move down-right".Split(',')[dir], solutionIcon);
        Debug.LogFormat("[X-Ray #{0}] Correct symbol is on button #{1}.", _moduleId, solutionIx + 1);

        if (_coroutine != null)
            StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(RunLights(col, row, dir));
    }

    private void setButtonHandler(int i, int solution)
    {
        Buttons[i].OnInteract = delegate
        {
            Buttons[i].AddInteractionPunch();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttons[i].transform);

            if (_isSolved)
                return false;
            if (i != solution)
            {
                Debug.LogFormat("[X-Ray #{0}] You pressed button #{1}, which is wrong. Resetting module.", _moduleId, i + 1);
                Module.HandleStrike();
                Initialize();
            }
            else
            {
                Debug.LogFormat("[X-Ray #{0}] You pressed button #{1}. Module solved.", _moduleId, i + 1);
                Module.HandlePass();
                _isSolved = true;
                StopCoroutine(_coroutine);
                foreach (var scanLight in ScanLights)
                    scanLight.SetActive(false);
            }
            return false;
        };
    }

    private IEnumerator RunLights(int col, int row, int dir)
    {
        var icons = new[] { col, row + 12, dir + 24 };
        icons.Shuffle();

        const int _ulongsPerScanline = (_iconWidth + 63) / 64;
        const float _pixelWidth = 0.138f / _iconWidth;
        const float _secondsPerIcon = 4f;

        var mode = Rnd.Range(0, 3);  // 0 = top to bottom; 1 = bottom to top; 2 = back and forth
        Debug.LogFormat("[X-Ray #{0}] Scanning {1}.", _moduleId, "from top to bottom,from bottom to top,back and forth".Split(',')[mode]);

        var prevScanline = -1;
        while (true)
        {
            int curScanline;
            switch (mode)
            {
                case 0: // top to bottom
                    curScanline = Mathf.FloorToInt((Time.time % (3 * _secondsPerIcon)) * (_iconHeight / _secondsPerIcon));
                    break;

                case 1: // bottom to top
                    curScanline = Mathf.FloorToInt((3 * _secondsPerIcon - Time.time % (3 * _secondsPerIcon)) * (_iconHeight / _secondsPerIcon));
                    break;

                default:    // alternating
                    var e = Time.time % (6 * _secondsPerIcon);
                    curScanline = e > 3 * _secondsPerIcon
                        ? Mathf.FloorToInt((6 * _secondsPerIcon - e) * (_iconHeight / _secondsPerIcon))
                        : Mathf.FloorToInt(e * (_iconHeight / _secondsPerIcon));
                    break;
            }

            if (curScanline < 3 * _iconHeight && curScanline != prevScanline)
            {
                var icon = RawBits.Icons[icons[curScanline / _iconHeight]];
                var scanlineStart = (curScanline % _iconHeight) * _ulongsPerScanline;
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

    public string TwitchHelpMessage = "Press a button with !{0} press 3 or !{0} press bl. Buttons are TL, T, BL, B, BR.";
    private static Dictionary<string, int> _twitchButtonMap = new Dictionary<string, int>
    {
        { "tl", 1 }, { "t", 2 }, { "tm", 2 }, { "tc", 2 }, { "tr", 2 }, { "bl", 3 }, { "b", 4 }, { "bm", 4 }, { "bc", 4 }, { "br", 5 }
    };

    public KMSelectable[] ProcessTwitchCommand(string command)
    {
        if (!command.StartsWith("press ", StringComparison.InvariantCultureIgnoreCase) || command.Length < 7)
            return null;
        string buttonInput = command.Substring(6).ToLowerInvariant();

        int buttonId;
        if ((int.TryParse(buttonInput, out buttonId) || _twitchButtonMap.TryGetValue(buttonInput, out buttonId)) && buttonId > 0 && buttonId <= Buttons.Length)
            return new[] { Buttons[buttonId - 1] };
        return null;
    }
}
