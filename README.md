# 🐫 Camel Up Simulator

A full-stack turn-by-turn simulator for the board game **Camel Up**, featuring a real-time **Monte Carlo EV (Expected Value) Engine** that calculates the mathematically optimal action for each player on every turn.

**Live Demo:** [camelup-simulator.vercel.app](#) <!-- replace with your live URL -->  
**Portfolio:** [tommy-do.web.app](https://tommy-do.web.app/)  
**Author:** Tommy Do · [GitHub](https://github.com/T0MMi3)

---

## What is Camel Up?

Camel Up is a board game where 5 colored camels race around a 16-space track. Players earn coins by correctly betting on which camel wins each leg (round) and which camel wins or loses the overall race. Crazy camels (white and black) move backwards and can carry normal camels on their backs, creating complex stacking interactions.

Each turn a player chooses one of five actions:
- **Roll** — shake the pyramid to move a random camel (+1 coin guaranteed)
- **Leg Bet** — bet on which camel finishes the current leg in 1st or 2nd place
- **Desert Tile** — place a cheering (+1) or booing (-1) tile to redirect camels
- **Winner Bet** — bet on the overall race winner (hidden pile, pays up to 8 coins)
- **Loser Bet** — bet on the overall race loser (hidden pile, pays up to 8 coins)

---

## Why Monte Carlo Over AI/ML?

Camel Up has **fully observable state and deterministic rules** — every possible outcome can be simulated exactly. This makes Monte Carlo simulation the mathematically correct tool:

- **No training data needed** — works correctly from game 1
- **Explainable** — every recommendation cites exact probabilities and EV
- **Adapts instantly** — recalculates from exact board state every turn
- **More accurate than ML** — ML would introduce approximation error into a problem with an exact solution

An LLM or neural network would be the wrong tool here. Monte Carlo is not a workaround — it is the optimal approach for fully observable stochastic games.

---

## Features

### EV Engine
- **5,000-run leg simulation** — calculates 1st/2nd/last place probabilities for the current leg
- **2,000-run full race simulation** — separate simulation for final bet EVs that runs until a camel crosses the finish line, correctly modeling multi-leg race dynamics
- **Leg bet EV** — ranks all available leg bet cards by expected value accounting for current card values (5, 3, 2, 2) and portfolio composition
- **Desert tile EV** — evaluates every legal tile placement for both cheering and booing, factoring in portfolio EV change and tile hit probability
- **Final bet EV** — uses full race probabilities with dynamic confidence thresholds that scale with game progress, outlier detection for clearly losing camels, and a penalty for holding multiple bets on different colors
- **Portfolio-aware recommendation** — compares all actions simultaneously and recommends the single highest-EV play

### Game Features
- Turn-by-turn simulation matching real Camel Up rules
- Visual drag-and-drop board setup
- Crazy camel (white/black) backwards movement with stack carrying
- Desert tile adjacency and occupation rules enforced
- Leg end scoring with summary modal
- Game over screen with final bet breakdown and payout history
- Undo last move (snapshot/restore pattern)
- Hidden winner/loser piles — only pile counts are visible to other players, colors are private
- Enriched turn log

---

## Tech Stack

| Layer | Technology |
|---|---|
| Frontend | React 19, Vite 8 |
| Backend API | ASP.NET Core 7, C# |
| Game Logic | C# class library (CamelUpSimulator) |
| Database | SQLite via Microsoft.Data.Sqlite |
| Simulation | Custom Monte Carlo engine (no external ML libraries) |
| Deployment | Vercel (frontend) · Railway (API) |

---

## Architecture

```
CamelUp-Simulator/
├── CamelUpSimulator/          # C# game logic library
│   ├── Game.cs                # Core game state and scoring
│   ├── Board.cs               # Board, camel movement, desert tiles
│   ├── Player.cs              # Player actions and bet management
│   ├── ProbabilityEngine.cs   # Monte Carlo simulation engine
│   ├── DicePyramid.cs         # Dice state management
│   └── GameLogger.cs          # SQLite game logging
│
├── CamelUpApi/                # ASP.NET Core REST API
│   ├── Controllers/
│   │   └── GameController.cs  # Game endpoints + EV calculation
│   ├── Services/
│   │   └── GameService.cs     # Game state + undo snapshot management
│   └── Models/
│       └── MoveRequest.cs     # Request models
│
└── camelup-ui/                # React frontend
    ├── src/
    │   ├── App.jsx            # Main app, state management, API calls
    │   └── BoardSetup.jsx     # Drag-and-drop board setup component
    └── vite.config.js
```

### Key Design Decisions

**Simulation separation** — leg probabilities and race probabilities are computed by separate simulations. Leg simulation stops after all current dice are rolled. Race simulation runs full multi-leg games until a camel crosses space 16. Using leg probabilities for final bets was a subtle bug that produced overconfident early-game recommendations.

**Snapshot undo** — before every move, `GameService` serializes camel positions, player points, held bets, dice state, and leg bet deck state into a `GameSnapshot`. Undo restores this snapshot in O(1) without maintaining a full history stack.

**Hidden information** — winner and loser pile colors are private per real game rules. The state endpoint only exposes pile counts (public) and the current player's own bets. The EV engine uses pessimistic payout tier estimation (assumes all existing pile cards are correct) since colors are unknown.

**Dynamic confidence thresholds** — final bet recommendations require higher confidence early in the game (≥60% probability in leg 1) scaling down to ≥35% late game. An outlier factor further lowers the threshold for camels that are significantly worse than the field average.

---

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | `/game/setup` | Initialize game with players and starting board |
| GET | `/game/state` | Full game state (hidden-information aware) |
| GET | `/game/ev` | Monte Carlo EV analysis for current player |
| POST | `/game/move` | Apply a move (roll/legbet/deserttile/winnerbet/loserbet) |
| POST | `/game/undo` | Restore previous game state |
| POST | `/game/finalize` | Score final bets and return game results |

---

## Running Locally

**Prerequisites:** .NET 7 SDK, Node.js 18+

```bash
# Clone the repo
git clone https://github.com/T0MMi3/CamelUp-Simulator.git
cd CamelUp-Simulator

# Start the API
cd CamelUpApi
dotnet run
# API runs on http://localhost:5012

# Start the frontend (new terminal)
cd ../camelup-ui
npm install
npm run dev
# UI runs on http://localhost:5173
```

---

## EV Engine Deep Dive

The core insight is that Camel Up's outcome space is small enough to simulate exhaustively each turn. With 5 dice each rolling 1-3 and a board of 16 spaces, a single leg has at most 3^5 × 5! = 29,160 possible outcomes. Monte Carlo samples this space efficiently without enumerating it.

**Leg simulation pseudocode:**
```
for each simulation:
    clone current board state
    shuffle remaining dice randomly
    for each die:
        roll 1-3 randomly
        move camel (applying desert tile effects)
        if race finished: break
    record 1st, 2nd, last place camel
compute probabilities from counts
```

**EV formula for a leg bet card:**
```
EV = P(1st) × card_value + P(2nd) × 1 + P(other) × (-1)
```

**Final bet EV with confidence gating:**
```
if race_last_probability >= dynamic_threshold:
    EV = P(last) × payout_tier - existing_bets_penalty
else:
    EV = -∞  (not recommended)
```

Where `payout_tier` is determined pessimistically by total cards already in the pile (8 → 5 → 3 → 2 → 1).

---

## Contact

**Tommy Do**  
[tommy-do.web.app](https://tommy-do.web.app/) · [GitHub](https://github.com/T0MMi3)