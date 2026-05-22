import { useState, useRef } from "react";

// ── Constants ─────────────────────────────────────────────────────────────────
const ALL_CAMELS = ["blue", "green", "red", "yellow", "purple", "white", "black"];
const NUM_SPACES = 16;

const CAMEL_STYLE = {
  blue:   { bg: "#3B82F6", text: "#fff" },
  green:  { bg: "#22C55E", text: "#fff" },
  red:    { bg: "#EF4444", text: "#fff" },
  yellow: { bg: "#EAB308", text: "#000" },
  purple: { bg: "#A855F7", text: "#fff" },
  white:  { bg: "#F1F5F9", text: "#334155" },
  black:  { bg: "#1E293B", text: "#fff" },
};

// ── Shared styles ─────────────────────────────────────────────────────────────
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
  padding: "9px 20px",
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
  padding: "8px 16px",
  fontSize: 13,
  fontWeight: 600,
  cursor: "pointer",
};

function SectionLabel({ children }) {
  return (
    <div style={{
      fontSize: 11, fontWeight: 700, color: "#b45309",
      letterSpacing: "0.1em", textTransform: "uppercase",
      marginBottom: 10, borderBottom: "1px solid #2d1e0a", paddingBottom: 4,
    }}>
      {children}
    </div>
  );
}

// ── Camel chip (draggable) ────────────────────────────────────────────────────
function CamelToken({ color, draggable = true, onDragStart, small, faded }) {
  const s = CAMEL_STYLE[color];
  const isCrazy = color === "white" || color === "black";

  return (
    <div
      draggable={draggable}
      onDragStart={onDragStart}
      title={`${color}${isCrazy ? " (crazy camel)" : ""}`}
      style={{
        display: "inline-flex",
        alignItems: "center",
        justifyContent: "center",
        background: s.bg,
        color: s.text,
        borderRadius: small ? 4 : 6,
        width: small ? 28 : 52,
        height: small ? 18 : 32,
        fontSize: small ? 9 : 11,
        fontWeight: 800,
        letterSpacing: "0.05em",
        textTransform: "uppercase",
        border: `2px solid rgba(255,255,255,${isCrazy ? "0.5" : "0.2"})`,
        boxShadow: faded
          ? "none"
          : "0 2px 6px rgba(0,0,0,0.5), inset 0 1px 0 rgba(255,255,255,0.15)",
        cursor: draggable ? "grab" : "default",
        opacity: faded ? 0.25 : 1,
        userSelect: "none",
        flexShrink: 0,
        transition: "transform 0.1s, opacity 0.2s",
        outline: isCrazy ? `2px dashed ${s.text === "#fff" ? "rgba(255,255,255,0.4)" : "rgba(0,0,0,0.3)"}` : "none",
        outlineOffset: -3,
      }}
      onMouseEnter={e => { if (draggable) e.currentTarget.style.transform = "scale(1.08)"; }}
      onMouseLeave={e => { e.currentTarget.style.transform = "scale(1)"; }}
    >
      {color.slice(0, 2).toUpperCase()}
    </div>
  );
}

// ── Board space (drop target) ─────────────────────────────────────────────────
function BoardSpace({ index, camels, onDrop, onRemoveCamel, isOver }) {
  return (
    <div
      onDragOver={e => e.preventDefault()}
      onDrop={() => onDrop(index)}
      style={{
        minWidth: 54,
        flex: "0 0 54px",
        minHeight: 90,
        background: isOver
          ? "#2a1f0a"
          : camels.length > 0 ? "#1a1208" : "#120c04",
        border: isOver
          ? "2px dashed #b45309"
          : camels.length > 0
          ? "1.5px solid #4a3018"
          : "1px solid #251a0a",
        borderRadius: 8,
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        padding: "4px 2px 6px",
        transition: "background 0.15s, border 0.15s",
        position: "relative",
      }}
    >
      {/* Space number */}
      <span style={{
        fontSize: 10, color: "#6b4c24", fontWeight: 700,
        marginBottom: 4, letterSpacing: "0.04em",
      }}>
        {index + 1}
      </span>

      {/* Camel stack — bottom to top */}
      <div style={{
        display: "flex", flexDirection: "column-reverse",
        gap: 2, flex: 1, justifyContent: "flex-end", alignItems: "center",
      }}>
        {camels.map((color, stackIdx) => (
          <div
            key={`${color}-${stackIdx}`}
            onClick={() => onRemoveCamel(index, stackIdx)}
            title={`Click to remove ${color}`}
            style={{ cursor: "pointer" }}
          >
            <CamelToken color={color} draggable={false} small />
          </div>
        ))}
      </div>

      {/* Drop hint */}
      {camels.length === 0 && (
        <span style={{ fontSize: 9, color: "#3d2a12", marginTop: "auto" }}>drop</span>
      )}
    </div>
  );
}

// ── Main BoardSetup component ─────────────────────────────────────────────────
export default function BoardSetup({ onConfirm, players, setPlayers }) {
  // spaces[i] = array of camel color strings, bottom→top
  const [spaces, setSpaces] = useState(() =>
    Array.from({ length: NUM_SPACES }, () => [])
  );
  const [overSpace, setOverSpace] = useState(null);
  const [newPlayer, setNewPlayer] = useState("");
  const [error, setError] = useState("");
  const [firstPlayer, setFirstPlayer] = useState(0);
  const dragging = useRef(null); // { color } being dragged from tray

  // Which camels are already placed?
  const placedCamels = new Set(spaces.flat());
  const trayColors = ALL_CAMELS.filter(c => !placedCamels.has(c));

  const handleTrayDragStart = (color) => {
    dragging.current = color;
  };

  const handleDrop = (spaceIndex) => {
    if (!dragging.current) return;
    const color = dragging.current;
    dragging.current = null;
    setOverSpace(null);

    if (placedCamels.has(color)) return;

    const isCrazy = color === "white" || color === "black";

    // Normal camels: spaces 1–3 (indices 0–2)
    // Crazy camels: spaces 14–16 (indices 13–15)
    if (!isCrazy && spaceIndex > 2) {
      setError("Normal camels can only start on spaces 1–3.");
      return;
    }
    if (isCrazy && spaceIndex < 13) {
      setError("Crazy camels (white/black) can only start on spaces 14–16.");
      return;
    }

    setError("");
    setSpaces(prev => {
      const next = prev.map(s => [...s]);
      next[spaceIndex] = [...next[spaceIndex], color];
      return next;
    });
  };

  const handleRemoveCamel = (spaceIndex, stackIdx) => {
    setSpaces(prev => {
      const next = prev.map(s => [...s]);
      next[spaceIndex] = next[spaceIndex].filter((_, i) => i !== stackIdx);
      return next;
    });
  };

  const handleReset = () => {
    setSpaces(Array.from({ length: NUM_SPACES }, () => []));
    setError("");
  };

  const handleQuickPlace = () => {
    const newSpaces = Array.from({ length: NUM_SPACES }, () => []);
    const normalCamels = ["blue", "green", "red", "yellow", "purple"];
    const crazyCamels = ["white", "black"];

    // Normal camels on spaces 1–3 (indices 0–2), stacked
    normalCamels.forEach((color, i) => {
      const spaceIdx = i % 3; // spreads across spaces 0, 1, 2
      newSpaces[spaceIdx] = [...newSpaces[spaceIdx], color];
    });

    // Crazy camels on spaces 14–15 (indices 13–14)
    crazyCamels.forEach((color, i) => {
      newSpaces[13 + i] = [color];
    });

    setSpaces(newSpaces);
    setError("");
  };

  const handleConfirm = () => {
    setError("");
    if (players.length < 2) {
      setError("Add at least 2 players.");
      return;
    }
    const placed = spaces.flat();
    const normalPlaced = placed.filter(c => c !== "white" && c !== "black");
    if (normalPlaced.length < 5) {
      setError("Place all 5 normal camels (blue, green, red, yellow, purple) on the board.");
      return;
    }

    const reordered = [
        ...players.slice(firstPlayer),
        ...players.slice(0, firstPlayer)
    ];

    onConfirm(reordered, spaces);
  };

  const addPlayer = () => {
    if (newPlayer.trim() && players.length < 6) {
      setPlayers([...players, newPlayer.trim()]);
      setNewPlayer("");
    }
  };

  return (
    <div style={{ maxWidth: 980, margin: "0 auto" }}>
      <h2 style={{ color: "#e8c97e", marginBottom: 6, fontSize: 22, fontFamily: "Georgia, serif" }}>
        🐫 New Game Setup
      </h2>
      <p style={{ color: "#6b4c24", fontSize: 13, marginBottom: 28 }}>
        Drag camels onto board spaces to set starting positions. Click a camel on the board to remove it.
      </p>

      {/* ── Players ── */}
      <div style={{ marginBottom: 28 }}>
        <SectionLabel>Players</SectionLabel>
        <div style={{ display: "flex", flexWrap: "wrap", gap: 8, marginBottom: 12 }}>
          {players.map((p, i) => (
            <span key={i} style={{
              background: "#2d1e0a", border: "1.5px solid #4a3018",
              borderRadius: 20, padding: "4px 12px", fontSize: 13,
              color: "#e8c97e", display: "flex", alignItems: "center", gap: 6,
            }}>
              {p}
              <button
                onClick={() => setPlayers(players.filter((_, j) => j !== i))}
                style={{ background: "none", border: "none", color: "#a07040", cursor: "pointer", fontSize: 14, padding: 0 }}
              >×</button>
            </span>
          ))}
        </div>
        {players.length < 6 && (
          <div style={{ display: "flex", gap: 8 }}>
            <input
              value={newPlayer}
              onChange={e => setNewPlayer(e.target.value)}
              onKeyDown={e => e.key === "Enter" && addPlayer()}
              placeholder="Player name"
              style={inputStyle}
            />
            <button onClick={addPlayer} style={secondaryBtn}>Add</button>
          </div>
        )}
      </div>

      {/* ── Who goes first ── */}
      {players.length > 1 && (
        <div style={{ marginBottom: 28 }}>
          <SectionLabel>Who goes first?</SectionLabel>
          <select
            value={firstPlayer}
            onChange={e => setFirstPlayer(+e.target.value)}
            style={inputStyle}
          >
            {players.map((p, i) => (
              <option key={i} value={i}>{p}</option>
            ))}
          </select>
        </div>
      )}

      {/* ── Camel Tray ── */}
      <div style={{ marginBottom: 20 }}>
        <SectionLabel>Camel Tray — drag to place</SectionLabel>
        <div style={{
          display: "flex", gap: 12, flexWrap: "wrap",
          background: "#0f0804", border: "1.5px solid #2d1e0a",
          borderRadius: 10, padding: "14px 16px", minHeight: 60,
          alignItems: "center",
        }}>
          {ALL_CAMELS.map(color => (
            <div
              key={color}
              style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 5 }}
            >
              {placedCamels.has(color) ? (
                <CamelToken color={color} draggable={false} faded />
              ) : (
                <CamelToken
                  color={color}
                  onDragStart={() => handleTrayDragStart(color)}
                />
              )}
              <span style={{
                fontSize: 9, color: placedCamels.has(color) ? "#3d2a12" : "#6b4c24",
                textTransform: "uppercase", letterSpacing: "0.06em", fontWeight: 700,
              }}>
                {color === "white" || color === "black" ? `${color} ★` : color}
              </span>
            </div>
          ))}

          {trayColors.length === 0 && (
            <span style={{ color: "#4a3018", fontSize: 13 }}>All camels placed ✓</span>
          )}
        </div>
      </div>

      {/* ── Board ── */}
      <div style={{ marginBottom: 20 }}>
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 10 }}>
          <SectionLabel>Race Board (spaces 1–16)</SectionLabel>
          <div style={{ display: "flex", gap: 8 }}>
            <button onClick={handleQuickPlace} style={secondaryBtn}>⚡ Quick Place</button>
            <button onClick={handleReset} style={secondaryBtn}>↺ Reset</button>
          </div>
        </div>

        <div style={{
          overflowX: "auto", paddingBottom: 8,
          background: "#0a0603", border: "1px solid #2d1e0a",
          borderRadius: 10, padding: 12,
        }}>
          <div style={{ display: "flex", gap: 5, minWidth: "max-content" }}>
            {spaces.map((camels, i) => (
              <div
                key={i}
                onDragOver={e => { e.preventDefault(); setOverSpace(i); }}
                onDragLeave={() => setOverSpace(null)}
                onDrop={() => handleDrop(i)}
              >
                <BoardSpace
                  index={i}
                  camels={camels}
                  onDrop={handleDrop}
                  onRemoveCamel={handleRemoveCamel}
                  isOver={overSpace === i}
                />
              </div>
            ))}

            {/* Finish zone indicator */}
            <div style={{
              minWidth: 54, flex: "0 0 54px", minHeight: 90,
              background: "linear-gradient(135deg,#1a0e04,#2d1608)",
              border: "2px solid #b45309", borderRadius: 8,
              display: "flex", flexDirection: "column",
              alignItems: "center", justifyContent: "center", gap: 4,
            }}>
              <span style={{ fontSize: 20 }}>🏁</span>
              <span style={{ fontSize: 9, color: "#b45309", fontWeight: 700, letterSpacing: "0.06em" }}>FINISH</span>
            </div>
          </div>
        </div>

        <p style={{ fontSize: 11, color: "#4a3018", marginTop: 8 }}>
          ★ = crazy camel (white/black move backwards) · Click a camel on the board to remove it · Stack order = bottom→top
        </p>
      </div>

      {/* ── Status & confirm ── */}
      <div style={{
        display: "flex", alignItems: "center", gap: 16,
        background: "#0f0804", border: "1px solid #2d1e0a",
        borderRadius: 8, padding: "12px 16px", marginBottom: 20,
      }}>
        <div style={{ flex: 1, display: "flex", gap: 20, flexWrap: "wrap" }}>
          {ALL_CAMELS.map(color => {
            const placed = placedCamels.has(color);
            const s = CAMEL_STYLE[color];
            return (
              <div key={color} style={{ display: "flex", alignItems: "center", gap: 5 }}>
                <div style={{
                  width: 10, height: 10, borderRadius: 2,
                  background: placed ? s.bg : "#2d1e0a",
                  border: `1.5px solid ${placed ? s.bg : "#4a3018"}`,
                  transition: "background 0.2s",
                }} />
                <span style={{
                  fontSize: 11, color: placed ? "#a07040" : "#3d2a12",
                  textTransform: "capitalize", fontWeight: placed ? 600 : 400,
                }}>
                  {color}
                </span>
              </div>
            );
          })}
        </div>
      </div>

      {error && (
        <p style={{ color: "#f87171", fontSize: 13, marginBottom: 12 }}>{error}</p>
      )}

      <button onClick={handleConfirm} style={{ ...primaryBtn, fontSize: 15, padding: "11px 28px" }}>
        Start Game →
      </button>
    </div>
  );
}
