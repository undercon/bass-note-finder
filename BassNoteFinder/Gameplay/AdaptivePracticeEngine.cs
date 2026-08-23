namespace BassNoteFinder.Gameplay;

/// <summary>
/// Selects practice notes using recent accuracy, response time, and musical context.
/// Weak notes receive more attention while a recency guard prevents drilling the
/// same pitch class repeatedly.
/// </summary>
public sealed class AdaptivePracticeEngine
{
    private const int RecentHistoryLength = 8;
    private readonly Dictionary<int, NoteProgress> _progress = new();
    private readonly Queue<int> _recentPitchClasses = new();
    private readonly Random _random;
    private long _selectionNumber;

    public AdaptivePracticeEngine(Random? random = null)
    {
        _random = random ?? new Random();
    }

    public void RecordResult(int pitchClass, int mistakes, TimeSpan responseTime)
    {
        if (pitchClass is < 0 or > 11)
        {
            throw new ArgumentOutOfRangeException(nameof(pitchClass));
        }

        mistakes = Math.Max(0, mistakes);
        double seconds = Math.Max(0, responseTime.TotalSeconds);
        double errorDifficulty = mistakes == 0
            ? 0
            : Math.Min(1, 0.65 + ((mistakes - 1) * 0.15));
        double speedDifficulty = Math.Clamp((seconds - 3) / 7, 0, 1);
        double sampleDifficulty = (errorDifficulty * 0.6) + (speedDifficulty * 0.4);

        if (!_progress.TryGetValue(pitchClass, out NoteProgress? progress))
        {
            progress = new NoteProgress();
            _progress[pitchClass] = progress;
        }

        progress.Difficulty = progress.Observations == 0
            ? sampleDifficulty
            : (progress.Difficulty * 0.62) + (sampleDifficulty * 0.38);
        progress.Observations++;
    }

    public int ChooseNext(IReadOnlyCollection<int> availablePitchClasses)
    {
        int[] candidates = availablePitchClasses
            .Where(pitchClass => pitchClass is >= 0 and <= 11)
            .Distinct()
            .Order()
            .ToArray();

        if (candidates.Length == 0)
        {
            throw new ArgumentException("At least one valid pitch class is required.", nameof(availablePitchClasses));
        }

        _selectionNumber++;
        int[] eligible = ApplyRecencyGuard(candidates);
        int? previous = _recentPitchClasses.Count > 0 ? _recentPitchClasses.Last() : null;

        int selected = eligible
            .Select(pitchClass => new
            {
                PitchClass = pitchClass,
                Score = CalculatePriority(pitchClass, previous) + (_random.NextDouble() * 0.3)
            })
            .OrderByDescending(candidate => candidate.Score)
            .First()
            .PitchClass;

        if (!_progress.TryGetValue(selected, out NoteProgress? selectedProgress))
        {
            selectedProgress = new NoteProgress();
            _progress[selected] = selectedProgress;
        }
        selectedProgress.LastSelected = _selectionNumber;

        _recentPitchClasses.Enqueue(selected);
        while (_recentPitchClasses.Count > RecentHistoryLength)
        {
            _recentPitchClasses.Dequeue();
        }

        return selected;
    }

    private int[] ApplyRecencyGuard(int[] candidates)
    {
        if (candidates.Length == 1 || _recentPitchClasses.Count == 0)
        {
            return candidates;
        }

        int guardLength = candidates.Length >= 4 ? 2 : 1;
        HashSet<int> guarded = _recentPitchClasses
            .Reverse()
            .Take(guardLength)
            .ToHashSet();
        int[] eligible = candidates.Where(pitchClass => !guarded.Contains(pitchClass)).ToArray();
        return eligible.Length > 0 ? eligible : candidates;
    }

    private double CalculatePriority(int pitchClass, int? previous)
    {
        bool hasProgress = _progress.TryGetValue(pitchClass, out NoteProgress? progress);
        double difficulty = hasProgress ? progress!.Difficulty : 0;
        double unseenBonus = !hasProgress || progress!.Observations == 0 ? 0.8 : 0;
        double ageBonus = !hasProgress || progress!.LastSelected == 0
            ? 0.6
            : Math.Min(0.8, (_selectionNumber - progress.LastSelected) * 0.08);
        int recentCount = _recentPitchClasses.Count(recent => recent == pitchClass);
        double repetitionPenalty = recentCount * 0.32;
        double patternBonus = previous.HasValue ? GetPatternBonus(previous.Value, pitchClass) : 0;

        return 1 + (difficulty * 3.4) + unseenBonus + ageBonus + patternBonus - repetitionPenalty;
    }

    private static double GetPatternBonus(int fromPitchClass, int toPitchClass)
    {
        int ascendingDistance = (toPitchClass - fromPitchClass + 12) % 12;
        int shortestDistance = Math.Min(ascendingDistance, 12 - ascendingDistance);
        return shortestDistance switch
        {
            2 => 0.5, // adjacent notes in a scale
            5 => 0.65, // fourth/fifth movement
            1 => 0.25, // chromatic neighbour
            3 or 4 => 0.2, // familiar chord tones
            _ => 0
        };
    }

    private sealed class NoteProgress
    {
        public int Observations { get; set; }
        public double Difficulty { get; set; }
        public long LastSelected { get; set; }
    }
}
