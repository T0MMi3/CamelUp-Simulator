from fastapi import FastAPI
from pydantic import BaseModel

app = FastAPI()

class DesertTileState(BaseModel):
    position: int
    type: str
    owner: str

class GameState(BaseModel):
    spaces: list[list[str]]
    dice_remaining: list[str]
    turn_player: str
    camel_order: list[str]
    desert_tiles: list[DesertTileState]
    current_leg: int

@app.get("/")
def read_root():
    return {"message": "CamelUp AI server is running!"}

@app.post("/predict")
def predict_best_move(state: GameState):
    normal_camels = [c for c in state.camel_order if c not in ["white", "black"]]

    if not state.dice_remaining:
        return {"suggestion": "No dice remaining. End the leg."}

    leader = normal_camels[0] if normal_camels else "unknown"

    suggestion = (
        f"Player {state.turn_player}: current leader is {leader}. "
        f"Dice remaining: {', '.join(state.dice_remaining)}. "
        f"Consider a leg bet on {leader} if its top card is worth more than rolling."
    )

    return {
        "suggestion": suggestion,
        "leader": leader,
        "dice_remaining": state.dice_remaining,
        "current_leg": state.current_leg
    }