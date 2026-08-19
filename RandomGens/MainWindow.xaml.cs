using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace PokajanRandomizer;

public partial class MainWindow : Window
{
    private const int SlotsPerRow = 5;
    private const int ClaimSlotCount = 5;
    private const double SmallCardWidth = 74;
    private const double SmallCardHeight = 99;
    private const double BonusCardWidth = 188;
    private const double BonusCardHeight = 252;
    private const double ClaimCardWidth = 88;
    private const double ClaimCardHeight = 118;

    private static readonly Brush OrangeBrush = new SolidColorBrush(Color.FromRgb(240, 138, 42));
    private static readonly Brush BlueBrush = new SolidColorBrush(Color.FromRgb(61, 126, 255));
    private static readonly Brush PinkBrush = new SolidColorBrush(Color.FromRgb(242, 107, 160));

    private readonly MemberData memberData;
    private readonly SettingsStore settingsStore = new();
    private readonly DispatcherTimer infoHintTimer;
    private readonly SeatState[] seats =
    {
        new(0, "Player 1"),
        new(1, "Player 2"),
        new(2, "Player 3"),
        new(3, "Player 4")
    };
    private readonly Button[] pokajanButtons = new Button[4];
    private readonly TextBlock[] coinLabels = new TextBlock[4];
    private readonly SlotDraft[] claimSlots = Enumerable.Range(0, ClaimSlotCount).Select(_ => new SlotDraft()).ToArray();
    private int? cardsToRemove;

    private RoundResult? currentRound;
    private SeatState? claimWinner;
    private PayoutResult? pendingPayout;
    private int pickerSlotIndex = -1;

    public MainWindow()
    {
        InitializeComponent();

        memberData = RoundPicker.LoadData();
        BuildEmptyRows();
        BuildSeats();
        SetPokajanEnabled(false);

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
        CoinSettlement.ResetCoins(seats);
        RefreshCoinLabels();
        RenderRound(RoundPicker.CreateRound(memberData));
        SetPokajanEnabled(true);
    }

    private void InfoButton_OnClick(object sender, RoutedEventArgs e)
    {
        DismissHint();
        ShowInfoOverlay();
    }

    private void MainWindow_OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        DismissHint();
        if (e.Key != Key.Escape)
        {
            return;
        }

        if (CardPickerOverlay.Visibility == Visibility.Visible)
        {
            HideCardPicker();
            e.Handled = true;
            return;
        }

        if (ClaimOverlay.Visibility == Visibility.Visible)
        {
            HideClaimOverlay();
            e.Handled = true;
            return;
        }

        if (InfoOverlay.Visibility == Visibility.Visible)
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

    private void ClaimPanel_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void ShowInfoOverlay()
    {
        InfoBodyText.Text = ShuffleInfo.BuildBody(cardsToRemove);

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

    private void BuildSeats()
    {
        SeatHost1.Child = CreateSeatPanel(seats[0]);
        SeatHost2.Child = CreateSeatPanel(seats[1]);
        SeatHost3.Child = CreateSeatPanel(seats[2]);
        SeatHost4.Child = CreateSeatPanel(seats[3]);
    }

    private FrameworkElement CreateSeatPanel(SeatState seat)
    {
        var nameLabel = new TextBlock
        {
            Foreground = Brushes.White,
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Text = seat.DisplayName
        };

        var nameBox = new TextBox
        {
            Width = 140,
            FontSize = 18,
            Visibility = Visibility.Collapsed,
            Text = seat.DisplayName
        };

        var penButton = new Button
        {
            Width = 32,
            Height = 32,
            Margin = new Thickness(8, 0, 0, 0),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Cursor = Cursors.Hand,
            Content = new TextBlock
            {
                Text = "✎",
                FontSize = 18,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };

        void EndNameEdit()
        {
            seat.Name = string.IsNullOrWhiteSpace(nameBox.Text) ? seat.DefaultName : nameBox.Text.Trim();
            nameLabel.Text = seat.DisplayName;
            nameBox.Visibility = Visibility.Collapsed;
            nameLabel.Visibility = Visibility.Visible;
        }

        penButton.Click += (_, _) =>
        {
            nameBox.Text = seat.DisplayName;
            nameLabel.Visibility = Visibility.Collapsed;
            nameBox.Visibility = Visibility.Visible;
            nameBox.Focus();
            nameBox.SelectAll();
        };
        nameBox.LostFocus += (_, _) => EndNameEdit();
        nameBox.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            EndNameEdit();
            e.Handled = true;
        };

        var coins = new TextBlock
        {
            Margin = new Thickness(0, 6, 0, 10),
            HorizontalAlignment = HorizontalAlignment.Center,
            Foreground = Brushes.White,
            FontSize = 30,
            FontWeight = FontWeights.Black,
            Text = seat.Coins.ToString()
        };
        coinLabels[seat.Id] = coins;

        var pokajan = new Button
        {
            Width = 150,
            Height = 42,
            Background = new SolidColorBrush(Color.FromRgb(232, 255, 240)),
            Foreground = new SolidColorBrush(Color.FromRgb(20, 87, 38)),
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            BorderBrush = Brushes.Transparent,
            Content = "Pokajan!",
            Tag = seat
        };
        pokajan.Click += PokajanButton_OnClick;
        pokajanButtons[seat.Id] = pokajan;

        var nameRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        nameRow.Children.Add(nameLabel);
        nameRow.Children.Add(nameBox);
        nameRow.Children.Add(penButton);

        var panel = new StackPanel
        {
            MinWidth = 180,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        panel.Children.Add(nameRow);
        panel.Children.Add(coins);
        panel.Children.Add(pokajan);
        return panel;
    }

    private void SetPokajanEnabled(bool enabled)
    {
        foreach (var button in pokajanButtons)
        {
            button.IsEnabled = enabled;
        }
    }

    private void RefreshCoinLabels()
    {
        foreach (var seat in seats)
        {
            coinLabels[seat.Id].Text = seat.Coins.ToString();
        }
    }

    private void PokajanButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: SeatState winner } || currentRound is null)
        {
            return;
        }

        DismissHint();
        OpenClaim(winner);
    }

    private void OpenClaim(SeatState winner)
    {
        claimWinner = winner;
        pendingPayout = null;
        foreach (var slot in claimSlots)
        {
            slot.Member = null;
            slot.Color = null;
        }

        ClaimPickTitle.Text = $"{winner.DisplayName}'s Pokajan";
        ClaimErrorText.Text = string.Empty;
        ShowClaimPage(ClaimPickPage);
        RefreshClaimSlots();
        ClaimOverlay.Visibility = Visibility.Visible;
    }

    private void HideClaimOverlay()
    {
        HideCardPicker();
        ClaimOverlay.Visibility = Visibility.Collapsed;
        claimWinner = null;
        pendingPayout = null;
    }

    private void ShowClaimPage(UIElement page)
    {
        ClaimPickPage.Visibility = Visibility.Collapsed;
        ClaimSourcePage.Visibility = Visibility.Collapsed;
        ClaimPayerPage.Visibility = Visibility.Collapsed;
        ClaimDeltaPage.Visibility = Visibility.Collapsed;
        page.Visibility = Visibility.Visible;
    }

    private void RefreshClaimSlots()
    {
        ClaimSlotsHost.Children.Clear();
        for (var i = 0; i < claimSlots.Length; i++)
        {
            ClaimSlotsHost.Children.Add(CreateClaimSlot(i, claimSlots[i]));
        }
    }

    private FrameworkElement CreateClaimSlot(int index, SlotDraft slot)
    {
        var column = new StackPanel
        {
            Margin = new Thickness(8, 0, 8, 0),
            Width = ClaimCardWidth + 16
        };

        FrameworkElement face;
        if (slot.Member is null)
        {
            face = CreateBlankSlot(ClaimCardWidth, ClaimCardHeight);
        }
        else
        {
            face = CreateCardElement(slot.Member, false, ClaimCardWidth, ClaimCardHeight);
        }

        face.Cursor = Cursors.Hand;
        face.MouseLeftButtonDown += (_, _) => OpenCardPicker(index);
        column.Children.Add(face);

        if (slot.Member is not null)
        {
            column.Children.Add(CreateColorRow(index, slot));
        }

        return column;
    }

    private FrameworkElement CreateColorRow(int index, SlotDraft slot)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };

        row.Children.Add(CreateColorChip(slot, CardColor.Orange, OrangeBrush));
        row.Children.Add(CreateColorChip(slot, CardColor.Blue, BlueBrush));
        row.Children.Add(CreateColorChip(slot, CardColor.Pink, PinkBrush));
        return row;
    }

    private Button CreateColorChip(SlotDraft slot, CardColor color, Brush brush)
    {
        var selected = slot.Color == color;
        var button = new Button
        {
            Width = 22,
            Height = 22,
            Margin = new Thickness(3, 0, 3, 0),
            Background = brush,
            BorderBrush = selected ? Brushes.White : Brushes.Transparent,
            BorderThickness = new Thickness(selected ? 3 : 1),
            Cursor = Cursors.Hand,
            Content = string.Empty
        };
        button.Click += (_, _) =>
        {
            slot.Color = color;
            ClaimErrorText.Text = string.Empty;
            RefreshClaimSlots();
        };
        return button;
    }

    private void OpenCardPicker(int index)
    {
        if (currentRound is null)
        {
            return;
        }

        pickerSlotIndex = index;
        CardPickerHost.Children.Clear();
        CardPickerRemoveButton.Visibility = claimSlots[index].Member is null
            ? Visibility.Collapsed
            : Visibility.Visible;
        foreach (var member in currentRound.Rows.SelectMany(row => row.Members))
        {
            var card = CreateCardElement(member, false, ClaimCardWidth, ClaimCardHeight);
            card.Cursor = Cursors.Hand;
            card.Margin = new Thickness(6);
            var picked = member;
            card.MouseLeftButtonDown += (_, _) => PickClaimMember(picked);
            CardPickerHost.Children.Add(card);
        }

        CardPickerOverlay.Visibility = Visibility.Visible;
    }

    private void PickClaimMember(MemberCard member)
    {
        if (pickerSlotIndex < 0)
        {
            return;
        }

        if (CountOtherCopies(pickerSlotIndex, member) >= 3)
        {
            ClaimErrorText.Text = "A triple is 3 cards of the same member. Remove an extra card.";
            HideCardPicker();
            return;
        }

        var slot = claimSlots[pickerSlotIndex];
        var sameMember = slot.Member is not null && PayoutCalculator.IsSameMember(slot.Member, member);
        slot.Member = member;
        if (!sameMember)
        {
            slot.Color = null;
        }

        ClaimErrorText.Text = string.Empty;
        HideCardPicker();
        RefreshClaimSlots();
    }

    private void CardPickerRemoveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (pickerSlotIndex < 0)
        {
            return;
        }

        var slot = claimSlots[pickerSlotIndex];
        slot.Member = null;
        slot.Color = null;
        ClaimErrorText.Text = string.Empty;
        HideCardPicker();
        RefreshClaimSlots();
    }

    private int CountOtherCopies(int exceptIndex, MemberCard member)
    {
        var count = 0;
        for (var i = 0; i < claimSlots.Length; i++)
        {
            if (i == exceptIndex)
            {
                continue;
            }

            var existing = claimSlots[i].Member;
            if (existing is null)
            {
                continue;
            }

            if (PayoutCalculator.IsSameMember(existing, member))
            {
                count++;
            }
        }

        return count;
    }

    private void HideCardPicker()
    {
        CardPickerOverlay.Visibility = Visibility.Collapsed;
        pickerSlotIndex = -1;
    }

    private void CardPickerOverlay_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        HideCardPicker();
    }

    private void ClaimCancelButton_OnClick(object sender, RoutedEventArgs e)
    {
        HideClaimOverlay();
    }

    private void ClaimConfirmButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (currentRound is null || claimWinner is null)
        {
            return;
        }

        var pickedMembers = claimSlots.Where(slot => slot.Member is not null).ToList();
        if (pickedMembers.Count < 3)
        {
            ClaimErrorText.Text = "Pick 3 to 5 cards.";
            return;
        }

        if (pickedMembers.Any(slot => slot.Color is null))
        {
            ClaimErrorText.Text = "Pick a color (orange, blue, or pink) for every card.";
            return;
        }

        var filled = pickedMembers
            .Select(slot => new ClaimedCard(slot.Member!, slot.Color!.Value))
            .ToList();

        var payout = PayoutCalculator.TryCalculate(filled, currentRound.BonusMember, currentRound.Rows);
        if (payout is null)
        {
            ClaimErrorText.Text = "Need 3 of the same member, or one full generation.";
            return;
        }

        pendingPayout = payout;
        ClaimPayoutHint.Text = $"{payout.Total} coins  ({payout.TableRate} + {payout.BonusExtra} bonus)";
        ShowClaimPage(ClaimSourcePage);
    }

    private void ClaimSelfPulled_OnClick(object sender, RoutedEventArgs e)
    {
        if (claimWinner is null || pendingPayout is null)
        {
            return;
        }

        ShowDeltas(CoinSettlement.ApplySelfPulled(seats, claimWinner, pendingPayout));
    }

    private void ClaimDiscarded_OnClick(object sender, RoutedEventArgs e)
    {
        if (claimWinner is null)
        {
            return;
        }

        ClaimPayerHost.Children.Clear();
        foreach (var seat in seats.Where(item => item.Id != claimWinner.Id))
        {
            var payer = seat;
            var button = new Button
            {
                Width = 220,
                Height = 50,
                Margin = new Thickness(0, 0, 0, 12),
                Background = new SolidColorBrush(Color.FromRgb(232, 255, 240)),
                Foreground = new SolidColorBrush(Color.FromRgb(20, 87, 38)),
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                BorderBrush = Brushes.Transparent,
                Content = payer.DisplayName
            };
            button.Click += (_, _) => ApplyDiscardPayout(payer);
            ClaimPayerHost.Children.Add(button);
        }

        ShowClaimPage(ClaimPayerPage);
    }

    private void ApplyDiscardPayout(SeatState payer)
    {
        if (claimWinner is null || pendingPayout is null)
        {
            return;
        }

        ShowDeltas(CoinSettlement.ApplyDiscarded(seats, claimWinner, payer, pendingPayout));
    }

    private void ShowDeltas(IReadOnlyList<CoinDelta> deltas)
    {
        RefreshCoinLabels();
        ClaimDeltaHost.Children.Clear();
        foreach (var delta in deltas)
        {
            var sign = delta.Change > 0 ? "+" : string.Empty;
            ClaimDeltaHost.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 6, 0, 6),
                Foreground = Brushes.White,
                FontSize = 22,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Text = $"{delta.Seat.DisplayName}:  {delta.OldCoins}  →  {sign}{delta.Change}  →  {delta.NewCoins}"
            });
        }

        ShowClaimPage(ClaimDeltaPage);
    }

    private void ClaimDeltaDone_OnClick(object sender, RoutedEventArgs e)
    {
        HideClaimOverlay();
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
        currentRound = round;
        RowsHost.Children.Clear();
        foreach (var row in round.Rows)
        {
            RowsHost.Children.Add(CreateRowShell(row.Label, row.Generation, row.Members));
        }

        BonusCardHost.Child = CreateCardElement(round.BonusMember, true);
        cardsToRemove = round.CardsToRemove;
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
            cardsPanel.Children.Add(CreateBlankSlot(SmallCardWidth, SmallCardHeight));
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

    private FrameworkElement CreateCardElement(MemberCard member, bool isBonus, double? widthOverride = null, double? heightOverride = null)
    {
        var image = AssetResolver.TryLoad(member);
        if (image is null)
        {
            return CreatePlaceholderCard(member.Generation, member.Member, isBonus, widthOverride, heightOverride);
        }

        var width = widthOverride ?? (isBonus ? BonusCardWidth : SmallCardWidth);
        var height = heightOverride ?? (isBonus ? BonusCardHeight : SmallCardHeight);

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

    private static FrameworkElement CreateBlankSlot(double width, double height)
    {
        var slot = new Border
        {
            Width = width,
            Height = height,
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

    private FrameworkElement CreatePlaceholderCard(
        string? generation,
        string member,
        bool isBonus,
        double? widthOverride = null,
        double? heightOverride = null)
    {
        var width = widthOverride ?? (isBonus ? BonusCardWidth : SmallCardWidth);
        var height = heightOverride ?? (isBonus ? BonusCardHeight : SmallCardHeight);
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
                Text = GenerationLabels.For(generation)
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

}
