import { useState, useEffect, useCallback } from "react";
import BoardSetup from "./BoardSetup";

// ── Config ────────────────────────────────────────────────────────────────────
const API_BASE = "http://localhost:5012"; // point to your .NET API

const CAMEL_COLORS = {
  blue:   { bg: "#3B82F6", text: "#fff", emoji: "🐫" },
  green:  { bg: "#22C55E", text: "#fff", emoji: "🐫" },
  red:    { bg: "#EF4444", text: "#fff", emoji: "🐫" },
  yellow: { bg: "#EAB308", text: "#000", emoji: "🐫" },
  purple: { bg: "#A855F7", text: "#fff", emoji: "🐫" },
  white:  { bg: "#F8FAFC", text: "#334155", emoji: "🐪" },
  black:  { bg: "#1E293B", text: "#fff", emoji: "🐪" },
};

const TILE_COLORS = { cheering: "#22C55E", booing: "#EF4444" };

// ── Helpers ───────────────────────────────────────────────────────────────────
async function apiFetch(path, options = {}) {
  const res = await fetch(`${API_BASE}${path}`, {
    headers: { "Content-Type": "application/json" },
    ...options,
  });
  const text = await res.text();
  let data;
  try { data = JSON.parse(text); } catch { data = text; }
  if (!res.ok) throw new Error(typeof data === "string" ? data : data?.title || data?.message || "API error");
  return data;
}

function CamelChip({ color, small }) {
  const c = CAMEL_COLORS[color] || { bg: "#888", text: "#fff" };
  return (
    <span
      style={{
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        background: c.bg,
        color: c.text,
        borderRadius: small ? 4 : 6,
        padding: small ? "1px 6px" : "3px 10px",
        fontSize: small ? 11 : 13,
        fontWeight: 700,
        letterSpacing: "0.03em",
        border: "1.5px solid rgba(0,0,0,0.15)",
        boxShadow: "0 1px 3px rgba(0,0,0,0.18)",
        textTransform: "capitalize",
        whiteSpace: "nowrap",
        flexShrink: 0,
      }}
    >
      {color}
    </span>
  );
}

function EVBar({ value, max = 3 }) {
  const pct = Math.max(0, Math.min(100, ((value + max) / (2 * max)) * 100));
  const col = value > 0.5 ? "#22C55E" : value > 0 ? "#EAB308" : "#EF4444";
  return (
    <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
      <div
        style={{
          flex: 1,
          height: 8,
          background: "#2d2016",
          borderRadius: 4,
          overflow: "hidden",
        }}
      >
        <div
          style={{
            width: `${pct}%`,
            height: "100%",
            background: col,
            borderRadius: 4,
            transition: "width 0.4s ease",
          }}
        />
      </div>
      <span
        style={{
          fontSize: 12,
          fontWeight: 700,
          color: col,
          minWidth: 42,
          textAlign: "right",
        }}
      >
        {value > 0 ? "+" : ""}{value.toFixed(2)}
      </span>
    </div>
  );
}

// ── Board ─────────────────────────────────────────────────────────────────────
function BoardRow({ spaces, tiles, numSpaces }) {
  return (
    <div style={{ display: "flex", gap: 4, overflowX: "auto", paddingBottom: 8 }}>
      {Array.from({ length: numSpaces }, (_, i) => {
        const camelsHere = spaces.filter((c) => c.position === i);
        const tile = tiles.find((t) => t.position === i);
        const isFinish = i === numSpaces - 1;

        return (
          <div
            key={i}
            style={{
              minWidth: 60,
              flex: "0 0 60px",
              minHeight: 80,
              background: isFinish
                ? "linear-gradient(135deg,#b45309,#92400e)"
                : tile
                ? `${TILE_COLORS[tile.type]}22`
                : "#1c1208",
              border: isFinish
                ? "2px solid #b45309"
                : tile
                ? `2px solid ${TILE_COLORS[tile.type]}`
                : "1px solid #3d2a12",
              borderRadius: 8,
              display: "flex",
              flexDirection: "column",
              alignItems: "center",
              padding: "4px 2px",
              position: "relative",
              transition: "background 0.3s",
            }}
          >
            {/* Space label */}
            <span
              style={{
                fontSize: 10,
                color: "#a07040",
                fontWeight: 700,
                marginBottom: 2,
              }}
            >
              {isFinish ? "🏁" : i + 1}
            </span>

            {/* Camel stack (bottom → top) */}
            <div
              style={{
                display: "flex",
                flexDirection: "column-reverse",
                gap: 1,
                flex: 1,
                justifyContent: "flex-end",
              }}
            >
              {[...camelsHere]
                .sort((a, b) => a.stackHeight - b.stackHeight)
                .map((c) => {
                  const col = CAMEL_COLORS[c.color] || { bg: "#888", text: "#fff" };
                  return (
                    <div
                      key={c.color}
                      title={c.color}
                      style={{
                        width: 28,
                        height: 16,
                        background: col.bg,
                        borderRadius: 3,
                        border: "1.5px solid rgba(255,255,255,0.25)",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                        fontSize: 9,
                        fontWeight: 800,
                        color: col.text,
                        textTransform: "uppercase",
                        letterSpacing: "0.04em",
                        boxShadow: "0 1px 2px rgba(0,0,0,0.4)",
                      }}
                    >
                      {c.color.slice(0, 2)}
                    </div>
                  );
                })}
            </div>

            {/* Desert tile indicator */}
            {tile && (
              <div
                style={{
                  position: "absolute",
                  bottom: 2,
                  right: 2,
                  fontSize: 11,
                  title: `${tile.owner}'s ${tile.type} tile`,
                }}
              >
                {tile.type === "cheering" ? "👍" : "👎"}
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
}

// ── EV Panel ──────────────────────────────────────────────────────────────────
function EVPanel({ evData, loading }) {
  if (loading)
    return (
      <div style={{ color: "#a07040", padding: 20, textAlign: "center" }}>
        <span style={{ fontSize: 24 }}>⏳</span>
        <p style={{ marginTop: 8 }}>Running Monte Carlo simulation…</p>
      </div>
    );

  if (!evData) return null;

  return (
    <div>
      {/* Recommendation banner */}
      <div
        style={{
          background: "linear-gradient(135deg,#1a3a1a,#0f2a0f)",
          border: "1.5px solid #22C55E",
          borderRadius: 10,
          padding: "12px 16px",
          marginBottom: 20,
        }}
      >
        <div style={{ fontSize: 11, color: "#22C55E", fontWeight: 700, marginBottom: 4 }}>
          ▶ BEST ACTION ({evData.simulationsRun.toLocaleString()} simulations)
        </div>
        <div style={{ fontSize: 15, color: "#d1fae5", fontWeight: 600 }}>
          {evData.recommendation}
        </div>
      </div>

      {/* Camel odds */}
      <div style={{ marginBottom: 20 }}>
        <SectionLabel>Leg Win Probabilities</SectionLabel>
        <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
          {evData.camelProbabilities.map((c) => (
            <div key={c.color} style={{ display: "flex", alignItems: "center", gap: 10 }}>
              <CamelChip color={c.color} small />
              <div style={{ flex: 1 }}>
                <div
                  style={{ display: "flex", justifyContent: "space-between", marginBottom: 2 }}
                >
                  <span style={{ fontSize: 11, color: "#a07040" }}>1st {c.winPct}%</span>
                  <span style={{ fontSize: 11, color: "#6b4c24" }}>2nd {c.secondPct}%</span>
                </div>
                <div
                  style={{
                    height: 6,
                    background: "#2d2016",
                    borderRadius: 3,
                    overflow: "hidden",
                  }}
                >
                  <div
                    style={{
                      width: `${c.winPct}%`,
                      height: "100%",
                      background: CAMEL_COLORS[c.color]?.bg || "#888",
                      borderRadius: 3,
                      transition: "width 0.4s",
                    }}
                  />
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* Leg bet EVs */}
      <div style={{ marginBottom: 20 }}>
        <SectionLabel>Leg Bet Expected Values</SectionLabel>
        <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
          {evData.legBetOptions.map((opt) => (
            <div key={opt.color} style={{ display: "flex", alignItems: "center", gap: 10 }}>
              <CamelChip color={opt.color} small />
              <div style={{ flex: 1 }}>
                <EVBar value={opt.cardEV} />
              </div>
            </div>
          ))}
          {evData.legBetOptions.length === 0 && (
            <span style={{ color: "#6b4c24", fontSize: 13 }}>No leg bets available</span>
          )}
        </div>
      </div>

      {/* Roll EV */}
      <div style={{ marginBottom: 20 }}>
        <SectionLabel>Roll (Pyramid Ticket)</SectionLabel>
        <div style={{ display: "flex", alignItems: "center", gap: 10 }}>
          <span style={{ fontSize: 22 }}>🎲</span>
          <div style={{ flex: 1 }}>
            <EVBar value={evData.rollEV} />
          </div>
          <span style={{ fontSize: 11, color: "#a07040" }}>+1 guaranteed</span>
        </div>
      </div>

      {/* Desert tile recs */}
      {evData.desertTileOptions.length > 0 && (
        <div>
          <SectionLabel>Desert Tile Placements (Top 5)</SectionLabel>
          <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            {evData.desertTileOptions.map((t, i) => (
              <div
                key={i}
                style={{
                  display: "flex",
                  alignItems: "center",
                  gap: 10,
                  background: "#1c1208",
                  borderRadius: 6,
                  padding: "6px 10px",
                }}
              >
                <span style={{ fontSize: 16 }}>
                  {t.type === "cheering" ? "👍" : "👎"}
                </span>
                <span style={{ fontSize: 13, color: "#e8c97e", minWidth: 80 }}>
                  Space {t.positionLabel} · {t.type}
                </span>
                <div style={{ flex: 1 }}>
                  <EVBar value={t.evGain} />
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {evData.finalBetOptions?.length > 0 && (
        <div style={{ marginTop: 20 }}>
          <SectionLabel>Final Bet EVs</SectionLabel>
          <div style={{ display: "flex", flexDirection: "column", gap: 6 }}>
            {evData.finalBetOptions.map((opt) => (
              <div key={opt.color} style={{
                display: "flex", alignItems: "center", gap: 10,
                background: "#1c1208", borderRadius: 6, padding: "6px 10px",
              }}>
                <CamelChip color={opt.color} small />
                <div style={{ flex: 1 }}>
                  <div style={{ display: "flex", justifyContent: "space-between", marginBottom: 3 }}>
                    <span style={{ fontSize: 11, color: "#a07040" }}>
                      🏆 Winner (pays {opt.winnerPayout})
                    </span>
                    <span style={{
                        fontSize: 12, fontWeight: 700,
                        color: opt.winnerEV > 0 ? "#22C55E" : opt.winnerEV === null ? "#4a3018" : "#EF4444"
                    }}>
                        {opt.winnerEV === null ? "low conf." : `${opt.winnerEV > 0 ? "+" : ""}${opt.winnerEV.toFixed(2)}`}
                    </span>
                  </div>
                  <div style={{ display: "flex", justifyContent: "space-between" }}>
                    <span style={{ fontSize: 11, color: "#a07040" }}>
                      😩 Loser (pays {opt.loserPayout})
                    </span>
                    <span style={{
                      fontSize: 12, fontWeight: 700,
                      color: opt.loserEV > 0 ? "#22C55E" : opt.loserEV === null ? "#4a3018" : "#EF4444"
                    }}>
                      {opt.loserEV === null ? "low conf." : `${opt.loserEV > 0 ? "+" : ""}${opt.loserEV.toFixed(2)}`}
                    </span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}

// ── Move Panel ────────────────────────────────────────────────────────────────
function MovePanel({ gameState, onMove, loading }) {
  const [action, setAction] = useState("legbet");
  const [color, setColor] = useState("");
  const [roll, setRoll] = useState(1);
  const [betColor, setBetColor] = useState("");
  const [tilePos, setTilePos] = useState(1);
  const [tileType, setTileType] = useState("cheering");
  const [error, setError] = useState("");
  const [crazyCamel, setCrazyCamel] = useState("white");

  if (!gameState) return null;

  const remainingDice = gameState.diceRemaining || [];
  const camelColors = (gameState.camels || [])
    .filter((c) => c.color !== "white" && c.color !== "black")
    .map((c) => c.color);

  const availableLegBetColors = action === "legbet"
    ? Object.entries(gameState.legBetDecks || {})
        .filter(([_, cards]) => cards.length > 0)
        .map(([color]) => color)
    : camelColors;

  const currentPlayerData = (gameState.players || [])
    .find(p => p.name === gameState.currentPlayer);

  const currentPlayerUsedColors = (currentPlayerData?.finalBets || [])
    .map(b => b.color);

  const availableWinnerColors = camelColors.filter(c => !currentPlayerUsedColors.includes(c));
  const availableLoserColors = camelColors.filter(c => !currentPlayerUsedColors.includes(c));

  const submit = async () => {
    setError("");
    try {
      const body = { action };

      if (action === "roll") {
        if (!color) { setError("Please select a camel color."); return; }
        body.color = color;
        body.roll = roll;
        if (color === "grey") body.crazyCamel = crazyCamel;
      }

      if (action === "legbet") {
        if (!betColor) { setError("Please select a camel color."); return; }
        body.betColor = betColor;
      }

      if (action === "deserttile") {
        body.tilePosition = tilePos;
        body.tileType = tileType;
      }

      if (action === "winnerbet" || action === "loserbet") {
        if (!betColor) { setError("Please select a camel color."); return; }
        body.betColor = betColor;
      }

      await onMove(body);
    } catch (e) {
      setError(e.message);
    }
  };

  return (
    <div>
      <SectionLabel>Make a Move</SectionLabel>

      {/* Action selector */}
      <div style={{ display: "flex", gap: 6, marginBottom: 6 }}>
        {["legbet", "roll"].map((a) => (
          <button key={a} onClick={() => setAction(a)} style={action === a ? primaryBtn : secondaryBtn}>
            {a === "legbet" ? "🎴 Leg Bet" : "🎲 Roll"}
          </button>
        ))}
      </div>
      <div style={{ display: "flex", gap: 6, marginBottom: 6 }}>
        {["winnerbet", "loserbet"].map((a) => (
          <button key={a} onClick={() => setAction(a)} style={action === a ? primaryBtn : secondaryBtn}>
            {a === "winnerbet" ? "🏆 Winner" : "😩 Loser"}
          </button>
        ))}
      </div>
      <div style={{ display: "flex", gap: 6, marginBottom: 16 }}>
        <button onClick={() => setAction("deserttile")} style={action === "deserttile" ? primaryBtn : secondaryBtn}>
          🏜 Desert Tile
        </button>
      </div>

      {/* Fields */}
      {action === "roll" && (
        <div style={{ display: "flex", gap: 10, flexWrap: "wrap", marginBottom: 12 }}>
          <div>
            <label style={labelStyle}>Camel Color</label>
            <select
              value={color}
              onChange={(e) => setColor(e.target.value)}
              style={inputStyle}
            >
              <option value="">— pick —</option>
              {remainingDice.map((d) => (
                <option key={d} value={d}>{d}</option>
              ))}
            </select>
          </div>
          <div>
            <label style={labelStyle}>Roll (1–3)</label>
            <select value={roll} onChange={(e) => setRoll(+e.target.value)} style={inputStyle}>
              <option value={1}>1</option>
              <option value={2}>2</option>
              <option value={3}>3</option>
            </select>
          </div>
          {color === "grey" && (
            <div>
              <label style={labelStyle}>Which Crazy Camel?</label>
              <select value={crazyCamel} onChange={(e) => setCrazyCamel(e.target.value)} style={inputStyle}>
                <option value="white">White</option>
                <option value="black">Black</option>
              </select>
            </div>
          )}
        </div>
      )}

      {(action === "legbet" || action === "winnerbet" || action === "loserbet") && (
        <div style={{ marginBottom: 12 }}>
          <label style={labelStyle}>Camel Color</label>
          <select value={betColor} onChange={(e) => setBetColor(e.target.value)} style={inputStyle}>
            <option value="">— pick —</option>
            {(action === "legbet"
              ? availableLegBetColors
              : action === "winnerbet"
              ? availableWinnerColors
              : availableLoserColors
            ).map((c) => (
              <option key={c} value={c}>{c}</option>
            ))}
          </select>
        </div>
      )}

      {action === "deserttile" && (
        <div style={{ display: "flex", gap: 10, flexWrap: "wrap", marginBottom: 12 }}>
          <div>
            <label style={labelStyle}>Space (1–16)</label>
            <input
              type="number"
              min={1}
              max={16}
              value={tilePos}
              onChange={(e) => setTilePos(+e.target.value)}
              style={{ ...inputStyle, width: 70 }}
            />
          </div>
          <div>
            <label style={labelStyle}>Type</label>
            <select value={tileType} onChange={(e) => setTileType(e.target.value)} style={inputStyle}>
              <option value="cheering">👍 Cheering (+1)</option>
              <option value="booing">👎 Booing (−1)</option>
            </select>
          </div>
        </div>
      )}

      {error && <p style={{ color: "#f87171", fontSize: 13, marginBottom: 10 }}>{error}</p>}

      <button onClick={submit} disabled={loading} style={primaryBtn}>
        {loading ? "Applying…" : "Apply Move"}
      </button>
    </div>
  );
}

// ── Player Scoreboard ─────────────────────────────────────────────────────────
function Scoreboard({ players, currentPlayer }) {
  if (!players?.length) return null;

  return (
    <div>
      <SectionLabel>Scoreboard</SectionLabel>
      <div style={{ display: "flex", flexDirection: "column", gap: 8 }}>
        {[...players]
          .sort((a, b) => b.points - a.points)
          .map((p) => (
            <div
              key={p.name}
              style={{
                display: "flex",
                alignItems: "center",
                gap: 10,
                background: p.name === currentPlayer ? "#1a2e10" : "#1c1208",
                border: `1.5px solid ${p.name === currentPlayer ? "#22C55E" : "#3d2a12"}`,
                borderRadius: 8,
                padding: "8px 12px",
              }}
            >
              <span style={{ fontSize: 18 }}>{p.name === currentPlayer ? "▶" : " "}</span>
              <span style={{ flex: 1, fontWeight: 600, color: "#e8c97e" }}>{p.name}</span>
              <span
                style={{
                  background: "#b45309",
                  color: "#fff",
                  borderRadius: 5,
                  padding: "2px 8px",
                  fontSize: 13,
                  fontWeight: 700,
                }}
              >
                🪙 {p.points}
              </span>
              <div style={{ display: "flex", gap: 4, flexWrap: "wrap" }}>
                {p.legBets.map((b, i) => (
                  <CamelChip key={i} color={b.color} small />
                ))}
                {p.winnerBetCount > 0 && (
                  <span style={{
                    background: "#1a3a1a", border: "1px solid #22C55E",
                    borderRadius: 4, padding: "1px 6px", fontSize: 11,
                    color: "#22C55E", fontWeight: 700
                  }}>
                    🏆 {p.winnerBetCount}
                  </span>
                )}
                {p.loserBetCount > 0 && (
                  <span style={{
                    background: "#2a0f0f", border: "1px solid #EF4444",
                    borderRadius: 4, padding: "1px 6px", fontSize: 11,
                    color: "#EF4444", fontWeight: 700
                  }}>
                    😩 {p.loserBetCount}
                  </span>
                )}
              </div>
            </div>
          ))}
      </div>
    </div>
  );
}

// ── Dice Remaining ────────────────────────────────────────────────────────────
function DiceRow({ dice }) {
  if (!dice) return null;
  return (
    <div style={{ display: "flex", gap: 6, flexWrap: "wrap", marginBottom: 16 }}>
      {dice.map((d) => {
        const col = d === "grey" ? { bg: "#6B7280", text: "#fff" } : CAMEL_COLORS[d] || { bg: "#888", text: "#fff" };
        return (
          <div
            key={d}
            title={d}
            style={{
              width: 32,
              height: 32,
              borderRadius: 6,
              background: col.bg,
              color: col.text,
              display: "flex",
              alignItems: "center",
              justifyContent: "center",
              fontSize: 10,
              fontWeight: 800,
              textTransform: "uppercase",
              border: "2px solid rgba(255,255,255,0.2)",
              boxShadow: "0 2px 4px rgba(0,0,0,0.4)",
            }}
          >
            {d.slice(0, 2)}
          </div>
        );
      })}
      {!dice.length && (
        <span style={{ color: "#6b4c24", fontSize: 13 }}>All dice rolled — leg ending</span>
      )}
    </div>
  );
}

// ── Shared Styles ─────────────────────────────────────────────────────────────
const inputStyle = {
  background: "#1c1208",
  border: "1.5px solid #4a3018",
  borderRadius: 6,
  color: "#e8c97e",
  padding: "6px 10px",
  fontSize: 13,
  outline: "none",
};

const primaryBtn = {
  background: "linear-gradient(135deg,#b45309,#92400e)",
  color: "#fff",
  border: "none",
  borderRadius: 7,
  padding: "9px 18px",
  fontSize: 13,
  fontWeight: 700,
  cursor: "pointer",
  letterSpacing: "0.04em",
};

const secondaryBtn = {
  background: "#2d1e0a",
  color: "#a07040",
  border: "1.5px solid #4a3018",
  borderRadius: 7,
  padding: "8px 14px",
  fontSize: 13,
  fontWeight: 600,
  cursor: "pointer",
};

const tagStyle = {
  background: "#2d1e0a",
  border: "1.5px solid #4a3018",
  borderRadius: 20,
  padding: "4px 12px",
  fontSize: 13,
  color: "#e8c97e",
  display: "flex",
  alignItems: "center",
};

const btnInline = {
  background: "none",
  border: "none",
  color: "#a07040",
  cursor: "pointer",
  fontSize: 14,
  padding: 0,
};

const labelStyle = {
  display: "block",
  fontSize: 11,
  color: "#a07040",
  marginBottom: 4,
  fontWeight: 600,
  letterSpacing: "0.05em",
  textTransform: "uppercase",
};

function Section({ title, children }) {
  return (
    <div style={{ marginBottom: 20 }}>
      {title && <SectionLabel>{title}</SectionLabel>}
      {children}
    </div>
  );
}

function SectionLabel({ children }) {
  return (
    <div
      style={{
        fontSize: 11,
        fontWeight: 700,
        color: "#b45309",
        letterSpacing: "0.1em",
        textTransform: "uppercase",
        marginBottom: 10,
        borderBottom: "1px solid #2d1e0a",
        paddingBottom: 4,
      }}
    >
      {children}
    </div>
  );
}

// ── Main App ──────────────────────────────────────────────────────────────────
export default function App() {
  const [phase, setPhase] = useState("setup"); // "setup" | "playing"
  const [gameState, setGameState] = useState(null);
  const [players, setPlayers] = useState(["Tommy"]);
  const [evData, setEvData] = useState(null);
  const [loadingEV, setLoadingEV] = useState(false);
  const [loadingMove, setLoadingMove] = useState(false);
  const [log, setLog] = useState([]);
  const [tab, setTab] = useState("board"); // "board" | "ev" | "log"
  const [turnKey, setTurnKey] = useState(0);
  const [finalResult, setFinalResult] = useState(null);
  const [legSummary, setLegSummary] = useState(null);

  const fetchState = useCallback(async () => {
    const state = await apiFetch("/game/state");
    setGameState(state);
    return state;
  }, []);

  const fetchEV = useCallback(async () => {
    setLoadingEV(true);
    try {
      const ev = await apiFetch("/game/ev?simulations=5000");
      setEvData(ev);
    } catch (e) {
      console.error("EV fetch failed:", e);
    } finally {
      setLoadingEV(false);
    }
  }, []);

  const handleSetup = async (players, spaces) => {
    await apiFetch("/game/setup", {
      method: "POST",
      body: JSON.stringify({ players, spaces }),
    });
    const state = await fetchState();
    setLog([`Game started. First player: ${state.currentPlayer}`]);
    setPhase("playing");
    fetchEV();
  };

  const handleMove = async (moveBody) => {
    setLoadingMove(true);
    try {
      const result = await apiFetch("/game/move", {
        method: "POST",
        body: JSON.stringify(moveBody),
      });
      setLog((prev) => [`${result.player} → ${result.logMessage}`, ...prev.slice(0, 49)]);
      await fetchState();
      fetchEV();
      setTurnKey(k => k + 1);

      if (result.legEnded) {
        setLegSummary({ order: result.legCamelOrder, results: result.legSummary });
      }

      if (result.isRaceFinished) {
        const finalResult = await apiFetch("/game/finalize", { method: "POST" });
        setFinalResult(finalResult);
        setPhase("finished");
      }
    } finally {
      setLoadingMove(false);
    }
  };

  const NUM_SPACES = 17;

  return (
    <div
      style={{
        minHeight: "100vh",
        background: "#0d0903",
        color: "#c8a060",
        fontFamily: "'Georgia', serif",
      }}
    >
      {/* Header */}
      <div
        style={{
          background: "linear-gradient(135deg,#1a0f05,#2d1e0a)",
          borderBottom: "2px solid #4a3018",
          padding: "16px 24px",
          display: "flex",
          alignItems: "center",
          gap: 16,
        }}
      >
        <div style={{ fontSize: 28 }}>🐫</div>
        <div>
          <div
            style={{
              fontSize: 22,
              fontWeight: 700,
              color: "#e8c97e",
              letterSpacing: "0.05em",
            }}
          >
            CAMEL UP SIMULATOR
          </div>
          <div style={{ fontSize: 12, color: "#6b4c24", letterSpacing: "0.1em" }}>
            MONTE CARLO EV ENGINE
          </div>
        </div>
        {phase === "playing" && gameState && (
          <div
            style={{
              marginLeft: "auto",
              background: "#1a3a1a",
              border: "1.5px solid #22C55E",
              borderRadius: 8,
              padding: "8px 14px",
              fontSize: 13,
              color: "#d1fae5",
            }}
          >
            <span style={{ color: "#22C55E", fontWeight: 700 }}>▶ </span>
            {gameState.currentPlayer}'s turn — Leg {gameState.currentLeg}
          </div>
        )}
        {phase !== "setup" && (
          <button
            onClick={() => { setPhase("setup"); setGameState(null); setEvData(null); }}
            style={{ ...secondaryBtn, marginLeft: phase === "setup" ? "auto" : 0 }}
          >
            New Game
          </button>
        )}
      </div>

      {/* Setup */}
      {phase === "setup" && (
        <div style={{ padding: 32 }}>
          <BoardSetup
            onConfirm={handleSetup}
            players={players}
            setPlayers={setPlayers}
          />
        </div>
      )}

      {/* Finished */}
      {phase === "finished" && finalResult && (
        <div style={{ padding: 32, maxWidth: 700, margin: "0 auto" }}>
          <div style={{ textAlign: "center", marginBottom: 32 }}>
            <div style={{ fontSize: 56, marginBottom: 8 }}>🏁</div>
            <h2 style={{ color: "#e8c97e", fontSize: 32, margin: 0 }}>Race Over!</h2>
            <p style={{ color: "#6b4c24", fontSize: 14, marginTop: 8 }}>
              Winner camel: <CamelChip color={finalResult.winnerCamel} /> &nbsp;
              Loser camel: <CamelChip color={finalResult.loserCamel} />
            </p>
            <p style={{ color: "#6b4c24", fontSize: 13 }}>
              Final order: {finalResult.finalCamelOrder.map((c, i) => (
                <span key={c}><CamelChip color={c} small />{i < finalResult.finalCamelOrder.length - 1 ? " → " : ""}</span>
              ))}
            </p>
          </div>

          {/* Final standings */}
          <div style={{ marginBottom: 24 }}>
            <SectionLabel>Final Standings</SectionLabel>
            {finalResult.finalStandings.map((p) => (
              <div key={p.name} style={{
                display: "flex", alignItems: "center", gap: 12,
                background: p.rank === 1 ? "#1a3a1a" : "#1c1208",
                border: `1.5px solid ${p.rank === 1 ? "#22C55E" : "#3d2a12"}`,
                borderRadius: 8, padding: "10px 16px", marginBottom: 8,
              }}>
                <span style={{ fontSize: 20, minWidth: 32 }}>
                  {p.rank === 1 ? "🥇" : p.rank === 2 ? "🥈" : p.rank === 3 ? "🥉" : `#${p.rank}`}
                </span>
                <span style={{ flex: 1, fontWeight: 600, color: "#e8c97e", fontSize: 16 }}>{p.name}</span>
                <span style={{
                  background: "#b45309", color: "#fff",
                  borderRadius: 6, padding: "3px 12px", fontWeight: 700, fontSize: 15,
                }}>🪙 {p.points}</span>
              </div>
            ))}
          </div>

          {/* Bet breakdown */}
          <div style={{ marginBottom: 32 }}>
            <SectionLabel>Final Bet Breakdown</SectionLabel>
            {finalResult.betBreakdown.map((b, i) => (
              <div key={i} style={{
                display: "flex", alignItems: "center", gap: 10,
                background: b.correct ? "#0f2a0f" : "#2a0f0f",
                border: `1px solid ${b.correct ? "#22C55E" : "#EF4444"}`,
                borderRadius: 6, padding: "7px 12px", marginBottom: 6,
              }}>
                <span style={{ fontSize: 14 }}>{b.correct ? "✅" : "❌"}</span>
                <span style={{ fontWeight: 600, color: "#e8c97e", minWidth: 80 }}>{b.player}</span>
                <span style={{ color: "#a07040", fontSize: 13 }}>{b.pile} pile</span>
                <CamelChip color={b.color} small />
                <span style={{ marginLeft: "auto", fontWeight: 700,
                  color: b.correct ? "#22C55E" : "#EF4444" }}>
                  {b.payout > 0 ? `+${b.payout}` : b.payout} 🪙
                </span>
              </div>
            ))}
          </div>

          <button onClick={() => { setPhase("setup"); setGameState(null); setEvData(null); setFinalResult(null); }}
            style={{ ...primaryBtn, fontSize: 15, padding: "11px 28px" }}>
            Play Again
          </button>
        </div>
      )}

      {/* Playing */}
      {phase === "playing" && gameState && (
        <div
          style={{
            display: "grid",
            gridTemplateColumns: "1fr 340px",
            gap: 0,
            minHeight: "calc(100vh - 72px)",
          }}
        >
          {/* Main area */}
          <div style={{ padding: 24, borderRight: "1px solid #2d1e0a" }}>
            {/* Tab bar */}
            <div
              style={{
                display: "flex",
                gap: 4,
                marginBottom: 20,
                borderBottom: "1px solid #2d1e0a",
                paddingBottom: 0,
              }}
            >
              {[
                { id: "board", label: "🗺 Board" },
                { id: "ev", label: "📊 EV Analysis" },
                { id: "log", label: "📋 Log" },
              ].map((t) => (
                <button
                  key={t.id}
                  onClick={() => setTab(t.id)}
                  style={{
                    background: tab === t.id ? "#2d1e0a" : "none",
                    border: "none",
                    borderBottom: tab === t.id ? "2px solid #b45309" : "2px solid transparent",
                    color: tab === t.id ? "#e8c97e" : "#6b4c24",
                    padding: "8px 16px",
                    fontSize: 13,
                    fontWeight: 600,
                    cursor: "pointer",
                    letterSpacing: "0.04em",
                  }}
                >
                  {t.label}
                </button>
              ))}
            </div>

            {tab === "board" && (
              <div>
                <SectionLabel>Dice Remaining</SectionLabel>
                <DiceRow dice={gameState.diceRemaining} />
                <SectionLabel>Race Board</SectionLabel>
                <BoardRow
                  spaces={gameState.camels || []}
                  tiles={gameState.desertTiles || []}
                  numSpaces={NUM_SPACES}
                />
                <div style={{ marginTop: 24 }}>
                  <Scoreboard
                    players={gameState.players}
                    currentPlayer={gameState.currentPlayer}
                  />
                </div>
              </div>
            )}

            {tab === "ev" && (
              <EVPanel evData={evData} loading={loadingEV} />
            )}

            {tab === "log" && (
              <div>
                <SectionLabel>Turn Log</SectionLabel>
                {log.length === 0 && (
                  <span style={{ color: "#4a3018", fontSize: 13 }}>No moves yet.</span>
                )}
                <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
                  {log.map((entry, i) => (
                    <div
                      key={i}
                      style={{
                        fontSize: 13,
                        color: i === 0 ? "#e8c97e" : "#6b4c24",
                        padding: "4px 0",
                        borderBottom: "1px solid #1c1208",
                      }}
                    >
                      {log.length - i}. {entry}
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* Right sidebar */}
          <div
            style={{
              padding: 24,
              background: "#0f0704",
              display: "flex",
              flexDirection: "column",
              gap: 24,
            }}
          >
            {/* EV quick badge */}
            {evData && !loadingEV && (
              <div
                style={{
                  background: "#1a1005",
                  border: "1px solid #2d1e0a",
                  borderRadius: 8,
                  padding: "10px 14px",
                  cursor: "pointer",
                }}
                onClick={() => setTab("ev")}
              >
                <div style={{ fontSize: 10, color: "#6b4c24", marginBottom: 4, letterSpacing: "0.08em" }}>
                  TOP ACTION
                </div>
                <div style={{ fontSize: 12, color: "#22C55E", fontWeight: 600 }}>
                  {evData.recommendation.slice(0, 55)}{evData.recommendation.length > 55 ? "…" : ""}
                </div>
              </div>
            )}

            <MovePanel
              key={turnKey}
              gameState={gameState}
              onMove={handleMove}
              loading={loadingMove}
            />

            <div style={{ marginTop: "auto" }}>
              {gameState?.canUndo && (
                <button
                  onClick={async () => {
                    await apiFetch("/game/undo", { method: "POST" });
                    await fetchState();
                    fetchEV();
                    setTurnKey(k => k + 1);
                  }}
                  style={{ ...secondaryBtn, width: "100%", marginBottom: 8, borderColor: "#EF4444", color: "#EF4444" }}
                >
                  ↩ Undo Last Move
                </button>
              )}
              <button
                onClick={fetchEV}
                disabled={loadingEV}
                style={{ ...secondaryBtn, width: "100%" }}
              >
                {loadingEV ? "⏳ Simulating…" : "🔄 Refresh EV Analysis"}
              </button>
            </div>
          </div>
        </div>
      )}
    {legSummary && (
        <div style={{
          position: "fixed", inset: 0, background: "rgba(0,0,0,0.75)",
          display: "flex", alignItems: "center", justifyContent: "center",
          zIndex: 1000,
        }}>
          <div style={{
            background: "#1a0f05", border: "2px solid #b45309",
            borderRadius: 12, padding: 32, maxWidth: 500, width: "90%",
            maxHeight: "80vh", overflowY: "auto",
          }}>
            <div style={{ textAlign: "center", marginBottom: 20 }}>
              <div style={{ fontSize: 32, marginBottom: 8 }}>🏕️</div>
              <h3 style={{ color: "#e8c97e", margin: 0, fontSize: 20 }}>Leg Complete!</h3>
              <p style={{ color: "#6b4c24", fontSize: 13, marginTop: 6 }}>
                Final order: {legSummary.order.map((c, i) => (
                  <span key={c}>
                    <CamelChip color={c} small />
                    {i < legSummary.order.length - 1 ? " → " : ""}
                  </span>
                ))}
              </p>
            </div>

            {legSummary.results.map((player) => (
              <div key={player.playerName} style={{
                background: "#120c04", border: "1px solid #3d2a12",
                borderRadius: 8, padding: "10px 14px", marginBottom: 10,
              }}>
                <div style={{
                  fontWeight: 700, color: "#e8c97e", fontSize: 14, marginBottom: 8,
                  display: "flex", justifyContent: "space-between"
                }}>
                  <span>{player.playerName}</span>
                  <span style={{ color: "#a07040", fontSize: 12 }}>
                    🎲 {player.pyramidTickets} ticket{player.pyramidTickets !== 1 ? "s" : ""}
                  </span>
                </div>

                {player.bets.length === 0 ? (
                  <span style={{ color: "#4a3018", fontSize: 13 }}>No leg bets placed</span>
                ) : (
                  player.bets.map((bet, i) => (
                    <div key={i} style={{
                      display: "flex", alignItems: "center", gap: 10,
                      background: bet.payout > 0 ? "#0f2a0f" : "#2a0f0f",
                      border: `1px solid ${bet.payout > 0 ? "#22C55E" : "#EF4444"}`,
                      borderRadius: 6, padding: "5px 10px", marginBottom: 4,
                    }}>
                      <CamelChip color={bet.color} small />
                      <span style={{ fontSize: 12, color: "#a07040" }}>
                        finished {bet.position}{bet.position === 1 ? "st" : bet.position === 2 ? "nd" : bet.position === 3 ? "rd" : "th"}
                      </span>
                      <span style={{
                        marginLeft: "auto", fontWeight: 700, fontSize: 13,
                        color: bet.payout > 0 ? "#22C55E" : "#EF4444"
                      }}>
                        {bet.payout > 0 ? `+${bet.payout}` : bet.payout} 🪙
                      </span>
                    </div>
                  ))
                )}
              </div>
            ))}

            <button
              onClick={() => setLegSummary(null)}
              style={{ ...primaryBtn, width: "100%", marginTop: 8 }}
            >
              Continue to Leg {legSummary.results.length > 0 ? "→" : ""}
            </button>
          </div>
        </div>
      )}
      {legSummary && (
      <div style={{
        position: "fixed", inset: 0, background: "rgba(0,0,0,0.75)",
        display: "flex", alignItems: "center", justifyContent: "center",
        zIndex: 1000,
      }}>
        <div style={{
          background: "#1a0f05", border: "2px solid #b45309",
          borderRadius: 12, padding: 32, maxWidth: 500, width: "90%",
          maxHeight: "80vh", overflowY: "auto",
        }}>
          <div style={{ textAlign: "center", marginBottom: 20 }}>
            <div style={{ fontSize: 32, marginBottom: 8 }}>🏕️</div>
            <h3 style={{ color: "#e8c97e", margin: 0, fontSize: 20 }}>Leg Complete!</h3>
            <p style={{ color: "#6b4c24", fontSize: 13, marginTop: 6 }}>
              Final order: {legSummary.order.map((c, i) => (
                <span key={c}>
                  <CamelChip color={c} small />
                  {i < legSummary.order.length - 1 ? " → " : ""}
                </span>
              ))}
            </p>
          </div>

          {legSummary.results.map((player) => (
            <div key={player.playerName} style={{
              background: "#120c04", border: "1px solid #3d2a12",
              borderRadius: 8, padding: "10px 14px", marginBottom: 10,
            }}>
              <div style={{
                fontWeight: 700, color: "#e8c97e", fontSize: 14, marginBottom: 8,
                display: "flex", justifyContent: "space-between"
              }}>
                <span>{player.playerName}</span>
                <span style={{ color: "#a07040", fontSize: 12 }}>
                  🎲 {player.pyramidTickets} ticket{player.pyramidTickets !== 1 ? "s" : ""}
                </span>
              </div>

              {player.bets.length === 0 ? (
                <span style={{ color: "#4a3018", fontSize: 13 }}>No leg bets placed</span>
              ) : (
                player.bets.map((bet, i) => (
                  <div key={i} style={{
                    display: "flex", alignItems: "center", gap: 10,
                    background: bet.payout > 0 ? "#0f2a0f" : "#2a0f0f",
                    border: `1px solid ${bet.payout > 0 ? "#22C55E" : "#EF4444"}`,
                    borderRadius: 6, padding: "5px 10px", marginBottom: 4,
                  }}>
                    <CamelChip color={bet.color} small />
                    <span style={{ fontSize: 12, color: "#a07040" }}>
                      finished {bet.position}{bet.position === 1 ? "st" : bet.position === 2 ? "nd" : bet.position === 3 ? "rd" : "th"}
                    </span>
                    <span style={{
                      marginLeft: "auto", fontWeight: 700, fontSize: 13,
                      color: bet.payout > 0 ? "#22C55E" : "#EF4444"
                    }}>
                      {bet.payout > 0 ? `+${bet.payout}` : bet.payout} 🪙
                    </span>
                  </div>
                ))
              )}
            </div>
          ))}

          <button
            onClick={() => setLegSummary(null)}
            style={{ ...primaryBtn, width: "100%", marginTop: 8 }}
          >
            Continue to Leg {legSummary.results.length > 0 ? "→" : ""}
          </button>
        </div>
      </div>
    )}
    </div>
  );
}
