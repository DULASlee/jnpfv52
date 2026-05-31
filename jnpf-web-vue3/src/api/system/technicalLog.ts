import { defHttp } from '/@/utils/http/axios';

enum Api {
  Prefix = '/api/system/technical-log',
}

// 获取错误日志列表
export function getErrorLogList(params) {
  return defHttp.get({ url: Api.Prefix + '/errors', params });
}

// 获取慢请求日志列表
export function getSlowRequestList(params) {
  return defHttp.get({ url: Api.Prefix + '/slow-requests', params });
}

// 根据 TraceId 获取全链路日志
export function getTraceDetail(traceId: string) {
  return defHttp.get({ url: Api.Prefix + '/trace/' + traceId });
}
