# VRAM Planning

Why the breakdown is split rather than shown as one total, and how to act on it.

---

## Why a breakdown

"Model size" is the number people plan around, and it is the one that misleads. A model that loads comfortably can still run you out of memory once the context window grows, because the weights are only part of what sits on the card.

The breakdown separates:

- **Model weights** — fixed once the model is chosen. The number quoted on the model's page.
- **KV cache** — grows with how much conversation is being held. This is the part that surprises people.
- **Context window** — the ceiling you have configured. A larger window reserves more.
- **Overhead** — the runtime's own working memory, plus whatever else is on the card.

RimWorld is itself using the GPU to draw the game. On a single-card machine you are sharing.

---

## The usual failure

A model loads fine, works for a while, and then responses slow dramatically or stop. Almost always this is the context window: as conversation accumulates, the KV cache grows until the total no longer fits, and the runtime spills into system memory. Spilled inference is not slightly slower — it is slower by an order of magnitude.

The readout shows this coming, because you can watch the cache portion grow while the weights stay flat.

---

## What to change, in order

**Reduce the context window first.** It is the biggest lever and the least destructive — RimSynapse is explicitly designed to work down to small context windows, and the agent is told about the few tools a request needs rather than all of them.

**Then consider a smaller or more heavily quantised model.** A quantised model that fits comfortably will beat a larger one that spills, every time.

**Then look at what else is on the card.** Other applications, browser tabs with hardware acceleration, and RimWorld itself all take a share.

---

## Warnings

The mod warns as you approach your limit rather than after you cross it, since the symptom of crossing it is the whole thing becoming slow rather than an obvious error.

Warnings are **advice**. Nothing is changed for you, no request is blocked, and no setting is adjusted. If you know why you are close to the limit and it is fine, ignore them.

---

## If the model runs on another machine

None of this applies to the machine running RimWorld. The readout will show a largely idle GPU, which is correct — the work is happening elsewhere, and this mod monitors local hardware only.
