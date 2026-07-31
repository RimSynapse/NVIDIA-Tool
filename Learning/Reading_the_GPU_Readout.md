# Reading the GPU Readout

What the numbers mean, and which ones are worth reacting to.

---

## GPU statistics

**VRAM used / total** — the single most important number when running a local model. Once a model no longer fits, it either fails to load or spills into system memory and becomes dramatically slower.

**Utilisation** — how busy the GPU is. Under RimSynapse this is spiky rather than steady: near zero between requests, high during one. **A low average is normal** and does not mean the model is idle or misconfigured.

**Temperature** — thermal throttling shows up here before it shows up as slow responses. A card that is hot and slow is being throttled, not overloaded.

**Power draw** — mostly useful as corroboration. Power tracking utilisation confirms the GPU is genuinely working rather than waiting on something else.

---

## Request metrics

**Response time** — end to end for one LLM request. This is what RimSynapse's own capability tiering measures to decide how ambitious it can be, so it directly shapes how the mods behave.

**Tokens and throughput** — tokens produced per second. Throughput is the honest measure of model speed; response time also includes queueing and prompt processing.

**Queue depth** — how many requests are waiting. **This is the number people misread.** A long response time with a deep queue means the system is busy, not slow — the request spent its time waiting. A long response time with an empty queue means the model itself is slow. Those need opposite fixes.

---

## Reading it in practice

**Everything feels sluggish, GPU utilisation is low, queue is deep.** Requests are backing up faster than they complete. Reduce how much is being asked for, or accept a slower cadence.

**Response times crept up over a long session, temperature is high.** Thermal throttling. It is a cooling problem, not a configuration one.

**VRAM near the limit and responses suddenly much slower.** The model has likely spilled out of VRAM. Reduce the context window or use a smaller model — see [VRAM Planning](VRAM_Planning).

**Everything looks idle and nothing happens.** The GPU is not the problem. Check that RimSynapse Core can reach your backend at all; this mod monitors hardware, not connectivity.

**Nothing is reported at all.** See [Troubleshooting](Troubleshooting).
