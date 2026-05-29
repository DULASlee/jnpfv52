/* process.env.NODE_ENV设置生产环境模式 */
// #ifndef MP
const baseURL = process.env.NODE_ENV === "production" ? "" : "http://localhost:5000"
const webSocketUrl = process.env.NODE_ENV === "production" ? "/websocket" :
	"ws://localhost:5000/api/message/websocket"
const report = process.env.NODE_ENV === 'production' ? '/Report' : 'http://localhost:8200'
const flow = process.env.NODE_ENV === 'production' ? '' : 'http://localhost:3100'
// #endif

// #ifdef MP
const baseURL = "http://localhost:5000"
const webSocketUrl = "ws://localhost:5000/api/message/websocket"
const report = 'http://localhost:8200'
const flow = 'http://localhost:3100'
// #endif

const define = {
	copyright: "Copyright @ 2026 面包树科技有限公司版权所有",
	sysVersion: "V5.2",
	baseURL, // 接口前缀
	report,
	flow,
	webSocketUrl,
	comUploadUrl: baseURL + '/api/file/Uploader/',
	timeout: 1000000,
	aMapWebKey: '09485f01587712b3c04e5a9abf324237',
	cipherKey: 'EY8WePvjM5GGwQzn', // 加密key
}
export default define
