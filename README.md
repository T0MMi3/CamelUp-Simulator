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
CamelUp-Simulator
│
├── Board.cs # board state and tile effects
├── Camel.cs # camel position and stacking
├── DicePyramid.cs # dice randomization
├── Player.cs # player bets and coins
├── LegBet.cs # leg betting mechanics
├── GameEngine.cs # main game logic
└── Program.cs # simulation entry point

---

## Key Mechanics Implemented

### Camel Stacking

Camels stack on top of each other and move together when the bottom camel moves.
Top
C
B
A
Bottom

If camel **A moves**, the entire stack moves.

---

### Dice Pyramid

Each camel has a die that can roll:
1
2
3

Each die is rolled **once per leg**, introducing controlled randomness.

---

### Desert Tiles

Players may place tiles that:

- move camels forward (+1)
- move camels backward (-1)

These also affect stacking behavior.

---

## Example Simulation Output
Leg 1 Results

Blue Camel: 1st
Green Camel: 2nd
Yellow Camel: 3rd
White Camel: 4th
Orange Camel: 5th

Player Scores

Player 1: 8 coins
Player 2: 5 coins
Player 3: 3 coins

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
