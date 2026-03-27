# Camel Up Strategy Simulator

A C#-based simulation engine for the board game Camel Up (Second Edition), modeling complex game state, probabilistic outcomes, and betting strategies.  

This project implements full gameplay mechanics and integrates a Python-based service to provide AI-assisted move recommendations, demonstrating system design, simulation modeling, and cross-language communication.

---

## Overview

Camel Up is a racing and betting board game where players wager on which camel will win each leg or the overall race.

Because camel movement depends on:
- random dice rolls
- stacking mechanics
- desert tiles
- partial race information

it creates a complex probabilistic system.

This project simulates the full game mechanics to explore strategy and decision making.

---

Technologies Used

- C#
- .NET
- Python (FastAPI)
- Object-Oriented Design
- Simulation Modeling
- REST API Communication

---

## Features

- Full camel movement simulation
- Camel stacking logic
- Dice pyramid randomness
- Leg betting evaluation
- Final winner betting
- Desert tile placement mechanics
- Game state tracking
- Player scoring system

---

## Example Game Flow

1. Initialize camels and board positions
2. Roll dice from the pyramid
3. Move camels according to roll
4. Update camel stack positions
5. Evaluate leg bets
6. Update player coins
7. Repeat until race finishes

---

## Architecture
The project is structured using object-oriented design principles:

- Game.cs → Controls overall game flow, turns, and progression
- Board.cs → Manages camel positions, stacks, and tile effects
- Player.cs → Handles player actions, bets, and scoring
- DicePyramid.cs → Tracks dice rolls and remaining dice per round
- Bet.cs → Represents betting logic and payout structure
- AiClient.cs / PythonAdvisor.cs → Communicates with the Python-based recommendation service

The system separates core game logic from external decision-support components, allowing for extensibility and future improvements.

---

## AI Recommendation System

This project includes a Python-based recommendation service that evaluates the current game state and suggests optimal player actions.

- Communicates with the C# simulator via HTTP
- Processes game state data to generate move recommendations
- Designed as a decision-support system for strategy evaluation

This demonstrates integration between C# and Python systems and introduces AI-assisted decision making into a simulation environment.

---

## Key Mechanics Implemented

### Camel Stacking

Camels stack on top of each other and move together when the bottom camel moves.<br>
Top<br>
C<br>
B<br>
A<br>
Bottom

If camel **A moves**, the entire stack moves.

---

### Dice Pyramid

Each camel has a die that can roll:<br>
1<br>
2<br>
3<br>

Each die is rolled **once per leg**, introducing controlled randomness.

---

### Desert Tiles

Players may place tiles that:

- move camels forward (+1)<br>
- move camels backward (-1)<br>

These also affect stacking behavior.

---

## Example Simulation Output
Leg 1 Results

Blue Camel: 1st<br>
Green Camel: 2nd<br>
Yellow Camel: 3rd<br>
White Camel: 4th<br>
Orange Camel: 5th<br>

Player Scores

Player 1: 8 coins<br>
Player 2: 5 coins<br>
Player 3: 3 coins<br>

---
## How to Run

1. Start the Python AI Server<br>
pip install -r requirements.txt<br>
python ai_server.py<br>
2. Run the C# Simulator<br>
dotnet run<br>


## Future Improvements

- Monte Carlo race simulations
- AI betting strategy evaluation
- Win probability estimation
- Visualization of race states
- Web interface for interactive gameplay


## License

MIT License
