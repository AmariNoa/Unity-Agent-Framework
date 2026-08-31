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
