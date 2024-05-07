namespace CelSerEngine.WpfBlazor;

/// <summary>
/// Felt cute, might delete later
/// This class is designed to manage a single shared state across all components for applying the desired theme.
/// This is perhaps a little bit of an overkill at the moment.
/// </summary>
public class ThemeManager
{
    private bool _isDark = false;
    public bool IsDark
    {
        get => _isDark;
        set
        {
            if (_isDark != value)
            {
                _isDark = value;
                NotifyStateChanged();
            }
        }
    }
    public event Action? OnThemeChanged;
    private void NotifyStateChanged() => OnThemeChanged?.Invoke();

}
