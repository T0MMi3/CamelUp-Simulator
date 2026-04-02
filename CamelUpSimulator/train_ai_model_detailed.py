import pandas as pd
import joblib
from sklearn.model_selection import train_test_split
from sklearn.tree import DecisionTreeClassifier
from sklearn.metrics import classification_report

print("📂 Loading training_log.csv ...")

expected_cols = [
    "timestamp", "player", "leg_number", "board_state", "dice",
    "winner_pile_bets", "loser_pile_bets",
    "ai_suggestion", "actual_action", "action_detail"
]

try:
    df = pd.read_csv(
        "training_log.csv",
        quotechar='"',
        skipinitialspace=True,
        engine="python",
        on_bad_lines="skip",
        names=expected_cols,
        header=0
    )
except Exception as e:
    print(f"[ERROR] Could not load CSV: {e}")
    exit(1)

print(f"✅ Loaded {len(df)} rows from training_log.csv\n")

# Drop missing or empty labels
df = df.dropna(subset=["actual_action"])
df["action_detail"] = df["action_detail"].fillna("")

# Combine category + detail into one label
df["label"] = df.apply(
    lambda x: f"{x['actual_action']}:{x['action_detail']}" if x["action_detail"] else x["actual_action"],
    axis=1
)

# Basic numeric features
df["num_camels_on_board"] = df["board_state"].apply(lambda x: str(x).count(","))
df["num_dice_remaining"] = df["dice"].apply(lambda x: 0 if pd.isna(x) else len(str(x).split(",")))
df["winner_bets"] = df["winner_pile_bets"].astype(float)
df["loser_bets"] = df["loser_pile_bets"].astype(float)
df["leg_number"] = df["leg_number"].astype(int)

X = df[["num_camels_on_board", "num_dice_remaining", "winner_bets", "loser_bets", "leg_number"]]
y = df["label"]

print("Action label examples:")
print(df["label"].value_counts().head(10))
print()

X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.25, random_state=42)

model = DecisionTreeClassifier(max_depth=8, random_state=42)
model.fit(X_train, y_train)

y_pred = model.predict(X_test)
print("=== MODEL PERFORMANCE ===")
print(classification_report(y_test, y_pred))

joblib.dump((model, X.columns.tolist()), "camelup_ai_model_detailed.pkl")
print("\n✅ Model saved to camelup_ai_model_detailed.pkl")
print("You can now run 'uvicorn ai_server_detailed:app --reload' to use detailed suggestions.")
