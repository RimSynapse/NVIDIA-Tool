# RimSynapse NVIDIA Tool

Welcome to the documentation for **RimSynapse - NVIDIA Tool**, the GPU monitoring and LLM performance dashboard for the RimSynapse suite.

RimSynapse runs a language model alongside your game. This mod tells you what that is costing you — VRAM, temperature, utilisation, and how long each request actually took — without leaving RimWorld.

## Table of Contents

- [NVIDIA Tool Overview](NVIDIA_Tool_Overview)
- [Reading the GPU Readout](Reading_the_GPU_Readout)
- [VRAM Planning](VRAM_Planning)
- [Troubleshooting](Troubleshooting)

---

## Before you start

- **This is a monitoring tool.** It reads GPU state and reports it. It does not change model settings, allocate memory, or alter how RimSynapse behaves.
- **NVIDIA hardware only.** Readings come from NVIDIA's own tooling. On other GPUs the mod loads and stays quiet rather than reporting wrong numbers.
- **The overlay is off by default.** Turn it on from the toolbar button or in mod settings.
- **If your language model runs on another machine**, this mod monitors the machine running RimWorld, which will show little GPU activity. That is correct — the work is happening elsewhere.
