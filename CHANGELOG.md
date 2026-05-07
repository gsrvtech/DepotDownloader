# Changelog

> **Note:** This is a custom fork of [SteamRE/DepotDownloader](https://github.com/SteamRE/DepotDownloader), hosted at [gsrvtech/DepotDownloader](https://github.com/gsrvtech/DepotDownloader), maintained primarily for use with personal [Pelican Panel](https://pelican.dev) eggs. It contains bug fixes and optimizations beyond the upstream release and is kept in sync with patches from the original project.

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.5.0]

### Security

- Added path traversal protection (CWE-22) in `DownloadWebFile` and `DownloadSteam3AsyncDepotFile` via a new `ValidateFilePath()` helper that canonicalizes paths and enforces that all written files stay within the designated install directory. A malicious or compromised manifest / LanCache server could otherwise have written files outside the target directory.
- Added a 30-second evaluation timeout to user-supplied regular expressions loaded from a filelist (`-filelist`). Without a timeout, a pathological regex pattern could cause a ReDoS and block the download thread indefinitely.

### Fixed

- Set `<CetCompat>false</CetCompat>` in the project file to prevent a hard crash (`TypeInitializationException` in `System.Threading.TimerQueue`) on Windows 10 IoT Enterprise LTSC 2021, which lacks kernel-level CET/Shadow Stack support required by .NET's default compatibility flag.
- `uint.Parse` in `GetSteam3AppBuildNumber` replaced with `uint.TryParse` to prevent an unhandled `FormatException` when the Steam API returns a malformed build number value.
- `ulong.Parse` (two occurrences) in `GetSteam3DepotManifest` replaced with `ulong.TryParse` to prevent an unhandled `FormatException` when the Steam API returns a malformed manifest GID.
- `SingleOrDefault` in `DownloadSteam3AsyncDepotFile` replaced with `FirstOrDefault` to prevent an `InvalidOperationException` in the unlikely case that a depot manifest contains duplicate file name entries.
- `ProcessPublishedFileAsync` now enforces a maximum recursion depth of 30 to prevent a `StackOverflowException` when a Steam Workshop collection is nested excessively deep.
- `nextServer` in `CDNClientPool` is now declared `volatile` to prevent stale cached reads in multi-threaded download scenarios. The modulo operation was also changed to use unsigned arithmetic (`(uint)nextServer % (uint)servers.Count`) to avoid an `IndexOutOfRangeException` caused by a negative result after an `int` overflow.
- `ProcessDepotManifestAndFiles` now returns `null` immediately after cancelling the `CancellationTokenSource` when `manifestRequestCode` is 0, preventing a subsequent download attempt with an invalid request code.
- `DisplayQrCode` now temporarily sets `Console.OutputEncoding` to UTF-8 before rendering the QR code and restores the previous encoding afterwards, fixing QR codes displaying as `?` characters on Windows systems using a non-Unicode code page (e.g. Japanese Shift-JIS).
- `DownloadSteam3AsyncDepotFile` now unconditionally calls `SetExecutable(true)` for files flagged as executable, instead of skipping the call when the previous manifest already recorded the executable flag. When a file is updated, it is recreated via `FileMode.Create` and loses its Unix execute permission; the previous condition incorrectly assumed no chmod was needed in that case.
- The manifest request code expiration check in `ProcessDepotManifestAndFiles` now uses `DateTime.UtcNow` instead of `DateTime.Now`, avoiding incorrect expiry behaviour during daylight saving time transitions.
- `ValidateFilePath` now uses `StringComparison.Ordinal` on non-Windows platforms instead of `OrdinalIgnoreCase`, reflecting that Linux and macOS file systems are case-sensitive. The previous comparison could allow a path traversal attack via a manifest entry that differs from the install directory only in letter case.
- The return value of `Task.Run(steam3.TickCallbacks)` in `InitializeSteam3` is now discarded explicitly (`_ = Task.Run(…)`), silencing the compiler warning about an unobserved task.

### Changed

- Updated `SteamKit2` from `3.3.1` to `3.4.0`.
- Updated target framework from `net9.0` to `net10.0` (required by SteamKit2 3.4.0).
- Updated .NET SDK version in `global.json` from `9.0.100` to `10.0.100`.
- `AdlerHash` in `Util` now reads the input stream in buffered 64 KB chunks instead of byte-by-byte via `stream.ReadByte()`. This eliminates a significant performance bottleneck during file validation of large depot files.
- `DecodeHexString` in `Util` now delegates to `Convert.FromHexString()` (available since .NET 5), removing the manual loop that allocated a `string` for every two-character pair.
- Retry log messages for manifest and chunk downloads now clarify that the error is non-fatal and a retry is in progress (e.g. `"Encountered ServiceUnavailable for depot manifest … Retrying…"` instead of `"Encountered error downloading depot manifest…"`).
- `DownloadConfig.MaxDownloads` now defaults to `8` at the type level instead of relying solely on the `Program.cs` call site, ensuring the correct default applies in all code paths.
- `ProcessPublishedFileAsync` and `DownloadPubfileAsync` now use a named tuple `(string filename, string url)` instead of `ValueTuple<string, string>`, replacing `.Item1`/`.Item2` accesses with named fields for clarity.
- Chunk file writes in `DownloadSteam3AsyncDepotFileChunk` now use `RandomAccess.WriteAsync` with a `SafeFileHandle` opened in `DownloadSteam3AsyncDepotFile`, replacing the previous `FileStream` + `SemaphoreSlim(1)` pattern. Because `RandomAccess` is offset-based and inherently thread-safe, the serialising lock is no longer needed and all chunks for a given file can be written truly concurrently.
- The copy-chunk loop in `DownloadSteam3AsyncDepotFile` now rents its per-chunk temp buffer from `ArrayPool<byte>.Shared` instead of allocating a new `byte[]` each iteration, reducing GC pressure during incremental depot updates.
- `CDNClientPool.GetConnection()` now uses `Interlocked.Increment` to distribute requests across all available CDN servers in a true round-robin fashion. Previously `nextServer` was never incremented on successful connections, so all concurrent chunk downloads targeted the same CDN server and were subject to per-IP rate limiting; `ReturnBrokenConnection` is simplified accordingly.
- `DownloadSteam3AsyncDepotFiles` now pipelines file pre-processing and chunk downloading: file pre-allocation/validation writes chunks into an `UnboundedChannel<T>` as soon as they are identified, and chunk downloads begin immediately via a concurrent consumer, instead of waiting for all files to be pre-processed before any network transfer starts.
