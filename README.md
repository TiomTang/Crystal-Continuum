# Crystal Continuum

[简体中文](README.zh-CN.md)

**An independent community fork of Crystal, focused on performance, longstanding fixes, and continued development.**

[![Client and shared: MIT](https://img.shields.io/badge/Client%20%26%20shared-MIT-blue.svg)](LICENSE-MIT.txt)
[![Server: GPL v3 only](https://img.shields.io/badge/Server-GPL--3.0--only-blue.svg)](LICENSE-GPL-3.0.txt)

## About this fork

Crystal Continuum builds on [Crystal](https://github.com/Suprcode/Crystal), the C# server and client engine for The Legend of Mir 2, originally created by Jamie and contributors and developed by the LOMCN community.

Upstream has entered maintenance mode, with contributions limited to bug fixes and small, easily reviewed changes rather than larger features or substantial rework. This fork provides a place to continue development beyond that scope while retaining the original project's foundations and attribution. See the [upstream maintainer's statement](https://github.com/Suprcode/Crystal/pull/1204#issuecomment-6638943563).

The current focus is to **fix and refine existing systems across the client, server, and administration tools, and rework localization into a more consistent, maintainable system**. This is not just a Chinese translation pack: English remains the source language, with Simplified Chinese resources maintained alongside the shared localization infrastructure.

This is an independently maintained community fork, not an official successor or a release endorsed by the upstream team.

## Highlights beyond upstream

### Performance, not just more features

Client UI and rendering performance are major priorities for this fork. Compared with upstream, the changes span interface refreshes, scene and effect rendering, graphics resource management, and frame presentation, with a focus on reducing redundant work and unnecessary CPU usage.

- **More efficient UI refreshes**: reduce whole-interface redraws and repeated recreation of minimap quest markers, cutting main-thread and texture overhead.
- **Leaner scene and effect rendering**: reduce redundant submissions and state changes when drawing maps and weather effects.
- **Smoother resource cleanup**: spread graphics cleanup work over time to reduce frame-time disruption from bulk scans and releases.
- **Less wasted CPU time**: improve client waiting and server monster scheduling rather than continuously spinning when there is no work to process.
- **Lower-latency windowed presentation**: introduce a low-latency presentation path, alongside aspect-ratio-preserving display in windowed, borderless maximized, and fullscreen modes.

These describe implemented optimizations relative to upstream, not a fixed FPS improvement on every system. Actual gains depend on hardware, resolution, scene, and load.

### Addressing longstanding upstream issues

Alongside new capabilities, this fork focuses on problems in existing workflows that affect everyday play and administration.

- **Display and interaction**: fix freezes and stutter when switching fullscreen modes, unstable selection of small or flying monsters during animations, and clipped long text in NPC dialogs.
- **Pets and heroes**: improve pet pursuit and target contention, hero assistance, and equipment transfers to reduce combat interaction problems.
- **Quests and items**: fix excess delivery of shared quest items, certain item-stacking failures, and potion duration and stat-display issues.
- **Data editing and saving**: fix drop editing that discards original file content and broken references after monster renaming, and improve shutdown and save-failure handling.

### Reworked localization for maintainable multilingual support

- Unify localization across game messages and server administration UI, making new features less likely to miss translation coverage.
- Keep English as the source language, with readable fallback text when translations are missing and a foundation for additional languages.
- Continue expanding Simplified Chinese coverage, aligning skill and guild terminology, and completing skill descriptions and administration-tool text.

### Integrated script and quest editing

- Write NPC scripts, event scripts, and quest definitions directly in the administration tools instead of switching between external editors.
- Syntax highlighting, structure outlines, context-aware command suggestions, and page navigation make longer scripts easier to maintain.
- Check scripts while editing and locate problems by source file and line before putting changes into use.
- Search a centralized command list for command descriptions, permissions, and script sources.

### Clearer combat information and companion management

- Inspect target health, level, and available monster combat stats on hover.
- Select heroes and pets directly from a companion status bar, making healing and support easier in crowded scenes.
- Configure stat inheritance so Taoist summons and Wizard pets can grow with their owner's attributes, with separate rules for each class.

### Standalone launcher and networking improvements

- A dedicated launcher brings together game startup, display settings, resource updates, and file repair, with update rollback and interrupted-update recovery.
- IPv4/IPv6 dual-stack support offers more connection options across network environments.
- Improved handling of abnormal connections reduces interference from probe traffic with normal player admission and server operation.

## Branches

- **`master`**: the English-first community branch for shared fixes and selected features. Simplified Chinese resources remain available.
- **`dev`**: ongoing development, experiments, and customized gameplay rules.

The highlights above describe changes present in the current community branch relative to upstream. Additional `dev` changes are not automatically included in `master`. Use matching client and server versions when updating, especially for changes that affect the network protocol.

## Chinese database and related projects

- **[Crystal.Database-ZhCN](https://github.com/TiomTang/Crystal.Database-ZhCN)**: the separately maintained Chinese server database project, including Chinese game data and NPC scripts.
- [Upstream Crystal](https://github.com/Suprcode/Crystal)
- [Upstream database](https://github.com/Suprcode/Crystal.Database)
- [Crystal map editor](https://github.com/Suprcode/Crystal.MapEditor)
- [LOMCN build guide](https://www.lomcn.net/wiki/index.php/Getting_Started)
- [LOMCN Crystal wiki](https://www.lomcn.net/wiki/index.php/Crystal)

The engine and database are separate projects. Check their branch and content requirements when assembling a server; neither repository grants rights to third-party game assets.

## Attribution and licensing

Thanks to Crystal's original author, Jamie, the [upstream contributors](https://github.com/Suprcode/Crystal/graphs/contributors), and the LOMCN community for the foundation of this project. This fork is maintained by [TiomTang](https://github.com/TiomTang). Original authors retain copyright in their contributions.

Licensing is component-specific, not a choice of either license for the whole repository:

| Component | License |
| --- | --- |
| `Client/` and `Shared/` | [MIT](LICENSE-MIT.txt) |
| `Server/`, `Server.MirForms/`, and `Tests/` | [GPL-3.0-only](LICENSE-GPL-3.0.txt) |
| Other original Crystal tools, build files, and documentation | MIT, unless explicitly stated otherwise |
| Third-party software and assets | Their own terms; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) |

Preserve applicable copyright, attribution, and license notices. Distribution of GPL-covered server binaries requires access to the complete corresponding source under GPL v3. Merely running a modified server does not require publishing changes solely because players connect over the network.

Read [LICENSE](LICENSE) for the full scope and historical contributor-permission qualifications, and [CONTRIBUTING.md](CONTRIBUTING.md) for contribution terms. This overview does not resolve historical licensing questions or replace the license texts. Game artwork, sounds, maps, trademarks, and other third-party content require their own permissions.
