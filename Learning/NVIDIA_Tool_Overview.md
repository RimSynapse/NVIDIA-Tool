# NVIDIA Tool Overview

RimSynapse runs a language model in the background while you play. This mod makes that visible: what the GPU is doing, where VRAM is going, and how long requests are taking.

---

## What it shows you

**Live GPU statistics** — VRAM in use, temperature, utilisation percentage and power draw, refreshed on a configurable interval (two seconds by default).

**A VRAM breakdown** — rather than one total, an estimate split into model weights, KV cache, context window and overhead. A model that "fits" until you raise the context window is the usual reason a setup stops working, and this is where you see that coming.

**VRAM warnings** — a heads-up when you are approaching your card's limit, with suggestions about model size and context window.

**Request metrics** — every LLM request RimSynapse makes: response time, token counts, throughput, and how deep the queue is. This is what tells you whether a stall is the model being slow or the queue being long, which are different problems.

---

## Where to find it

**The on-screen overlay** is a compact heads-up display. It is **off by default** — toggle it from the toolbar button or in mod settings.

**The developer tools window** is the full view, with tabs for GPU statistics, request history, VRAM analysis and configuration. Open it from the same toolbar button.

---

## How it reads the GPU

Two paths, and knowing which is which helps when something looks wrong:

- **`nvidia-smi`** — NVIDIA's own command-line tool, polled on an interval. This is the primary source and the one behind most of what you see.
- **NVML** — NVIDIA's management library, called directly for lower-overhead readings where available.

If neither is available — no NVIDIA card, drivers absent, or the tooling not on the system — the mod does not guess. It reports nothing rather than plausible-looking numbers.

See [Troubleshooting](Troubleshooting) if you see loader messages about NVML in your log at startup. They are expected on machines without it and are not an error.

---

## What it does not do

- It does not change your model, context window, or any RimSynapse setting. Warnings are advice; acting on them is yours.
- It does not manage VRAM or free memory.
- It does not monitor non-NVIDIA GPUs.
- It adds no gameplay of its own.

---

## Performance cost

Polling `nvidia-smi` has a small cost, which is why the interval is adjustable. If you are chasing frame time on a marginal machine, raising the interval or turning the overlay off costs you nothing but resolution in the readout.
