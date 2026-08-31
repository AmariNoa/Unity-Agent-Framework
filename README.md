# Unity Agent Framework

AI agent integration framework for Unity Editor.

Unity Editor 上の操作や Unity 向けツールを、外部 AI Agent (Claude Code / Codex などの MCP Client) から安全かつ構造的に利用するための基盤パッケージ。

## Status

設計フェーズ。実装は未着手。

## Planned components

- Agent Gateway (external process, MCP server)
- Unity Agent Core (tool registry, dispatcher, transaction, permission)
- Agent Integration SDK (native integration API for own packages)
- Official Unity Bridge (conditional, Unity 6 + com.unity.pipeline)

## Requirements

- Unity 2022.3 以降 (初期基準: Unity 2022.3.22f1 / VRChat 向けプロジェクト)

## License

MIT
