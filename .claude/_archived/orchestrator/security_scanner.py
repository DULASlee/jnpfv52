"""
JNPF V3.0 SecurityScanner — 安全扫描引擎 (完整实现)
======================================================
从 guard-write.mjs L3-L8 迁移的正则/AST 扫描逻辑。
状态机 persist_output 在落盘前强制调用，失败即熔断。

扫描层级映射:
  L3: 安全模式 — 硬编码密钥/密码/eval/命令注入
  L5: R4 多租户 — DisableGlobalFilter/Updateable无Where/原生SQL无WHERE
  L6: R7 SQL注入 — DROP/DELETE/SELECT/INSERT/UPDATE 拼接
  L7: R8 API权限 — IDynamicApiController 无权限声明
  L8: R6 前端泄漏 — setTimeout无clear/EventSource无retry
"""
import re
from typing import List, Tuple
from dataclasses import dataclass, field
from pathlib import Path


@dataclass
class SecurityFinding:
    """安全发现"""
    rule_id: str          # SEC-SQL-001 / SEC-TENANT-001 / SEC-AUTH-001 / SEC-LEAK-001
    level: str            # BLOCK / WARN / NOTE
    file: str
    line: int
    message: str
    evidence: str
    fix_hint: str


class SecurityScanner:
    """
    安全守卫：状态机在每个阶段落盘前强制调用。
    L4 模块边界检查由 guard-write.mjs:L4 通过文件路径完成（不在本类中）。
    """

    def __init__(self, project_root: str):
        self.root = Path(project_root)
        self.findings: List[SecurityFinding] = []

    def scan_all(self, changed_files: List[str]) -> Tuple[bool, List[SecurityFinding]]:
        """对所有变更文件执行 L3/L5/L6/L7/L8 安全扫描"""
        self.findings = []

        for filename in changed_files:
            path = self.root / filename
            if not path.exists():
                continue
            try:
                content = path.read_text(encoding='utf-8')
            except Exception:
                continue

            # 按文件类型选择扫描
            if filename.endswith('.cs'):
                self._scan_sql_injection(path, content)
                self._scan_tenant_isolation(path, content)
                self._scan_auth_attributes(path, content)
                self._scan_hardcoded_secrets(path, content)
                self._scan_eval_injection(path, content)

            if filename.endswith(('.vue', '.tsx', '.ts', '.jsx', '.js')):
                self._scan_frontend_leak(path, content)

        blocks = [f for f in self.findings if f.level == "BLOCK"]
        return len(blocks) == 0, self.findings

    # ═══════════════════════════════════════════════════════════
    # L3: 安全模式扫描
    # ═══════════════════════════════════════════════════════════

    def _scan_hardcoded_secrets(self, path: Path, content: str):
        """L3: 硬编码密钥/密码/Token"""
        pattern = re.compile(
            r'(?:api[_-]?key|apikey|secret|token|password|passwd|connectionString)'
            r'\s*[:=]\s*[\'"][A-Za-z0-9_\-!@#$%^&*+=\/]{16,}[\'"]',
            re.IGNORECASE
        )
        lines = content.split('\n')
        for i, line in enumerate(lines, 1):
            if pattern.search(line) and not line.strip().startswith('//'):
                self.findings.append(SecurityFinding(
                    rule_id="SEC-HARDCODE-001",
                    level="BLOCK",
                    file=str(path),
                    line=i,
                    message="硬编码密钥/密码/Token",
                    evidence=line.strip()[:120],
                    fix_hint="使用环境变量或密钥管理服务替代"
                ))

    def _scan_eval_injection(self, path: Path, content: str):
        """L3: eval()/命令注入"""
        lines = content.split('\n')
        for i, line in enumerate(lines, 1):
            if re.search(r'\beval\s*\(', line):
                self.findings.append(SecurityFinding(
                    rule_id="SEC-EVAL-001",
                    level="BLOCK",
                    file=str(path),
                    line=i,
                    message="eval() 动态代码执行 — 代码注入风险",
                    evidence=line.strip()[:120],
                    fix_hint="用 JSON.parse 或白名单替代"
                ))
            if re.search(
                r'\b(child_process\.exec|child_process\.spawn|os\.system|'
                r'subprocess\.call|shell_exec|popen|Process\.Start)\s*\(\s*\$',
                line, re.IGNORECASE
            ):
                self.findings.append(SecurityFinding(
                    rule_id="SEC-CMD-001",
                    level="BLOCK",
                    file=str(path),
                    line=i,
                    message="命令注入 — shell 命令拼接用户输入",
                    evidence=line.strip()[:120],
                    fix_hint="用参数数组形式替代字符串拼接"
                ))

    # ═══════════════════════════════════════════════════════════
    # L5: R4 多租户隔离
    # ═══════════════════════════════════════════════════════════

    def _scan_tenant_isolation(self, path: Path, content: str):
        """L5: 租户隔离扫描"""
        lines = content.split('\n')

        for i, line in enumerate(lines, 1):
            trimmed = line.strip()
            if trimmed.startswith('//') or trimmed.startswith('*'):
                continue
            if re.search(r'r4-safe', line, re.IGNORECASE):
                continue

            # B1: 原生 SQL 无 WHERE
            if (re.search(
                r'(Ado\.SqlQuery|SqlQueryable|GetDataTable)\s*\(\s*\$?@?"'
                r'[^"]*(?:SELECT|select)[^"]*(?:FROM|from)\s+\w+[^"]*"\s*\)',
                line
            ) and not re.search(r'where', line, re.IGNORECASE)):
                self.findings.append(SecurityFinding(
                    rule_id="SEC-TENANT-001",
                    level="BLOCK",
                    file=str(path),
                    line=i,
                    message="原生SQL查询疑似无WHERE子句 — 绕过ITenantFilter",
                    evidence=line.strip()[:150],
                    fix_hint="改用 Queryable<T>().Where(...) 或显式 .Where(\"TenantId = @tid\", ...)"
                ))
                continue

            # B2: DisableGlobalFilter
            if re.search(
                r'DisableGlobalFilter\s*\(\s*"?(TenantFilter|ITenantFilter|Tenant)"?\s*\)',
                line, re.IGNORECASE
            ):
                self.findings.append(SecurityFinding(
                    rule_id="SEC-TENANT-002",
                    level="BLOCK",
                    file=str(path),
                    line=i,
                    message="显式禁用租户全局过滤器 — 完全绕过跨租户隔离",
                    evidence=line.strip()[:150],
                    fix_hint="加注释 // r4-safe: <理由> 豁免，否则 NEVER 这样做"
                ))
                continue

            # B3: Updateable/Deleteable 无 Where
            if re.search(r'\.(Updateable|Deleteable)\s*<', line):
                block = ' '.join(lines[i-1:min(i+5, len(lines))])
                if not re.search(r'\.Where\s*\(', block):
                    self.findings.append(SecurityFinding(
                        rule_id="SEC-TENANT-003",
                        level="BLOCK",
                        file=str(path),
                        line=i,
                        message="Updateable/Deleteable 链未发现 .Where() — 跨租户修改/删除",
                        evidence=line.strip()[:150],
                        fix_hint="MUST 链式调用 .WhereColumns(...) 或 .Where(...) 限定租户范围"
                    ))

    # ═══════════════════════════════════════════════════════════
    # L6: R7 SQL 注入
    # ═══════════════════════════════════════════════════════════

    def _scan_sql_injection(self, path: Path, content: str):
        """L6: SQL 注入扫描 — DROP/DELETE/SELECT/INSERT/UPDATE 拼接"""
        lines = content.split('\n')

        for i, line in enumerate(lines, 1):
            # Pattern 1: DROP/TRUNCATE via string interpolation
            if re.search(
                r'\$"([^"]*\b(DROP\s+(TABLE|DATABASE|INDEX)|TRUNCATE\s+TABLE)\b[^"]*)"',
                line, re.IGNORECASE
            ):
                self.findings.append(SecurityFinding(
                    rule_id="SEC-SQL-001",
                    level="BLOCK",
                    file=str(path),
                    line=i,
                    message="SQL注入: DROP/TRUNCATE via string interpolation",
                    evidence=line.strip()[:200],
                    fix_hint="NEVER concatenate table names into SQL"
                ))
                continue

            # Pattern 2: DELETE FROM via string interpolation
            if re.search(r'\$"([^"]*\bDELETE\s+FROM\b[^"]*)"', line, re.IGNORECASE):
                self.findings.append(SecurityFinding(
                    rule_id="SEC-SQL-002",
                    level="BLOCK",
                    file=str(path),
                    line=i,
                    message="SQL注入: DELETE FROM via string interpolation",
                    evidence=line.strip()[:200],
                    fix_hint="使用参数化查询或 SqlSugar LINQ"
                ))
                continue

            # Pattern 3: SELECT/INSERT/UPDATE via string interpolation
            if re.search(
                r'\$"([^"]*\b(SELECT|INSERT\s+INTO|UPDATE\s+\w+\s+SET)\b[^"]*)"',
                line, re.IGNORECASE
            ):
                self.findings.append(SecurityFinding(
                    rule_id="SEC-SQL-003",
                    level="BLOCK",
                    file=str(path),
                    line=i,
                    message="SQL注入: DML via string interpolation",
                    evidence=line.strip()[:200],
                    fix_hint="使用 SqlSugar.Where() 或 SqlSugarParameter"
                ))
                continue

        # Pattern 4: string.Format with SQL
        if re.search(
            r'string\.Format\(\s*"[^"]*\b(SELECT|INSERT|UPDATE|DELETE|DROP)\b',
            content, re.IGNORECASE
        ):
            self.findings.append(SecurityFinding(
                rule_id="SEC-SQL-004",
                level="BLOCK",
                file=str(path),
                line=1,
                message="SQL注入: string.Format with SQL",
                evidence="string.Format(\"...SQL...\"...)",
                fix_hint="使用参数化查询替代 string.Format"
            ))

        # Pattern 5: Ado.SqlQuery/ExecuteCommand with string interpolation
        if re.search(r'\b(Ado\.SqlQuery|Ado\.ExecuteCommand)\s*\(\s*\$"', content, re.IGNORECASE):
            self.findings.append(SecurityFinding(
                rule_id="SEC-SQL-005",
                level="BLOCK",
                file=str(path),
                line=1,
                message="SQL注入: raw SQL with string interpolation",
                evidence="Ado.SqlQuery($\"...\") or Ado.ExecuteCommand($\"...\")",
                fix_hint="Use parameterized SqlSugarParameter instead"
            ))

    # ═══════════════════════════════════════════════════════════
    # L7: R8 API 权限
    # ═══════════════════════════════════════════════════════════

    def _scan_auth_attributes(self, path: Path, content: str):
        """L7: API 权限 — IDynamicApiController 无权限声明"""
        if not re.search(r':\s*IDynamicApiController\b', content):
            return

        has_security_define = bool(re.search(r'\[SecurityDefine\]', content))
        has_allow_anonymous = bool(re.search(r'\[AllowAnonymous\]', content))
        has_authorize = bool(re.search(r'\[Authorize\]', content))

        if not (has_security_define or has_allow_anonymous or has_authorize):
            self.findings.append(SecurityFinding(
                rule_id="SEC-AUTH-001",
                level="BLOCK",
                file=str(path),
                line=1,
                message="IDynamicApiController 类缺少权限声明 (R8红线)",
                evidence="class ... : IDynamicApiController",
                fix_hint="添加 [AllowAnonymous] / [SecurityDefine(\"权限码\")] / [Authorize]"
            ))

    # ═══════════════════════════════════════════════════════════
    # L8: R6 前端内存泄漏
    # ═══════════════════════════════════════════════════════════

    def _scan_frontend_leak(self, path: Path, content: str):
        """L8: 前端内存泄漏 — setTimeout/EventSource 无清理"""
        # 提取 <script> 块（Vue SFC）
        code = content
        script_match = re.search(r'<script[^>]*>([\s\S]*?)<\/script>', content, re.IGNORECASE)
        if script_match:
            code = script_match.group(1)

        has_set_timeout = bool(re.search(r'\bsetTimeout\s*\(', code))
        has_set_interval = bool(re.search(r'\bsetInterval\s*\(', code))
        has_clear_timeout = bool(re.search(r'\bclearTimeout\s*\(', code))
        has_clear_interval = bool(re.search(r'\bclearInterval\s*\(', code))
        has_on_unmounted = bool(re.search(
            r'\bonUnmounted\s*[\(\{]|\bonBeforeUnmount\s*[\(\{]', code
        ))

        if has_set_timeout and not has_clear_timeout and not has_on_unmounted:
            self.findings.append(SecurityFinding(
                rule_id="SEC-LEAK-001",
                level="BLOCK",
                file=str(path),
                line=1,
                message="setTimeout 无 clearTimeout/onUnmounted — 内存泄漏风险 (R6)",
                evidence="setTimeout(...) without cleanup",
                fix_hint="保存返回值到变量，在 onUnmounted 中 clearTimeout"
            ))

        if has_set_interval and not has_clear_interval and not has_on_unmounted:
            self.findings.append(SecurityFinding(
                rule_id="SEC-LEAK-002",
                level="BLOCK",
                file=str(path),
                line=1,
                message="setInterval 无 clearInterval/onUnmounted — 严重内存泄漏 (R6)",
                evidence="setInterval(...) without cleanup",
                fix_hint="保存返回值到变量，在 onUnmounted 中 clearInterval"
            ))

        has_event_source = bool(re.search(r'\bnew\s+EventSource\s*\(', code))
        if has_event_source:
            if not has_on_unmounted:
                self.findings.append(SecurityFinding(
                    rule_id="SEC-LEAK-003",
                    level="BLOCK",
                    file=str(path),
                    line=1,
                    message="EventSource 无 onUnmounted 清理 (R6)",
                    evidence="new EventSource(...) without cleanup",
                    fix_hint="在 onUnmounted 中调用 .close()"
                ))
            if not re.search(r'MAX_RETRIES|maxRetries|retryCount|reconnectLimit', code):
                self.findings.append(SecurityFinding(
                    rule_id="SEC-LEAK-004",
                    level="WARN",
                    file=str(path),
                    line=1,
                    message="EventSource 重连无 retry 上限 (R6.3)",
                    evidence="EventSource without MAX_RETRIES",
                    fix_hint="添加 MAX_RETRIES=5 + retryCount 计数器"
                ))


class SecurityGateBlocked(Exception):
    """安全门拦截异常"""
    def __init__(self, message: str, findings: List[SecurityFinding] = None):
        super().__init__(message)
        self.findings = findings or []
