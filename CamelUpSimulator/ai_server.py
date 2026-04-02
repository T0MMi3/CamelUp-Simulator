from fastapi import FastAPI
from pydantic import BaseModel

app = FastAPI()

class GameState(BaseModel):
    spaces: list[list[str]]
    dice_remaining: list[str]
    winner_pile_bets: int
    loser_pile_bets: int
    turn_player: str


@app.get("/")
def read_root():
    return {"message": "CamelUp AI server is running!"}


@app.post("/predict")
def predict_best_move(state: GameState):
    # Very simple placeholder logic — to be improved later
    remaining_dice = len(state.dice_remaining)
    winner_bets = state.winner_pile_bets
    loser_bets = state.loser_pile_bets

    # Heuristic: use pile info to decide tone of suggestion
    if remaining_dice > 3:
        action = "Take a leg bet"
    elif remaining_dice > 0:
        action = "Roll the pyramid"
    else:
        action = "Prepare for next leg"

    # Comment on game stage using final pile info
    if winner_bets + loser_bets > 3:
        note = "Players are committing to final bets — race nearing end!"
    else:
        note = "Early to mid-game stage."

    suggestion = f"[AI] {state.turn_player}, {action}. ({note})"
    return {"suggestion": suggestion}
