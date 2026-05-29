<template>
	<view class="schedule-v">
		<view class="calendar-v">
			<calendar :lunar="true" :selected="selected" :showMonth="true" @change="change" @initdate="initdate"
				:key='key' />
		</view>
		<view class="u-p-20 block">
			<view class="block-a u-font-24">
				{{dateDay}}
				<div>{{changedate}}</div>
			</view>
			<view class="u-m-b-20 list-box">
				<view v-for="(item,index) in scheduleList" :key="index" class="list">
					<view class="u-m-t-20 u-flex item" @click="goDetail(item.id,item.creatorUserId)">
						<text class="u-font-28">{{item.allDay?'全天':item.startTime}}</text>
						<view class="u-flex-col u-m-l-20 u-p-l-20 content u-font-28">
							<view>{{item.title}}</view>
							<view class="u-font-24">{{item.content}}</view>
						</view>
					</view>
				</view>
			</view>
			<view :class="scheduleList.length<3?'lunar1':'addlunar'">
				<view @click="goDetail()" class="add-title">+添加日程内容</view>
			</view>
		</view>
	</view>
</template>

<script>
	import calendar from './calendar/uni-calendar.vue'
	import {
		List
	} from '@/api/workFlow/schedule.js'
	export default {
		components: {
			direction: 'col',
			calendar,
		},
		props: {
			config: {
				type: Object,
				default: () => {}
			}
		},
		data() {
			return {
				selected: [],
				showForm: false,
				horizontal: 'right',
				vertical: 'bottom',
				direction: 'horizontal',
				pattern: {
					color: '#7A7E83',
					backgroundColor: '#fff',
					selectedColor: '#007AFF',
					buttonColor: "#007AFF"
				},
				changedate: '',
				scheduleList: [],
				exhibitionList: [],
				startDate: '',
				endDate: '',
				dateDay: '',
				query: {},
				options: [{
					text: '删除',
					style: {
						backgroundColor: '#dd524d'
					}
				}],
				startTime: "",
				formVisible: false,
				userInfo: {},
				key: +new Date(),
				toDay: '',
				sysConfigInfo: {}
			}
		},
		onLoad() {
			this.userInfo = uni.getStorageSync('userInfo') || {}
			this.sysConfigInfo = uni.getStorageSync('sysConfigInfo') || {}
		},
		onShow() {
			uni.$on('refresh', () => {
				this.key = +new Date()
			});
		},
		onUnload() {
			uni.$off('refresh')
		},
		methods: {
			/* 初始化请求 */
			initdate(cale, nowDate) {
				let canlender = cale.canlender;
				let weeks = cale.weeks;
				for (let i = 0; i < canlender.length; i++) {
					if (canlender[i].fullDate === nowDate.fullDate) {
						let day = this.toChinaDay(nowDate.lunar.nWeek)
						this.toDay = nowDate.fullDate
						this.dateTime = nowDate.fullDate
						this.dateDay = nowDate.month + '月' + nowDate.date + '日' + "  周" + day +
							" (今天)"
						this.changedate = ''
						if (this.sysConfigInfo.showLunarCalendar) this.changedate = '农历  ' + canlender[i].lunar.IMonthCn +
							canlender[i].lunar.IDayCn;
						break;
					}
				}
				let data = {
					weeks: weeks,
					canlender: canlender
				}
				this.$nextTick(() => {
					this.handleScheduleList(data)
				})
			},
			handleScheduleList(data) {
				let canlender = data.canlender
				let startTime = this.startDate = canlender[0].lunar.cYear + '-' + canlender[0].lunar.cMonth + '-' +
					canlender[0].lunar
					.cDay;
				let endTime = this.endDate = canlender[canlender.length - 1].lunar.cYear + '-' + canlender[canlender
						.length - 1].lunar
					.cMonth + '-' + canlender[canlender.length - 1].lunar.cDay;
				let query = {
					startTime: startTime,
					endTime: endTime,
					dateTime: data.fulldate || this.toDay
				}
				List(query).then(res => {
					let signList = res.data.signList;
					if (res.data.todayList) {
						this.scheduleList = res.data.todayList.map(o => ({
							...o,
							show: false
						}));
					}
					let selected = []
					for (let [key, value] of Object.entries(signList)) {
						const cYear = key.slice(0, 4)
						const cMonth = key.slice(4, 6)
						const cDay = key.slice(6, 8)
						let date = cYear + '-' + cMonth + '-' + cDay
						if (value && value != 0) {
							selected.push({
								date,
								info: ''
							})
						}
					}
					this.selected = selected
				})
			},
			change(e) {
				let weeks = e.cale.weeks;
				let canlender = e.cale.canlender;
				let lunar = e.lunar;
				lunar.cMonth = lunar.cMonth < 10 ? '0' + Number(lunar.cMonth) : lunar.cMonth
				lunar.cDay = lunar.cDay < 10 ? '0' + Number(lunar.cDay) : lunar.cDay
				let allDay = lunar.lYear + '-' + lunar.cMonth + '-' + lunar.cDay
				let srt = this.time(e.fulldate)
				let day = this.toChinaDay(lunar.nWeek)
				this.startTime = new Date(e.fulldate).getTime()
				this.dateDay = lunar.cMonth + '月' + lunar.cDay + '日' + "  周" + day +
					srt
				this.changedate = ''
				if (this.sysConfigInfo.showLunarCalendar) this.changedate = '农历  ' + lunar.IMonthCn + lunar.IDayCn;
				let data = {
					weeks: weeks,
					canlender: canlender,
					lunar: lunar,
					fulldate: e.fulldate
				}
				this.handleScheduleList(data)
			},
			goDetail(id = '', creatorUserId) {
				let type = this.userInfo.userId == creatorUserId ? true : false
				let url = id ? `/pages/workFlow/schedule/detail?id=${id}&type=${type}` :
					`/pages/workFlow/schedule/form?id=${id}&startTime=${this.startTime}&duration=${this.sysConfigInfo.duration}`
				uni.navigateTo({
					url
				})
			},
			open(index) {
				this.scheduleList[index].show = true;
				this.scheduleList.map((val, idx) => {
					if (index != idx) this.scheduleList[idx].show = false;
				})
			},
			toChinaDay(d) {
				if (d == 1) return '一'
				if (d == 2) return '二'
				if (d == 3) return '三'
				if (d == 4) return '四'
				if (d == 5) return '五'
				if (d == 6) return '六'
				if (d == 7) return '日'
			},
			time(date) {
				let time = new Date()
				if (new Date(date).getFullYear() == time.getFullYear() && new Date(date).getMonth() == time.getMonth()) {
					let time_str = "";
					if (new Date(date).getDate() === new Date().getDate()) {
						time_str = "  (今天)";
					} else if (new Date(date).getDate() === (new Date().getDate() - 1)) {
						time_str = "  (昨天)";
					} else if (new Date(date).getDate() === (new Date().getDate() + 1)) {
						time_str = "  (明天)";
					} else if (new Date(date).getDate() < new Date().getDate()) {
						time_str = "";
					}
					return time_str;
				}
				return ''
			}
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.schedule-v {
		width: 100%;

		.block {
			.block-a {
				background-color: #fff;
				padding: 20rpx 30rpx;
			}

			.list-box {
				.list {
					.item {
						width: 100%;
						background-color: #fff;
						height: 120rpx;
						align-items: center;
						padding: 0 20rpx;

						.content {
							flex: 1;
							border-left: 4rpx solid #2A7DFD;
							justify-content: center;
							height: 80rpx;
						}
					}
				}
			}

			.lunar1 {
				height: 124rpx;
				line-height: 124rpx;
				background-color: #fff;
			}

			.addlunar {
				height: 124rpx;
				line-height: 124rpx;
				border-top: 1rpx solid rgb(228, 231, 237);
			}

			.add-title {
				margin-left: 20rpx;
				font-size: 24rpx;
				color: #C0C0C0;
			}
		}

		.uni-calendar-item__weeks-box-item {
			height: 3.23rem;
		}
	}
</style>