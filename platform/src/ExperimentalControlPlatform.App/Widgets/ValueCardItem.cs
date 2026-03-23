namespace ExperimentalControlPlatform.App.Widgets;

public sealed class ValueCardItem : ObservableObject
{
    private string _title;
    private string _value;

    public ValueCardItem(string title, string value)
    {
        _title = title;
        _value = value;
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}
