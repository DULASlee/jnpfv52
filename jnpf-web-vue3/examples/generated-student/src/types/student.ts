// @jnpf-generated v1.0.0 entity=student type=types
// 生成时间：2026-06-13T02:04:04.624Z
// 此文件由 JNPF 代码生成器生成，可手动修改
// 重新生成时，未修改的区域将被覆盖

/* eslint-disable */

/** 学生管理 实体 */
export interface StudentEntity {
  /** 姓名 */
  employeeName: string;
  /** 性别 */
  gender: string;
  /** 出生日期 */
  birthDate?: string;
  /** 邮箱 */
  email?: string;
  /** 手机号 */
  phone?: string;
  /** 部门 */
  department: unknown;
  /** 岗位 */
  position?: unknown;
  /** 直属上级 */
  manager?: unknown;
  /** 入职日期 */
  entryDate: string;
  /** 薪资 */
  salary?: number;
  /** 是否试用期 */
  isProbation?: boolean;
  /** 技能标签 */
  skills?: string[];
  /** 头像 */
  avatar?: string[];
  /** 简历附件 */
  resume?: string[];
}

/** 学生管理 列表查询参数 */
export interface StudentQueryParams {
  currentPage: number;
  pageSize: number;
  /** 搜索：姓名 */
  employeeName?: string;
  /** 搜索：部门 */
  department?: string;
}

/** 学生管理 创建参数 */
export type CreateStudentParams = Omit<StudentEntity, 'id'>;

/** 学生管理 更新参数 */
export type UpdateStudentParams = Partial<StudentEntity>;