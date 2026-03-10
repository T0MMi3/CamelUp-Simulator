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
