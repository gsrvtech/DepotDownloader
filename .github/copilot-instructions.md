# Guidance for GitHub Copilot

This repository is a maintained fork of DepotDownloader.
Copilot should assist by improving and extending the existing codebase **without rewriting or replacing core components**.

## Important constraints

1. **Do NOT rewrite the architecture.**
   - Do not replace Steam3Session, ConsoleAuthenticator, or ContentDownloader.
   - Do not introduce parallel systems that duplicate existing functionality.
   - Do not create new authentication frameworks unless explicitly requested.

2. **Stay compatible with SteamKit2 v3.x**
   - Use the current IAuthenticator-based login flow.
   - Do not rely on deprecated LoginKey or legacy MachineAuth patterns.
   - Do not mix WebAPI cookie authentication with CM binary protocol login.

3. **Respect the existing file structure**
   - Only modify files that already exist in the repository.
   - Only add new files when explicitly instructed in comments.

4. **Do NOT remove or break existing features.**
   - All current CLI flags must continue to work.
   - All existing download logic must remain intact.

---

## Allowed improvements

Copilot may extend or improve the following areas:

### 1. Authentication robustness
Enhance the existing Steam3Session and ConsoleAuthenticator by:
- improving error handling
- adding retry logic
- making 2FA handling more resilient
- improving MachineAuth persistence using the SteamKit2 v3.x authenticator model

### 2. Optional: Add support for encrypted credential storage
If a TODO comment explicitly requests it, Copilot may:
- add a small helper class for encrypted credential storage (AES/DPAPI)
- integrate it into the existing login flow without replacing it

### 3. Optional: Add support for reusing SteamKit2 authenticator sessions
Copilot may:
- add logic to persist authenticator tokens
- reload them on next startup
- avoid unnecessary logins

### 4. Improve reliability of downloads
Copilot may:
- add retry logic to ContentDownloader
- improve chunk download error handling
- add optional logging or progress improvements

---

## What Copilot should NOT do

- Do not introduce a new "AuthManager" abstraction unless explicitly requested.
- Do not create a new "SteamDownloadService" that duplicates ContentDownloader.
- Do not add WebAPI cookie login (sessionid + steamLoginSecure) unless explicitly requested.
- Do not replace SteamKit2's built-in authentication flow.
- Do not restructure the project.
- Do not remove existing CLI options.

---

## How Copilot should behave

When encountering TODO comments in the code:
- Provide minimal, targeted implementations.
- Follow the existing coding style.
- Use SteamKit2 APIs correctly.
- Keep changes small and incremental.
- Avoid speculative architecture.

When adding new code:
- Prefer helper methods over new subsystems.
- Keep compatibility with the existing workflow.
- Ensure the code compiles without breaking existing features.

---

## Summary

Copilot should:
- **extend**, not **replace**
- **improve**, not **rewrite**
- **respect SteamKit2 v3.x**
- **follow existing patterns**
- **only implement what TODOs request**

This file defines the boundaries for AI-assisted development in this repository.
