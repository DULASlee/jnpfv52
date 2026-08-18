// mssql 类型声明兜底（M1b · mssql follow-up）
//
// 根因：mssql@12.7.0 的 package.json 无 "types"/"typings" 字段，node_modules/mssql 下无任何 .d.ts 文件。
// @types/mssql（DefinitelyTyped）对应 mssql <6 的回调式 API，与 12.x Promise API 不兼容，故不采用。
//
// 兜底：声明 SqlServerSADatabase 实际使用的最小成员（ConnectionPool/Request class + NVarChar/BigInt/Int/MAX）。
// 成员内部用 any（运行时 mssql API 正确，仅类型检查降级）。
// 影响面：SqlServerSADatabase 是可选后端（SA_DB_BACKEND=sqlserver，默认 inmemory），类型降级不影响主链。
//
// 后续可选：升级 mssql 到自带类型的版本，或为 12.x 向 DefinitelyTyped 贡献完整 .d.ts（真实类型优先）。
declare module 'mssql' {
  export class ConnectionPool {
    constructor(connectionString: string);
    connected: boolean;
    connect(): Promise<void>;
    request(): Request;
  }
  export class Request {
    input(name: string, type: any, value?: any): Request;
    query(command: string): Promise<{ recordset: any[] }>;
  }
  export const NVarChar: (length?: number | 'MAX') => any;
  export const BigInt: any;
  export const Int: any;
  export const MAX: 'MAX';
}
