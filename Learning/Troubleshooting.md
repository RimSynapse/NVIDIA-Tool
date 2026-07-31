# Troubleshooting

---

## "Fallback handler could not load library ... nvml" appears in my log

**This is expected on a machine without NVML, and nothing is broken.**

At startup you may see a run of lines like:

> Fallback handler could not load library ...MonoBleedingEdge/nvml
> Fallback handler could not load library ...MonoBleedingEdge/nvml.dll
> Fallback handler could not load library ...MonoBleedingEdge/libnvml

There are sixteen of them: four filename variants attempted four times each. They are Mono's library loader trying to resolve NVML, not exceptions, and the mod falls back to `nvidia-smi` without difficulty.

They are unhelpful for two reasons we accept as our fault: they are the **first alarming-looking text in the log**, and they appear before this mod can log anything of its own to explain them. If you are reading a log while diagnosing something unrelated, these are not it.

This is tracked and will be reduced to a single explanatory line.

---

## No GPU statistics at all

In rough order of likelihood:

- **Not an NVIDIA card.** This mod reads NVIDIA tooling only. It stays quiet rather than reporting numbers it cannot verify.
- **`nvidia-smi` not available.** It ships with the NVIDIA driver. If running it in a terminal fails, the mod cannot read anything either — fix it there first.
- **Your model runs on another machine.** The readout covers the machine running RimWorld. A remote backend means a quiet local GPU, correctly.
- **Overlay is off.** It is off by default. Toggle it from the toolbar button or mod settings — the developer tools window works regardless.

---

## Numbers look implausible

**VRAM higher than expected** — other applications share the card, and RimWorld is drawing the game on it too. The total is the card's, not the model's.

**Utilisation near zero with the model clearly working** — normal. Requests are spiky; a two-second poll frequently lands between them. Judge by response times rather than by instantaneous utilisation.

**Breakdown does not sum to the reported total** — the breakdown is an *estimate* of where memory is going, based on model and context settings. The total is measured. They will not agree exactly, and the breakdown is for reasoning about proportions rather than accounting.

---

## The overlay is in the way

Toggle it from the toolbar button. The developer tools window carries the same information without occupying screen space.

---

## Performance cost concerns

Polling has a cost, which is why the interval is adjustable. Raise it, or turn the overlay off and use the window when you want a reading. The mod is a diagnostic tool — if it is costing you frames, it has stopped doing its job.

---

## Reporting a problem

Include your GPU model, driver version, the output of running `nvidia-smi` yourself, and the `Player.log` from the run. The last of these usually settles whether the mod could not read the GPU or read it and reported something you did not expect — which are different problems.
