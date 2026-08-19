namespace PokajanRandomizer.Maui;

public partial class MainPage : ContentPage
{
    private const int SlotsPerRow = 5;
    private const int RowCount = 4;
    private const int ClaimSlotCount = 5;
    private const double CardAspect = 74.0 / 99.0;

    private static readonly Color OrangeColor = Color.FromRgb(240, 138, 42);
    private static readonly Color BlueColor = Color.FromRgb(61, 126, 255);
    private static readonly Color PinkColor = Color.FromRgb(242, 107, 160);
    private static readonly Color GainColor = Color.FromRgb(110, 235, 140);
    private static readonly Color LossColor = Color.FromRgb(255, 92, 92);

    private readonly MemberData memberData;
    private readonly IDispatcherTimer infoHintTimer;
    private readonly IDispatcherTimer deltaAnimTimer;
    private readonly List<(CoinDelta Delta, Label Label)> deltaAnimTargets = [];
    private DateTime deltaAnimStarted;
    private readonly SeatState[] seats =
    {
        new(0, "Player 1"),
        new(1, "Player 2"),
        new(2, "Player 3"),
        new(3, "Player 4")
    };
    private readonly Button[] pokajanButtons = new Button[4];
    private readonly Label[] coinLabels = new Label[4];
    private readonly Label[] nameLabels = new Label[4];
    private readonly SlotDraft[] claimSlots = Enumerable.Range(0, ClaimSlotCount).Select(_ => new SlotDraft()).ToArray();

    private RoundResult? currentRound;
    private SeatState? claimWinner;
    private PayoutResult? pendingPayout;
    private int pickerSlotIndex = -1;
    private int? cardsToRemove;
    private double cardWidth = 36;
    private double cardHeight = 48;
    private double bonusWidth = 90;
    private double bonusHeight = 120;
    private double lastBoardWidth;
    private double lastBoardHeight;

    private double ClaimCardWidth
    {
        get
        {
            if (Width <= 1)
            {
                return Math.Clamp(cardWidth * 1.15, 36, 72);
            }

            var scaled = Math.Max(cardWidth * 1.25, Width * 0.075);
            return Math.Clamp(scaled, 36, 128);
        }
    }

    private double ClaimCardHeight => ClaimCardWidth / CardAspect;

    public MainPage()
    {
        InitializeComponent();

        memberData = RoundPicker.LoadData();
        InitCardsGrid();
        BuildSeats();
        SetPokajanEnabled(false);
        ApplyChrome();

        infoHintTimer = Dispatcher.CreateTimer();
        infoHintTimer.Interval = TimeSpan.FromSeconds(5);
        infoHintTimer.Tick += InfoHintTimer_OnTick;

        deltaAnimTimer = Dispatcher.CreateTimer();
        deltaAnimTimer.Interval = TimeSpan.FromMilliseconds(16);
        deltaAnimTimer.Tick += DeltaAnimTimer_OnTick;

        Loaded += MainPage_OnLoaded;
    }

    protected override bool OnBackButtonPressed()
    {
        DismissHint();
        if (CardPickerOverlay.IsVisible)
        {
            HideCardPicker();
            return true;
        }

        if (ClaimOverlay.IsVisible)
        {
            HideClaimOverlay();
            return true;
        }

        if (InfoOverlay.IsVisible)
        {
            HideInfoOverlay();
            return true;
        }

        return false;
    }

    private void MainPage_OnLoaded(object? sender, EventArgs e)
    {
        RelayoutBoard();
        if (HintSettings.InfoHintShown)
        {
            return;
        }

        infoHintTimer.Start();
    }

    private void BoardHost_OnSizeChanged(object? sender, EventArgs e)
    {
        RelayoutBoard();
    }

    private void RelayoutBoard()
    {
        var availW = BoardHost.Width;
        var availH = BoardHost.Height;
        if (availW <= 1 || availH <= 1)
        {
            return;
        }

        if (Math.Abs(availW - lastBoardWidth) < 1 && Math.Abs(availH - lastBoardHeight) < 1)
        {
            return;
        }

        lastBoardWidth = availW;
        lastBoardHeight = availH;

        var fitW = availW * 0.86;
        const double newGameReserve = 52;
        var fitH = Math.Max(40, (availH - newGameReserve) * 0.70);
        const double hGap = 3;
        const double vGap = 4;
        const double bonusPadX = 8;
        var labelW = Math.Clamp(fitW * 0.05, 16, 32);
        var bonusText = Math.Clamp(fitH * 0.08, 11, 18);
        var bonusMargin = 10;

        var cardH = (fitH - vGap * (RowCount - 1)) / RowCount;
        var cardW = cardH * CardAspect;
        var bonusCardH = Math.Min(cardH * 2.05, fitH * 0.78);
        var bonusCardW = bonusCardH * CardAspect;
        var bonusFrameW = bonusCardW + bonusPadX * 2;
        var cardsW = cardW * SlotsPerRow + hGap * (SlotsPerRow - 1) + labelW;
        var totalW = cardsW + bonusMargin + bonusFrameW;

        if (totalW > fitW)
        {
            var scale = fitW / totalW;
            cardW *= scale;
            cardH *= scale;
            bonusCardW *= scale;
            bonusCardH *= scale;
            bonusFrameW *= scale;
            labelW *= scale;
            cardsW *= scale;
        }

        cardWidth = Math.Max(16, cardW);
        cardHeight = Math.Max(22, cardH);
        bonusWidth = Math.Max(40, bonusCardW);
        bonusHeight = Math.Max(54, bonusCardH);

        CardsGrid.WidthRequest = cardsW;
        CardsGrid.HeightRequest = cardHeight * RowCount + 4 * (RowCount - 1);
        BonusFrame.WidthRequest = bonusFrameW;
        BonusFrame.HeightRequest = -1;
        BonusCardHost.WidthRequest = bonusWidth;
        BonusCardHost.HeightRequest = bonusHeight;
        BonusTitle.FontSize = Math.Clamp(bonusText, 11, 18);

        ApplyChrome();
        RefreshBoard();
    }

    private void ApplyChrome()
    {
        var font = Math.Clamp(cardHeight * 0.28, 11, 18);
        var coinFont = Math.Clamp(cardHeight * 0.36, 13, 24);
        var buttonH = Math.Clamp(cardHeight * 0.42, 26, 40);
        var buttonW = Math.Clamp(cardWidth * 2.4, 88, 140);

        NewGameButton.HeightRequest = Math.Clamp(cardHeight * 0.5, 28, 44);
        NewGameButton.WidthRequest = Math.Clamp(cardWidth * 3.2, 110, 180);
        NewGameButton.FontSize = Math.Clamp(font + 2, 13, 18);
        NewGameButton.Margin = new Thickness(0);

        InfoButton.HeightRequest = buttonH;
        InfoButton.WidthRequest = Math.Clamp(cardWidth * 1.5, 52, 72);
        InfoButton.FontSize = font;
        InfoHintLabel.FontSize = Math.Max(11, font - 1);

        for (var i = 0; i < seats.Length; i++)
        {
            if (nameLabels[i] is not null)
            {
                nameLabels[i].FontSize = font;
            }

            if (coinLabels[i] is not null)
            {
                coinLabels[i].FontSize = coinFont;
            }

            if (pokajanButtons[i] is not null)
            {
                pokajanButtons[i].HeightRequest = buttonH;
                pokajanButtons[i].WidthRequest = buttonW;
                pokajanButtons[i].FontSize = Math.Max(11, font - 1);
            }
        }
    }

    private void InitCardsGrid()
    {
        CardsGrid.RowDefinitions.Clear();
        CardsGrid.ColumnDefinitions.Clear();
        for (var row = 0; row < RowCount; row++)
        {
            CardsGrid.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        }

        for (var col = 0; col < SlotsPerRow; col++)
        {
            CardsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        }

        CardsGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        RefreshBoard();
    }

    private void RefreshBoard()
    {
        if (currentRound is null)
        {
            BuildEmptyRows();
            return;
        }

        RenderRound(currentRound, resetTag: false);
    }

    private void InfoHintTimer_OnTick(object? sender, EventArgs e)
    {
        infoHintTimer.Stop();
        if (HintSettings.InfoHintShown)
        {
            return;
        }

        HintSettings.InfoHintShown = true;
        InfoHintPopup.IsVisible = true;
    }

    private void NewGameButton_OnClick(object? sender, EventArgs e)
    {
        DismissHint();
        CoinSettlement.ResetCoins(seats);
        RefreshCoinLabels();
        RenderRound(RoundPicker.CreateRound(memberData));
        SetPokajanEnabled(true);
    }

    private void InfoButton_OnClick(object? sender, EventArgs e)
    {
        DismissHint();
        InfoBodyText.Text = ShuffleInfo.BuildBody(cardsToRemove);
        InfoBodyText.MaximumWidthRequest = Math.Max(280, Width * 0.62);
        InfoOverlay.IsVisible = true;
    }

    private void InfoOverlay_OnTapped(object? sender, TappedEventArgs e)
    {
        HideInfoOverlay();
    }

    private void HideInfoOverlay()
    {
        InfoOverlay.IsVisible = false;
    }

    private void DismissHint()
    {
        if (!InfoHintPopup.IsVisible && !infoHintTimer.IsRunning)
        {
            return;
        }

        infoHintTimer.Stop();
        InfoHintPopup.IsVisible = false;
        HintSettings.InfoHintShown = true;
    }

    private void BuildSeats()
    {
        SeatHost1.Content = CreateSeatPanel(seats[0], horizontal: true);
        SeatHost2.Content = CreateSeatPanel(seats[1], horizontal: false);
        SeatHost3.Content = CreateSeatPanel(seats[2], horizontal: true);
        SeatHost4.Content = CreateSeatPanel(seats[3], horizontal: false);
    }

    private View CreateSeatPanel(SeatState seat, bool horizontal)
    {
        var nameLabel = new Label
        {
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            LineBreakMode = LineBreakMode.TailTruncation,
            Text = seat.DisplayName
        };
        nameLabels[seat.Id] = nameLabel;

        var nameBox = new Entry
        {
            WidthRequest = 110,
            FontSize = 14,
            IsVisible = false,
            Text = seat.DisplayName,
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#33000000")
        };

        var penButton = new Button
        {
            Text = "✎",
            WidthRequest = 28,
            HeightRequest = 28,
            Padding = 0,
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.White,
            FontSize = 14
        };

        void EndNameEdit()
        {
            seat.Name = string.IsNullOrWhiteSpace(nameBox.Text) ? seat.DefaultName : nameBox.Text.Trim();
            nameLabel.Text = seat.DisplayName;
            nameBox.IsVisible = false;
            nameLabel.IsVisible = true;
        }

        penButton.Clicked += (_, _) =>
        {
            nameBox.Text = seat.DisplayName;
            nameLabel.IsVisible = false;
            nameBox.IsVisible = true;
            nameBox.Focus();
        };
        nameBox.Unfocused += (_, _) => EndNameEdit();
        nameBox.Completed += (_, _) => EndNameEdit();

        var coins = new Label
        {
            HorizontalOptions = LayoutOptions.Center,
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            Text = seat.Coins.ToString()
        };
        coinLabels[seat.Id] = coins;

        var pokajan = new Button
        {
            Text = "Pokajan!",
            BackgroundColor = Color.FromRgb(232, 255, 240),
            TextColor = Color.FromRgb(20, 87, 38)
        };
        pokajan.Clicked += (_, _) =>
        {
            if (currentRound is null)
            {
                return;
            }

            DismissHint();
            OpenClaim(seat);
        };
        pokajanButtons[seat.Id] = pokajan;

        var nameRow = new HorizontalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 2,
            Children = { nameLabel, nameBox, penButton }
        };

        if (horizontal)
        {
            coins.Margin = new Thickness(8, 0);
            pokajan.Margin = new Thickness(8, 0, 0, 0);
            return new HorizontalStackLayout
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Spacing = 4,
                Children = { nameRow, coins, pokajan }
            };
        }

        coins.Margin = new Thickness(0, 2, 0, 4);
        return new VerticalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            Children = { nameRow, coins, pokajan }
        };
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
        ClaimOverlay.IsVisible = true;
    }

    private void HideClaimOverlay()
    {
        StopDeltaAnimation(snap: true);
        HideCardPicker();
        ClaimOverlay.IsVisible = false;
        claimWinner = null;
        pendingPayout = null;
    }

    private void ShowClaimPage(View page)
    {
        ClaimPickPage.IsVisible = false;
        ClaimSourcePage.IsVisible = false;
        ClaimPayerPage.IsVisible = false;
        ClaimDeltaPage.IsVisible = false;
        page.IsVisible = true;
    }

    private void RefreshClaimSlots()
    {
        ClaimSlotsHost.Children.Clear();
        for (var i = 0; i < claimSlots.Length; i++)
        {
            ClaimSlotsHost.Children.Add(CreateClaimSlot(i, claimSlots[i]));
        }
    }

    private View CreateClaimSlot(int index, SlotDraft slot)
    {
        View face = slot.Member is null
            ? CreateBlankSlot(ClaimCardWidth, ClaimCardHeight)
            : CreateCardElement(slot.Member, false, ClaimCardWidth, ClaimCardHeight);

        AddTap(face, () => OpenCardPicker(index));

        var column = new VerticalStackLayout
        {
            Spacing = 4,
            WidthRequest = ClaimCardWidth,
            Children = { face }
        };

        if (slot.Member is not null)
        {
            column.Children.Add(CreateColorRow(slot));
        }

        return column;
    }

    private View CreateColorRow(SlotDraft slot)
    {
        var gap = Math.Clamp(ClaimCardWidth * 0.08, 3, 10);
        var chipW = Math.Max(8, (ClaimCardWidth - gap * 2) / 3);
        var chipH = chipW;

        return new HorizontalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            WidthRequest = ClaimCardWidth,
            Spacing = gap,
            Children =
            {
                CreateColorChip(slot, CardColor.Orange, OrangeColor, chipW, chipH),
                CreateColorChip(slot, CardColor.Blue, BlueColor, chipW, chipH),
                CreateColorChip(slot, CardColor.Pink, PinkColor, chipW, chipH)
            }
        };
    }

    private View CreateColorChip(SlotDraft slot, CardColor color, Color brush, double width, double height)
    {
        var selected = slot.Color == color;
        var radius = Math.Max(2, width * 0.18);
        var outline = selected ? Math.Max(1.5, width * 0.12) : 0;
        var host = new Grid
        {
            WidthRequest = width,
            HeightRequest = height,
            MinimumWidthRequest = 0,
            MinimumHeightRequest = 0,
            MaximumWidthRequest = width,
            MaximumHeightRequest = height
        };

        if (selected)
        {
            host.Children.Add(new BoxView
            {
                Color = Colors.White,
                CornerRadius = radius,
                MinimumWidthRequest = 0,
                MinimumHeightRequest = 0
            });
        }

        host.Children.Add(new BoxView
        {
            Color = brush,
            CornerRadius = Math.Max(1.5, radius - 0.5),
            Margin = new Thickness(outline),
            MinimumWidthRequest = 0,
            MinimumHeightRequest = 0
        });
        AddTap(host, () =>
        {
            slot.Color = color;
            ClaimErrorText.Text = string.Empty;
            RefreshClaimSlots();
        });
        return host;
    }

    private void OpenCardPicker(int index)
    {
        if (currentRound is null)
        {
            return;
        }

        pickerSlotIndex = index;
        CardPickerHost.Children.Clear();
        var pickerHeight = Math.Min(Height * 0.55, 280);
        CardPickerScroll.MaximumHeightRequest = pickerHeight;
        CardPickerHost.WidthRequest = Math.Min(Width * 0.8, 720);
        CardPickerRemoveButton.IsVisible = claimSlots[index].Member is not null;
        foreach (var member in currentRound.Rows.SelectMany(row => row.Members))
        {
            var card = CreateCardElement(member, false, ClaimCardWidth, ClaimCardHeight);
            card.Margin = new Thickness(4);
            var picked = member;
            AddTap(card, () => PickClaimMember(picked));
            CardPickerHost.Children.Add(card);
        }

        CardPickerOverlay.IsVisible = true;
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

    private void CardPickerRemoveButton_OnClick(object? sender, EventArgs e)
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
        CardPickerOverlay.IsVisible = false;
        pickerSlotIndex = -1;
    }

    private void CardPickerOverlay_OnTapped(object? sender, TappedEventArgs e)
    {
        HideCardPicker();
    }

    private static void CardPickerPanel_OnTapped(object? sender, TappedEventArgs e)
    {
    }

    private void ClaimCancelButton_OnClick(object? sender, EventArgs e)
    {
        HideClaimOverlay();
    }

    private void ClaimConfirmButton_OnClick(object? sender, EventArgs e)
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

    private void ClaimSelfPulled_OnClick(object? sender, EventArgs e)
    {
        if (claimWinner is null || pendingPayout is null)
        {
            return;
        }

        ShowDeltas(CoinSettlement.ApplySelfPulled(seats, claimWinner, pendingPayout));
    }

    private void ClaimDiscarded_OnClick(object? sender, EventArgs e)
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
                Text = payer.DisplayName,
                WidthRequest = Math.Clamp(cardWidth * 4, 140, 220),
                HeightRequest = Math.Clamp(cardHeight * 0.5, 32, 44),
                Margin = new Thickness(0, 0, 0, 8),
                BackgroundColor = Color.FromRgb(232, 255, 240),
                TextColor = Color.FromRgb(20, 87, 38)
            };
            button.Clicked += (_, _) => ApplyDiscardPayout(payer);
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
        StopDeltaAnimation(snap: false);
        ClaimDeltaHost.Children.Clear();

        var gained = deltas.Where(delta => delta.Change > 0).ToList();
        var lost = deltas.Where(delta => delta.Change < 0).ToList();
        if (gained.Count == 0 || lost.Count == 0)
        {
            ShowClaimPage(ClaimDeltaPage);
            return;
        }

        if (lost.Count == 1)
        {
            ClaimDeltaHost.Children.Add(BuildDiscardedDeltaView(gained[0], lost[0]));
            StartDeltaAnimation();
            ShowClaimPage(ClaimDeltaPage);
            return;
        }

        ClaimDeltaHost.Children.Add(BuildSelfPulledDeltaView(gained[0], lost));
        StartDeltaAnimation();
        ShowClaimPage(ClaimDeltaPage);
    }

    private View BuildDiscardedDeltaView(CoinDelta winner, CoinDelta payer)
    {
        var font = Math.Clamp(cardHeight * 0.34, 15, 24);
        return new HorizontalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 12,
            Children =
            {
                CreateDeltaLine(payer, font),
                CreateDeltaArrow("→", font),
                CreateDeltaLine(winner, font)
            }
        };
    }

    private View BuildSelfPulledDeltaView(CoinDelta winner, IReadOnlyList<CoinDelta> payers)
    {
        var font = Math.Clamp(cardHeight * 0.32, 14, 22);
        var grid = new Grid
        {
            HorizontalOptions = LayoutOptions.Center,
            ColumnSpacing = 10,
            RowSpacing = 8
        };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

        for (var i = 0; i < payers.Count; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var name = CreateDeltaLine(payers[i], font);
            var arrow = CreateDeltaArrow("→", font);
            Grid.SetRow(name, i);
            Grid.SetColumn(name, 0);
            Grid.SetRow(arrow, i);
            Grid.SetColumn(arrow, 1);
            grid.Children.Add(name);
            grid.Children.Add(arrow);
        }

        var winnerName = CreateDeltaLine(winner, font + 2);
        winnerName.VerticalOptions = LayoutOptions.Center;
        Grid.SetRow(winnerName, 0);
        Grid.SetColumn(winnerName, 2);
        Grid.SetRowSpan(winnerName, Math.Max(1, payers.Count));
        grid.Children.Add(winnerName);
        return grid;
    }

    private Label CreateDeltaLine(CoinDelta delta, double fontSize)
    {
        var label = new Label
        {
            Text = CoinDeltaAnimator.FormatLine(delta.Seat.DisplayName, delta.OldCoins, delta.Change, delta.Change),
            TextColor = delta.Change > 0 ? GainColor : LossColor,
            FontSize = fontSize,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center
        };
        deltaAnimTargets.Add((delta, label));
        return label;
    }

    private static Label CreateDeltaArrow(string arrow, double fontSize)
    {
        return new Label
        {
            Text = arrow,
            TextColor = Colors.White,
            FontSize = fontSize + 4,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center
        };
    }

    private void StartDeltaAnimation()
    {
        deltaAnimStarted = DateTime.UtcNow;
        ApplyDeltaAnim(0);
        deltaAnimTimer.Start();
    }

    private void StopDeltaAnimation(bool snap)
    {
        deltaAnimTimer.Stop();
        if (snap)
        {
            ApplyDeltaAnim(1);
            RefreshCoinLabels();
        }

        deltaAnimTargets.Clear();
    }

    private void DeltaAnimTimer_OnTick(object? sender, EventArgs e)
    {
        var t = (DateTime.UtcNow - deltaAnimStarted).TotalMilliseconds / 2000.0;
        if (t >= 1)
        {
            deltaAnimTimer.Stop();
            ApplyDeltaAnim(1);
            RefreshCoinLabels();
            return;
        }

        ApplyDeltaAnim(t);
    }

    private void ApplyDeltaAnim(double t)
    {
        foreach (var (delta, label) in deltaAnimTargets)
        {
            var (coins, remaining) = CoinDeltaAnimator.At(delta, t);
            label.Text = CoinDeltaAnimator.FormatLine(delta.Seat.DisplayName, coins, delta.Change, remaining);
            coinLabels[delta.Seat.Id].Text = coins.ToString();
        }
    }

    private void ClaimDeltaDone_OnClick(object? sender, EventArgs e)
    {
        HideClaimOverlay();
    }

    private void BuildEmptyRows()
    {
        FillCardsGrid(null);
        BonusCardHost.Content = CreatePlaceholderCard(null, "Bonus", true, bonusWidth, bonusHeight);
    }

    private void RenderRound(RoundResult round, bool resetTag = true)
    {
        currentRound = round;
        FillCardsGrid(round.Rows);
        BonusCardHost.Content = CreateCardElement(round.BonusMember, true, bonusWidth, bonusHeight);
        if (resetTag)
        {
            cardsToRemove = round.CardsToRemove;
        }
    }

    private void FillCardsGrid(IReadOnlyList<GenerationRow>? rows)
    {
        CardsGrid.Children.Clear();
        for (var row = 0; row < RowCount; row++)
        {
            var members = rows is not null && row < rows.Count
                ? rows[row].Members
                : Array.Empty<MemberCard>();
            var label = rows is not null && row < rows.Count ? rows[row].Label : string.Empty;

            for (var col = 0; col < SlotsPerRow; col++)
            {
                View cell = col < members.Count
                    ? CreateCardElement(members[col], false)
                    : CreateBlankSlot();
                CardsGrid.Add(cell, col, row);
            }

            var genLabel = new Label
            {
                Text = label,
                TextColor = Color.FromArgb("#BEFFFFFF"),
                FontAttributes = FontAttributes.Bold,
                FontSize = Math.Clamp(cardHeight * 0.38, 12, 24),
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(6, 0, 0, 0)
            };
            CardsGrid.Add(genLabel, SlotsPerRow, row);
        }
    }

    private View CreateCardElement(MemberCard member, bool isBonus, double? widthOverride = null, double? heightOverride = null)
    {
        var image = CardImageLoader.TryLoad(member);
        if (image is null)
        {
            return CreatePlaceholderCard(member.Generation, member.Member, isBonus, widthOverride, heightOverride);
        }

        var border = new Border
        {
            StrokeThickness = 0,
            BackgroundColor = Colors.Transparent,
            Content = new Image
            {
                Source = image,
                Aspect = Aspect.Fill
            }
        };
        ApplyCardSize(border, isBonus, widthOverride, heightOverride);
        return border;
    }

    private View CreateBlankSlot(double? widthOverride = null, double? heightOverride = null)
    {
        var slot = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Color.FromArgb("#46FFFFFF"),
            Content = new Label
            {
                Text = "▶",
                TextColor = Color.FromArgb("#A0FFFFFF"),
                FontSize = Math.Clamp(cardHeight * 0.22, 10, 16),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            }
        };
        ApplyCardSize(slot, false, widthOverride, heightOverride);
        return slot;
    }

    private View CreatePlaceholderCard(
        string? generation,
        string member,
        bool isBonus,
        double? widthOverride = null,
        double? heightOverride = null)
    {
        var label = string.IsNullOrWhiteSpace(member) ? "?" : member;
        var stack = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center
        };

        if (isBonus && !string.IsNullOrWhiteSpace(generation))
        {
            stack.Children.Add(new Label
            {
                HorizontalOptions = LayoutOptions.Center,
                TextColor = Colors.White,
                FontSize = Math.Clamp(bonusHeight * 0.12, 12, 20),
                FontAttributes = FontAttributes.Bold,
                Text = GenerationLabels.For(generation)
            });
        }

        stack.Children.Add(new Label
        {
            Margin = new Thickness(4),
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap,
            TextColor = Colors.White,
            FontSize = isBonus ? Math.Clamp(bonusHeight * 0.1, 11, 16) : Math.Clamp(cardHeight * 0.18, 8, 12),
            FontAttributes = FontAttributes.Bold,
            Text = label
        });

        var border = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = isBonus ? 14 : 8 },
            StrokeThickness = 2,
            Stroke = Colors.White,
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1),
                GradientStops =
                {
                    new GradientStop(Color.FromRgb(64, 146, 83), 0),
                    new GradientStop(Color.FromRgb(28, 96, 48), 1)
                }
            },
            Content = stack
        };
        ApplyCardSize(border, isBonus, widthOverride, heightOverride);
        return border;
    }

    private void ApplyCardSize(View view, bool isBonus, double? widthOverride, double? heightOverride)
    {
        if (widthOverride is null && heightOverride is null && !isBonus)
        {
            view.HorizontalOptions = LayoutOptions.Fill;
            view.VerticalOptions = LayoutOptions.Fill;
            return;
        }

        view.WidthRequest = widthOverride ?? (isBonus ? bonusWidth : cardWidth);
        view.HeightRequest = heightOverride ?? (isBonus ? bonusHeight : cardHeight);
    }

    private static void AddTap(View view, Action action)
    {
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => action();
        view.GestureRecognizers.Add(tap);
    }
}
