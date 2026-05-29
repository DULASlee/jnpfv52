// 将对象序列化为查询字符串
function stringifyQuery(obj) {
	let ret = [];
	for (let key in obj) {
		if (obj.hasOwnProperty(key)) {
			ret.push(encodeURIComponent(key) + '=' + encodeURIComponent(obj[key]));
		}
	}
	return ret.join('&');
}
export function refreshCurrentPage() {
	// 获取当前页面的路径
	let currentPage = getCurrentPages().pop();
	if (currentPage) {
		// 重启当前页面
		uni.reLaunch({
			url: `/${currentPage.route}` + (currentPage.options ? `?${stringifyQuery(currentPage.options)}` :
				'')
		});
	}
}