import { defHttp } from '/@/utils/http/axios';

enum Api {
  Prefix = '/api/extend/OperationLog',
}

export function getOperationLogList(data) {
  return defHttp.get({ url: Api.Prefix, data });
}

export function delOperationLog(data) {
  return defHttp.delete({ url: Api.Prefix, data });
}

export function getOperationLogInfo(id) {
  return defHttp.get({ url: Api.Prefix + '/' + id });
}
