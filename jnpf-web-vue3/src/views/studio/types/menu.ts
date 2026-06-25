/**
 * Studio 菜单类型定义 (Sprint 1)
 * API 返回 PascalCase，前端直接用 PascalCase
 */
export interface StudioMenuItem {
  Id: number;
  ParentId: number;
  Name: string;
  Icon?: string;
  Url?: string;
  Sort: number;
  Comment?: string;
  DataScope: string;
  ExpandPhase: string;
  BadgeCount: number;
  Children: StudioMenuItem[];
}

export interface TotpVerifyRequest {
  code: string;
}

export interface TotpVerifyResponse {
  success: boolean;
  token?: string;
  message?: string;
}
