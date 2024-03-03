#nullable enable
using System;
using System.Linq;
using BoysheO.Buffers.PooledBuffer.Linq;
using BoysheO.Extensions;
using Collections.Pooled;

public class SRTReader : IDisposable
{
    private readonly PooledList<TimeSpan> _from = new();
    private readonly PooledList<TimeSpan> _to = new();
    private readonly PooledList<string> _subtitlesText = new();

    public void Clear()
    {
        _from.Clear();
        _to.Clear();
        _subtitlesText.Clear();
    }

    private void Add(TimeSpan from, TimeSpan to, string content)
    {
        _from.Add(from);
        _to.Add(to);
        _subtitlesText.Add(content);
    }

    public void Load(string srtContent)
    {
        if (srtContent.IsNullOrEmpty()) throw new ArgumentOutOfRangeException(nameof(srtContent));

        var lines = srtContent.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None);

        var currentState = eReadState.Index;
        Clear();

        int currentIndex = 0;
        TimeSpan currentFrom = TimeSpan.Zero, currentTo = TimeSpan.Zero;
        var currentText = string.Empty;
        for (var l = 0; l < lines.Length; l++)
        {
            var line = lines[l];

            switch (currentState)
            {
                case eReadState.Index:
                {
                    if (Int32.TryParse(line, out var index))
                    {
                        currentIndex = index;
                        currentState = eReadState.Time;
                    }
                }
                    break;
                case eReadState.Time:
                {
                    line = line.Replace(',', '.');
                    var parts = line.Split(new[] {"-->"}, StringSplitOptions.RemoveEmptyEntries);

                    // Parse the timestamps
                    if (parts.Length == 2)
                    {
                        if (TimeSpan.TryParse(parts[0], out var fromTime))
                        {
                            if (TimeSpan.TryParse(parts[1], out var toTime))
                            {
                                currentFrom = fromTime;
                                currentTo = toTime;
                                currentState = eReadState.Text;
                            }
                        }
                    }
                }
                    break;
                case eReadState.Text:
                {
                    if (currentText != string.Empty)
                        currentText += "\r\n";

                    currentText += line;

                    // When we hit an empty line, consider it the end of the text
                    if (string.IsNullOrEmpty(line) || l == lines.Length - 1)
                    {
                        Add(currentFrom, currentTo, currentText);
                        currentText = string.Empty;
                        currentState = eReadState.Index;
                    }
                }
                    break;
            }
        }

        using var idxs = _from.Select((_, i) => i).ToPooledList();
        idxs.Sort((a, b) => _from[a].CompareTo(_from[b]));
        using var fromCopy = _from.ToPooledListBuffer();
        using var toCopy = _to.ToPooledListBuffer();
        using var textCopy = _subtitlesText.ToPooledListBuffer();
        for (var i = idxs.Count - 1; i >= 0; i--)
        {
            var idx = idxs[i];
            _from[i] = fromCopy[idx];
            _to[i] = toCopy[idx];
            _subtitlesText[i] = textCopy[idx];
        }
    }

    /// <summary>
    /// return "" for no subtitle
    /// </summary>
    public string GetByTime(TimeSpan passedTime)
    {
        var idx = _to.BinarySearch(passedTime);
        if (idx >= 0) return _subtitlesText[idx];
        idx = ~idx;
        if (idx >= _to.Count) return "";
        //此时，idx为字幕在哪个to前
        var from = _from[idx];
        return passedTime > from ? _subtitlesText[idx] : "";
    }

    private enum eReadState
    {
        Index,
        Time,
        Text
    }

    public void Dispose()
    {
        _from.Dispose();
        _to.Dispose();
        _subtitlesText.Dispose();
    }
}