<template>
	<view class="">
		<view class="u-m-b-20">
			<HCard :cardData="cardData">
				<template #content>
					<view class="sale-module">
						<view class="sale-module-box u-flex">
							<view class="head-tabs u-flex"
								:style="{'justify-content':list.length>3?'flex-start':'space-between'}">
								<view class="head-tabs-item" v-for="(item,index) in list" :key="index">
									<view class="u-flex-col">
										<text class="u-font-24">{{item.text1}}</text>
										<text class="u-font-28" style="color: #1890FF;">{{item.text2}}</text>
										<text class="u-font-20" style="color: #008B4F;">{{item.text3}}</text>
									</view>
								</view>
							</view>
						</view>
					</view>
				</template>
			</HCard>
		</view>
		<view class="u-m-b-20">
			<HCard>
				<template #content>
					<view class="sale-module">
						<view class="sale-module-box u-flex">
							<view class="head-tabs u-flex"
								:style="{'justify-content':list2.length>3?'flex-start':'space-between'}">
								<view class="head-tabs-item" v-for="(item,index) in list2" :key="index">
									<view class="u-flex-col head-tabs-inner">
										<view class="img" :class="item.icon" :style="{'background':item.bgColor}">
										</view>
										<text class="u-font-24 u-m-b-10 u-m-t-10">{{item.text1}}</text>
										<text class="u-font-28" style="font-weight: 700;">{{item.text3}}</text>
									</view>
								</view>
							</view>
						</view>
					</view>
				</template>
			</HCard>
		</view>
		<view class="u-m-b-20">
			<HCard :cardData='cardData2'>
				<template #content>
					<view class="charts-box">
						<qiun-data-charts type="tarea" :opts="opts" :chartData="chartData" :ontouch="true" />
					</view>
				</template>
			</HCard>
		</view>
		<view class="u-m-b-20">
			<HCard :cardData='cardData3'>
				<template #content>
					<view class="notice-v">
						<view class="notice-box">
							<view class="notice-inner u-flex" v-for="(item,index) in noticeList" :key="index">
								<view class="l"
									:style="{'color':item.type==1?'#093FF4':'#05ED05','background-color':item.type==1?'#e5ebfe':'#e6ffe5'}">
									{{item.title}}
								</view>
								<view class="c u-line-1 u-font-28">{{item.content}}</view>
								<span class="r u-font-24">{{item.time}}</span>
							</view>
						</view>
					</view>
				</template>
			</HCard>
		</view>
		<view class="u-m-b-20">
			<HCard :cardData='cardData4'>
				<template #content>
					<view class="proList-v">
						<view class="proList-box">
							<view class="proList-inner" v-for="(item,index) in proListList" :key="index">
								<view class="u-flex u-m-b-20 t">
									<u-image width="40rpx" height="40rpx" :src="item.imgUrl" class="img"></u-image>
									<text class="u-font-28 u-m-l-10 title">{{item.title}}</text>
									<text class="u-font-24 time">5{{item.time}}</text>
								</view>
								<view class="c u-line-3">
									{{item.content}}
								</view>
								<view class="b">
									<span>项目组名：{{item.name}}</span>
								</view>
							</view>
						</view>
					</view>
				</template>
			</HCard>
		</view>
	</view>
</template>
<script>
	import HCard from './HCard'
	import img1 from '@/pages/portal/static/image/1.png'
	import img2 from '@/pages/portal/static/image/2.png'
	import img3 from '@/pages/portal/static/image/3.png'

	export default {
		components: {
			HCard
		},
		data() {
			return {
				opts: {
					color: ["#1890FF", "#91CB74", "#FAC858", "#EE6666", "#73C0DE", "#3CA272", "#FC8452", "#9A60B4",
						"#ea7ccc"
					],
					padding: [15, 15, 0, 15],
					enableScroll: true,
					legend: {
						show: false
					},
					xAxis: {
						disableGrid: true,
						scrollShow: true,
						itemCount: 4
					},
					yAxis: {
						gridType: "dash",
						dashLength: 2,
						data: [{
							min: null,
							max: 10000,
						}]
					},
					extra: {
						area: {
							type: "curve",
							opacity: 0.2,
							addLine: true,
							width: 2,
							gradient: true,
							activeType: "hollow"
						}
					}
				},
				chartData: {},
				proListList: [{
						imgUrl: img1,
						title: "销售管理信息项目",
						time: '5 小时前',
						name: '超越超越卓尔不凡',
						content: '销售管理信息项目是为全面提升企业市场占有率和竞争力，越来越多企业已经意识到上销售管理软件销售管理信息项目是为全面提升企业市场占有率和竞争力，越来越多企业已经意识到上销售管理软件销售管理信息项目是为全面提升企业市场占有率和竞争力，越来越多企业已经意识到上销售管理软件'
					},
					{
						imgUrl: img1,
						title: "知识库管理搭建",
						time: '5 小时前',
						name: '快乐每一天',
						content: '知识库管理搭建是一个企业内部信息的汇总，建立知识库，对这些知识进行统一管理能够有效帮助企业提高'
					},
					{
						imgUrl: img2,
						title: "报表样式调整",
						time: '5 小时前',
						name: '超越超越卓尔不凡',
						content: '报表引用UReport2是一款基于架构在Spring之上纯Java的高性能报表引擎，通过迭代单元格可以实现任意'
					},
					{
						imgUrl: img3,
						title: "企业门户平台",
						time: '5 小时前',
						name: '智慧成功人士',
						content: '企业统一门户为企业提供一个统一入口访问企业各种资源信息，企业的员工、客户、合作伙伴和供应商等都可'
					}
				],
				list: [{
						text1: "今日交易总额",
						text2: "¥ 124,546,233",
						text3: "+2.34%"
					},
					{
						text1: "销售目标完成率",
						text2: "92 %",
						text3: "-0.3%"
					},
					{
						text1: "活动剩余时间",
						text2: "23:26:59",
						text3: "活动时间48小时"
					},
					{
						text1: "每秒交易总额",
						text2: "￥ 2,663",
						text3: "+9.34%"
					},
					{
						text1: "在售商品总数",
						text2: "569 件",
						text3: ""
					}
				],
				noticeList: [{
						title: '通知',
						content: '中秋国庆双节放假通知',
						time: '2020-09-19',
						type: 1
					},
					{
						title: '公告',
						content: '你好朋友，感谢使用 Baobab快速开发平台新版本',
						time: '2020-09-18',
						type: 2
					},
					{
						title: '通知',
						content: '月饼发放通知，请各部门主管到行政部领取',
						time: '2020-09-17',
						type: 1
					},
					{
						title: '公告',
						content: '本季度销售之星名单公示',
						time: '2020-09-16',
						type: 2
					},
					{
						title: '通知',
						content: '公司上班时间调整通知，下月执行',
						time: '2020-09-15',
						type: 1
					},
					{
						title: '公告',
						content: '公司新上任总裁任命书',
						time: '2020-09-14',
						type: 2
					},
					{
						title: '公告',
						content: '午餐补贴通知，本月生效',
						time: '2020-09-13',
						type: 2
					},
					{
						title: '通知',
						content: '公司技术交流培训课通知',
						time: '2020-09-12',
						type: 1
					},
					{
						title: '通知',
						content: '关于公司组织员工秋季旅游通知',
						time: '2020-09-11',
						type: 1
					}
				],
				list2: [{
						bgColor: '#f2ebfb',
						icon: 'icon-ym icon-ym-wf-payDistribution',
						text1: "总销售额",
						text3: "102,400"
					},
					{
						bgColor: '#edf8fe',
						icon: 'icon-ym icon-ym-generator-Panel',
						text1: "访问量",
						text3: "81,212"
					},
					{
						bgColor: '#fef3ef',
						icon: 'icon-ym icon-ym-extend-folder',
						text1: "支付笔数",
						text3: "9,280"
					},
					{
						bgColor: '#ffeff2',
						icon: 'icon-ym icon-ym-Refresh',
						text1: "线上购物转化率",
						text3: "13,600"
					},
				],
				cardData: {
					'title': "销售指数",
					'viceTitle': '活动实时交易情况',
					'card': {
						'titleBgColor': "#fff",
						'cardIcon': "icon-ym icon-ym-generator-bar",
						'titleFontColor': '#6a6a6a',
						'viceTitleFontColor': '#606266',
						'titleFontSize': 14,
						'viceTitleFontSize': 12,
						'titleFontWeight': false,
						'cardRightBtn': '',
						'titleLeft': 'left'
					}
				},
				cardData2: {
					'title': "实时数据",
					'viceTitle': '',
					'card': {
						'titleBgColor': "#fff",
						'cardIcon': "icon-ym icon-ym-liveData",
						'titleFontColor': '#6a6a6a',
						'viceTitleFontColor': '#606266',
						'titleFontSize': 14,
						'viceTitleFontSize': 12,
						'titleFontWeight': false,
						'cardRightBtn': '',
						'titleLeft': 'left'
					}
				},
				cardData3: {
					'title': "公告通知",
					'viceTitle': '',
					'card': {
						'titleBgColor': "#fff",
						'cardIcon': "icon-ym icon-ym-xitong",
						'titleFontColor': '#6a6a6a',
						'viceTitleFontColor': '#606266',
						'titleFontSize': 14,
						'viceTitleFontSize': 12,
						'titleFontWeight': false,
						'cardRightBtn': '查看更多>',
						'titleLeft': 'left'
					}
				},
				cardData4: {
					'title': "进行中的项目",
					'viceTitle': '',
					'card': {
						'titleBgColor': "#fff",
						'cardIcon': "icon-ym icon-ym-tree-department",
						'titleFontColor': '#6a6a6a',
						'viceTitleFontColor': '#606266',
						'titleFontSize': 14,
						'viceTitleFontSize': 12,
						'titleFontWeight': false,
						'cardRightBtn': '查看更多>',
						'titleLeft': 'left'
					}
				}
			}
		},
		created() {
			this.getServerData();
		},
		methods: {
			getServerData() {
				//模拟从服务器获取数据时的延时
				setTimeout(() => {
					//模拟服务器返回数据，如果数据格式和标准格式不同，需自行按下面的格式拼接
					let res = {
						categories: ["2AM", "4AM", "6AM", "8AM", "10AM", "12AM", "2AM", "4AM", "6AM", "8AM",
							"10AM", "12AM"
						],
						series: [{
							name: "成交量A",
							data: [0, 2820, 8932, 5700, 7901, 4934, 5000, 3000, 4090, 2330, 3820, 0]
						}]
					};
					this.chartData = JSON.parse(JSON.stringify(res));
				}, 500);
			},
		}
	}
</script>

<style lang="scss">
	.charts-box {
		width: 100%;
		height: 660rpx;
		padding: 20rpx;
	}

	.proList-v {
		width: 100%;
		height: 100%;

		.proList-box {
			width: 100%;
			max-height: 600rpx;
			overflow-y: scroll;
			padding: 20rpx 20rpx 0rpx 20rpx;

			.proList-inner {
				margin-bottom: 20rpx;
				border-bottom: 1rpx solid #ebebeb;

				.t {
					.img {}

					.title {
						color: #303133;
						flex: 0.8;
					}

					.time {
						flex: 0.2;
						text-align: right;
					}
				}

				.c {
					margin: 20rpx 0;
					color: #606266;
				}

				.b {}
			}
		}
	}

	.notice-v {
		width: 100%;
		height: 100%;

		.notice-box {
			width: 100%;
			max-height: 400rpx;
			overflow-y: scroll;
			padding: 0 20rpx;

			.notice-inner {
				// margin-bottom: 20rpx;
				justify-content: flex-start;
				padding: 16rpx 0;

				.l {
					width: 64rpx;
					height: 36rpx;
					font-size: 24rpx;
					border-radius: 4rpx;
					line-height: 36rpx;
					text-align: center;
				}

				.c {
					flex: 0.8;
					width: 61%;
					margin-left: 20rpx;
					text-align: left;
				}

				.r {
					flex: 0.3;
					text-align: right;
				}
			}
		}
	}

	.sale-module {
		.sale-module-box {
			justify-content: space-between;
			height: 100%;

			.head-tabs {
				width: 100%;
				overflow-x: scroll;
				align-items: flex-start;

				.head-tabs-item {
					width: 220rpx;
					display: flex;
					flex-direction: column;
					align-items: center;
					justify-content: center;
					font-size: 28rpx;
					color: #303133;
					line-height: 40rpx;
					flex-shrink: 0;
					height: 100%;
					padding: 20rpx 0;
					border-right: 1rpx solid #EEF0F4;
					text-align: center;

					.head-tabs-inner {
						align-items: center;

						.img {
							width: 75rpx;
							height: 75rpx;
							text-align: center;
							line-height: 75rpx;
							border-radius: 50%;
							flex-shrink: 0;
							font-size: 36rpx;
						}

						.icon-ym-wf-payDistribution {
							color: #713bdb;
						}

						.icon-ym-Refresh {
							color: #fc5180;

						}

						.icon-ym-extend-folder {
							color: #ff8b58;
						}

						.icon-ym-generator-Panel {
							color: #4ab8ff;
						}
					}
				}
			}
		}
	}
</style>