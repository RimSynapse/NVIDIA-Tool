# Changelog

Full version history for RimSynapse - NVIDIA Tool. The mod page and Workshop description show only the latest release; every earlier version is recorded here.

## v0.7.1 - VRAM Accuracy and Quieter Startup
- Fixed - VRAM breakdown reconciles. The "VRAM Status" popup previously summed the full LM Studio estimate - including the portion the estimator assumes is offloaded to system RAM - and attributed models running on a remote LM Studio host to local VRAM, so the components did not add up to the reported total. The dialog now counts only GPU-resident memory: the offloaded LM Studio portion is labelled as system RAM rather than VRAM, and a remote host contributes zero local VRAM instead of a phantom amount. Estimated lines are marked with a tilde so measured and estimated figures are no longer conflated. (#15)
- Fixed - Quieter startup. On a machine without a resolvable NVML library, the tool no longer emits roughly 16 "Fallback handler could not load library" lines to Player.log before it can log anything of its own. It now probes for the library once via the Windows loader and, when NVML is absent, logs a single line noting that GPU VRAM advisories are unavailable. VRAM advisories are unaffected where NVML is present. (#13)
- Requires Core v0.7.0; saves and settings carry over unchanged.

## v0.7.0 - Regions and Territories Compatibility
- Moves in step with RimSynapse Core v0.7.0.
- Requires Core v0.7.0; saves and settings carry over unchanged.

## v0.6.1
- Fixed - mod list metadata: the in-game mod list still showed v0.5.2 with no v0.6.0 notes. Version and changelog now agree in every place they are stated.
- Roadmap updated: 0.7 is now Regions and Territories compatibility - the groundwork the Factions work depends on. Everything after it shifts up one release.

## v0.6.0
- Requires RimSynapse Core v0.6.0. This release moves in step with Core's Agent and Tool Foundation update - your saves and settings carry over unchanged.
- Documentation: in-game wiki guides updated; "MCP" renamed to game tools throughout, matching Core's native tool-calling engine.

## v0.5.2
- Maintenance release: no gameplay changes. Version aligned with the rest of the RimSynapse suite, which carries fixes in Core and Psychology.
- Licence: now PolyForm Noncommercial 1.0.0. Free to use, modify and share for any noncommercial purpose.

## v0.5.1
- Playtest improvements: general stability enhancements and compatibility optimizations for playtesting.

## v0.4.0
- Updated to support RimSynapse Core v0.4.0 (Multi-provider routing and Image generation).
