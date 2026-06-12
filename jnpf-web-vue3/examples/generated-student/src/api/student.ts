// @jnpf-generated v1.0.0 entity=student type=api
// 生成时间：2026-06-12T15:11:18.006Z
// 此文件由 JNPF 代码生成器生成，可手动修改

/* eslint-disable */
import request from '@/utils/request';
import type { StudentEntity, StudentQueryParams, CreateStudentParams, UpdateStudentParams } from './types';

const BASE_URL = '/api/student';

/** 学生管理 列表查询 */
export function getStudentList(params: StudentQueryParams) {
  return request.get<StudentEntity[]>(`${BASE_URL}/list`, { params });
}

/** 学生管理 详情 */
export function getStudentDetail(id: string) {
  return request.get<StudentEntity>(`${BASE_URL}/${id}`);
}

/** 学生管理 新增 */
export function createStudent(data: CreateStudentParams) {
  return request.post<StudentEntity>(BASE_URL, data);
}

/** 学生管理 更新 */
export function updateStudent(id: string, data: UpdateStudentParams) {
  return request.put(`${BASE_URL}/${id}`, data);
}

/** 学生管理 删除 */
export function deleteStudent(id: string) {
  return request.delete(`${BASE_URL}/${id}`);
}

/** 学生管理 批量删除 */
export function batchDeleteStudent(ids: string[]) {
  return request.delete(`${BASE_URL}/batch`, { data: { ids } });
}
