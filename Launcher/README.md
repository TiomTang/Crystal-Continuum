# Launcher configuration

`Launcher.ini.example` is documentation, not a live configuration. It is not copied to build output and does not replace an existing operator file. Create your own root `Launcher.ini` with real values before publishing it through the existing patch publisher.

| Section / key | Meaning |
| --- | --- |
| Launcher / Host | Base HTTP(S) patch URL; a leading `www.` is read as `http://` and a missing trailing slash is added in memory. Empty disables update requests. |
| Launcher / PatchFile | Publisher list name, normally `PList.gz`. The launcher requests its `.gz` form from Host, so `PList` and `PList.gz` both work. The binary manifest format and payload URL rules are unchanged. |
| Launcher / ServerName | Label shown in the launcher window; empty hides it. |
| Launcher / Browser | Announcement URL for the WebView2 panel; empty hides the panel, an invalid URL only shows a warning. |
| Launcher / NeedLogin, Login, Password | Optional patch HTTP Basic authentication. These are operator distribution credentials, **not game account credentials**; use HTTPS and do not distribute a private administrative password. |
| Launcher / Language | Launcher UI language. The neutral English text is compiled into `Launcher.exe`; other languages are compiled resx satellite resources selected from the same assembly (for example `zh-Hans`). Accepts a culture name (`zh-CN`, `zh-Hans`), a neutral language (`zh`) or a language name (`Chinese`). Empty falls back to the legacy `[Game] Language` key, then the player preference, then the Windows UI culture and its parents, then English. |
| Network / IPAddress, Port | Game endpoint used for sessions started from this launcher. Empty keeps the server saved in the client configuration. IPv4, IPv6 and DNS host names are supported; port range 1–65535. |
| Graphics / Resolution, ResolutionHeight | Display size override for sessions started from this launcher; both keys must be filled in together. Empty keeps the client's saved size. |
| Graphics / DisplayMode | `0` windowed, `1` maximized, `2` fullscreen. Empty keeps the client's saved mode. |
| Graphics / FPSCap, AlwaysOnTop | `True`/`False` display overrides. Empty keeps the client's saved value. |

Only keys that are actually filled in become client arguments: `--host`, `--port`, `--resolution`, `--display-mode`, `--fps-cap` and `--topmost` are omitted otherwise, so the launcher never pins a default of its own. The client saves every override it receives into its own configuration before connecting, so a configured value also becomes the client's stored setting for later direct starts. The configuration switch in the settings window adds `--test-config` and selects `Mir2Test.ini` instead of `Mir2Config.ini`. An explicit value that is malformed — bad host, port outside 1–65535, height without width, display mode outside 0–2, non-boolean flag — stops the launcher instead of being silently ignored: startup reports the specific error, and an operator file re-read after an update shows `Invalid launcher configuration. See Launcher.log.` and keeps the launch button disabled.

The application never creates or saves `Launcher.ini`. The existing updater may replace only this exact root INI; nested `Launcher.ini` and all other INI files remain protected. Updated publishing settings are read with a fresh reader after completion and before game launch. The in-flight update retains its original host/list/authentication snapshot.

## Player state

`Launcher.User.ini` is local personal state: `[Launcher] AutoStart`, `TestConfig`, `Migrated`; `[Game] Language`. It is created on the first save, stored locally and never replaced by patch entries. `[Game] Language` is used only when neither language key in `Launcher.ini` is set. Game account/password state stays in game configuration and never enters process arguments.

Game display and network settings belong to the client's own `Mir2Config.ini` / `Mir2Test.ini`. The settings window shows those values and, when the player saves, writes back only the keys that changed; a changed value equal to a client default removes the key again, so the client keeps owning its defaults. Network settings are displayed but not editable in the launcher.

Legacy migration runs once, on the first start that can read the selected client configuration: personal `[Graphics]` keys still present in `Launcher.User.ini` are copied into client configuration keys that are not defined yet, then `[Launcher] Migrated` is set. Client values that already exist win, a legacy value equal to the client default is not written, and the legacy keys stay in the preference file for rollback; the client configuration is authoritative afterwards. A client configuration that exists but cannot be read stops the migration instead of being overwritten, and it is retried on the next start.

If no operator file exists, nothing is overridden: the launcher contacts no update or announcement URL and starts the client with its own saved configuration. It may still create the separate personal INI. File freshness compares the original manifest length and timestamp, **not content hashes**. Normal checks intentionally do not hash unchanged resources; use **Repair files** to download every managed entry again, including corruption with identical size and timestamp. The confirmation warns about bandwidth. Personal settings and unknown files are never cleaned by repair.

## Build and publish

The launcher is packaged as its own artifact; `Launcher.csproj` no longer writes into the client output folder. There are two outputs because the launcher is developed and shipped differently:

| Use | Command | Output |
| --- | --- | --- |
| Everyday build | `dotnet build Launcher/Launcher.csproj -c Debug` (or `Release`) | `Build/Launcher/Debug` / `Build/Launcher/Release` |
| Shipped artifact | `dotnet publish Launcher/Launcher.csproj -p:PublishProfile=win-x64` | `Build/Launcher/publish/win-x64` |

Everyday Debug/Release builds stay framework-dependent and multi-file so the external update helper can be exercised without a publish. The `win-x64` publish profile is the only place the single-file properties live: `SelfContained=false`, `PublishSingleFile=true`, `RuntimeIdentifier=win-x64`, `IncludeNativeLibrariesForSelfExtract=true`, `PublishTrimmed=false`, `PublishReadyToRun=false`, `DebugType=None` and `DebugSymbols=false`. It removes known stale launcher companion files and omits WebView2 XML documentation. Use a clean output directory to produce the distribution artifact.

The published artifact is one framework-dependent `Launcher.exe`, approximately 2.5 MB. The launcher's managed assemblies, the WebView2 SDK assemblies, the WebView2 native loader and the compiled resx translations are all inside it; `IncludeNativeLibrariesForSelfExtract` writes only the native loader to `%TEMP%` at startup instead of leaving a loose `runtimes/` tree, and no `.pdb`, `.deps.json` or `.runtimeconfig.json` appears next to the EXE. The .NET runtime itself is not bundled, so the folder is still a single file.

The build machine needs MSVC x64 C++ tooling and the Windows SDK. Players only run `Launcher.exe` from the published folder. It depends on the .NET 8 Desktop Runtime x64; when the runtime is missing, a built-in helper downloads the Microsoft installer, verifies its signature and requests Windows elevation, then continues starting the launcher. The first install requires network access. The announcement panel additionally uses the WebView2 Runtime.

The client project no longer references the launcher: a build-only project reference copied the launcher apphost and its language files into the client folder, which is exactly what the separate output avoids. Neither artifact depends on the other's build output, so they can be published, installed and updated independently; an installation still places both in one folder, and the launcher finds the client through `Client.exe`.

The launcher owns its translations in `Launcher/LauncherLocalization/*.resx`: `LauncherStrings.resx` is the neutral English source and `LauncherStrings.<culture>.resx` (for example `LauncherStrings.zh-Hans.resx`) adds a translation. They are compiled into satellite resources, not copied as JSON packs, and the single-file publish embeds them in `Launcher.exe`. It never reads or writes the client's `Localization/`, which the client owns, so both artifacts can be assembled into the same installation without either one overwriting the other's language files. Culture names are matched case-insensitively through the normal `ResourceManager` parent chain (`zh-CN` → `zh-Hans` → `zh`); with no explicit setting the culture is chosen from the Windows UI culture by exact name, then each parent culture, then the language name (`Chinese`), then English. Unknown keys, a missing translation and an unreadable resource all fall back to the English source text without blocking startup. Adding a launcher language means adding a `.resx` to the launcher project, not extracting a pack from the client's packs.

`Launcher.ini.example` is not copied to the output, and the icon is embedded in `Launcher.exe` from the launcher's own `MIR2.ICO` instead of the client's copy. The `Launcher` directory contains the project sources, icon, translations and publish profile required for a standalone build and publish.

## Transaction and recovery

Downloads are streamed into `.mir2-transaction`, with HTTP, compressed-byte/expanded-length and gzip trailer checks. All required files are staged before any installed file is changed. A free-space budget includes staging and rollback backups. Download cancellation leaves installed files unchanged; application and rollback run outside the UI and must not be interrupted deliberately. An update that touches only `Data/`, `Map/`, `Sound/`, `Localization/` or the root `Launcher.ini` is applied and verified in the running launcher; any other path — including `Launcher.exe` and its compiled translations — defers the whole batch to the external helper and one restart.

The first journal is flushed and renamed inside a separate `.mir2-initializing-<id>` directory before that directory is atomically published as `.mir2-transaction`. An interruption before publication cannot block game startup or a later update. Recovery removes an unpublished directory only when its sealed, empty Downloading journal matches the directory ID and it contains no other files. Empty directories, partial `journal.next` files, invalid journals and unknown contents are preserved without being treated as active transactions; do not recursively delete them. Older `.mir2-transaction` directories without a valid journal remain fail-closed and require manual inspection.

Before creating a rollback scratch copy, the sealed journal records ownership intent. Recovery may rebuild that copy only while the backup still matches the old hash, the installed target still matches this transaction, and existing scratch bytes are a prefix of that backup. Unowned or changed scratch files are retained rather than deleted.

A helper with its complete runtime dependencies is copied outside the installation. After the original launcher exits, it acquires the installation's exclusive lease and applies the whole set, including the launcher executable. From the published single-file layout the helper copy is that one `Launcher.exe`, which already carries the runtime and translations; from a framework-dependent Debug/Release build the helper copy also carries `Launcher.dll`, its `.deps.json`/`.runtimeconfig.json`, the WebView2 assemblies and the satellite translation folders. Original files are backed up and flushed before atomic per-file replacement. A persistent, atomically written journal records intent before replacement. Uncommitted interruptions roll back, including removal of newly created targets only when their content matches this transaction. Unknown artifacts, changed targets, invalid paths, reparse points and changed backups stop recovery and retain evidence. The game refuses to start while transaction recovery is unresolved.

Local SHA256 identifies staged/backup bytes for recovery only. The old publisher supplies no remote cryptographic hash; this is not patch authenticity verification. The journal is sealed with Windows DPAPI, bound to the installation path and original Windows account. Keep that account available for automatic recovery.

Commit requires a separate invocation of the new launcher to read its configuration, load its dependencies, construct its main form, and write a token acknowledgement. A successful `Process.Start` alone is not success. This startup probe is not a game login, browser/WebView runtime, or long-term application health guarantee. After commit the normal launcher restarts and checks again before launching the game.

If both the updater helper and original process are terminated after the installed executable has been replaced, the installed entry point may not work. Read `.mir2-transaction/RECOVERY.txt` for the external helper location and run, using the original Windows account:

```text
"<external-helper-directory>/Launcher.exe" --recover-update "<installation-directory>"
```

The external helper works independently of installed Launcher files. Successful helpers remove only their own marked, unchanged dependency copies after exit; failed helpers and recovery evidence are retained. Do not delete `.mir2-transaction` or its backups when recovery is unresolved. If the original helper was manually removed or the Windows account's DPAPI keys are lost, automatic recovery is not guaranteed; preserve the installation and obtain assistance rather than deleting backups.

Old `.launcher-update/pending.json` batches are rejected with a manual recovery message and left untouched; they are not silently migrated or deleted. Process-kill recovery tests do not constitute a real hard-power-loss guarantee: hardware, filesystem and SMB persistence behavior remain outside that claim.
