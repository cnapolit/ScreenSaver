using CommunityToolkit.Mvvm.ComponentModel;
using ScreenSaver.Models;
using System.IO;

namespace ScreenSaver;

public class ScreenSaverViewModel : ObservableObject
{
    public string? BackgroundPath
    {
        get => field;
        set
        {
            if (!ValidPath(value)) return;
            field = value;
            OnPropertyChanged();
        }
    }

    public string? LogoPath
    {
        get => field;
        set
        {
            if (!ValidPath(value)) return;
            field = value;
            OnPropertyChanged();
        }
    }

    public string? VideoPath
    {
        get => field;
        set
        {
            if (!ValidPath(value)) return;
            field = value;
            OnPropertyChanged();
        }
    }

    public string? MusicPath
    {
        get => field;
        set
        {
            if (!ValidPath(value)) return;
            field = value;
            OnPropertyChanged();
        }
    }

    public string? ClockText
    {
        get => field;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public string? DateText
    {
        get => field;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public ScreenSaverSettings? Settings
    {
        get => field;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    private static bool ValidPath(string? value) => !string.IsNullOrWhiteSpace(value) && File.Exists(value);
}
