# Changelog

> **Note:** This is a custom fork of [SteamRE/DepotDownloader](https://github.com/SteamRE/DepotDownloader), hosted at [gsrvtech/DepotDownloader](https://github.com/gsrvtech/DepotDownloader), maintained primarily for use with personal [Pelican Panel](https://pelican.dev) eggs. It contains bug fixes and optimizations beyond the upstream release and is kept in sync with patches from the original project.

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.5.1]

### Added
- `ConsoleAuthenticator`: Clearer error message when a 2FA code is incorrect (including a note that a new code has been sent for email auth).
- `ConsoleAuthenticator`: Maximum of 3 attempts for 2FA codes; login is aborted after the limit is exceeded.
- `ConsoleAuthenticator`: Added hint to use `-no-mobile` in the Steam Mobile App confirmation prompt.
- `Program`: Per-file completion line now shows download speed (MB/s) and estimated time remaining (ETA mm:ss).

### Changed
- `HttpClientFactory`: Now accepts and acts on the `HttpClientPurpose` parameter passed by SteamKit2's `WithHttpClientFactory`. CDN connections use a 300-second timeout; all other connections use 30 seconds.
- `Util`: Replaced the hand-rolled `AdlerHash(Stream, int)` implementation with `SteamKit2.CDN.DepotChunk.AdlerHash(ReadOnlySpan<byte>)`, which is SteamKit2's optimized unrolled variant. The `uint` result is now compared directly against `ChunkData.Checksum` (also `uint`), removing the `BitConverter.GetBytes` round-trip.
- `Program`: `-max-downloads` is now validated (1–50); values outside this range exit with an error message.
- `Program`: `-app 0` is now rejected as invalid.
- `Program`: `-depot 0` is now rejected as invalid.
- `Program`: A manifest ID of `0` now produces a warning.

### Fixed

- `ContentDownloader`: Workshop web file downloads (`DownloadWebFile`) now use `HttpClientPurpose.CDN`, applying the 300-second timeout instead of the 30-second WebAPI timeout.
- `ContentDownloader`: `DownloadWebFile` now uses `GetAsync` + `EnsureSuccessStatusCode()` to surface HTTP error responses (e.g. 403, 404) as exceptions instead of silently writing an empty file.
- `ContentDownloader`: `DownloadWebFile` now retries up to 5 times on failure with `min(500 * attempt, 10000) ms` backoff, consistent with chunk download behavior.
- `ContentDownloader`: Workshop collections now batch-fetch all children's details in a single Steam API call (`GetPublishedFileDetailsBatchAsync`) instead of one request per child, reducing latency for large collections.
- `ContentDownloader`: Workshop collection children are now processed in parallel (`Task.WhenAll`) instead of sequentially, reducing total processing time for large collections.
- `ContentDownloader`: Workshop web file downloads in `DownloadPubfileAsync` are now run concurrently, limited to `MaxDownloads` parallel downloads (same semaphore used for chunk downloads).
- `HttpClientFactory`: Added shared static `CdnClient` instance for workshop web file downloads, eliminating per-request `HttpClient` instantiation and enabling TCP connection reuse.
- `Steam3Session`: `GetPublishedFileDetailsBatchAsync` now chunks requests into batches of 100 IDs to avoid hitting Steam API limits with very large collections.
- `ContentDownloader`: Workshop collection traversal now tracks visited published file IDs using a `ConcurrentDictionary`. Cycles (A → B → A) and cross-references that would otherwise trigger redundant parallel API bursts up to the depth limit are detected and skipped immediately. This prevents a malformed or malicious collection from causing an exponential fan-out of Steam API requests.
- `Steam3Session`: `EResult.ServiceUnavailable` during login now triggers a reconnect instead of aborting, preventing unnecessary download failures when Steam CM servers are temporarily overloaded.
- `Steam3Session`: Connection retry delay now uses exponential backoff (`min(1000 * 2^(n-1), 30000) ms`) instead of a linear `1000 * n ms` ramp, reducing hammering of CM servers under sustained load.
- `ContentDownloader`: Chunk downloads now retry up to 10 times before giving up (previously retried indefinitely). Each retry waits `min(500 * attempt, 10000) ms` with exponential backoff to avoid flooding CDN servers.
- `ContentDownloader`: Retry log messages for chunk downloads now include the current attempt and maximum (`attempt N/10`) for easier diagnosis.

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
