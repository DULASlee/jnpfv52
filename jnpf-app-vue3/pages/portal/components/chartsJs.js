import jnpf from '@/utils/jnpf'
import {
	geojson,
	getAtlas
} from '@/api/portal/portal.js'
import {
	getDataInterfaceRes
} from '@/api/common'
let timer;
export default {
	data() {
		return {
			show: false,
			mapData: [],
			mapList: [],
			mapType: '',
			markPoints: [],
			pointLoading: false,
			opts: {
				update: true,
				fontSize: 10,
				padding: [40, 15, 30, 15],
				extra: {
					map: {
						mercator: true,
					},
				},
			},
			regionStep: [],
			stepMapList: [],
			option: {}
		}
	},
	created() {
		this.init()
		if (this.config.dataType === 'dynamic') {
			uni.$off('proRefresh')
			uni.$on('proRefresh', () => {
				this.handelChart()
			})
		}
	},
	methods: {
		init() {
			if (timer) clearInterval(timer)
			this.handelChart()
			if (!this.config.allRefresh.autoRefresh && this.config?.refresh?.autoRefresh) {
				timer = setInterval(this.handelChart, this.config.refresh.autoRefreshTime * 60000)
			}
		},
		async handelChart() {
			this.customEchartsKey()
			if (this.config.dataType === 'dynamic') {
				if (!this.config.propsApi) return
				const query = {
					paramList: this.config.templateJson
				};
				getDataInterfaceRes(this.config.propsApi, query).then(res => {
					this.config.option.defaultValue = this.handleMappingConfig(res.data, this.config)
					this.handleAttrs()
				})
			} else {
				this.handleAttrs()
			}
		},
		// 自定义echarts属性取app的
		customEchartsKey() {
			this.config.option = {
				...this.config.option,
				...this.config.appOption
			}
			if (['customEcharts'].includes(this.config.jnpfKey)) {
				this.config.dataType = this.config.appDataType
				this.config.propsApi = this.config.appPropsApi
				this.config.propsName = this.config.appPropsName
				this.config.templateJson = this.config.appTemplateJson
				this.config.refresh = this.config.appRefresh ? this.config.appRefresh : {}
			}
			this.config.option.urlAddress = this.config.option.appUrlAddress
		},
		// 处理字段映射
		handleMappingConfig(resultData, config) {
			let defaultValue = undefined
			if (['rankList', 'barChart', 'pieChart', 'tableList'].includes(this.config.jnpfKey)) {
				defaultValue = this.config.option.defaultValue || []
			}
			let mappingConfig = config.mappingConfig
			let result = []

			resultData.forEach(data => {
				let temp = {}
				for (let item of mappingConfig) {
					if (item.field === '系列') {
						temp['type'] = item.value ? data[item.value] : data['type']
					}
					if (item.field === '维度') {
						temp['name'] = item.value ? data[item.value] : data['name']
					}
					if (item.field === '数值') {
						temp['value'] = item.value ? data[item.value] : data['value']
					}
					if (item.field === '最大值') {
						temp['max'] = item.value ? data[item.value] : data['max']
					}
					if (item.field === '时间戳') {
						temp['timestamp'] = item.value ? data[item.value] : data['timestamp']
					}
					if (item.field === '经度') {
						temp['long'] = item.value ? data[item.value] : data['long']
					}
					if (item.field === '纬度') {
						temp['lat'] = item.value ? data[item.value] : data['lat']
					}
				}
				result.push(temp)
			})
			return result
		},
		handleParam(e) {
			let jnpfKey = this.config.jnpfKey
			if (jnpfKey == 'customEcharts') return
			let item = {}
			let choose = e.opts.series && e.opts.series[0]
			let chooseIndex = e.currentIndex['index']
			let name = ''
			let type = ''
			let value = ''
			if (jnpfKey === "barChart") {
				name = e.opts.categories[chooseIndex]
				value = choose.data[chooseIndex]
				type = choose.name
			}
			if (jnpfKey === "pieChart") {
				name = choose.name
				value = choose.data
				let series = this.option.chartData.series && this.option.chartData.series[0]
				type = series.name
			}
			if (jnpfKey === "lineChart") {
				name = e.opts.categories[chooseIndex]
				value = choose.data[chooseIndex]
				type = choose.name
			}
			if (jnpfKey === "radarChart") {
				chooseIndex = e.currentIndex
				name = e.opts.categories[chooseIndex]
				value = choose.data[chooseIndex]
				type = choose.name
			}
			if (jnpfKey === "mapChart" && choose.properties.childrenNum == 0) {
				name = choose.properties.name
				item.long = choose.properties.center[0]
				item.lat = choose.properties.center[1]
			}
			if (chooseIndex < 0 && jnpfKey !== "mapChart") return
			if (jnpfKey === "mapChart" && choose.properties.childrenNum != 0) return
			item.name = name
			item.value = value
			item.type = type
			item.urlAddress = this.config.option.urlAddress
			this.jnpf.solveAddressParam(item, this.config)
			this.jnpf.jumpLink(item.urlAddress)
		},
		handleAttrs() {
			let chartTitle = {
				titleText: this.config.option.titleText, //主标题
				titleTextStyleColor: this.config.option.titleTextStyleColor, //主标题字体颜色
				titleTextStyleFontSize: this.config.option.titleTextStyleFontSize * 2 + 'rpx', //主标题字体大小[12-25px]
				titleTextStyleFontWeight: this.config.option.titleTextStyleFontWeight ? 700 : 0, //主标题是否加粗
				titleLeft: this.config.option.titleLeft === 'left' ? 'flex-start' : this.config.option
					.titleLeft === 'right' ? 'flex-end' : 'center', //主子标题位置[left,center,right]
				titleBgColor: this.config.option.titleBgColor, //主子标题背景色[rgba(),#303133]
				// 图表副标题设置
				titleSubtext: this.config.option.titleSubtext, //子标题
				titleSubtextStyleColor: this.config.option.titleSubtextStyleColor, //子标题字体颜色
				titleSubtextStyleFontSize: this.config.option.titleSubtextStyleFontSize * 2 +
					'rpx', //子标题字体大小[12-25px]
				titleSubtextStyleFontWeight: this.config.option.titleSubtextStyleFontWeight ? 700 : 0, //子标题是否加粗
			}
			if (['customEcharts'].includes(this.config.jnpfKey)) {
				this.$nextTick(() => {
					this.show = true
					this.key = +new Date()
				})
				return
			}

			let defVal = this.config.option.defaultValue
			let chartVal = JSON.parse(JSON.stringify(defVal)) || []
			let typeArr = Array.from(new Set(chartVal.map((item) => item.type)))
			let axisData = Array.from(new Set(chartVal.map((item) => item.name)))
			let seriesData = []
			let colorList = [];
			let chartsType = "";
			let Ymin = []; //y轴最小
			let Ymax = []; //y轴最大
			let type = "";
			let yAxis = {};
			let xAxis = {};
			if (!['mapChart', 'customEcharts'].includes(this.config.jnpfKey)) {
				typeArr.forEach((title, index) => {
					const type = this.getType(title, this.config)
					let obj = {
						name: title,
						type: type
					}
					if (this.config.option.seriesLabelShow) {
						obj.textSize = (this.config.option.seriesLabelFontSize / 2) < 12 ? 12 : this.config
							.option
							.seriesLabelFontSize / 2
						obj.textColor = this.config.option.seriesLabelColor
					}
					let chartArr = chartVal.filter((item) => title === item.type)
					if (this.config.jnpfKey === 'barChart' || this.config.jnpfKey === 'lineChart' || this
						.config
						.jnpfKey ==
						'radarChart') {
						obj['data'] = chartArr.map((item) => item.value)
					} else {
						obj['data'] = chartArr.map((item) => {
							return {
								value: item.value,
								name: item.name,
							}
						})
						if (this.config.option.showZero) obj['data'] = obj['data'].filter((item) => item
							.value != 0)
						this.key = +new Date
					}
					Ymin.push(Math.min(...obj['data']))
					Ymax.push(Math.max(...obj['data']))
					seriesData.push(obj);
				})
				xAxis = {
					disabled: !this.config.option.xAxisShow, //不绘制X轴
					axisLine: this.config.option.xAxisShow, //绘制坐标轴轴线
					axisLineColor: !this.config.option.xAxisShow ? '#fff' : this.config.option
						.xAxisAxisLineLineStyleColor, //坐标轴轴线颜色，默认#CCCCCC
					title: "", //X轴标题
					titleFontSize: this.config.option.xAxisNameTextStyleFontSize, //标题字体大小
					titleFontColor: this.config.option.xAxisNameTextStyleColor, //X轴名称字体颜色
					titleOffsetY: -20, //标题纵向偏移距离，负数为向上偏移，正数向下偏移
					titleOffsetX: -300, //标题横向偏移距离，负数为向左偏移，正数向右偏移
					fontSize: (this.config.option.xAxisAxisLabelTextStyleFontSize / 2) < 14 ? 14 : this.config
						.option
						.xAxisAxisLabelTextStyleFontSize / 2, //X轴标签字体大小
					fontColor: this.config.option.xAxisAxisLabelTextStyleColor, //X轴标签字体颜色
					rotateAngle: this.config.option.xAxisAxisLabelRotate, ////X轴标签角度
					rotateLabel: this.config.option.xAxisAxisLabelRotate > 0 ? true : false, //【旋转】数据点（刻度点）文字
					gridColor: this.config.option.xAxisSplitLineLineStyleColor, //纵向网格颜色，默认#CCCCCC
					splitNumber: 4, //X轴网格数量，纵向网格数量(竖着的)
					disableGrid: !this.config.option.xAxisShow ? !this.config.option.xAxisShow : !this.config
						.option.xAxisSplitLineShow, //不绘制纵向网格(即默认绘制网格)
					scrollShow: axisData.length > 5 ? true : false, //是否显示滚动条，配合拖拽滚动使用（即仅在启用 enableScroll 时有效
					scrollAlign: "left", //滚动条初始位置
					scrollColor: "#A6A6A6", //滚动条颜色，默认#A6A6A6
					scrollBackgroundColor: "#EFEBEF", //滚动条底部背景颜色，默认#EFEBEF
					itemCount: axisData.length > 5 ? 6 : 5, //单屏数据密度即图表可视区域内显示的X轴数据点数量，仅在启用enableScroll时有效
				};
				yAxis = {
					data: [{
						position: "left",
						title: "",
						min: this.config.option.styleType == 6 ? -jnpf.toRound(Math.abs(Math.min(...
								Ymin)
							.toString().length > 2 ? Math.min(...Ymin) - 200 : Math
							.min(
								...Ymin) - 50)) : 0,
						max: jnpf.toRound(Math.max(...Ymax)),
						axisLine: true, //坐标轴轴线是否显示（数据还能显示）
						axisLineColor: this.config.option
							.yAxisAxisLineLineStyleColor, //坐标轴轴线颜色，默认#CCCCCC
						disabled: !this.config.option.yAxisShow, //不绘制Y轴（刻度和轴线都不绘制）
						fontColor: this.config.option
							.yAxisAxisLabelTextStyleColor, //数据点（刻度点）字体颜色，默认#666666
						fontSize: (this.config.option.yAxisAxisLabelTextStyleFontSize / 2) < 12 ? 12 : this
							.config
							.option.yAxisAxisLabelTextStyleFontSize / 2, //数据点（刻度点）字体颜色，默认#666666
					}],
					padding: 10, //多个Y轴间的间距
					gridSet: "number", //横向向网格数量类型设置,可选值,'auto','array'
					disableGrid: !this.config.option.yAxisSplitLineShow, //不绘制横向向网格(即默认绘制网格)
					splitNumber: 4, //【指定数量】的横向向网格数量，此数量与Y轴数据点是否为小数有关，如果指定了max，请指定为能被max-min整除的数值
					gridType: "solid", //横向向网格线型
					gridColor: this.config.option.yAxisSplitLineLineStyleColor, //横向网格颜色，默认#CCCCCC
				};
				this.config.option.colorList.forEach((o, i) => {
					if (o.color1) colorList.push(o.color1)
				})
			}
			let opts = {
				color: colorList, //主题颜色，16进制颜色格式，Array格式
				padding: [15, 15, 0, 15], //画布填充边距[上,右,下,左]，Array格式
				enableScroll: axisData.length > 5 ? true :
				false, //开启图表可拖拽滚动，开启后ontouch需要赋值为true，X轴配置里需要配置itemCount单屏幕数据点数量
				dataLabel: this.config.option.seriesLabelShow, //是否显示图表区域内数据点上方的数据文案
				legend: {
					fontColor: this.config.jnpfKey === 'pieChart' ? "#666666" : "",
					show: this.config.option.legendShow, //是否显示图例标识
					position: 'top',
					float: 'right',
					fontSize: (this.config.option.legendTextStyleFontSize / 2) < 14 ? 14 : this.config.option
						.legendTextStyleFontSize / 2
				},
				extra: {
					tooltip: {
						showBox: this.config.option.tooltipShow, //提示语显示
						fontSize: (this.config.option.tooltipTextStyleFontSize / 2) < 14 ? 14 : this.config
							.option
							.tooltipTextStyleFontSize / 2, //提示语字体大小
						fontColor: this.config.option.tooltipTextStyleColor, //提示语字体颜色
						bgColor: this.config.option.tooltipBgColor || '#000000' //提示窗口的背景颜色
					},
					mix: {
						column: {}
					},
					column: {
						type: 'group',
						width: this.config.option.seriesBarWidth, //柱体宽度
						activeBgColor: "#000000",
						activeBgOpacity: 0.08,
						linearType: "none",
						barBorderRadius: [this.config.option.seriesItemStyleBarBorderRadius, this.config.option
							.seriesItemStyleBarBorderRadius, this.config.option
							.seriesItemStyleBarBorderRadius,
							this.config.option.seriesItemStyleBarBorderRadius
						], //自定义4个圆角半径[左上,右上,右下,左下]
						seriesGap: 5, //多series每个柱子之间的间距
						customColor: colorList, //扩展渐变色
						barBorderCircle: false
					}
				}
			}
			if (this.config.jnpfKey === 'barChart') {
				if (this.config.option.styleType == 5 || this.config.option.styleType == 1 ||
					this.config.option.styleType == 4 || this.config.option.styleType == 6) {
					type = 'group'
					if (this.config.option.styleType == 6) {
						opts.extra.column.barBorderRadius = []

						// opts.extra.column.width = 50
						// yAxis.splitNumber = (yAxis.data[0].max / 500) + (Math.abs(yAxis.data[0].min) / 500)
					}
				} else {
					type = 'stack'
					opts.extra.mix.column = {
						width: this.config.option.seriesBarWidth, //柱体宽度
						barBorderCircle: false, //启用分组柱状图半圆边框
						barBorderRadius: [this.config.option.seriesItemStyleBarBorderRadius, this.config.option
							.seriesItemStyleBarBorderRadius, this.config.option
							.seriesItemStyleBarBorderRadius,
							this.config.option.seriesItemStyleBarBorderRadius
						], //自定义4个圆角半径[左上,右上,右下,左下]
					}
				}
				chartsType = this.config.option.styleType == 7 ? 'mix' : 'column'
				opts.xAxis = {
					...xAxis
				}
				opts.yAxis = {
					...yAxis
				}
				opts.extra.column.type = type
			} else if (this.config.jnpfKey === "pieChart") {
				chartsType = 'pie'
				opts.fontColor = this.config.option.seriesLabelColor
				opts.fontSize = (this.config.option.seriesLabelFontSize / 2) < 14 ? 14 : this.config.option
					.seriesLabelFontSize / 2
				let pieChartObj = {
					borderColor: "#FFFFFF",
					borderWidth: 3,
					activeOpacity: 0.5,
					offsetAngle: -90,
					labelWidth: 15,
					border: false,
				}
				let pieChartObj2 = {
					offsetX: 0,
					offsetY: 0,
					name: "",
				}
				if (this.config.option.styleType == 1) {
					if (this.config.option.roseType) chartsType = 'rose'
					opts.extra[chartsType] = {
						...pieChartObj
					}
				} else {
					chartsType = 'ring';
					opts.title = {
						fontSize: 15,
						color: "#666666",
						...pieChartObj2
					};
					opts.subtitle = {
						fontSize: 25,
						color: "#7cb5ec",
						...pieChartObj2
					}
					opts.extra[chartsType] = {
						ringWidth: 60,
						activeRadius: 10,
						...pieChartObj
					}
				}
			} else if (this.config.jnpfKey === "lineChart") {
				chartsType = this.config.option.areaStyle ? 'area' : 'line'
				type = this.config.option.styleType == 2 ? 'curve' : this.config.option.styleType == 3 ? 'step' :
					'straight'
				let lineChartObj = {
					type: type,
					width: this.config.option.seriesLineStyleWidth
				}
				opts.extra[chartsType] = {
					...lineChartObj
				}
				opts.xAxis = {
					...xAxis
				}
				opts.yAxis = {
					...yAxis
				}
			} else if (this.config.jnpfKey === "radarChart") {
				chartsType = "radar";
				type = this.config.option.styleType == 1 ? chartsType : 'circle'
				opts.fontSize = (this.config.option.radarAxisNameFontSize / 2) < 14 ? 14 : this.config.option
					.radarAxisNameFontSize / 2
				opts.fontColor = this.config.option.seriesLabelColor
				opts.extra[chartsType] = {
					gridType: type,
					gridColor: "#CCCCCC",
					gridCount: 5,
					opacity: 0.2,
					max: 200,
					labelShow: true,
					border: true,
					max: 100,
					labelColor: this.config.option.radarAxisNameColor
				}
			} else {
				this.getAtlas()
				chartsType = "map";
				this.config.option.markPoints = []
				opts = {
					update: true,
					fontSize: this.config.option.geoLabelFontSize,
					padding: [15, 15, 30, 15],
					dataLabel: this.config.option.geoLabelShow,
					fontColor: this.config.option.geoLabelColor,
					extra: {
						tooltip: {
							showBox: this.config.option.tooltipShow,
							fontColor: this.config.option.tooltipTextStyleColor || '#000',
							//fontSize: this.config.option.tooltipTextStyleFontSize,
							bgColor: this.config.option.tooltipBgColor || '#fff',
							bgOpacity: 1
						},
						map: {
							mercator: false,
							border: true,
							borderWidth: this.config.option.geoBorderWidth / 2,
							borderColor: this.config.option.geoBorderColor,
							activeBorderColor: "#F04864",
							activeFillColor: "#FACC14",
							activeFillOpacity: 1,
							active: true,
							activeTextColor: "#FFFFFF",
							fillOpacity: 1
						},
					},
				}
				this.config.option.defaultValue.forEach(o => {
					this.config.option.markPoints.push({
						latitude: o.lat,
						longitude: o.long,
						name: o.name,
						value: o.value
					})
				})
			}
			let chartData = {
				categories: axisData,
				series: seriesData,
				opts,
				type: chartsType
			}
			if (chartsType != 'map') {
				if (chartTitle.titleText && chartTitle.titleSubtext) {
					chartData.opts.legend.padding = 50
				} else if (chartTitle.titleText && !chartTitle.titleSubtext) {
					chartData.opts.legend.padding = 30
				} else {
					chartData.opts.legend.padding = 5
				}
			}
			this.config.option.chartData = chartData
			this.config.option.chartTitle = chartTitle
			this.option = this.config.option
			this.$nextTick(() => {
				this.show = true
				this.key = +new Date()
			})
		},
		/* 获取地图树 */
		getAtlas() {
			getAtlas().then(res => {
				this.mapList = jnpf.treeToArray(res.data);
				this.regionStep = []
				this.drawChina()
			})
		},
		/* 获取地图数据 */
		drawChina(type) {
			if (Array.isArray(this.option.mapType) && !type) this.mapType = this.option.mapType[this.option.mapType
				.length - 1]
			geojson(this.mapType).then(res => {
				let series = JSON.parse(JSON.stringify(res.data.features)) || [];
				for (var i = 0; i < series.length; i++) {
					if (series[i].geometry.type === 'Polygon' && !type) {
						if (this.mapType == 150000) {
							series[i].geometry.coordinates = series[i].geometry.coordinates
						} else {
							series[i].geometry.coordinates = [series[i].geometry.coordinates]
						}
					}
					series[i].value = Math.floor(Math.random() * 1000)
					series[i].color = this.option.geoAreaColor
				}
				this.mapData = series
				this.stepMapList.push(this.mapData)
				this.option.chartData.series = series
				this.key = +new Date()
			})
		},
		/* 地图点击路径 */
		regionStepClick(e, index) {
			if (index == 0) {
				this.stepMapList = []
				this.regionStep = []
				for (let i = 0; i < this.mapList.length; i++) {
					if (this.mapList[i].enCode == this.option.mapType[this.option.mapType.length - 1]) {
						this.regionStep.push({
							name: this.mapList[i].fullName,
							adcode: this.mapList[i].enCode
						})
					}
				}
				this.drawChina()
				return
			}
			this.mapType = e.adcode
			this.stepMapList.splice(index + 1, this.regionStep.length - index - 1)
			this.regionStep.splice(index + 1, this.regionStep.length - index - 1)
			this.drawChina(1)
		},
		/* 地图点击 */
		getIndex(e) {
			// 处理跳转参数
			this.handleParam(e)

			let current = this.mapList.filter(o => o.enCode == this.option.mapType[this.option.mapType.length - 1])
			if (!this.config.option.drillDown) return
			let mapData = this.mapData[e.currentIndex] ? this.mapData[e.currentIndex] : []
			this.mapType = mapData.properties.adcode
			let acroutes = mapData.properties.acroutes
			let selectName = mapData.properties.name
			let adcode = mapData.properties.adcode
			this.regionStep.unshift({
				name: current[0].fullName,
				adcode: current[0].enCode
			})
			this.regionStep.push({
				name: selectName,
				adcode: adcode
			})
			//对象数组去重
			this.regionStep = this.regionStep.filter((a, b, c) => {
				return c.findIndex(x => x.name === a.name) === b
			})
			this.pointLoading = false
			this.drawChina(1)
		},
		getType(title) {
			if (this.config.jnpfKey == 'barChart') {
				if (this.config.option.styleType == 7) {
					const arr = this.config.option.barType.find(ele => title == ele.title)
					if (arr && arr.type) {
						if (arr.type == 'bar') return 'column'
						return arr.type
					}
				}
				return 'column'
			} else if (this.config.jnpfKey == 'lineChart') {
				return 'line'
			} else if (this.config.jnpfKey == 'pieChart') {
				return 'pie'
			} else {
				return 'radar'
			}
		},
		/* 设置地图点 */
		setPoints(e) {
			const mapData = e.opts.chartData.mapData;
			this.option.markPoints = this.option.markPoints.slice(0, this.option.appShowNumber)
			for (var i = 0; i < this.option.markPoints.length; i++) {
				const points = this.coordinateToPoint(this.option.markPoints[i].longitude, this.option.markPoints[i]
					.latitude, mapData
					.bounds,
					mapData.scale, mapData.xoffset, mapData.yoffset, mapData.mercator)
				this.option.markPoints[i].x = points.x;
				this.option.markPoints[i].y = points.y;
			}
			this.pointLoading = true
			this.$refs.charts.key = +new Date();
		},
		/* 经纬度转画布坐标 */
		coordinateToPoint(longitude, latitude, bounds, scale, xoffset, yoffset, mercator) {
			var x = longitude;
			var y = latitude;
			if (mercator == true) {
				x = longitude * 20037508.34 / 130;
				y = Math.log(Math.tan((90 + latitude) * Math.PI / 360)) / (Math.PI / 180);
				y = y * 20037508.34 / 180;
			}
			return {
				x: (x - bounds.xMin) * scale + xoffset,
				y: (bounds.yMax - y) * scale + yoffset
			};
		}
	}
}