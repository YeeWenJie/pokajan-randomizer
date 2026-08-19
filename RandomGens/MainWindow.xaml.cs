using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PokajanRandomizer;

public partial class MainWindow : Window
{
    private const int SlotsPerRow = 5;
    private const double SmallCardWidth = 74;
    private const double SmallCardHeight = 99;
    private const double BonusCardWidth = 188;
    private const double BonusCardHeight = 252;

    private readonly MemberData memberData;
    private readonly SettingsStore settingsStore = new();
    private readonly DispatcherTimer infoHintTimer;

    public MainWindow()
    {
        InitializeComponent();

        memberData = RoundPicker.LoadData();
        BuildEmptyRows();

        infoHintTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        infoHintTimer.Tick += InfoHintTimer_OnTick;

        Loaded += MainWindow_OnLoaded;
        PreviewMouseDown += (_, _) => DismissHint();
        PreviewKeyDown += MainWindow_OnPreviewKeyDown;
    }

    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        var settings = settingsStore.Load();
        if (settings.InfoHintShown)
        {
            return;
        }

        infoHintTimer.Start();
    }

    private void InfoHintTimer_OnTick(object? sender, EventArgs e)
    {
        infoHintTimer.Stop();

        var settings = settingsStore.Load();
        if (settings.InfoHintShown)
        {
            return;
        }

        settings.InfoHintShown = true;
        settingsStore.Save(settings);
        InfoHintPopup.Visibility = Visibility.Visible;
    }

    private void NewGameButton_OnClick(object sender, RoutedEventArgs e)
    {
        DismissHint();
        RenderRound(RoundPicker.CreateRound(memberData));
    }

    private void InfoButton_OnClick(object sender, RoutedEventArgs e)
    {
        DismissHint();
        ShowInfoOverlay();
    }

    private void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        DismissHint();
        if (e.Key == Key.Escape && InfoOverlay.Visibility == Visibility.Visible)
        {
            HideInfoOverlay();
            e.Handled = true;
        }
    }

    private void InfoOverlay_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        HideInfoOverlay();
    }

    private void InfoPanel_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        HideInfoOverlay();
        e.Handled = true;
    }

    private void ShowInfoOverlay()
    {
        var cardsToRemoveText = NewGameButton.Tag is int cardsToRemove
            ? cardsToRemove.ToString()
            : "extra cards until the deck is 100 (the exact number appears here after New Game)";

        InfoBodyText.Text =
            "1. Take out the 4 gen cards that you got (each character has 9 cards: 3 pink, 3 blue, 3 orange). Check who the bonus card is, shuffle that character's 9 cards first, and take one out — that card is the bonus card.\n" +
            "2. Shuffle the remaining cards.\n" +
            $"3. Then take out {cardsToRemoveText} cards so that it can be a 100 card deck.\n" +
            "4. Then deal 7 cards to each person.";

        InfoOverlay.Visibility = Visibility.Visible;
    }

    private void HideInfoOverlay()
    {
        InfoOverlay.Visibility = Visibility.Collapsed;
    }

    private void DismissHint()
    {
        if (InfoHintPopup.Visibility != Visibility.Visible && !infoHintTimer.IsEnabled)
        {
            return;
        }

        infoHintTimer.Stop();
        InfoHintPopup.Visibility = Visibility.Collapsed;

        var settings = settingsStore.Load();
        if (settings.InfoHintShown)
        {
            return;
        }

        settings.InfoHintShown = true;
        settingsStore.Save(settings);
    }

    private void BuildEmptyRows()
    {
        RowsHost.Children.Clear();
        for (var i = 0; i < 4; i++)
        {
            RowsHost.Children.Add(CreateRowShell($"Row {i + 1}", string.Empty));
        }

        BonusCardHost.Child = CreatePlaceholderCard(null, "Bonus", true);
    }

    private void RenderRound(RoundResult round)
    {
        RowsHost.Children.Clear();
        foreach (var row in round.Rows)
        {
            RowsHost.Children.Add(CreateRowShell(row.Label, row.Generation, row.Members));
        }

        BonusCardHost.Child = CreateCardElement(round.BonusMember, true);
        NewGameButton.Tag = round.CardsToRemove;
    }

    private UIElement CreateRowShell(string label, string generation, IReadOnlyList<MemberCard>? members = null)
    {
        var cardsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        foreach (var member in members ?? Array.Empty<MemberCard>())
        {
            cardsPanel.Children.Add(CreateCardElement(member, false));
        }

        while (cardsPanel.Children.Count < SlotsPerRow)
        {
            cardsPanel.Children.Add(CreateBlankSlot());
        }

        var lineWidth = (SmallCardWidth + 8) * SlotsPerRow;

        var cardsColumn = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center
        };
        cardsColumn.Children.Add(cardsPanel);
        cardsColumn.Children.Add(new Border
        {
            Margin = new Thickness(4, 6, 0, 0),
            Height = 2,
            Width = lineWidth,
            HorizontalAlignment = HorizontalAlignment.Left,
            Background = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255))
        });

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 4, 0, 4),
            VerticalAlignment = VerticalAlignment.Center
        };
        row.Children.Add(cardsColumn);
        row.Children.Add(new TextBlock
        {
            Margin = new Thickness(10, 0, 0, 8),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = new SolidColorBrush(Color.FromArgb(190, 255, 255, 255)),
            FontSize = 28,
            FontWeight = FontWeights.Black,
            Text = string.IsNullOrWhiteSpace(generation) ? string.Empty : label
        });

        return row;
    }

    private FrameworkElement CreateCardElement(MemberCard member, bool isBonus)
    {
        var image = AssetResolver.TryLoad(member);
        if (image is null)
        {
            return CreatePlaceholderCard(member.Generation, member.Member, isBonus);
        }

        var width = isBonus ? BonusCardWidth : SmallCardWidth;
        var height = isBonus ? BonusCardHeight : SmallCardHeight;

        return new Border
        {
            Width = width,
            Height = height,
            Margin = isBonus ? new Thickness(0) : new Thickness(4, 0, 4, 0),
            Background = Brushes.Transparent,
            Child = new Image
            {
                Source = image,
                Stretch = Stretch.Fill,
                SnapsToDevicePixels = true
            }
        };
    }

    private FrameworkElement CreateBlankSlot()
    {
        var slot = new Border
        {
            Width = SmallCardWidth,
            Height = SmallCardHeight,
            Margin = new Thickness(4, 0, 4, 0),
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromArgb(70, 255, 255, 255))
        };

        var triangle = new Polygon
        {
            Fill = new SolidColorBrush(Color.FromArgb(160, 255, 255, 255)),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Points = new PointCollection
            {
                new Point(0, 0),
                new Point(14, 9),
                new Point(0, 18)
            }
        };

        slot.Child = triangle;
        return slot;
    }

    private FrameworkElement CreatePlaceholderCard(string? generation, string member, bool isBonus)
    {
        var width = isBonus ? BonusCardWidth : SmallCardWidth;
        var height = isBonus ? BonusCardHeight : SmallCardHeight;
        var label = string.IsNullOrWhiteSpace(member) ? "?" : member;

        var border = new Border
        {
            Width = width,
            Height = height,
            CornerRadius = new CornerRadius(isBonus ? 18 : 12),
            Margin = isBonus ? new Thickness(0) : new Thickness(4, 0, 4, 0),
            BorderThickness = new Thickness(2),
            BorderBrush = Brushes.White,
            Background = new LinearGradientBrush(
                Color.FromRgb(64, 146, 83),
                Color.FromRgb(28, 96, 48),
                new Point(0.5, 0),
                new Point(0.5, 1))
        };

        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        if (isBonus && !string.IsNullOrWhiteSpace(generation))
        {
            stack.Children.Add(new TextBlock
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.White,
                FontSize = 24,
                FontWeight = FontWeights.Black,
                Text = BuildBonusTopLabel(generation)
            });
        }

        stack.Children.Add(new TextBlock
        {
            Margin = new Thickness(8),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Foreground = Brushes.White,
            FontSize = isBonus ? 22 : 12,
            FontWeight = FontWeights.Bold,
            Text = label
        });

        border.Child = stack;
        return border;
    }

    private static string BuildBonusTopLabel(string generation) => generation switch
    {
        "Gen0" => "0",
        "Gen1" => "1",
        "Gen2" => "2",
        "Gen3" => "3",
        "Gen4" => "4",
        "Gen5" => "5",
        "ID Gen1" => "ID1",
        "ID Gen2" => "ID2",
        "ID Gen3" => "ID3",
        "Gamers" => "Ga",
        "Promise" => "Pr",
        "Myth" => "My",
        "HoloX" => "X",
        "Advent" => "Ad",
        "ReGloss" => "Rg",
        _ => generation
    };
}
