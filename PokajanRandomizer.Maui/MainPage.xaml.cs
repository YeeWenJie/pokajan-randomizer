namespace PokajanRandomizer.Maui;

public partial class MainPage : ContentPage
{
    private const int SlotsPerRow = 5;
    private const int ClaimSlotCount = 5;

    private static readonly Color OrangeColor = Color.FromRgb(240, 138, 42);
    private static readonly Color BlueColor = Color.FromRgb(61, 126, 255);
    private static readonly Color PinkColor = Color.FromRgb(242, 107, 160);

    private readonly MemberData memberData;
    private readonly IDispatcherTimer infoHintTimer;
    private readonly SeatState[] seats =
    {
        new(0, "Player 1"),
        new(1, "Player 2"),
        new(2, "Player 3"),
        new(3, "Player 4")
    };
    private readonly Button[] pokajanButtons = new Button[4];
    private readonly Label[] coinLabels = new Label[4];
    private readonly SlotDraft[] claimSlots = Enumerable.Range(0, ClaimSlotCount).Select(_ => new SlotDraft()).ToArray();

    private RoundResult? currentRound;
    private SeatState? claimWinner;
    private PayoutResult? pendingPayout;
    private int pickerSlotIndex = -1;
    private int? cardsToRemove;

    private double SmallCardWidth => CompactLayout ? 46 : 62;
    private double SmallCardHeight => CompactLayout ? 62 : 84;
    private double BonusCardWidth => CompactLayout ? 108 : 140;
    private double BonusCardHeight => CompactLayout ? 144 : 188;
    private double ClaimCardWidth => CompactLayout ? 54 : 72;
    private double ClaimCardHeight => CompactLayout ? 72 : 96;

    private static bool CompactLayout
    {
        get
        {
            var info = DeviceDisplay.MainDisplayInfo;
            var dipWidth = Math.Max(info.Width, info.Height) / info.Density;
            return dipWidth < 1000;
        }
    }

    public MainPage()
    {
        InitializeComponent();

        memberData = RoundPicker.LoadData();
        BuildEmptyRows();
        BuildSeats();
        SetPokajanEnabled(false);

        infoHintTimer = Dispatcher.CreateTimer();
        infoHintTimer.Interval = TimeSpan.FromSeconds(5);
        infoHintTimer.Tick += InfoHintTimer_OnTick;

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
        if (HintSettings.InfoHintShown)
        {
            return;
        }

        infoHintTimer.Start();
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
        SeatHost1.Content = CreateSeatPanel(seats[0]);
        SeatHost2.Content = CreateSeatPanel(seats[1]);
        SeatHost3.Content = CreateSeatPanel(seats[2]);
        SeatHost4.Content = CreateSeatPanel(seats[3]);
    }

    private View CreateSeatPanel(SeatState seat)
    {
        var nameLabel = new Label
        {
            TextColor = Colors.White,
            FontSize = CompactLayout ? 16 : 20,
            FontAttributes = FontAttributes.Bold,
            VerticalOptions = LayoutOptions.Center,
            Text = seat.DisplayName
        };

        var nameBox = new Entry
        {
            WidthRequest = 120,
            FontSize = 16,
            IsVisible = false,
            Text = seat.DisplayName,
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#33000000")
        };

        var penButton = new Button
        {
            Text = "✎",
            WidthRequest = 32,
            HeightRequest = 32,
            Padding = 0,
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.White,
            FontSize = 16
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
            Margin = new Thickness(0, 4, 0, 6),
            HorizontalOptions = LayoutOptions.Center,
            TextColor = Colors.White,
            FontSize = CompactLayout ? 22 : 28,
            FontAttributes = FontAttributes.Bold,
            Text = seat.Coins.ToString()
        };
        coinLabels[seat.Id] = coins;

        var pokajan = new Button
        {
            Text = "Pokajan!",
            WidthRequest = CompactLayout ? 120 : 140,
            HeightRequest = 38,
            BackgroundColor = Color.FromRgb(232, 255, 240),
            TextColor = Color.FromRgb(20, 87, 38),
            FontSize = 14
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

        return new VerticalStackLayout
        {
            MinimumWidthRequest = CompactLayout ? 130 : 160,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new HorizontalStackLayout
                {
                    HorizontalOptions = LayoutOptions.Center,
                    Spacing = 4,
                    Children = { nameLabel, nameBox, penButton }
                },
                coins,
                pokajan
            }
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
            Margin = new Thickness(6, 0),
            WidthRequest = ClaimCardWidth + 12,
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
        return new HorizontalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 6, 0, 0),
            Spacing = 6,
            Children =
            {
                CreateColorChip(slot, CardColor.Orange, OrangeColor),
                CreateColorChip(slot, CardColor.Blue, BlueColor),
                CreateColorChip(slot, CardColor.Pink, PinkColor)
            }
        };
    }

    private View CreateColorChip(SlotDraft slot, CardColor color, Color brush)
    {
        var selected = slot.Color == color;
        var chip = new Border
        {
            WidthRequest = 22,
            HeightRequest = 22,
            BackgroundColor = brush,
            Stroke = selected ? Colors.White : Colors.Transparent,
            StrokeThickness = selected ? 3 : 1,
            StrokeShape = new RoundRectangle { CornerRadius = 4 }
        };
        AddTap(chip, () =>
        {
            slot.Color = color;
            ClaimErrorText.Text = string.Empty;
            RefreshClaimSlots();
        });
        return chip;
    }

    private void OpenCardPicker(int index)
    {
        if (currentRound is null)
        {
            return;
        }

        pickerSlotIndex = index;
        CardPickerHost.Children.Clear();
        foreach (var member in currentRound.Rows.SelectMany(row => row.Members))
        {
            var card = CreateCardElement(member, false, ClaimCardWidth, ClaimCardHeight);
            card.Margin = new Thickness(6);
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

    private void HideCardPicker()
    {
        CardPickerOverlay.IsVisible = false;
        pickerSlotIndex = -1;
    }

    private void CardPickerOverlay_OnTapped(object? sender, TappedEventArgs e)
    {
        HideCardPicker();
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
            ClaimErrorText.Text = "Need 3+ of the same member, or one full generation.";
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
                WidthRequest = 200,
                HeightRequest = 44,
                Margin = new Thickness(0, 0, 0, 10),
                BackgroundColor = Color.FromRgb(232, 255, 240),
                TextColor = Color.FromRgb(20, 87, 38),
                FontSize = 16
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
        RefreshCoinLabels();
        ClaimDeltaHost.Children.Clear();
        foreach (var delta in deltas)
        {
            var sign = delta.Change > 0 ? "+" : string.Empty;
            ClaimDeltaHost.Children.Add(new Label
            {
                Margin = new Thickness(0, 4),
                TextColor = Colors.White,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                Text = $"{delta.Seat.DisplayName}:  {delta.OldCoins}  →  {sign}{delta.Change}  →  {delta.NewCoins}"
            });
        }

        ShowClaimPage(ClaimDeltaPage);
    }

    private void ClaimDeltaDone_OnClick(object? sender, EventArgs e)
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

        BonusCardHost.Content = CreatePlaceholderCard(null, "Bonus", true);
    }

    private void RenderRound(RoundResult round)
    {
        currentRound = round;
        RowsHost.Children.Clear();
        foreach (var row in round.Rows)
        {
            RowsHost.Children.Add(CreateRowShell(row.Label, row.Generation, row.Members));
        }

        BonusCardHost.Content = CreateCardElement(round.BonusMember, true);
        cardsToRemove = round.CardsToRemove;
    }

    private View CreateRowShell(string label, string generation, IReadOnlyList<MemberCard>? members = null)
    {
        var cardsPanel = new HorizontalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Spacing = 0
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
        var cardsColumn = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                cardsPanel,
                new BoxView
                {
                    Margin = new Thickness(4, 4, 0, 0),
                    HeightRequest = 2,
                    WidthRequest = lineWidth,
                    HorizontalOptions = LayoutOptions.Start,
                    Color = Color.FromArgb("#5AFFFFFF")
                }
            }
        };

        return new HorizontalStackLayout
        {
            Margin = new Thickness(0, 2),
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                cardsColumn,
                new Label
                {
                    Margin = new Thickness(8, 0, 0, 6),
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = Color.FromArgb("#BEFFFFFF"),
                    FontSize = CompactLayout ? 20 : 26,
                    FontAttributes = FontAttributes.Bold,
                    Text = string.IsNullOrWhiteSpace(generation) ? string.Empty : label
                }
            }
        };
    }

    private View CreateCardElement(MemberCard member, bool isBonus, double? widthOverride = null, double? heightOverride = null)
    {
        var image = CardImageLoader.TryLoad(member);
        if (image is null)
        {
            return CreatePlaceholderCard(member.Generation, member.Member, isBonus, widthOverride, heightOverride);
        }

        var width = widthOverride ?? (isBonus ? BonusCardWidth : SmallCardWidth);
        var height = heightOverride ?? (isBonus ? BonusCardHeight : SmallCardHeight);
        return new Border
        {
            WidthRequest = width,
            HeightRequest = height,
            Margin = isBonus ? 0 : new Thickness(3, 0),
            StrokeThickness = 0,
            BackgroundColor = Colors.Transparent,
            Content = new Image
            {
                Source = image,
                Aspect = Aspect.Fill
            }
        };
    }

    private static View CreateBlankSlot(double width, double height)
    {
        return new Border
        {
            WidthRequest = width,
            HeightRequest = height,
            Margin = new Thickness(3, 0),
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            BackgroundColor = Color.FromArgb("#46FFFFFF"),
            Content = new Label
            {
                Text = "▶",
                TextColor = Color.FromArgb("#A0FFFFFF"),
                FontSize = 16,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            }
        };
    }

    private View CreatePlaceholderCard(
        string? generation,
        string member,
        bool isBonus,
        double? widthOverride = null,
        double? heightOverride = null)
    {
        var width = widthOverride ?? (isBonus ? BonusCardWidth : SmallCardWidth);
        var height = heightOverride ?? (isBonus ? BonusCardHeight : SmallCardHeight);
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
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                Text = GenerationLabels.For(generation)
            });
        }

        stack.Children.Add(new Label
        {
            Margin = new Thickness(6),
            HorizontalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap,
            TextColor = Colors.White,
            FontSize = isBonus ? 16 : 10,
            FontAttributes = FontAttributes.Bold,
            Text = label
        });

        return new Border
        {
            WidthRequest = width,
            HeightRequest = height,
            StrokeShape = new RoundRectangle { CornerRadius = isBonus ? 16 : 10 },
            Margin = isBonus ? 0 : new Thickness(3, 0),
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
    }

    private static void AddTap(View view, Action action)
    {
        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => action();
        view.GestureRecognizers.Add(tap);
    }
}
