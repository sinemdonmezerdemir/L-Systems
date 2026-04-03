# L-System Generator 🌿

[🚀 Live Demo / WebGL Build](https://sinemdonmezerdemir.github.io/Fractal-L-System-Web/)

An L-System (Lindenmayer System) generator built with Unity and C#. It generates recursive fractals and plant geometries through string rewriting and renders them via a custom turtle graphics rasterizer with direct pixel manipulation.

## 🚀 Features

* **Custom Rendering Engine:** Bypasses Unity's `LineRenderer` for a highly optimized, `NativeArray`-based direct pixel rasterization using Bresenham's line algorithm.
* **Mathematical Precision:** Preserves aspect ratio through bounding-box fitting, with dynamic resolution mapping tied to the UI container size.
* **Modern UI Toolkit:** Built completely using Unity's modern UI Toolkit (HTML/CSS-like architecture) avoiding outdated uGUI paradigms. Features a fully responsive, mobile-friendly design with Dark/Light theme switching.
* **WebGL Ready:** Fully configured for browser-based deployment with platform-aware export handling.
* **Animation & Export:** Step-by-step visual animation of the recursive generation process, and direct-to-disk `.png` exporting capabilities.

## 🧬 The Mathematics

Lindenmayer Systems are parallel rewriting systems. The generator starts with an initial state (Axiom) and recursively applies production rules to expand the string. 

For example, generating the **Sierpinski Triangle**:
* **Axiom:** `F-G-G`
* **Rules:** `F -> F-G+F+G-F`, `G -> GG`
* **Angle:** `120°`

## 🛠️ Architecture

The project strictly adheres to the separation of concerns:
1. **`LSystemData` (Model):** `ScriptableObject` defining the parameters and rules.
2. **`LSystemGenerator` (Logic):** Static engine handling the string expansion and geometry interpretation.
3. **`LSystemRenderer` (View):** Component handling the low-level pixel manipulation and texture generation.
4. **`FractalStudioUIToolkit` (Controller):** Handles user input, UI bindings, and dynamic resolution syncing.

## 💻 Usage

1. Select a preset from the **Preset Management** dropdown (e.g., Koch Snowflake, Cantor, Sierpinski).
2. Tweak the **Axiom**, **Angle**, and **Iterations**. 
3. Add or remove **Production Rules** using the `+` button.
4. Click **Generate Fractal** for instant computation, or **Animate Step-by-Step** for visualization.
5. Export your high-resolution result via the **Export PNG** button.

## 📄 License

This project is open-source. Feel free to fork, modify, and contribute.

https://github.com/user-attachments/assets/5d44ca76-eac4-4659-9fdc-f2edeb41e8b8

