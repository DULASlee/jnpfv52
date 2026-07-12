import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const dir = path.dirname(fileURLToPath(import.meta.url));
const out = path.join(dir, 'business-locker-requirement.txt');

const block = `智能更衣柜管理系统业务需求（验收用大附件）

一、业务背景
企业厂区需要统一管理员工更衣柜分配、开柜、归还与权限回收，支持人脸/指纹开柜，管理员可远程授权与审计。

二、角色
1. 员工：申请柜位、开柜、查看本人柜位
2. 管理员：分配柜位、回收权限、查看操作日志
3. 设备运维：登记设备、处理故障、查看设备状态

三、业务事件（必须覆盖）
EV-001 员工申请柜位；EV-002 管理员审批分配；EV-003 人脸开柜；EV-004 指纹开柜；EV-005 柜位归还；EV-006 权限回收；EV-007 操作审计查询；EV-008 设备故障上报；EV-009 远程开柜授权；EV-010 柜位盘点。

四、数据实体
User、Cabinet、CabinetUnit、Permission、OperationLog、Device、DeviceLog、PermissionAssignment。
字段需包含主键、外键、状态、时间戳。

五、非功能
并发开柜、审计可追溯、附件需求全文入库不得截断。

`;

let s = '';
while (s.length < 22_000) s += block + '\n';
fs.writeFileSync(out, s, 'utf8');
console.log('wrote', out, fs.statSync(out).size, 'bytes');
