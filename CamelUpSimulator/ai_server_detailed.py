from fastapi import FastAPI
import joblib
import pandas as pd

app = FastAPI()

model, feature_names = joblib.load("camelup_ai_model_detailed.pkl")

@app.get("/")
def home():
    return {"message": "CamelUp Detailed AI server running"}

@app.post("/predict")
def predict(state: dict):
    # Expect the same numeric features the game already sends
    features = pd.DataFrame([[
        state.get("num_camels_on_board", 0),
        state.get("num_dice_remaining", 0),
        state.get("winner_bets", 0),
        state.get("loser_bets", 0),
        state.get("leg_number", 1)
    ]], columns=feature_names)

    label = model.predict(features)[0]
    suggestion = f"[AI] {state.get('turn_player', 'Player')}, {label}"
    return {"suggestion": suggestion}
