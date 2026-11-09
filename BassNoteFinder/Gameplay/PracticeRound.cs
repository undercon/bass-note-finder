namespace BassNoteFinder.Gameplay;

internal sealed class PracticeRound
{
    private readonly HashSet<int> _pitchClasses = new();
    private readonly Queue<int> _remaining = new();
    private readonly HashSet<int> _missed = new();
    private int? _previous;

    public void Reset(int? previous)
    {
        _pitchClasses.Clear();
        _remaining.Clear();
        _missed.Clear();
        _previous = previous;
    }

    public void RecordMiss(int pitchClass) => _missed.Add(pitchClass);

    public int ChooseNext(IReadOnlySet<int> pitchClasses)
    {
        if (!_pitchClasses.SetEquals(pitchClasses))
        {
            Reset(_previous);
            _pitchClasses.UnionWith(pitchClasses);
        }

        if (_remaining.Count == 0)
        {
            int[] round = _pitchClasses.ToArray();
            Random.Shared.Shuffle(round);
            if (_missed.Count > 0)
            {
                round = round.OrderByDescending(_missed.Contains).ToArray();
            }

            if (round.Length > 1 && round[0] == _previous)
            {
                int next = _missed.Count > 0 ? 1 : Random.Shared.Next(1, round.Length);
                (round[0], round[next]) = (round[next], round[0]);
            }

            foreach (int pitchClass in round)
            {
                _remaining.Enqueue(pitchClass);
            }
            _missed.Clear();
        }

        int selected = _remaining.Dequeue();
        _previous = selected;
        return selected;
    }
}
