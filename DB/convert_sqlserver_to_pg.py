#!/usr/bin/env python3
"""SQL Server → PostgreSQL 转换脚本
处理 SSMS 导出的完整脚本：CREATE TABLE + INSERT + CREATE INDEX
"""
import re
import sys
from collections import OrderedDict

def read_sqlserver_file(path):
    """读取 UTF-16LE 编码的 SQL Server 脚本"""
    with open(path, encoding='utf-16-le') as f:
        content = f.read()
    # 去掉 BOM
    if content.startswith('\ufeff'):
        content = content[1:]
    # 统一换行符为 \n
    content = content.replace('\r\n', '\n').replace('\r', '\n')
    return content

def skip_block_level_statements(block):
    """跳过块内的会话设置语句和注释行"""
    lines = block.split('\n')
    kept = []
    skip_patterns = [
        r'^SET\s+(ANSI|QUOTED|IDENTITY_INSERT)',
        r'^/\*.*Object:.*\*/',
        r'^USE\s+\[',
        r'^PRINT\s+',
    ]
    for line in lines:
        stripped = line.strip()
        skip = False
        for pat in skip_patterns:
            if re.match(pat, stripped, re.I):
                skip = True
                break
        if not skip:
            kept.append(line)
    return '\n'.join(kept)

def convert_type(sqlserver_type):
    """转换 SQL Server 数据类型到 PostgreSQL"""
    t = sqlserver_type.strip().lower()
    # nvarchar/varchar
    m = re.match(r'(nvarchar|varchar|nchar|char)\s*\((\d+)\)', t)
    if m:
        size = int(m.group(2))
        return f'varchar({size})' if m.group(1) in ('varchar', 'nvarchar') else f'char({size})'
    if re.match(r'(nvarchar|varchar)\s*\(max\)', t):
        return 'text'
    if t in ('nvarchar', 'varchar', 'nchar', 'char'):
        return 'text' if 'var' in t else 'char(1)'
    # datetime
    if t in ('datetime', 'datetime2', 'smalldatetime'):
        return 'timestamp'
    if t == 'date':
        return 'date'
    if t == 'time':
        return 'time'
    # 整数
    if t == 'int':
        return 'integer'
    if t == 'bigint':
        return 'bigint'
    if t == 'smallint':
        return 'smallint'
    if t == 'tinyint':
        return 'smallint'
    if t == 'bit':
        return 'smallint'  # JNPF 用 0/1，不用 boolean
    # decimal/numeric
    m = re.match(r'(decimal|numeric)\s*\((\d+),\s*(\d+)\)', t)
    if m:
        return f'numeric({m.group(2)},{m.group(3)})'
    if re.match(r'(decimal|numeric)\s*\(\d+\)', t):
        m2 = re.match(r'(decimal|numeric)\s*\((\d+)\)', t)
        return f'numeric({m2.group(2)},0)'
    if t in ('decimal', 'numeric'):
        return 'numeric'
    # float
    if t == 'float':
        return 'double precision'
    if t == 'real':
        return 'real'
    # money
    if t in ('money', 'smallmoney'):
        return 'numeric(19,4)'
    # text
    if t in ('text', 'ntext'):
        return 'text'
    # uniqueidentifier
    if t == 'uniqueidentifier':
        return 'varchar(50)'
    # binary
    if t in ('binary', 'varbinary', 'image', 'timestamp'):
        return 'bytea'
    # xml
    if t == 'xml':
        return 'xml'
    # 默认：返回原类型名（可能已经是 PG 类型）
    return sqlserver_type

def parse_create_table(block):
    """解析 CREATE TABLE 块，返回表名和列定义列表"""
    # 提取表名
    m = re.match(r'CREATE TABLE \[dbo\]\.\[([^\]]+)\]\s*\((.*)\)\s*(?:ON|TEXTIMAGE)', block, re.S | re.I)
    if not m:
        # 尝试不匹配 ON/TEXTIMAGE
        m = re.match(r'CREATE TABLE \[dbo\]\.\[([^\]]+)\]\s*\((.*)\)', block, re.S | re.I)
        if not m:
            return None, None, None
    
    table_name = m.group(1)
    body = m.group(2)
    
    # 分割列/约束定义（按逗号分割，但要处理括号内的逗号）
    parts = []
    depth = 0
    current = ''
    for char in body:
        if char == '(':
            depth += 1
            current += char
        elif char == ')':
            depth -= 1
            current += char
        elif char == ',' and depth == 0:
            parts.append(current.strip())
            current = ''
        else:
            current += char
    if current.strip():
        parts.append(current.strip())
    
    columns = []
    constraints = []
    primary_key_cols = None
    
    for part in parts:
        part = part.strip()
        if not part:
            continue
        
        # CONSTRAINT ... PRIMARY KEY
        if re.match(r'CONSTRAINT\s+\[?[\w]+\]?\s+PRIMARY\s+KEY', part, re.I):
            pk_match = re.search(r'PRIMARY\s+KEY\s+(?:CLUSTERED\s+)?\(([^)]+)\)', part, re.I)
            if pk_match:
                cols = re.findall(r'\[([^\]]+)\]', pk_match.group(1))
                primary_key_cols = cols
            continue
        
        # 裸 PRIMARY KEY (cols)
        if re.match(r'PRIMARY\s+KEY', part, re.I):
            pk_match = re.search(r'PRIMARY\s+KEY\s+(?:CLUSTERED\s+)?\(([^)]+)\)', part, re.I)
            if pk_match:
                cols = re.findall(r'\[([^\]]+)\]', pk_match.group(1))
                primary_key_cols = cols
            continue
        
        # CONSTRAINT ... DEFAULT
        if re.match(r'CONSTRAINT\s+\[?[\w]+\]?\s+DEFAULT', part, re.I):
            # 提取默认值和列名
            dm = re.match(r'CONSTRAINT\s+\[?[\w]+\]?\s+DEFAULT\s+\(([^)]*)\)\s+FOR\s+\[?(\w+)\]?', part, re.I)
            if dm:
                default_val = dm.group(1).strip()
                col_name = dm.group(2)
                constraints.append(('default', col_name, default_val))
            continue
        
        # 列定义: [colname] [type](args) [IDENTITY] [NULL|NOT NULL] [DEFAULT]
        col_match = re.match(
            r'\[([^\]]+)\]\s+(?:\[(\w+)\]|(\w+))\s*(\([^)]*\))?\s*'
            r'(IDENTITY\s*\([^)]*\))?\s*'
            r'(NOT\s+NULL|NULL)?\s*'
            r'(DEFAULT\s+\([^)]*\))?',
            part, re.I
        )
        if col_match:
            col_name = col_match.group(1)
            type_name = col_match.group(2) or col_match.group(3)
            type_args = col_match.group(4) or ''
            identity = col_match.group(5)
            nullable = col_match.group(6) or ''
            default = col_match.group(7)
            
            full_type = type_name + type_args
            pg_type = convert_type(full_type)
            
            col_def = {
                'name': col_name.lower(),
                'type': pg_type,
                'nullable': nullable.upper().strip() if nullable else '',
                'identity': bool(identity),
                'default': '',
            }
            
            if default:
                dm = re.match(r'DEFAULT\s+\(([^)]*)\)', default, re.I)
                if dm:
                    col_def['default'] = convert_default_value(dm.group(1).strip())
            
            columns.append(col_def)
        else:
            # 无法解析的约束，记录
            constraints.append(('unknown', '', part))
    
    return table_name.lower(), columns, (primary_key_cols, constraints)

def convert_default_value(val):
    """转换默认值"""
    val = val.strip()
    if val.upper() == 'GETDATE()':
        return "now()"
    if re.match(r"^\d+$", val):
        return val
    if re.match(r"^N?'[^']*'$", val):
        return val.lstrip('N')
    if val.upper() == 'NULL':
        return 'NULL'
    return val

def generate_pg_create_table(table_name, columns, pk_info):
    """生成 PostgreSQL CREATE TABLE 语句"""
    primary_key_cols, constraints = pk_info
    
    lines = [f'CREATE TABLE IF NOT EXISTS {table_name} (']
    
    col_lines = []
    default_map = {}  # col_name -> default
    
    for c in constraints:
        if c[0] == 'default':
            default_map[c[1].lower()] = c[2]
    
    for col in columns:
        parts = [f'  {col["name"]}', col['type']]
        
        if col['identity']:
            # IDENTITY → GENERATED BY DEFAULT AS IDENTITY (允许显式插入)
            parts.append('GENERATED BY DEFAULT AS IDENTITY')
        elif col['default'] or col['name'] in default_map:
            dv = col['default'] or default_map[col['name']]
            parts.append(f'DEFAULT {dv}')
        
        if col['nullable']:
            parts.append(col['nullable'])
        
        col_lines.append(' '.join(parts))
    
    if primary_key_cols:
        pk_cols = ', '.join(c.lower() for c in primary_key_cols)
        col_lines.append(f'  PRIMARY KEY ({pk_cols})')
    
    lines.append(',\n'.join(col_lines))
    lines.append(');')
    return '\n'.join(lines)

def convert_insert(line):
    """转换 INSERT 语句"""
    # INSERT [dbo].[table] ([col1], [col2]) VALUES (v1, v2)
    m = re.match(r'INSERT\s+\[dbo\]\.\[([^\]]+)\]\s*\(([^)]*)\)\s*VALUES\s*\((.*)\)', line, re.I | re.S)
    if not m:
        return None
    
    table = m.group(1).lower()
    cols_raw = m.group(2)
    values_raw = m.group(3)
    
    # 提取列名
    cols = re.findall(r'\[([^\]]+)\]', cols_raw)
    cols_str = ', '.join(c.lower() for c in cols)
    
    # 转换值
    values_str = convert_values(values_raw)
    
    return f'INSERT INTO {table} ({cols_str}) VALUES ({values_str});'

def convert_values(values_raw):
    """转换 VALUES 部分"""
    result = ''
    i = 0
    while i < len(values_raw):
        # N'字符串'
        if values_raw[i:i+2] == "N'":
            end = values_raw.index("'", i+2)
            result += "'" + values_raw[i+2:end] + "'"
            i = end + 1
        # 普通字符串 '...'
        elif values_raw[i] == "'":
            # 找到配对的引号（处理转义）
            j = i + 1
            while j < len(values_raw):
                if values_raw[j] == "'":
                    if j + 1 < len(values_raw) and values_raw[j+1] == "'":
                        j += 2
                        continue
                    break
                j += 1
            result += values_raw[i:j+1]
            i = j + 1
        # CAST(N'...' AS DateTime)
        elif values_raw[i:i+5].upper() == 'CAST(':
            cast_end = find_matching_paren(values_raw, i)
            cast_content = values_raw[i+5:cast_end]
            # N'2026-...' AS DateTime
            dm = re.match(r"N?'([^']*)'\s+AS\s+(\w+)", cast_content, re.I)
            if dm:
                val = dm.group(1)
                typ = dm.group(2).lower()
                if 'datetime' in typ or typ == 'date' or typ == 'time':
                    # 转换日期格式 T → 空格
                    val = val.replace('T', ' ')
                    result += f"'{val}'::timestamp"
                else:
                    result += f"'{val}'::{typ}"
            else:
                result += values_raw[i:cast_end+1]
            i = cast_end + 1
        else:
            result += values_raw[i]
            i += 1
    return result

def find_matching_paren(s, start):
    """找到匹配的右括号位置"""
    depth = 0
    for i in range(start, len(s)):
        if s[i] == '(':
            depth += 1
        elif s[i] == ')':
            depth -= 1
            if depth == 0:
                return i
    return len(s) - 1

def convert_create_index(line):
    """转换 CREATE INDEX 语句"""
    m = re.match(r'CREATE\s+(NONCLUSTERED\s+|CLUSTERED\s+|UNIQUE\s+)*INDEX\s+\[([^\]]+)\]\s+ON\s+\[dbo\]\.\[([^\]]+)\]\s*\(([^)]*)\)', line, re.I)
    if not m:
        return None
    is_unique = bool(m.group(1) and 'UNIQUE' in m.group(1).upper())
    idx_name = m.group(2).lower()
    table = m.group(3).lower()
    cols_raw = m.group(4)
    cols = re.findall(r'\[([^\]]+)\]', cols_raw)
    cols_str = ', '.join(c.lower() for c in cols)
    unique = 'UNIQUE ' if is_unique else ''
    return f'CREATE {unique}INDEX IF NOT EXISTS {idx_name} ON {table} ({cols_str});'

def process_file(input_path, output_path, db_name):
    """处理单个 SQL Server 脚本文件"""
    print(f'读取: {input_path}', file=sys.stderr)
    content = read_sqlserver_file(input_path)
    print(f'内容长度: {len(content)} 字符', file=sys.stderr)
    
    # 先按 GO 分割成块（GO 单独一行）
    blocks = re.split(r'\nGO\s*\n', content)
    print(f'分割为 {len(blocks)} 个块', file=sys.stderr)
    
    output = []
    output.append(f'-- PostgreSQL 转换脚本')
    output.append(f'-- 源文件: {input_path.split("/")[-1]}')
    output.append(f'-- 目标数据库: {db_name}')
    output.append(f'-- 转换时间: 2026-06-21')
    output.append('')
    output.append('BEGIN;')
    output.append('')
    
    create_count = 0
    insert_count = 0
    index_count = 0
    skip_count = 0
    
    for block in blocks:
        block = block.strip()
        if not block:
            continue
        
        # 跳过块内的会话设置和注释
        block = skip_block_level_statements(block).strip()
        if not block:
            continue
        
        # CREATE TABLE
        if re.match(r'CREATE TABLE', block, re.I):
            table_name, columns, pk_info = parse_create_table(block)
            if table_name and columns:
                output.append(generate_pg_create_table(table_name, columns, pk_info))
                output.append('')
                create_count += 1
            else:
                print(f'  [WARN] 无法解析 CREATE TABLE: {block[:80]}', file=sys.stderr)
                skip_count += 1
        
        # INSERT
        elif re.match(r'INSERT\s+\[dbo\]', block, re.I):
            for line in block.split('\n'):
                line = line.strip()
                if re.match(r'INSERT', line, re.I):
                    converted = convert_insert(line)
                    if converted:
                        output.append(converted)
                        insert_count += 1
        
        # SET IDENTITY_INSERT ON/OFF
        elif re.match(r'SET IDENTITY_INSERT', block, re.I):
            continue  # 跳过
        
        # CREATE INDEX
        elif re.match(r'CREATE\s+(NONCLUSTERED|CLUSTERED|UNIQUE)?\s*INDEX', block, re.I):
            for line in block.split('\n'):
                line = line.strip()
                converted = convert_create_index(line)
                if converted:
                    output.append(converted)
                    index_count += 1
        
        # ALTER TABLE (添加约束)
        elif re.match(r'ALTER TABLE', block, re.I):
            # 简单跳过 ALTER TABLE，约束已在 CREATE TABLE 里处理
            skip_count += 1
        
        else:
            skip_count += 1
    
    output.append('')
    output.append('COMMIT;')
    output.append('')
    
    with open(output_path, 'w', encoding='utf-8') as f:
        f.write('\n'.join(output))
    
    print(f'转换完成: {output_path}', file=sys.stderr)
    print(f'  CREATE TABLE: {create_count}', file=sys.stderr)
    print(f'  INSERT: {insert_count}', file=sys.stderr)
    print(f'  CREATE INDEX: {index_count}', file=sys.stderr)
    print(f'  跳过: {skip_count}', file=sys.stderr)

if __name__ == '__main__':
    # 主库
    process_file(
        '/Users/huanyuan/JNPF-AI/jnpfv52/DB/ZXAPINIT.SQL',
        '/tmp/zxapinit_pg.sql',
        'zxaf_v1_devtest1'
    )
    print()
    # 调度库
    process_file(
        '/Users/huanyuan/JNPF-AI/jnpfv52/DB/jnpf_sundial_INIT.sql',
        '/tmp/jnpf_sundial_pg.sql',
        'jnpf_sundial'
    )
