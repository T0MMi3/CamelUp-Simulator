import pandas as pd
import re
import joblib
from sklearn.model_selection import train_test_split
from sklearn.tree import DecisionTreeClassifier
from sklearn.metrics import classification_report

# -----------------------------------------
# 1. Load data safely, forgiving bad commas
# -----------------------------------------
rows = []
with open("training_log.csv", "r", encoding="utf-8") as f:
    for line in f:
        # Split only the first 3 commas reliably: timestamp, player, leg_number
        parts = line.strip().split(",", 3)
        if len(parts) < 4:
            continue
        timestamp, player, leg_number, rest = parts

        # The rest contains board_state,dice,winner,loser,ai_suggestion,actual_action
        # Let's match the *last 5 commas* (since AI suggestion may have commas too)
        m = re.match(r"(.+),([^,]+),([^,]+),([^,]+),\"?(.*?)\"?,([^,]+)$", rest)
        if not m:
            continue
        board_state, dice, winner_pile_bets, loser_pile_bets, ai_suggestion, actual_action = m.groups()

        rows.append({
            "timestamp": timestamp,
            "player": player,
            "leg_number": leg_number,
            "board_state": board_state,
            "dice": dice,
            "winner_pile_bets": winner_pile_bets,
            "loser_pile_bets": loser_pile_bets,
            "ai_suggestion": ai_suggestion,
            "actual_action": actual_action
        })

df = pd.DataFrame(rows)
print(f"Parsed {len(df)} rows cleanly from training_log.csv")
print(df.head(3))
print()

# -----------------------------------------
# 2. Basic cleaning and validation
# -----------------------------------------
# Drop any rows missing an action label
df = df.dropna(subset=["actual_action"])

# Keep only known action labels
valid_actions = ["Leg Bet", "Desert Tile", "Pyramid Roll", "Final Bet"]
df = df[df["actual_action"].isin(valid_actions)]

# -----------------------------------------
# 3. Basic feature engineering
# -----------------------------------------
df["num_camels_on_board"] = df["board_state"].apply(lambda x: str(x).count(","))
df["num_dice_remaining"] = df["dice"].apply(lambda x: 0 if pd.isna(x) else len(str(x).split(",")))
df["winner_bets"] = df["winner_pile_bets"].astype(float)
df["loser_bets"] = df["loser_pile_bets"].astype(float)
df["leg_number"] = df["leg_number"].astype(int)

# Map actions to numeric classes
label_map = {
    "Leg Bet": 0,
    "Desert Tile": 1,
    "Pyramid Roll": 2,
    "Final Bet": 3
}
df["action_id"] = df["actual_action"].map(label_map)

# Select features + labels
X = df[["num_camels_on_board", "num_dice_remaining", "winner_bets", "loser_bets", "leg_number"]]
y = df["action_id"]

print(f"Training on {len(X)} examples with {X.shape[1]} features")
print()

# -----------------------------------------
# 4. Train/test split + model training
# -----------------------------------------
X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.25, random_state=42
)

model = DecisionTreeClassifier(max_depth=6, random_state=42)
model.fit(X_train, y_train)

# Evaluate model
y_pred = model.predict(X_test)
print("=== MODEL PERFORMANCE ===")
print(classification_report(y_test, y_pred, target_names=label_map.keys()))

# -----------------------------------------
# 5. Save model to disk
# -----------------------------------------
joblib.dump((model, label_map, X.columns.tolist()), "camelup_ai_model.pkl")
print("\nModel saved successfully to camelup_ai_model.pkl")
print(f"Columns used for training: {list(X.columns)}")
print("You can now run 'uvicorn ai_server:app --reload' to start using it.")
