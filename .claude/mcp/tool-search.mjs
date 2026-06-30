#!/usr/bin/env node
/**
 * JNPF Tool Search MCP Server
 *
 * 零依赖 MCP Server，通过 JSON-RPC 2.0 over stdio 与 Claude Code 通信。
 * 提供 jnpf_tool_search 工具：自然语言意图 → 推荐最佳工具 + 置信度 + 理由。
 *
 * 匹配算法：关键词匹配 (0-50) + 意图模式 (0-30) - 反关键词惩罚 (0-20)
 * confidence = clamp(rawScore / 80, 0, 1)
 */

import * as readline from "node:readline";

// ============================================================================
// Tool Registry (static data)
// ============================================================================

const TOOLS = [
  // ── MCP: CodeGraph ──
  {
    name: "codegraph_explore",
    provider: "mcp",
    useCase: "exploratory",
    keywords: [
      "explore", "discover", "find related", "semantic search", "concept",
      "region", "area", "what code handles", "how is X implemented",
      "find implementation pattern", "similar to", "architecture of",
      "design of", "understand how", "how does", "codebase exploration",
      "business logic", "domain", "where is the code for"
    ],
    antiKeywords: [
      "exact string", "literal text", "grep for", "search for text",
      "file name", "which file", "read file", "show me the file"
    ],
    rationale: "Semantic exploration of code regions. Discovers related symbols, their source, and call paths. Best for understanding unfamiliar code or business concepts.",
    alternative: "Grep for exact text search; Glob for finding files by name.",
  },
  {
    name: "codegraph_node",
    provider: "mcp",
    useCase: "inspection",
    keywords: [
      "show me", "inspect", "class definition", "method signature",
      "interface", "what does", "source of", "definition of",
      "show code for", "callers of", "called by", "who calls",
      "who uses", "what calls", "what uses", "call chain",
      "upstream", "downstream", "dependency of", "dependencies of",
      "symbol", "function signature", "API surface"
    ],
    antiKeywords: [
      "find all files", "search everywhere", "grep", "text match",
      "search for string", "all occurrences in files",
    ],
    rationale: "Inspect a specific symbol with its full source and caller/callee relationships. Best for understanding a known class/method/interface.",
    alternative: "Read for known file paths; Grep for text-level search only.",
  },

  // ── MCP: Self ──
  {
    name: "jnpf_tool_search",
    provider: "mcp",
    useCase: "meta",
    keywords: [
      "which tool", "what tool", "how to find", "best tool for",
      "tool for", "search tools", "right tool", "correct tool",
      "should i use", "what to use", "tool selection"
    ],
    antiKeywords: [],
    rationale: "Meta-search for the right tool. Use when unsure which tool fits your task.",
    alternative: "Directly use the tool you're most familiar with, then fall back to this if it doesn't work.",
  },

  // ── Built-in: Read ──
  {
    name: "Read",
    provider: "builtin",
    useCase: "reading",
    keywords: [
      "read file", "open", "show contents", "look at", "inspect file",
      "what's in", "check file", "view source", "see code", "examine",
      "show me the file", "display", "print file"
    ],
    antiKeywords: [
      "find which file", "search for", "discover", "explore",
      "who calls", "callers", "callees", "impact", "dependencies"
    ],
    rationale: "Read complete file contents. Best when you already know the exact file path.",
    alternative: "Glob to discover files; codegraph_explore for semantic discovery.",
  },

  // ── Built-in: Write ──
  {
    name: "Write",
    provider: "builtin",
    useCase: "writing",
    keywords: [
      "create file", "write file", "new file", "overwrite", "save as",
      "create new", "generate file", "scaffold"
    ],
    antiKeywords: [
      "modify part", "change line", "update function", "edit", "fix",
      "patch", "refactor", "rename", "adjust"
    ],
    rationale: "Write or overwrite entire files. Best for creating new files or full replacements.",
    alternative: "Edit for targeted changes within existing files.",
  },

  // ── Built-in: Edit ──
  {
    name: "Edit",
    provider: "builtin",
    useCase: "editing",
    keywords: [
      "edit", "modify", "change", "update", "fix", "patch", "refactor",
      "rename", "replace", "adjust", "correct", "tweak", "alter",
      "rewrite function", "change line", "update method"
    ],
    antiKeywords: [
      "create new file", "from scratch", "new file", "overwrite entire",
    ],
    rationale: "Targeted edits within existing files. Best for modifying specific lines, functions, or blocks.",
    alternative: "Write for new files or full file replacements.",
  },

  // ── Built-in: Grep ──
  {
    name: "Grep",
    provider: "builtin",
    useCase: "search",
    keywords: [
      "grep", "search for text", "search for", "find string", "find text",
      "pattern match", "regex", "all occurrences of", "where is X used as text",
      "find in files", "literal search", "exact match", "search code for",
      "search for '", 'search for "', "rg", "ripgrep",
      "text search", "find all '", "find all \"", "find all occurrences",
      "search in files", "search all files", "search files for",
      "find every", "scan for", "scan files", "look for string",
      "look for text", "content search", "search content",
    ],
    antiKeywords: [
      "who calls", "callers", "callees", "impact", "dependencies",
      "call chain", "related symbols", "what calls", "called by",
      "upstream", "downstream", "blast radius",
    ],
    rationale: "Fast regex content search. Best for exact text/string matching across files. NOT for call-chain or dependency queries.",
    alternative: "codegraph_node for callers/callees; codegraph_explore for semantic exploration.",
  },

  // ── Built-in: Glob ──
  {
    name: "Glob",
    provider: "builtin",
    useCase: "search",
    keywords: [
      "find file", "list files", "file pattern", "glob", "which files",
      "where is the file", "locate file", "file name", "*.cs", "*.vue",
      "files named", "files matching", "directory listing", "ls"
    ],
    antiKeywords: [
      "file contents", "inside file", "read file", "search code",
      "file contains", "what's in the file", "show me the code"
    ],
    rationale: "File pattern matching by name. Best for discovering file locations and directory structure.",
    alternative: "Grep for searching file contents; Read for inspecting file contents.",
  },

  // ── Built-in: Bash ──
  {
    name: "Bash",
    provider: "builtin",
    useCase: "execution",
    keywords: [
      "run command", "execute", "shell", "terminal", "build", "compile",
      "git", "dotnet", "npm", "script", "cli",
      "codegraph callers", "codegraph callees", "codegraph impact",
      "codegraph explore", "codegraph node", "codegraph affected",
      "codegraph query", "codegraph sync", "codegraph status",
      "impact analysis", "blast radius", "what would break",
      "what will break", "what breaks", "affected by change",
      "ripple effect", "downstream impact", "side effects",
    ],
    antiKeywords: [
      "search file contents", "read a file", "find text in code",
      "explore without codegraph",
    ],
    rationale: "Execute shell commands. Best for git operations, builds, scripts, and CodeGraph CLI (when you need precise caller/callee lists).",
    alternative: "Use codegraph MCP tools (codegraph_explore/codegraph_node) for code exploration when possible; Bash with codegraph CLI for batch queries.",
  },

  // ── Built-in: TaskCreate ──
  {
    name: "TaskCreate",
    provider: "builtin",
    useCase: "planning",
    keywords: [
      "todo", "task list", "track", "plan tasks", "organize work",
      "task management", "create task"
    ],
    antiKeywords: [],
    rationale: "Create structured task tracking for complex multi-step work.",
    alternative: "Direct execution for single-step tasks.",
  },
];

// ============================================================================
// Synonym Map
// ============================================================================

const SYNONYMS = {
  caller:  ["calls", "invokes", "caller", "calling", "invocation", "upstream", "who calls", "called by", "what calls", "calling function"],
  callee:  ["callee", "calls into", "dependency", "depends on", "downstream", "calls", "what does X call", "invokes"],
  impact:  ["impact", "affects", "affected by", "blast radius", "what breaks", "consequences", "side effects", "what would break", "ripple effect", "change impact"],
  find:    ["find", "search", "locate", "discover", "look for", "where is", "hunt"],
  edit:    ["edit", "modify", "change", "update", "fix", "patch", "refactor", "rename", "adjust", "correct", "tweak", "alter"],
  explore: ["explore", "discover", "understand", "learn about", "what is", "how does", "investigate", "dive into", "look into"],
  read:    ["read", "inspect", "view", "show", "examine", "look at", "see", "display", "print"],
  create:  ["create", "new", "make", "generate", "scaffold", "add", "build"],
};

// ============================================================================
// Intent Patterns
// ============================================================================

const INTENT_PATTERNS = [
  { regex: /(?:who|what)\s+(?:calls|invokes|uses|references)\s+/i,          boost: "codegraph_node" },
  { regex: /(?:find|get|show)\s+(?:all\s+)?(?:callers|references)\s+(?:of|for|to)\s+/i, boost: "codegraph_node" },
  { regex: /(?:what|who)\s+does\s+\w+\s+(?:call|depend|rely)\s/i,           boost: "codegraph_node" },
  { regex: /(?:impact|blast.radius|what\s+(?:would\s+)?break|affected\s+by|side.effects|what\s+happens?\s+if\s+(?:i|we)\s+(?:change|modify|remove|delete))/i, boost: "Bash" },
  { regex: /(?:change|modify|update)\s+\w+.*(?:what|how|impact|affect|break|consequence)/i, boost: "Bash" },
  { regex: /(?:find|get)\s+(?:all\s+)?files?\s+(?:named|called|matching)/i,  boost: "Glob" },
  { regex: /(?:search|grep|find)\s+(?:for\s+)?['"][\w.-]+['"]/i,       boost: "Grep" },
  { regex: /(?:search|grep|find)\s+(?:for\s+)?\w+\s+in\s+(?:all\s+)?(?:files|\*)/i, boost: "Grep" },
  { regex: /(?:search|find|grep)\s+(?:all\s+)?(?:occurrences|matches)\s+of/i, boost: "Grep" },
  { regex: /\b(?:search|grep|find)\b.*\b(?:text|string|pattern|TODO|FIXME|HACK)\b/i, boost: "Grep" },
  { regex: /(?:read|show|display|print)\s+(?:the\s+)?(?:file|contents|code|source)/i, boost: "Read" },
  { regex: /(?:edit|change|modify|fix|update|refactor|rename)\s/i,           boost: "Edit" },
  { regex: /(?:explore|discover|investigate|understand|how\s+(?:does|is|are|do))/i, boost: "codegraph_explore" },
  { regex: /(?:which|what)\s+tool/i,                                         boost: "jnpf_tool_search" },
  { regex: /(?:create|new|make|generate|scaffold)\s+(?:a\s+)?(?:new\s+)?file/i, boost: "Write" },
  { regex: /(?:codegraph)\s+(?:callers|callees|impact|explore|node|query)/i, boost: "Bash" },
];

// ============================================================================
// Scoring Engine
// ============================================================================

function expandQuery(query) {
  const lower = query.toLowerCase();
  const terms = new Set([lower]);

  for (const [root, syns] of Object.entries(SYNONYMS)) {
    for (const syn of syns) {
      if (lower.includes(syn.toLowerCase())) {
        terms.add(root);
        for (const s of syns) {
          if (lower.includes(s.toLowerCase())) terms.add(s);
        }
      }
    }
  }
  return [...terms].join(" ");
}

function scoreTool(tool, query) {
  const lower = query.toLowerCase();
  let score = 0;
  const reasons = [];

  // Dimension 1: Keyword match (0-50)
  let kwCount = 0;
  for (const kw of tool.keywords) {
    const kwLower = kw.toLowerCase();
    if (lower.includes(kwLower)) {
      const isPhrase = kw.includes(" ");
      const pts = isPhrase ? 15 : 10;
      score += pts;
      kwCount++;
      if (reasons.length < 4) reasons.push(`keyword: "${kw}"`);
    }
  }
  // Multi-keyword bonus: 3+ matches = high confidence signal
  if (kwCount >= 3) {
    score += 10;
    reasons.push(`multi-keyword (${kwCount} matches)`);
  }

  // Dimension 2: Intent pattern match (0-30)
  for (const pat of INTENT_PATTERNS) {
    if (pat.regex.test(lower) && pat.boost === tool.name) {
      score += 25;
      reasons.push("intent pattern match");
      break;
    }
  }

  // Secondary: partial intent match (tool name mentioned in patterns for other tools)
  for (const pat of INTENT_PATTERNS) {
    if (pat.regex.test(lower) && pat.boost !== tool.name) {
      // Check if the non-matched tool still has relevant keywords in query
      // This catches cases where both grep and codegraph could apply
      break;
    }
  }

  // Dimension 3: Anti-keyword penalty (-20 to 0)
  let penalty = 0;
  for (const ak of tool.antiKeywords) {
    if (lower.includes(ak.toLowerCase())) {
      penalty += 15;
      reasons.push(`ANTI: "${ak}"`);
    }
  }
  score -= penalty;

  // Normalize to 0-1
  const maxScore = 80;
  const confidence = Math.max(0, Math.min(1, score / maxScore));

  return { score, confidence, reasons };
}

function searchTools(query) {
  if (!query || query.trim().length < 2) {
    return {
      query: query || "",
      results: [],
      recommendation: "Please provide a more specific query describing what you want to do. Examples: 'find what calls ProcessPayment', 'search for TODO in all files', 'explore how authentication works', 'what tool for finding file by name?'",
    };
  }

  const expanded = expandQuery(query);
  const scored = TOOLS.map(tool => ({
    tool: tool.name,
    provider: tool.provider,
    useCase: tool.useCase,
    rationale: tool.rationale,
    alternative: tool.alternative,
    ...scoreTool(tool, expanded),
  }));

  // Sort by confidence desc, MCP preference tiebreaker
  scored.sort((a, b) => {
    if (Math.abs(b.confidence - a.confidence) < 0.05) {
      if (a.provider === "mcp" && b.provider !== "mcp") return -1;
      if (b.provider === "mcp" && a.provider !== "mcp") return 1;
    }
    return b.confidence - a.confidence;
  });

  const top3 = scored.slice(0, 3).map(s => ({
    tool: s.tool,
    provider: s.provider,
    confidence: Math.round(s.confidence * 100) / 100,
    rationale: s.rationale,
    useCase: s.useCase,
    alternative: s.alternative,
  }));

  const best = top3[0];
  const recommendation = best
    ? `Use **${best.tool}** (${best.provider}, confidence: ${Math.round(best.confidence * 100)}%). ${best.rationale}${top3.length > 1 && best.confidence < 0.7 ? ` Consider also: ${top3.slice(1).map(r => r.tool).join(", ")}.` : ""}`
    : "No matching tool found. Try rephrasing your query with more specific intent.";

  return { query, results: top3, recommendation };
}

// ============================================================================
// MCP JSON-RPC 2.0 Server
// ============================================================================

const SERVER_INFO = {
  name: "jnpf-tool-search",
  version: "1.0.0",
};

const TOOL_DEFINITION = {
  name: "jnpf_tool_search",
  description: `Find the right tool for your task using natural language.

This tool routes your intent to the best available tool (MCP or built-in). Use it when:
- Unsure whether to use CodeGraph MCP tools vs. Grep for code exploration
- Needing to find callers/callees of a symbol but don't know the right approach
- Wanting to understand the impact of changing code
- Looking for files by name vs. searching file contents
- New to the JNPF project's available tools

Returns ranked tool recommendations with confidence scores, rationale, and alternatives.

CRITICAL ROUTING RULES:
1. Code exploration (callers, callees, impact, dependencies) → CodeGraph MCP tools, NOT Grep
2. Finding exact text/strings in code → Grep, NOT CodeGraph
3. Finding files by name pattern → Glob, NOT Grep
4. Reading known file contents → Read
5. Making code changes → Edit (targeted) or Write (new file)
6. Running CLI commands (build, git, codegraph CLI) → Bash`,
  inputSchema: {
    type: "object",
    properties: {
      query: {
        type: "string",
        description: "Describe what you're trying to do in natural language. Be specific. Examples: 'find all callers of UserService.CreateUser', 'search for TODO in all .cs files', 'explore how the gate pipeline executes', 'find files named *Controller.cs', 'what tool should I use to check the impact of changing the BaseEntity class?'",
      },
    },
    required: ["query"],
  },
};

function handleRequest(msg) {
  const { method, id } = msg;

  switch (method) {
    case "initialize":
      return {
        jsonrpc: "2.0", id,
        result: {
          protocolVersion: "2024-11-05",
          serverInfo: SERVER_INFO,
          capabilities: { tools: {} },
        },
      };

    case "tools/list":
      return {
        jsonrpc: "2.0", id,
        result: { tools: [TOOL_DEFINITION] },
      };

    case "tools/call": {
      const { name, arguments: args } = msg.params;
      if (name !== "jnpf_tool_search") {
        return {
          jsonrpc: "2.0", id,
          error: { code: -32601, message: `Unknown tool: ${name}` },
        };
      }
      const result = searchTools(args?.query || "");
      return {
        jsonrpc: "2.0", id,
        result: {
          content: [{ type: "text", text: JSON.stringify(result, null, 2) }],
        },
      };
    }

    case "ping":
      return { jsonrpc: "2.0", id, result: {} };

    default:
      return {
        jsonrpc: "2.0", id,
        error: { code: -32601, message: `Method not found: ${method}` },
      };
  }
}

function isNotification(msg) {
  return msg.id === undefined || msg.id === null;
}

// ============================================================================
// Main stdio Loop
// ============================================================================

async function main() {
  const rl = readline.createInterface({
    input: process.stdin,
    output: process.stdout,
    terminal: false,
  });

  // Warm-up: ensure the process is ready
  process.stderr.write("[tool-search] MCP server started\n");

  for await (const line of rl) {
    if (!line.trim()) continue;

    try {
      const msg = JSON.parse(line);
      if (isNotification(msg)) {
        // notifications don't get a response
        if (msg.method === "notifications/initialized") {
          process.stderr.write("[tool-search] Client initialized\n");
        }
        continue;
      }
      const response = handleRequest(msg);
      process.stdout.write(JSON.stringify(response) + "\n");
    } catch (err) {
      process.stderr.write(`[tool-search] Parse error: ${err.message}\n`);
      process.stdout.write(JSON.stringify({
        jsonrpc: "2.0",
        id: null,
        error: { code: -32700, message: `Parse error: ${err.message}` },
      }) + "\n");
    }
  }

  process.stderr.write("[tool-search] Server shutting down\n");
  process.exit(0);
}

main().catch(err => {
  process.stderr.write(`[tool-search] Fatal: ${err.message}\n`);
  process.exit(1);
});
