# Camel Up Simulator

A C# simulation engine for the board game **Camel Up (Second Edition)** that models camel movement, stacking mechanics, dice randomness, and betting strategies.  
The simulator allows players or AI models to analyze game states and evaluate optimal betting decisions.

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
CamelUp-Simulator<br>
│<br>
├── Board.cs # board state and tile effects<br>
├── Camel.cs # camel position and stacking<br>
├── DicePyramid.cs # dice randomization<br>
├── Player.cs # player bets and coins<br>
├── LegBet.cs # leg betting mechanics<br>
├── GameEngine.cs # main game logic<br>
└── Program.cs # simulation entry point<br>

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

## Technologies Used

- C#
- Object-Oriented Design
- Simulation Modeling
- Probability Analysis

---

## Future Improvements

- Monte Carlo race simulations
- AI betting strategy evaluation
- Win probability estimation
- Visualization of race states
- Web interface for interactive gameplay

---

## Why This Project

This project explores how **simulation and probabilistic modeling** can be used to analyze decision making in uncertain environments.

It also demonstrates object-oriented system design for modeling complex rule-based systems.

---

## License

MIT License
