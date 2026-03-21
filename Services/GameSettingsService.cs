namespace TriStrike.Services;

public class GameSettingsService
{
    public bool SoundEnabled { get; set; } = true;
    public bool AnimationsEnabled { get; set; } = true;

    public event Action? OnChange;

    public void NotifyChanged() => OnChange?.Invoke();
}
