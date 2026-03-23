using System;
using System.Collections.Generic;
using System.Linq;

namespace ExperimentalControlPlatform.App.Modals;

public sealed class DiagnosticsDialogViewModel
{
    public DiagnosticsDialogViewModel(string title, string subtitle, IReadOnlyList<DiagnosticsFieldViewModel> fields)
    {
        Title = title;
        Subtitle = subtitle;
        Fields = fields;
    }

    public string Title { get; }

    public string Subtitle { get; }

    public IReadOnlyList<DiagnosticsFieldViewModel> Fields { get; }

    public static DiagnosticsDialogViewModel FromSummary(string title, string subtitle, string summary)
    {
        var fields = summary
            .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
            .Select(ParseField)
            .ToList();

        return new DiagnosticsDialogViewModel(title, subtitle, fields);
    }

    private static DiagnosticsFieldViewModel ParseField(string line)
    {
        var separator = line.IndexOf(':');
        if (separator < 0)
        {
            return new DiagnosticsFieldViewModel(line, string.Empty);
        }

        var label = line[..separator].Trim();
        var value = line[(separator + 1)..].Trim();
        return new DiagnosticsFieldViewModel(label, value);
    }
}

public sealed class DiagnosticsFieldViewModel
{
    public DiagnosticsFieldViewModel(string label, string value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }

    public string Value { get; }
}
