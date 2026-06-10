# L-System Generator 🌿

[🚀 Live Demo / WebGL Build](https://sinemdonmezerdemir.github.io/Fractal-L-System-Web/)

An **L-System (Lindenmayer System)** generator built with **Unity** and **C#**. It produces recursive fractals and plant-like geometries through parallel string rewriting and renders them with a custom turtle-graphics rasterizer based on direct pixel manipulation.

---

## 🚀 Features

- **Custom rendering engine** — Bypasses Unity's `LineRenderer` in favour of a highly optimized, `NativeArray`-based pixel rasterizer using Bresenham's line algorithm (integer-only, no floating-point division).
- **LOD pruning** — Sub-trees that would render smaller than a single pixel are collapsed before drawing, keeping even very high iteration counts fast and responsive.
- **Mathematical precision** — Aspect ratio is preserved through bounding-box fitting, with dynamic resolution mapping tied to the UI container size.
- **Modern UI Toolkit** — Built entirely with Unity's UI Toolkit (HTML/CSS-like architecture) instead of legacy uGUI. Fully responsive, mobile-friendly, with Dark/Light theme switching.
- **WebGL ready** — Configured for browser-based deployment with platform-aware export handling.
- **Animation & export** — Step-by-step visual animation of the recursive generation process, plus direct-to-disk `.png` export.

---

## 🧬 The Mathematics

Lindenmayer systems are **parallel rewriting systems**. The generator starts from an initial state (the **axiom**) and, at each iteration, applies the **production rules** to every symbol *simultaneously*, expanding the string exponentially.

The resulting string is interpreted as turtle-graphics commands: `F` / `G` draw forward, `f` moves without drawing, `+` / `-` rotate by the turn angle, and `[` / `]` push and pop the turtle state for branching.

**Example — Sierpinski Triangle:**

| Parameter | Value |
| --- | --- |
| Axiom | `F+G+G` |
| Rules | `F → F+G-F-G+F`, `G → GG` |
| Angle | `120°` |

---

## 🛠️ Architecture

The project strictly adheres to **separation of concerns**:

| Layer | Class | Responsibility |
| --- | --- | --- |
| **Model** | `LSystemData` | `ScriptableObject` defining the parameters and rules |
| **Logic** | `LSystemGenerator` | Static engine handling string expansion and geometry interpretation |
| **View** | `LSystemRenderer` | Low-level pixel manipulation and texture generation |
| **Controller** | `FractalStudioUIToolkit` | User input, UI bindings, and dynamic resolution syncing |

---

## 💻 Usage

1. Select a preset from the **Preset Management** dropdown (e.g. Koch Snowflake, Cantor, Sierpinski).
2. Tweak the **Axiom**, **Angle**, and **Iterations**.
3. Add or remove **production rules** with the `+` button.
4. Click **Generate Fractal** for instant computation, or **Animate Step-by-Step** for visualization.
5. Export your high-resolution result with the **Export PNG** button.

---

## 📄 License

This project is open-source. Feel free to fork, modify, and contribute.
