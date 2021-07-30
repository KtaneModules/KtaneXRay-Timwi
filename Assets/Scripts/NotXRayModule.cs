using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using XRay;

using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Not X-Rays
/// Created by StKildaFan & Timwi
/// </summary>
public class NotXRayModule : XRayModuleBase
{
    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private NotXRayRules _rules;
    private int _table;
    private int _curCell;
    private int _solutionCell;
    private ScanningMode _mode;
    private ButtonDirection[] _directions;
    private bool _isMazeStage;
    private ScannerColor _scannerColor;

    private static readonly Dictionary<int, NotXRayRules> _ruleSeededRules = new Dictionary<int, NotXRayRules>();

    protected override void StartModule()
    {
        _moduleId = _moduleIdCounter++;
        _isSolved = false;

        var rnd = RuleSeedable.GetRNG();
        Debug.LogFormat("[Not X-Ray #{0}] Using rule seed: {1}", _moduleId, rnd.Seed);

        // RULE SEED
        if (_ruleSeededRules.ContainsKey(rnd.Seed))
            _rules = _ruleSeededRules[rnd.Seed];
        else
        {
            var _tables = new SymbolInfo[8][];
            var _notWallses = new int[8][];
            var sqs = Enumerable.Range(0, 49).Where(c => c != 0 && c != 6 && c != 42 && c != 48 && c != 24).ToArray();

            // First 55 symbols can be flipped, next 55 cannot
            const int numFlippable = 55;
            const int numUnflippable = 55;
            var unflippableSymbols = rnd.ShuffleFisherYates(Enumerable.Range(0, numUnflippable).Select(c => numFlippable + c).ToArray());
            var flippableSymbols = rnd.ShuffleFisherYates(Enumerable.Range(0, numFlippable).ToArray());

            var combinations = new List<int[]>();
            for (var ai = 0; ai < sqs.Length; ai++)
                for (var bi = ai + 1; bi < sqs.Length; bi++)
                    if (sqs[bi] / 7 > sqs[ai] / 7 && sqs[bi] % 7 < sqs[ai] % 7)
                        for (var ci = ai + 1; ci < sqs.Length; ci++)
                            if (ci != bi && sqs[ci] / 7 > sqs[ai] / 7 && sqs[ci] % 7 > sqs[ai] % 7 && sqs[ci] % 7 > sqs[bi] % 7)
                                for (var di = Math.Max(bi, ci) + 1; di < sqs.Length; di++)
                                    if (sqs[di] / 7 > sqs[bi] / 7 && sqs[di] / 7 > sqs[ci] / 7 && sqs[di] % 7 > sqs[bi] % 7 && sqs[di] % 7 < sqs[ci] % 7)
                                        combinations.Add(new int[] { sqs[ai], sqs[bi], sqs[ci], sqs[di] });

            for (var tableIx = 0; tableIx < 8; tableIx++)
            {
                var symbols = new List<SymbolInfo>();
                for (var i = 0; i < 4; i++)
                    symbols.Add(new SymbolInfo(flippableSymbols[tableIx * 4 + i], false));
                for (var i = 0; i < 4; i++)
                    symbols.Add(new SymbolInfo(flippableSymbols[tableIx * 4 + i], true));
                for (var i = 0; i < 3; i++)
                    symbols.Add(new SymbolInfo(unflippableSymbols[tableIx * 3 + i], false));

                // generate maze
                const int w = 7, h = 7;
                var notWalls = new List<int>();     // (cell << 1) + (right ? 1 : 0)
                var todo = Enumerable.Range(0, 49).Where(c => c != 0 && c != 6 && c != 42 && c != 48).ToList();
                var active = new List<int>();

                var start = rnd.Next(0, todo.Count);
                active.Add(todo[start]);
                todo.RemoveAt(start);

                while (todo.Count > 0)
                {
                    var activeIx = rnd.Next(0, active.Count);
                    var sq = active[activeIx];
                    var adjs = new List<int>();
                    if ((sq % w) > 0 && todo.Contains(sq - 1))
                        adjs.Add(sq - 1);
                    if ((sq % w) < w - 1 && todo.Contains(sq + 1))
                        adjs.Add(sq + 1);
                    if (((sq / w) | 0) > 0 && todo.Contains(sq - w))
                        adjs.Add(sq - w);
                    if (((sq / w) | 0) < h - 1 && todo.Contains(sq + w))
                        adjs.Add(sq + w);

                    if (adjs.Count == 0)
                    {
                        active.RemoveAt(activeIx);
                        continue;
                    }

                    var adj = adjs[rnd.Next(0, adjs.Count)];
                    todo.RemoveAt(todo.IndexOf(adj));
                    active.Add(adj);

                    if (adj == sq - 1)
                        notWalls.Add((adj << 1) | 1);
                    else if (adj == sq + 1)
                        notWalls.Add((sq << 1) | 1);
                    else if (adj == sq - w)
                        notWalls.Add(adj << 1);
                    else if (adj == sq + w)
                        notWalls.Add(sq << 1);
                }


                // generate symbol arrangements

                var backtracks = 0;
                Func<int, int[][], List<int[]>> findArrangement = null;
                findArrangement = (numSofar, combinationsLeft) =>
                {
                    if (numSofar == 11)
                        return new List<int[]>();
                    if (combinationsLeft.Length == 0)
                        return null;

                    var offset = rnd.Next(0, combinationsLeft.Length);
                    for (var ir = 0; ir < combinationsLeft.Length; ir++)
                    {
                        var i = (ir + offset) % combinationsLeft.Length;
                        var combination = combinationsLeft[i];
                        var combLeft = combinationsLeft.Where(cmb => cmb.All(c => !combination.Contains(c))).ToArray();
                        var result = findArrangement(numSofar + 1, combLeft);
                        if (result != null)
                        {
                            result.Add(combination);
                            return result;
                        }
                        if (backtracks > 500)
                            return null;
                    }
                    backtracks++;
                    return null;
                };

                List<int[]> arrangement;
                do
                {
                    backtracks = 0;
                    arrangement = findArrangement(0, combinations.ToArray());
                }
                while (arrangement == null);


                // generate symbol table
                var table = new SymbolInfo[49];
                for (var cell = 0; cell < 49; cell++)
                {
                    if (cell == 0 || cell == 6 || cell == 42 || cell == 48 || cell == 24)
                        continue;

                    var arrIx = arrangement.IndexOf(comb => comb.Contains(cell));
                    table[cell] = symbols[arrIx];
                }

                _tables[tableIx] = table;
                _notWallses[tableIx] = notWalls.ToArray();
            }

            _rules = new NotXRayRules(_tables, _notWallses);
            _ruleSeededRules[rnd.Seed] = _rules;
        }
        // END RULE SEED

        Initialize();
    }

    private void Initialize()
    {
        _table = Rnd.Range(0, 8);
        _mode = Rnd.Range(0, 2) != 0 ? ScanningMode.TopToBottom : ScanningMode.BottomToTop;

        var validCells = Enumerable.Range(0, 49).Except(new[] { 0, 6, 42, 48, 24 }).ToArray();
        _curCell = validCells.PickRandom();
        _solutionCell = validCells.PickRandom();

        _directions = ((ButtonDirection[]) Enum.GetValues(typeof(ButtonDirection))).Shuffle();
        var similarCells = validCells.Where(c => _rules.Tables[_table][c] == _rules.Tables[_table][_solutionCell]);
        _scannerColor =
            _solutionCell == similarCells.Min() ? ScannerColor.Red :
            _solutionCell == similarCells.Max() ? ScannerColor.Blue :
            _solutionCell % 7 == similarCells.Select(c => c % 7).Min() ? ScannerColor.White : ScannerColor.Yellow;

        Debug.LogFormat(@"[Not X-Ray #{0}] Buttons 1–4 go: {1}", _moduleId, _directions.Join(", "));
        Debug.LogFormat(@"[Not X-Ray #{0}] You’re in table #{1}, starting position {2}{3}.", _moduleId, _table + 1, (char) ('A' + _curCell % 7), _curCell / 7 + 1);
        Debug.LogFormat(@"[Not X-Ray #{0}] Scanning {1}.", _moduleId, _mode == ScanningMode.TopToBottom ? "top to bottom" : "bottom to top");

        _isMazeStage = false;
        StartLights(new[] { _rules.Tables[_table][_curCell] }, _mode, ScannerColor.Green, 2f);
    }

    protected override void handleButton(int btn)
    {
        Debug.LogFormat(@"<Not X-Ray #{0}> Pressed button {1}.", _moduleId, btn + 1);
        if (btn == 4)
        {
            handleSubmit();
            return;
        }

        var newCell = _curCell;
        var allowed = true;
        string error = null;
        switch (_directions[btn])
        {
            case ButtonDirection.Up:
                allowed = !_isMazeStage || (_curCell / 7 > 0 && _rules.Mazes[_table].Contains((_curCell - 7) << 1));
                newCell = _curCell % 7 + 7 * (_curCell % 7 == 0 || _curCell % 7 == 6 ? (_curCell / 7 + 3) % 5 + 1 : (_curCell / 7 + 6) % 7);
                error = "You tried to go up from {0}{1}, but there’s a wall there.";
                break;
            case ButtonDirection.Right:
                allowed = !_isMazeStage || (_curCell % 7 < 6 && _rules.Mazes[_table].Contains((_curCell << 1) | 1));
                newCell = 7 * (_curCell / 7) + (_curCell / 7 == 0 || _curCell / 7 == 6 ? (_curCell % 7 + 5) % 5 + 1 : (_curCell % 7 + 1) % 7);
                error = "You tried to go right from {0}{1}, but there’s a wall there.";
                break;
            case ButtonDirection.Down:
                allowed = !_isMazeStage || (_curCell / 7 < 6 && _rules.Mazes[_table].Contains(_curCell << 1));
                newCell = _curCell % 7 + 7 * (_curCell % 7 == 0 || _curCell % 7 == 6 ? (_curCell / 7 + 5) % 5 + 1 : (_curCell / 7 + 1) % 7);
                error = "You tried to go down from {0}{1}, but there’s a wall there.";
                break;
            case ButtonDirection.Left:
                allowed = !_isMazeStage || (_curCell % 7 > 0 && _rules.Mazes[_table].Contains(((_curCell - 1) << 1) | 1));
                newCell = 7 * (_curCell / 7) + (_curCell / 7 == 0 || _curCell / 7 == 6 ? (_curCell % 7 + 3) % 5 + 1 : (_curCell % 7 + 6) % 7);
                error = "You tried to go left from {0}{1}, but there’s a wall there.";
                break;
        }
        if (!allowed)
        {
            Debug.LogFormat(@"[Not X-Ray #{0}] {1}", _moduleId, string.Format(error, (char) ('A' + _curCell % 7), _curCell / 7 + 1));
            Module.HandleStrike();
        }
        else
        {
            _curCell = newCell;
            StartLights(
                newCell == 24 ? new[] { _rules.Tables[_table][_solutionCell] } : new[] { _rules.Tables[_table][_curCell] },
                newCell == 24 ? (btn % 2 == 0 ? ScanningMode.BottomToTop : ScanningMode.TopToBottom) : _mode,
                _isMazeStage ? _scannerColor : ScannerColor.Green,
                2f);
        }
    }

    private void handleSubmit()
    {
        if (_isMazeStage && _curCell == _solutionCell)
        {
            Debug.LogFormat(@"[Not X-Ray #{0}] Module solved.", _moduleId);
            MarkSolved();
        }
        else if (_isMazeStage || _curCell != 24)
        {
            Debug.LogFormat(@"[Not X-Ray #{0}] You pressed Submit on cell {1}{2}. Strike.", _moduleId, (char) ('A' + _curCell % 7), _curCell / 7 + 1);
            Module.HandleStrike();
            Initialize();
        }
        else
        {
            Debug.LogFormat(@"[Not X-Ray #{0}] Entering maze mode. Goal symbol is {2}. Color is {1}, so look at the {3} occurrence.",
                _moduleId, _scannerColor, _rules.Tables[_table][_solutionCell],
                _scannerColor == ScannerColor.Red ? "topmost" :
                _scannerColor == ScannerColor.Yellow ? "rightmost" :
                _scannerColor == ScannerColor.Blue ? "bottommost" : "leftmost");
            Debug.LogFormat(@"[Not X-Ray #{0}] Goal position: {1}{2}.", _moduleId, (char) ('A' + _solutionCell % 7), _solutionCell / 7 + 1);

            _isMazeStage = true;
            StartLights(new[] { _rules.Tables[_table][_solutionCell] }, ScanningMode.BottomToTop, _scannerColor, 2f);
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} press 34142 [buttons in reading order] | !{0} press TL BL BR [buttons are TL, T, BL, B, BR]";
#pragma warning restore 414

    public IEnumerable<KMSelectable> ProcessTwitchCommand(string command)
    {
        var options = _twitchButtonMap.Keys.Concat(_twitchButtonMap.Values.Select(v => v.ToString())).Join("|");
        var m = Regex.Match(command, string.Format(@"^\s*(?:press )?((?:{0})*)\s*$", options), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            return null;

        var str = m.Groups[1].Value;
        var matches = Regex.Matches(str, options, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        var buttons = new List<KMSelectable>();
        var ix = 0;
        int buttonId;
        for (var i = 0; i < matches.Count; i++)
        {
            if (matches[i].Index > ix && str.Substring(ix, matches[i].Index - ix).Any(c => !char.IsWhiteSpace(c)))
                return null;
            if (!((int.TryParse(matches[i].Value, out buttonId) || _twitchButtonMap.TryGetValue(matches[i].Value, out buttonId)) && buttonId > 0 && buttonId <= Buttons.Length))
                return null;
            buttons.Add(Buttons[buttonId - 1]);
            ix = matches[i].Index + matches[i].Length;
        }
        if (str.Length > ix && str.Substring(ix).Any(c => !char.IsWhiteSpace(c)))
            return null;
        return buttons;
    }
}
