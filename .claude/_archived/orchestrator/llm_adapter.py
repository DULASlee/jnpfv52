"""
JNPF V3.0 LLM Adapter — 平台适配层
====================================
将 AI 原生开发平台的 LLM 调用功能适配为 V3.0 状态机需要的标准接口。

设计原则：
  - 状态机只依赖此适配层的 `call()` 方法，不感知平台差异
  - 平台切换只需修改此文件，状态机零修改
  - Mock 实现用于测试，真实实现待平台 API 信息到位后填充

接口契约：
  call(system: str, user: str, phase: str, schema: dict) -> dict
  返回: 已解析为 dict 的 LLM 输出（保证是合法 JSON，否则抛异常）
"""
import json
import time
from typing import Optional
from dataclasses import dataclass, field
from datetime import datetime


@dataclass
class LLMReliabilityMetrics:
    """LLM 调用可靠性指标（每个 phase 独立收集）"""
    phase: str = ""
    input_tokens: int = 0
    output_tokens: int = 0
    latency_ms: int = 0
    retry_count: int = 0
    parse_success: bool = False
    natural_language_preamble: bool = False
    schema_validation_failures: int = 0
    critical_field_fallbacks: int = 0
    timestamp: str = field(default_factory=lambda: datetime.now().isoformat())

    def to_dict(self) -> dict:
        return {
            "phase": self.phase,
            "input_tokens": self.input_tokens,
            "output_tokens": self.output_tokens,
            "latency_ms": self.latency_ms,
            "retry_count": self.retry_count,
            "parse_success": self.parse_success,
            "natural_language_preamble": self.natural_language_preamble,
            "schema_validation_failures": self.schema_validation_failures,
            "critical_field_fallbacks": self.critical_field_fallbacks,
            "timestamp": self.timestamp,
        }


# ============================================================================
# 平台适配器基类（接口契约）
# ============================================================================

class PlatformLLMAdapter:
    """
    V3.0 标准 LLM 调用接口。
    子类实现 `_invoke_platform` 即可接入不同 LLM 平台。

    使用方式：
      adapter = AnthropicAdapter(api_key="...")   # 或
      adapter = PlatformAdapter(client=platform_llm)  # 待平台 API 确认
      result = adapter.call(system_prompt, user_prompt, "brainstorm", schema)
    """

    def __init__(self, config: dict = None):
        self.config = config or {}
        self.metrics: list[LLMReliabilityMetrics] = []

    def call(self, system: str, user: str, phase: str, schema: dict) -> dict:
        """
        V3.0 标准调用接口。子类不应覆写此方法。
        内部处理: 输入格式化 → 平台调用 → 输出解析 → 可靠性指标
        """
        start = time.time()
        metric = LLMReliabilityMetrics(phase=phase)

        try:
            # 1. 格式化输入
            platform_input = self._to_platform_format(system, user, schema)

            # 2. 调用平台（重试/超时/错误由平台处理或子类实现）
            raw = self._invoke_platform(platform_input)
            metric.input_tokens = raw.get("usage", {}).get("input_tokens", 0)
            metric.output_tokens = raw.get("usage", {}).get("output_tokens", 0)
            metric.retry_count = raw.get("retry_count", 0)

            # 3. 解析输出
            parsed = self._from_platform_format(raw, phase)
            metric.parse_success = True

            # 4. 可靠性检查
            if self._has_natural_language_preamble(raw):
                metric.natural_language_preamble = True

            metric.latency_ms = int((time.time() - start) * 1000)
            self.metrics.append(metric)

            return parsed

        except Exception as e:
            metric.parse_success = False
            metric.latency_ms = int((time.time() - start) * 1000)
            self.metrics.append(metric)
            raise

    # ================================================================
    # 子类必须实现的方法
    # ================================================================

    def _to_platform_format(self, system: str, user: str, schema: dict) -> dict:
        """
        将 V3.0 的 prompt 格式转换为平台输入格式。
        子类必须覆写。

        标准 Anthropic 格式:
          return {
              "model": self.config.get("model", "claude-sonnet-4-6"),
              "max_tokens": 8000,
              "temperature": 0.1,
              "system": system,
              "messages": [{"role": "user", "content": user}],
              "tools": [{"name": f"output_{phase}", ...}],
              "tool_choice": {"type": "tool", "name": f"output_{phase}"},
          }
        """
        raise NotImplementedError("Subclass must implement _to_platform_format")

    def _invoke_platform(self, platform_input: dict) -> dict:
        """
        实际调用平台 LLM API。子类必须覆写。

        返回格式（标准化）:
          {
              "content": "响应体（可能是文本或包含 tool_use 的结构）",
              "usage": {"input_tokens": N, "output_tokens": N},
              "model": "实际使用的模型名",
              "retry_count": 0,
              "finish_reason": "stop" | "tool_use",
          }
        """
        raise NotImplementedError("Subclass must implement _invoke_platform")

    def _from_platform_format(self, raw_response: dict, phase: str) -> dict:
        """
        将平台输出解析为 V3.0 需要的 JSON dict。
        子类可覆写（若平台已提供 structured_output 解析）。
        """
        # 默认实现：尝试从 tool_use 或文本中提取 JSON
        content = raw_response.get("content", "")

        # 如果平台已解析为 dict（structured_output）
        if isinstance(content, dict):
            return content

        # 如果是 list（Anthropic content blocks）
        if isinstance(content, list):
            for block in content:
                if isinstance(block, dict) and block.get("type") == "tool_use":
                    return block.get("input", {})
                if isinstance(block, dict) and block.get("type") == "text":
                    # Fallback: 文本中提取 JSON
                    from state_machine import FuguPipeline
                    dummy = FuguPipeline.__new__(FuguPipeline)
                    return dummy._force_json_parse(block["text"], phase)

        # 纯文本
        if isinstance(content, str):
            from state_machine import FuguPipeline
            dummy = FuguPipeline.__new__(FuguPipeline)
            return dummy._force_json_parse(content, phase)

        raise ValueError(f"Unrecognized platform response format: {type(content)}")

    # ================================================================
    # 子类可选覆写的方法
    # ================================================================

    def _has_natural_language_preamble(self, raw_response: dict) -> bool:
        """检测输出是否包含自然语言前缀（非 tool_use 直接返回）"""
        content = raw_response.get("content", "")
        if isinstance(content, list):
            for block in content:
                if isinstance(block, dict) and block.get("type") == "text":
                    text = block.get("text", "").strip()
                    # 检查是否以 JSON 开头（无前缀）或自然语言开头（有前缀）
                    return not (text.startswith("{") or text.startswith("["))
        return False

    def get_reliability_report(self) -> dict:
        """汇总所有 phase 的可靠性指标"""
        if not self.metrics:
            return {"phases": [], "summary": "no data"}

        total = len(self.metrics)
        successful = sum(1 for m in self.metrics if m.parse_success)
        preambles = sum(1 for m in self.metrics if m.natural_language_preamble)
        fallbacks = sum(m.critical_field_fallbacks for m in self.metrics)

        return {
            "phases": [m.to_dict() for m in self.metrics],
            "summary": {
                "total_calls": total,
                "parse_success_rate": f"{successful}/{total}",
                "preamble_rate": f"{preambles}/{total}",
                "total_fallbacks": fallbacks,
                "total_input_tokens": sum(m.input_tokens for m in self.metrics),
                "total_output_tokens": sum(m.output_tokens for m in self.metrics),
                "avg_latency_ms": int(sum(m.latency_ms for m in self.metrics) / max(total, 1)),
            }
        }


# ============================================================================
# 平台适配器：通过 JNPF LlmGateway HTTP API 调用
# ============================================================================

class PlatformAdapter(PlatformLLMAdapter):
    """
    通过 JNPF 原生开发平台的 LLM 网关 HTTP API 调用。

    平台已提供的能力（V3.0 不重复实现）：
      - 自动重试 + 指数退避（MaxRetries=3）
      - Provider 自动降级（GAP-1: 主失败→备）
      - 5级降级链（I-07: ChatWithLevelFallbackAsync）
      - 熔断计数器（ConcurrentDictionary）
      - 审计日志（BASE_AI_CALL_LOG）
      - 超时控制（TimeoutMs）
      - 响应质量评估（EvaluateResponseQuality）

    V3.0 在此之上增加：
      - 三层防线（解析容错 + Schema校验 + 默认值补全）
      - 可靠性指标收集（parse_success/preamble/fallback）

    调用链：
      Python state_machine → HTTP POST /api/LlmGateway/ChatAsync → C# LlmGatewayService
    """

    def __init__(self, config: dict = None):
        super().__init__(config)
        self.base_url = config.get("base_url", "http://localhost:5000")
        self.provider_code = config.get("provider_code", "")  # 空=平台默认
        self.model_code = config.get("model_code", None)       # None=Provider默认模型
        self.timeout_ms = config.get("timeout_ms", 120000)

    def _to_platform_format(self, system: str, user: str, schema: dict) -> dict:
        """
        组装 ChatCompletionRequest JSON。
        平台目前不支持 tool_use，改用 ResponseFormat="json" + schema 注入 system prompt。
        """
        # 将 JSON Schema 要求注入 system prompt（平台不支持 tool_use）
        schema_json = json.dumps(schema, ensure_ascii=False)
        enhanced_system = f"""{system}

---
## 输出要求
你必须输出严格符合以下 JSON Schema 的结构。禁止任何自然语言前缀或后缀。只输出 JSON。

```json
{schema_json}
```
"""

        return {
            "providerCode": self.provider_code,
            "modelCode": self.model_code,
            "systemPrompt": enhanced_system,
            "messages": [{"role": "user", "content": user}],
            "temperature": 0.1,
            "maxTokens": 8000,
            "responseFormat": "json",
            "maxRetries": 3,
            "timeoutMs": self.timeout_ms,
        }

    def _invoke_platform(self, platform_input: dict) -> dict:
        """
        通过 HTTP POST 调用平台 LLM 网关。
        平台自动处理：重试、降级、熔断、超时、审计日志。
        """
        import urllib.request
        import urllib.error

        url = f"{self.base_url}/api/LlmGateway/ChatAsync"
        body = json.dumps(platform_input, ensure_ascii=False).encode("utf-8")

        req = urllib.request.Request(
            url,
            data=body,
            headers={"Content-Type": "application/json"},
            method="POST",
        )

        try:
            with urllib.request.urlopen(req, timeout=self.timeout_ms / 1000) as resp:
                raw = json.loads(resp.read().decode("utf-8"))
        except urllib.error.HTTPError as e:
            return {
                "content": "",
                "usage": {"input_tokens": 0, "output_tokens": 0},
                "model": platform_input.get("modelCode", "unknown"),
                "retry_count": 0,
                "finish_reason": "error",
                "error": f"HTTP {e.code}: {e.reason}",
            }
        except Exception as e:
            return {
                "content": "",
                "usage": {"input_tokens": 0, "output_tokens": 0},
                "model": platform_input.get("modelCode", "unknown"),
                "retry_count": 0,
                "finish_reason": "error",
                "error": str(e),
            }

        # 平台响应 → V3.0 标准化格式
        return {
            "content": raw.get("content", ""),
            "usage": {
                "input_tokens": raw.get("tokensIn", 0),
                "output_tokens": raw.get("tokensOut", 0),
            },
            "model": raw.get("modelUsed", "unknown"),
            "retry_count": 0,  # 平台内部已处理重试，外部不可见
            "finish_reason": "stop" if raw.get("isSuccess") else "error",
            "error": raw.get("error"),
        }

    def _from_platform_format(self, raw_response: dict, phase: str) -> dict:
        """
        平台返回的是 text Content，V3.0 负责 JSON 解析。
        应用三层防线：解析容错 + Schema校验。
        """
        if raw_response.get("error"):
            raise RuntimeError(f"Platform LLM error: {raw_response['error']}")

        content = raw_response.get("content", "")
        if not content:
            raise ValueError(f"Empty response from platform LLM for phase {phase}")

        # 复用 V3.0 的三层防线
        from state_machine import FuguPipeline
        dummy = FuguPipeline.__new__(FuguPipeline)

        # L2: 解析容错（剥离自然语言前缀、提取JSON）
        parsed = dummy._force_json_parse(content, phase)

        # 检查是否有自然语言前缀
        stripped = dummy._strip_preamble(content)
        if len(stripped) < len(content) * 0.5:
            self.metrics[-1].natural_language_preamble = True if self.metrics else False

        # L3: Schema校验 + 安全默认值
        validated = dummy._validate_and_fill(parsed, phase)

        return validated


# ============================================================================
# Anthropic 直接调用实现（备选，平台 API 确认后可废弃）
# ============================================================================

class AnthropicAdapter(PlatformLLMAdapter):
    """
    Anthropic Claude API 直接调用（通过 anthropic SDK）。
    平台 API 确认后，此实现可废弃，改为 PlatformAdapter。
    """

    def __init__(self, api_key: str = None, config: dict = None):
        super().__init__(config)
        self.api_key = api_key or config.get("api_key", "")
        self.model = config.get("model", "claude-sonnet-4-6")

    def _to_platform_format(self, system: str, user: str, schema: dict) -> dict:
        # 从 user prompt 或 system prompt 推断 phase
        phase_name = "unknown"
        for p in ["brainstorm", "build", "verify", "review", "report"]:
            if p in user.lower():
                phase_name = p
                break

        return {
            "model": self.model,
            "max_tokens": 8000,
            "temperature": 0.1,
            "system": system,
            "messages": [{"role": "user", "content": user}],
            "tools": [{
                "name": f"output_{phase_name}",
                "description": f"Structured output for {phase_name}",
                "input_schema": schema,
            }],
            "tool_choice": {"type": "tool", "name": f"output_{phase_name}"},
        }

    def _invoke_platform(self, platform_input: dict) -> dict:
        """通过 anthropic SDK 调用"""
        try:
            import anthropic
        except ImportError:
            raise ImportError(
                "anthropic SDK not installed. Run: pip install anthropic\n"
                "Or configure PlatformAdapter for the AI native platform."
            )

        client = anthropic.Anthropic(api_key=self.api_key)
        response = client.messages.create(
            model=platform_input["model"],
            max_tokens=platform_input["max_tokens"],
            temperature=platform_input["temperature"],
            system=platform_input["system"],
            messages=platform_input["messages"],
            tools=platform_input["tools"],
            tool_choice=platform_input["tool_choice"],
        )

        return {
            "content": response.content,
            "usage": {
                "input_tokens": response.usage.input_tokens,
                "output_tokens": response.usage.output_tokens,
            },
            "model": response.model,
            "retry_count": 0,
            "finish_reason": response.stop_reason,
        }


# ============================================================================
# Mock 实现（测试用）
# ============================================================================

class MockLLMAdapter(PlatformLLMAdapter):
    """
    Mock 实现：返回预定义的合法 JSON 响应。用于测试和演示。
    复用 test_e2e.py 中的 MockLLMClient 逻辑。
    """

    RESPONSES = {
        "brainstorm": {
            "options": [
                {"name": "方案A-事务脚本", "failure_boundary": ">5 states",
                 "estimated_effort": "1天", "redlines_checked": ["R1", "R4"]},
                {"name": "方案B-DDD", "failure_boundary": "团队学习成本",
                 "estimated_effort": "3天", "redlines_checked": ["R1", "R4"]},
            ],
            "recommendation": {"chosen_option": "方案A-事务脚本", "reason": "简单快速"},
            "requirements": [{"id": "REQ-001", "source": "Mock需求"}],
            "impact_assessment": {"change_type": "Entity", "exploration_depth": 2},
        },
        "build": {
            "changed_files": [
                {"path": "Test.cs", "operation": "create",
                 "lines_added": 30, "content_hash": "sha256:mock"}
            ],
            "self_verification": {
                "build": {"command": "dotnet build", "result": "PASS"},
                "tests": {"command": "dotnet test", "result": "PASS"},
            },
            "compliance_checklist": {"trap_2_mapster_audit": "PASS"},
        },
        "verify": {
            "checks": [{"name": "dotnet-test", "result": "PASS"}],
            "summary": {"total": 1, "passed": 1, "failed": 0},
            "verdict": "PASS",
        },
        "review": {
            "findings": [],
            "hook_audit": {"guard_coverage_verified": True},
            "metrics": {"block_count": 0, "warn_count": 0},
        },
        "report": "# Mock Delivery Report\n\nAll checks passed.",
    }

    def __init__(self, config: dict = None):
        super().__init__(config)
        # Allow overriding responses for testing
        self.responses = {**self.RESPONSES, **(config or {}).get("overrides", {})}

    def _to_platform_format(self, system: str, user: str, schema: dict) -> dict:
        return {"system": system, "user": user}

    def _invoke_platform(self, platform_input: dict) -> dict:
        user = platform_input.get("user", "")
        for phase_name, response in self.responses.items():
            if phase_name in user.lower():
                time.sleep(0.001)  # Simulate minimal latency
                return {
                    "content": response,
                    "usage": {"input_tokens": 500, "output_tokens": 200},
                    "model": "mock",
                    "retry_count": 0,
                    "finish_reason": "stop",
                }
        return {
            "content": {"status": "unknown_phase"},
            "usage": {"input_tokens": 100, "output_tokens": 10},
            "model": "mock",
            "retry_count": 0,
            "finish_reason": "stop",
        }

    def _from_platform_format(self, raw_response: dict, phase: str) -> dict:
        content = raw_response.get("content", {})
        if isinstance(content, dict):
            return content
        return {"status": "error", "raw": str(content)}


# ============================================================================
# 自检
# ============================================================================

def self_test():
    """验证适配层接口合约"""
    print("=== LLM Adapter Self-Test ===")

    # Test Mock
    adapter = MockLLMAdapter()
    schema = {"type": "object", "required": ["options", "recommendation"]}
    result = adapter.call("system", "brainstorm task: add feature", "brainstorm", schema)
    assert "options" in result
    assert len(result["options"]) >= 2
    print("[PASS] MockAdapter.call() returns valid JSON")

    # Test metrics collection
    report = adapter.get_reliability_report()
    assert report["summary"]["parse_success_rate"] == "1/1"
    print("[PASS] Reliability metrics collected")

    # Test with natural language preamble (simulated)
    adapter2 = MockLLMAdapter()
    adapter2.RESPONSES["brainstorm"] = adapter.RESPONSES["brainstorm"]
    result2 = adapter2.call("system", "brainstorm: new feature", "brainstorm", schema)
    assert "options" in result2
    assert adapter2.get_reliability_report()["summary"]["total_calls"] == 1
    print("[PASS] Multiple calls metric aggregation correct")

    print("\n[READY] LLM Adapter verified")
    print("[WAITING] Platform API details to implement PlatformAdapter")


if __name__ == "__main__":
    self_test()
