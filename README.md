# Pokajan Randomizer

Windows app that picks **4 Hololive gens** and **1 bonus member** for a Pokajan game.

**New Game** rolls a round. **Info** shows the physical shuffle steps. The exact cards-to-remove number appears in Info after you press New Game.

## If you just want to play

You do not need the source code. Download only these two things and put them in the same folder:

- `PokajanRandomizer.exe`
- the `assets` folder (the whole folder, with `memberCards` still inside it)

```
some-folder/
  PokajanRandomizer.exe
  assets/
    memberCards/
```

On GitHub: open the repo → download `PokajanRandomizer.exe` and the `assets` folder (use **Code → Download ZIP** if that’s easier, then delete everything else). Double-click the exe. Cards will be blank if `assets` is missing or not next to the exe.

Gen1 and Gamers never land in the same round (Fubuki is in both).

---

## Update log

### v1 — base

First version of the randomizer.

- Pick 4 generations at random from `members.json`.
- Gen1 and Gamers are exclusive.
- Bonus member is one person from those 4 gens.
- Each character has 9 cards (3 pink, 3 blue, 3 orange).
- After you pull the 4 gens, shuffle, then remove extra cards until the deck is **100**.
- Deal 7 cards to each person.

Original Info steps:

1. Take out the 4 gen cards you got (each character has 9 cards: 3 pink, 3 blue, 3 orange).
2. Shuffle them.
3. Take out N cards so the deck is 100. `N = (members × 9) − 100`
4. Deal 7 cards to each person.

### v2 — hotfixes

- Card name + asset renames:
  - Robocco → Robocosan
  - Calli → Calliope
  - Ina → Inanis
  - Irys → IRyS
  - Biboo → Bijou
- Info rule 1: check who the bonus card is, shuffle that character’s 9 cards first, and take one out as the bonus card.
- Info rule 2: shuffle the remaining cards.
- Cards to remove is now **N − 1**, because that bonus card is already out. `N = (members × 9) − 1 − 100`

### v3 — coins and Pokajan

- App locks to fullscreen. Four seats around the board: Player 1 (down), Player 2 (right), Player 3 (up), Player 4 (left).
- Each seat starts at **1000** coins. Tap the pen to rename. New Game resets coins to 1000.
- **New Game** sits under the gen + bonus board.
- After a round, **Pokajan!** opens a claim: pick 3–5 cards from the 4 gens, then orange / blue / pink. Tap a filled slot to pick again.
- Valid hands: all the same member, or one full generation.
- Payout (same color is a higher rate, not extra on top of the base):
  - Triple: 120 / same color 840
  - 3-card gen: 180 / same color 480
  - 4-card gen: 300 / same color 840
  - 5-card gen: 480 / same color 1800
  - Bonus member in the claim: **+90 per card** (1 → +90, 3 → +270)
- Then choose **Self pulled** (the other 3 split the payout) or **Discarded** (pick who pays the full amount). A +/- page shows the coin change.
