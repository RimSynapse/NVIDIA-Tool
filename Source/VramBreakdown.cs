using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RimSynapse.NvidiaTool
{
    /// <summary>
    /// Estimates per-application VRAM breakdown without relying on
    /// NVML process enumeration (which requires elevated privileges).
    ///
    /// Sources:
    ///   RimWorld     → Unity's own Texture.currentTextureMemory + overhead
    ///   LM Studio    → Model parameter count parsed from Core settings
    ///   In-process   → RimSynapse mods that loaded a model into VRAM inside RimWorld's own
    ///                  process, registered via Core's GpuStats consumers channel (Core #104) —
    ///                  e.g. Local TTS's Kokoro model. Invisible to NVML per-process enumeration.
    ///   System       → Total VRAM used - RimWorld - LM Studio - in-process consumers
    /// </summary>
    internal static class VramBreakdown
    {
        private static float _rimworldMb;
        private static float _lmStudioMb;
        private static float _lmStudioVramMb;
        private static float _lmStudioRamMb;
        private static float _systemMb;
        private static bool _lmStudioIsRemote;
        private static List<GpuMemoryConsumer> _consumers = new List<GpuMemoryConsumer>();
        private static float _consumersMb;
        private static DateTime _lastUpdate = DateTime.MinValue;
        private const float UpdateIntervalSec = 3f;

        // ── Public accessors ──

        internal static float RimWorldMb => _rimworldMb;
        /// <summary>Full LM Studio estimate (VRAM-resident + any RAM-offloaded portion).</summary>
        internal static float LmStudioMb => _lmStudioMb;
        /// <summary>LM Studio portion actually resident on this GPU. Zero for a remote host.</summary>
        internal static float LmStudioVramMb => _lmStudioVramMb;
        /// <summary>LM Studio portion estimated to be offloaded to system RAM (not on the GPU).</summary>
        internal static float LmStudioRamMb => _lmStudioRamMb;
        internal static float SystemMb => _systemMb;
        /// <summary>True when the configured LM Studio endpoint is a remote host, so none of its VRAM is on this GPU.</summary>
        internal static bool LmStudioIsRemote => _lmStudioIsRemote;
        /// <summary>Resident in-process VRAM consumers registered by other RimSynapse mods (Core #104).</summary>
        internal static List<GpuMemoryConsumer> Consumers => _consumers;
        /// <summary>Total VRAM (MB) attributed to in-process consumers.</summary>
        internal static float ConsumersMb => _consumersMb;

        /// <summary>
        /// Refresh the breakdown. Call from the overlay's OnGUI (throttled internally).
        /// </summary>
        internal static void Refresh()
        {
            var now = DateTime.UtcNow;
            if ((now - _lastUpdate).TotalSeconds < UpdateIntervalSec) return;
            _lastUpdate = now;

            float totalUsedMb = NvidiaSmiReader.UsedVramMb;
            if (totalUsedMb <= 0f) return;

            // 1. RimWorld — query Unity's own GPU memory tracking
            _rimworldMb = GetRimWorldVramMb();

            // 2. LM Studio — estimate from loaded model parameters.
            //    Only counts toward LOCAL VRAM when the endpoint is on this machine:
            //    a remote LM Studio host runs on a different GPU, so attributing its
            //    estimate here would be phantom VRAM and would distort the System line too.
            _lmStudioIsRemote = IsLmStudioRemote();
            _lmStudioMb = _lmStudioIsRemote ? 0f : EstimateLmStudioVramMb();

            // 3. Split LM Studio into VRAM vs offloaded RAM based on what's physically possible
            float maxAvailableForLms = totalUsedMb - _rimworldMb;
            if (maxAvailableForLms < 0f) maxAvailableForLms = 0f;

            // Estimate System base overhead (e.g. max 1 GB or whatever is left)
            float systemEstimate = Math.Min(1024f, maxAvailableForLms);
            float lmsVramLimit = maxAvailableForLms - systemEstimate;
            if (lmsVramLimit < 0f) lmsVramLimit = 0f;

            if (_lmStudioMb > lmsVramLimit)
            {
                _lmStudioVramMb = lmsVramLimit;
                _lmStudioRamMb = _lmStudioMb - lmsVramLimit;
            }
            else
            {
                _lmStudioVramMb = _lmStudioMb;
                _lmStudioRamMb = 0f;
            }

            // 4. In-process consumers (Core #104): models loaded into VRAM inside RimWorld's own
            //    process (e.g. Local TTS's Kokoro). NVML can't see them separately and Unity's
            //    texture tracking misses them, so without this they inflate the System line.
            _consumers = GatherConsumers();
            _consumersMb = 0f;
            foreach (var c in _consumers) _consumersMb += c.vramMb;

            _systemMb = totalUsedMb - _rimworldMb - _lmStudioVramMb - _consumersMb;
            if (_systemMb < 0f) _systemMb = 0f;
        }

        /// <summary>
        /// Read the resident in-process VRAM consumers from Core's GpuStats channel (Core #104).
        /// Reads through Core (which the tool already depends on) — no coupling to the reporting
        /// mods. Non-resident (CPU) consumers report 0 and are filtered out.
        /// </summary>
        private static List<GpuMemoryConsumer> GatherConsumers()
        {
            var result = new List<GpuMemoryConsumer>();
            try
            {
                var snapshot = SynapseClient.Gpu?.ConsumersSnapshot();
                if (snapshot != null)
                {
                    foreach (var c in snapshot)
                        if (c != null && c.resident && c.vramMb > 0f)
                            result.Add(c);
                }
            }
            catch
            {
                // Older Core without the consumers channel — nothing to surface.
            }
            return result;
        }

        // ────────────────────────────────────────────────────────
        //  RimWorld VRAM (from Unity)
        // ────────────────────────────────────────────────────────

        /// <summary>
        /// Uses Unity's own memory APIs to determine how much VRAM
        /// RimWorld is consuming. No external calls needed.
        /// </summary>
        private static float GetRimWorldVramMb()
        {
            try
            {
                // Texture.currentTextureMemory = actual GPU-resident texture bytes
                // This is the most reliable Unity API for GPU memory tracking
                long texBytes = (long)Texture.currentTextureMemory;
                float texMb = texBytes / (1024f * 1024f);

                // Add overhead for:
                //   - Render targets / frame buffers (~15-25% of texture memory)
                //   - Shader programs, constant buffers
                //   - Mesh GPU buffers (vertex/index)
                //   - Unity internal GPU allocations
                // Conservative 40% overhead multiplier for a 2D-heavy game like RimWorld
                float estimatedTotalMb = texMb * 1.4f;

                // Floor: even a minimal RimWorld scene uses some GPU memory
                if (estimatedTotalMb < 50f) estimatedTotalMb = 50f;

                return estimatedTotalMb;
            }
            catch
            {
                // If Unity's API fails, return a reasonable default
                return 200f; // ~200 MB is typical for RimWorld
            }
        }

        // ────────────────────────────────────────────────────────
        //  LM Studio VRAM (estimated from model name)
        // ────────────────────────────────────────────────────────

        /// <summary>
        /// Estimates LM Studio's VRAM usage from the loaded model name.
        /// Parses parameter count (e.g., "12b", "7b") and applies
        /// Q4_K_M quantization estimate (~0.65 GB per billion params).
        /// </summary>
        private static float EstimateLmStudioVramMb()
        {
            try
            {
                string modelName = GetLoadedModelName();
                if (string.IsNullOrEmpty(modelName)) return 0f;

                float billionParams = ParseBillionParams(modelName);
                if (billionParams <= 0f) return 0f;

                // Q4_K_M quantization: ~0.65 GB per billion parameters
                // Add ~500 MB overhead for KV cache, context, runtime
                float estimateGb = (billionParams * 0.65f) + 0.5f;

                return estimateGb * 1024f; // convert GB → MB
            }
            catch
            {
                return 0f;
            }
        }

        /// <summary>
        /// True when RimSynapse Core is configured to talk to a remote LM Studio host.
        /// In that case the model runs on another machine's GPU, so none of its VRAM
        /// is resident on this GPU and it must not be attributed to local VRAM.
        /// </summary>
        private static bool IsLmStudioRemote()
        {
            try
            {
                return RimSynapseMod.Instance?.Settings?.IsRemoteUrl ?? false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get the currently loaded model name from Core.
        /// Prefers the LIVE model from ModelManager (queried from LM Studio API)
        /// over the persisted settings value.
        /// </summary>
        private static string GetLoadedModelName()
        {
            try
            {
                // Live model from LM Studio API (via Core's public API)
                string active = SynapseClient.ActiveModelName;
                if (!string.IsNullOrEmpty(active)) return active;

                // Fallback: user-selected model in settings
                var settings = RimSynapseMod.Instance?.Settings;
                return settings?.selectedModel;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Parse billion parameter count from model name.
        /// Handles patterns like:
        ///   "gemma-4-12b-qat"      → 12
        ///   "llama-3.3-70b"        → 70
        ///   "qwen2.5-7b-instruct"  → 7
        ///   "gemma-4-e4b"          → 4 (expert/MoE shorthand)
        ///   "gemma-4-26b-a4b"      → 26 (active params noted but total used)
        ///   "phi-4-mini-3.8b"      → 3.8
        /// </summary>
        internal static float ParseBillionParams(string modelName)
        {
            if (string.IsNullOrEmpty(modelName)) return 0f;

            modelName = modelName.ToLowerInvariant();

            // Match standard patterns: "12b", "7b", "70b", "3.8b", "0.5b"
            // Skip quantization markers: "q4b", "q8b"
            var match = Regex.Match(modelName, @"(?<![a-z])(\d+\.?\d*)b(?!\w)");
            if (match.Success)
            {
                float val;
                if (float.TryParse(match.Groups[1].Value, out val) && val > 0f)
                    return val;
            }

            // Handle MoE/expert notation: "e4b" = expert 4B, "a4b" = active 4B
            // (e.g., gemma-4-e4b = 4B active expert parameters)
            var moeMatch = Regex.Match(modelName, @"[ea](\d+\.?\d*)b(?!\w)");
            if (moeMatch.Success)
            {
                float val;
                if (float.TryParse(moeMatch.Groups[1].Value, out val) && val > 0f)
                    return val;
            }

            // Fallback: check for known size keywords
            if (modelName.Contains("mini")) return 3.8f;
            if (modelName.Contains("small")) return 7f;
            if (modelName.Contains("medium")) return 13f;
            if (modelName.Contains("large")) return 34f;

            return 0f;
        }
    }
}
