using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace AHFMCChat;

public sealed class EventHub
{
    readonly object _l = new();
    readonly List<ChatEvent> _buf = new();
    Action<ChatEvent>? _sink;

    public void Push(ChatEvent e)
    {
        Action<ChatEvent>? sink;
        lock (_l)
        {
            if (_sink == null) { _buf.Add(e); return; }
            sink = _sink;
        }
        sink(e);
    }

    public void Attach(Action<ChatEvent> sink)
    {
        lock (_l)
        {
            foreach (var e in _buf) sink(e);
            _buf.Clear();
            _sink = sink;
        }
    }
}

public sealed class MessageVm
{
    static readonly Brush[] Palette = new[]
        { "#7AA2FF", "#F2A65A", "#6FCF97", "#E58BC0", "#B39DFF", "#56CCF2", "#F2C94C", "#EB8F8F" }
        .Select(Make).ToArray();

    static Brush Make(string hex)
    {
        var b = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        b.Freeze();
        return b;
    }

    public string From { get; init; } = "";
    public string Text { get; init; } = "";
    public string TimeText { get; init; } = "";
    public bool Verified { get; init; }
    public string Initial => From.Length > 0 ? char.ToUpperInvariant(From[0]).ToString() : "?";
    public Brush NameBrush { get; init; } = Brushes.Gray;

    public static MessageVm FromEvent(ChatEvent e, bool mine)
    {
        bool warn = e.Kind == "warn";
        string from = warn ? "Warning" : e.Kind == "system" ? "Info" : e.From;

        int h = 0;
        foreach (char ch in from.ToLowerInvariant()) h = unchecked(h * 31 + ch);
        Brush brush = mine ? Palette[0]
                    : warn ? Brushes.IndianRed
                    : Palette[1 + (int)((uint)h % (uint)(Palette.Length - 1))];

        return new MessageVm
        {
            From = from,
            Text = e.Text,
            Verified = e.Verified && !mine,
            NameBrush = brush,
            TimeText = e.Time.Date == DateTime.Today ? e.Time.ToString("t") : e.Time.ToString("g")
        };
    }
}

public sealed class Conversation : INotifyPropertyChanged
{
    string _title;
    int _unread;

    public Conversation(string kind, string key, string title) { Kind = kind; Key = key; _title = title; }

    public string Kind { get; }
    public string Key { get; }
    public bool IsOwner { get; set; }
    public ObservableCollection<MessageVm> Messages { get; } = new();

    public string Title
    {
        get => _title;
        set { if (_title == value) return; _title = value; Raise(nameof(Title)); }
    }

    public int Unread
    {
        get => _unread;
        set { if (_unread == value) return; _unread = value; Raise(nameof(Unread)); Raise(nameof(HasUnread)); }
    }
    public bool HasUnread => _unread > 0;

    public string Section => Kind switch
    {
        "public" => "LOBBY", "appeal" => "APPEALS", "invite" => "INVITATIONS", "dm" => "DIRECT MESSAGES", "party" => "PARTIES", _ => "SYSTEM"
    };
    public int SectionOrder => Kind switch { "public" => 0, "appeal" => 1, "invite" => 2, "dm" => 3, "party" => 4, _ => 5 };
    public string Icon => Kind switch { "public" => "#", "dm" => "@", "party" => "◈", "invite" => "✉", "appeal" => "⚖", _ => "!" };

    public event PropertyChangedEventHandler? PropertyChanged;
    void Raise(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}
