using BassNoteFinder.MusicTheory;

namespace BassNoteFinder.Gameplay;

public interface IGameMode
{
    void OnActivate();
    void OnDeactivate();
    void OnNoteDetected(Note note, double centsOff);
    void OnNoteLost();
    void OnSpacePressed() { }
}
