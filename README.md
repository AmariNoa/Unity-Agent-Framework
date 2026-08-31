# Unity Agent Framework

AI agent integration framework for Unity Editor.

Unity Editor 上の操作や Unity 向けツールを、外部 AI Agent (Claude Code / Codex などの MCP Client) から安全かつ構造的に利用するための基盤パッケージ。

## Status

v0.1 実装フェーズ。Core 基盤 (Tool Registry / HTTP サーバー / Read Tool 5 種) と Gateway (stdio MCP Server) を実装済み。

## Setup (v0.1 開発版)

1. 本パッケージを Unity プロジェクトの Packages/ へ導入する (VPM リスティングからの配布は初回リリース時に開始)。
2. Agent Gateway を用意する。プラットフォーム別 VPM パッケージ (com.amari-noa.unity-agent-framework.gateway.win-x64 等) の配布開始までは、Unity-Agent-Gateway リポジトリからソース実行する:

   dotnet run --project src/UnityAgentGateway -- --project <Unity プロジェクトのパス>

3. MCP Client へ登録する。例 (Claude Code):

   claude mcp add unity -- dotnet run --project <Unity-Agent-Gateway>/src/UnityAgentGateway -- --project <Unity プロジェクトのパス>

   mcp.json 直接記述の例:

   {
     "mcpServers": {
       "unity": {
         "command": "unity-agent-gateway",
         "args": ["--project", "<Unity プロジェクトのパス>"]
       }
     }
   }

4. Unity Editor を開いた状態で MCP Client から接続すると、unity.project.info / unity.scene.list / unity.object.inspect / unity.selection.get / unity.console.get が利用できる (MCP 上の名前はドット→アンダースコア変換)。

## Planned components

- Agent Gateway (external process, MCP server)
- Unity Agent Core (tool registry, dispatcher, transaction, permission)
- Agent Integration SDK (native integration API for own packages)
- Official Unity Bridge (conditional, Unity 6 + com.unity.pipeline)

## Requirements

- Unity 2022.3 以降 (初期基準: Unity 2022.3.22f1 / VRChat 向けプロジェクト)

## License

MIT
