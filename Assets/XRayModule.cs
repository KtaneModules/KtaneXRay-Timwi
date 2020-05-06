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
    public KMRuleSeedable RuleSeedable;

    public GameObject[] ScanLights;
    public Texture[] ButtonLabels;
    public MeshRenderer[] ButtonLabelObjs;
    public KMSelectable[] Buttons;

    private const int _iconWidth = 180;
    private const int _iconHeight = 180;

    private static readonly string[] _seed1Converter = "a1n,a1f,b1n,b1f,c1n,c1f,d1n,d1f,e1n,e1f,h2f,h2n,d7n,j1n,h6n,g1n,a6n,a2n,k2n,h1n,a7n,e2n,d6n,b3n,a10n,b10n,c10n,d10n,e10n,f10n,i10n,h9n,i9n".Split(',');

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _isSolved;
    private Coroutine _coroutine;
    private int[] _table;
    private int _solution;
    private SymbolInfo[] _columns;
    private SymbolInfo[] _rows;
    private SymbolInfo[] _3x3;

    static SymbolInfo convertForSeed1(int icon)
    {
        var c = _seed1Converter[icon];
        return new SymbolInfo((c[0] - 'a') + 11 * (int.Parse(c.Substring(1, c.Length - 2)) - 1), c.EndsWith("f"));
    }

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        _isSolved = false;

        var rnd = RuleSeedable.GetRNG();
        Debug.LogFormat("[X-Ray #{0}] Using rule seed: {1}", _moduleId, rnd.Seed);

        if (rnd.Seed == 1)
        {
            _columns = Enumerable.Range(0, 12).Select(convertForSeed1).ToArray();
            _rows = Enumerable.Range(0, 12).Select(i => convertForSeed1(i + 12)).ToArray();
            _3x3 = Enumerable.Range(0, 9).Select(i => convertForSeed1(i + 24)).ToArray();
        }
        else
        {
            // Decide on the icons for the 3×3 table up top
            _3x3 = rnd.ShuffleFisherYates(Enumerable.Range(0, 22).ToArray()).Take(9).Select(x => new SymbolInfo(x + 88, false)).ToArray();

            // For the rows, we can use any non-symmetric icon
            _rows = rnd.ShuffleFisherYates(Enumerable.Range(0, 88).ToArray()).Take(12).Select(x => new SymbolInfo(x, x < 55 ? rnd.Next(0, 2) != 0 : false)).ToArray();

            // For the columns, we can only use flippable icons that we haven’t already used for rows
            var columnsRaw = rnd.ShuffleFisherYates(Enumerable.Range(0, 55).Where(x => !_rows.Any(r => r.Index == x)).ToArray());
            _columns = new SymbolInfo[12];
            for (var i = 0; i < 6; i++)
            {
                var f = rnd.Next(0, 2);
                _columns[2 * i] = new SymbolInfo(columnsRaw[i], f == 0);
                _columns[2 * i + 1] = new SymbolInfo(columnsRaw[i], f != 0);
            }
        }

        // Generate the 12×12 grid of numbers 1–5 (still part of rule seed)
        _table = new int[144];
        var nmbrs = new List<int>();
        for (var i = 0; i < 144; i++)
        {
            nmbrs.Clear();
            var x = i % 12;
            var y = (i / 12) | 0;
            for (var j = 0; j < 5; j++)
                if ((x == 0 || j != _table[i - 1]) &&
                    (y == 0 || j != _table[i - 12]) &&
                    (x == 0 || y == 0 || j != _table[i - 13]))
                    nmbrs.Add(j);
            var n = nmbrs[rnd.Next(0, nmbrs.Count)];
            _table[i] = n;
        }

        Initialize();
    }

    private void Initialize()
    {
        var col = Rnd.Range(0, 12);
        var row = Rnd.Range(0, 12);

        // This makes sure that we don’t go off the edge of the table
        var dir = Enumerable.Range(0, 9).Where(dr => !(col == 0 && dr % 3 == 0) && !(col == 11 && dr % 3 == 2) && !(row == 0 && dr / 3 == 0) && !(row == 11 && dr / 3 == 2)).PickRandom();
        _solution = _table[(row + dir / 3 - 1) * 12 + col + (dir % 3 - 1)];

        for (int i = 0; i < Buttons.Length; i++)
            setButtonHandler(i);

        Debug.LogFormat("[X-Ray #{0}] Column {1}, Row {2}: number there is {3}.", _moduleId, _columns[col], _rows[row], _table[row * 12 + col] + 1);
        Debug.LogFormat("[X-Ray #{0}] {1} = {2}. Solution is {3}.", _moduleId, _3x3[dir], "Move up-left,Move up,Move up-right,Move left,Stay put,Move right,Move down-left,Move down,Move down-right".Split(',')[dir], _solution + 1);

        if (_coroutine != null)
            StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(RunLights(col, row, dir));
    }

    private void setButtonHandler(int i)
    {
        Buttons[i].OnInteract = delegate
        {
            Buttons[i].AddInteractionPunch();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttons[i].transform);

            if (_isSolved)
                return false;
            if (i != _solution)
            {
                Debug.LogFormat("[X-Ray #{0}] You pressed {1}, which is wrong. Resetting module.", _moduleId, i + 1);
                Module.HandleStrike();
                Initialize();
            }
            else
            {
                Debug.LogFormat("[X-Ray #{0}] You pressed {1}. Module solved.", _moduleId, i + 1);
                Module.HandlePass();
                Audio.PlaySoundAtTransform("X-RaySolve", transform);
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
        var icons = new[] { _columns[col], _rows[row], _3x3[dir] };
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

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} press 3 [reading order] | !{0} press BL [buttons are TL, T, BL, B, BR]";
#pragma warning restore 414

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

    IEnumerator TwitchHandleForcedSolve()
    {
        Buttons[_solution].OnInteract();
        yield return new WaitForSeconds(.1f);
    }
}
