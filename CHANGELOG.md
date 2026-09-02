# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Package scaffold (package.json, README, CHANGELOG, LICENSE)
- Assembly definition scaffold: Editor/{SDK,Core,Gateway,Official} and Tests/Editor/{SDK,Core,Gateway,Official} asmdefs with meta files
- Tool contract types (errors, result envelope, object references, tool descriptors, jobs) with canonical JSON serialization shared with the gateway
- Editor-side agent HTTP core: editor state tracking, serial main-thread dispatcher, session bearer token, instance descriptor file, and a localhost-only HTTP server (status / tools / invoke) with 401/503 handling
- Tool registry with provider registration, [AgentToolProvider] discovery, canonical id / alias collision rejection, permission gate (mutation-derived levels, confirm / dryRun convention) and standard pagination
- Built-in read-only tools: unity.project.info, unity.scene.list, unity.object.inspect, unity.selection.get, unity.console.get
- Gateway installation check with console guidance and README setup instructions
- Machine-level instance registry (LocalAppData) so one gateway registration can discover every running editor, and a gateway binary mirror keeping a stable project-independent "current" path for MCP registrations
- Bootstrap diagnostic log (Library/UnityAgent/bootstrap.log, one rotation generation) recording domain load, server start / stop and startup failures with pid and timestamp, to diagnose a server that did not come back after a domain reload
- Bootstrap diagnostic log also records the first EditorApplication.update tick after a domain load and the editor focus state, to tell whether the update loop ran while the server was still down
