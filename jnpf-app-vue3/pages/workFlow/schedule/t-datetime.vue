<template>
	<view class="jnpf-dateTime">
		<u-input input-align='right' type="select" :select-open="selectShow" v-model="innerValue"
			:placeholder="placeholder" @click="openSelect" :disabled="disabled">
		</u-input>
		<u-popup v-model="selectShow" mode="bottom" @click="colse()">
			<view class="t-pop" @tap.stop>
				<view class="pop-main">
					<view class="top">
						<view class="top-l">
							<view @click="changeSwp('1')" :style="{color:sindex==1?'#1E79FF':'#333333'}">
								<text>{{checkyear}}</text>
							</view>
							<view @click="changeSwp('2')" :style="{color:sindex==2?'#1E79FF':'#333333'}"
								v-if="allDay==0">
								<text>{{checkhour}}:{{checkminute}}</text>
							</view>
						</view>
						<view class="top-r" @click="onOK()">
							<text>确定</text>
						</view>
					</view>
					<swiper class="swiper" circular :current-item-id="sindex">
						<swiper-item :item-id="'1'">
							<view class="mid">
								<scroll-view scroll-y="true" style="height: 960rpx" @scrolltolower="tolower">
									<uni-calendar ref="calendar" :insert="insert" :lunar='lunar'
										@change='calendarChange' :date='today' />
								</scroll-view>
							</view>
						</swiper-item>
						<swiper-item :item-id="'2'" v-if="allDay==0">
							<picker-view :indicator-style="indicatorStyle" :value="swiperTime" @change="bindChange"
								class="picker-view">
								<picker-view-column>
									<view class="item" v-for="(v,i) in 24" :key="i">{{i<10?'0'+i:i}}时</view>
								</picker-view-column>
								<picker-view-column>
									<view class="item" v-for="(v,i) in 12" :key="i">{{i<2?'0'+i*5:i*5}}分
									</view>
								</picker-view-column>
							</picker-view>
						</swiper-item>
					</swiper>

				</view>
			</view>
		</u-popup>
	</view>
</template>

<script>
	export default {
		name: "t-datetime",
		props: {
			type: {
				type: Number,
				default: 0 //默认时间后推0分钟
			},
			allDay: {
				type: Number,
				default: 0
			},
			modelValue: {
				type: String,
				default: ''
			},
			date: {
				type: Object,
				default: () => {}
			},
			placeholder: {
				type: String,
				default: '请选择'
			},
			delayMin: {
				type: Number,
				default: 0 //默认时间后推0分钟
			},
			disabled: {
				type: Boolean,
				default: false
			},
			canToday: { //是否可选择当天之前的时间
				type: Boolean,
				default: false
			},
		},
		data() {
			return {
				textList: ['日', '一', '二', '三', '四', '五', '六'],
				mList: [],
				checkyear: 0,
				checkmonth: 0,
				checkdate: 0,
				checkhour: 0,
				checkminute: 0,
				indicatorStyle: `height: 50px;`,
				sindex: '1',
				nowYear: 0,
				nowMonth: 0,
				nowDate: 0,
				lunar: false,
				insert: true,
				innerValue: '',
				selectShow: false,
				swiperTime: [],
				year: 0,
				hours: '',
				minutes: '',
				today: ''
			};
		},
		watch: {
			allDay(val) {
				let allTime = this.date.year + '-' + this.date.month + '-' + this.date.date
				this.today = allTime
				let srt = this.time(allTime)
				this.innerValue = ''
				if (srt) {
					this.innerValue = srt + this.date.month + '月' + this.date.date + '日'
				} else {
					if (this.date.year == this.year) {
						this.innerValue = this.date.month + '月' + this.date.date + '日'
					} else {
						this.innerValue = this.date.year + '年' + this.date.month + '月' + this.date.date + '日'
					}
				}
				if (this.allDay == 0) {
					this.innerValue = this.innerValue + '  ' + this.hours + ':' +
						this.minutes
				}
			},
			date(val) {
				this.init()
			},
		},
		created() {
			this.timestampToTime(+new Date())
			this.init()
		},
		methods: {
			tolower() {
				this.init()
			},
			openSelect() {
				if (this.disabled) return
				this.selectShow = true
				this.init()
			},
			calendarChange(e) {
				this.date.year = e.year
				this.date.month = e.month
				this.date.date = e.date
				this.date.date = this.date.date < 10 ? '0' + Number(this.date.date) : this.date.date
				this.date.month = this.date.month < 10 ? '0' + Number(this.date.month) : this.date.month
				this.checkyear = this.year == this.date.year ? this.date.month + '月' + this.date.date + '日' : this.date
					.year +
					'年' + this.date.month + '月' + this.date.date + '日'
				this.sindex = '2'
			},
			init() {
				this.innerValue = ''
				this.today = this.date.year + '-' + this.date.month + '-' + this.date.date
				this.date.minutes = this.date.minutes < 10 ? '0' + Number(this.date.minutes) : this.date.minutes
				this.date.hours = this.date.hours < 10 ? '0' + Number(this.date.hours) : this.date.hours
				this.date.date = this.date.date < 10 ? '0' + Number(this.date.date) : this.date.date
				this.date.month = this.date.month < 10 ? '0' + Number(this.date.month) : this.date.month
				this.checkyear = this.year == this.date.year ? this.date.month + '月' + this.date.date + '日' : this.date
					.year + '年' + this.date.month + '月' + this.date.date + '日'
				this.checkhour = this.date.hours
				this.checkminute = this.date.minutes < 10 ? '0' + Number(this.date.minutes) : this.date.minutes
				let checkminute = this.date.minutes / 5
				this.swiperTime = [Number(this.checkhour), checkminute]
				this.hours = this.date.hours
				this.minutes = this.date.minutes
				let allTime = this.date.year + '-' + this.date.month + '-' + this.date.date
				let srt = this.time(allTime)
				if (srt) {
					this.innerValue = srt + this.date.month + '月' + this.date.date + '日'
				} else {
					if (this.date.year == this.year) {
						this.innerValue = this.date.month + '月' + this.date.date + '日'
					} else {
						this.innerValue = this.date.year + '年' + this.date.month + '月' + this.date.date + '日'
					}
				}
				if (this.allDay == 0) {
					this.innerValue = this.innerValue + '  ' + this.hours + ':' +
						this.checkminute
				}

			},
			colse() {
				this.selectShow = false
			},
			bindChange(e) {
				const val = e.detail.value
				this.swiperTime = [val[0], val[1]]
				this.checkhour = Number(val[0]) < 10 ? '0' + val[0] : val[0]
				this.checkminute = val[1] == '天' ? '00' : val[1] < 2 ? '0' + val[1] * 5 : val[1] * 5
			},
			changeSwp(i) {
				this.sindex = i
			},
			onOK() {
				if (this.allDay == 1) {
					this.date.hours = '00'
					this.date.minutes = '00'
				} else {
					this.date.hours = this.checkhour
					this.date.minutes = this.checkminute
				}
				this.selectShow = false
				let allTime = this.date.year + '-' + this.date.month + '-' + this.date.date
				let srt = this.time(allTime)
				if (srt) {
					this.innerValue = srt + this.date.month + '月' + this.date.date + '日'
					if (this.allDay == 0) {
						this.innerValue = this.innerValue + '  ' + this.date.hours +
							':' + this.date.minutes
					}
				} else {
					if (this.date.year == this.year) {
						this.innerValue = this.date.month + '月' + this.date.date + '日'
					} else {
						this.innerValue = this.date.year + '年' + this.date.month + '月' + this.date.date + '日'
					}
					if (this.allDay == 0) {
						this.innerValue = this.innerValue + '  ' + this.date.hours +
							':' + this.date.minutes
					}

				}
				this.selectShow = false
				this.$emit('confirm', this.date, this.type)
			},
			timestampToTime(timestamp) {
				var date = new Date(timestamp);
				this.year = date.getFullYear();
			},
			time(date) {
				if (this.date.year != this.year) return false
				let time_str = "";
				if (new Date(date).getDate() === new Date().getDate()) {
					time_str = "今天 · ";
				} else if (new Date(date).getDate() === (new Date().getDate() - 1)) {
					time_str = "昨天 · ";
				} else if (new Date(date).getDate() === (new Date().getDate() + 1)) {
					time_str = "明天 · ";
				} else if (new Date(date).getDate() < new Date().getDate()) {
					time_str = "";
				}
				return time_str;
			}
		}
	}
</script>

<style lang="scss" scoped>
	.jnpf-dateTime {
		width: 100%;

		:deep(.u-drawer) {
			z-index: 999 !important;
		}
	}

	:deep(.uni-calendar-item__weeks-box-item) {
		line-height: 36rpx;
	}

	.t-pop {
		width: 100%;
		display: flex;
		justify-content: center;
		align-items: center;

		.pop-main {
			display: flex;
			flex-direction: column;
			justify-content: space-between;
			align-items: center;
			background-color: #fff;
			border-radius: 24px;
			height: 900rpx;
			width: 100%;

		}
	}

	.swiper {
		height: 840rpx;
		width: 100vw;
	}

	.top {
		display: flex;
		flex-direction: row;
		justify-content: space-between;
		align-items: center;
		width: 100%;
		margin: 20rpx 0;

		.top-l {
			display: flex;
			flex-direction: row;
			margin-left: 30rpx;
		}

		.top-r {
			margin-right: 30rpx;
			color: #1E79FF;
		}

	}

	.calendar {
		display: flex;
		flex-wrap: wrap;
		flex-direction: row;
		align-items: center;
		width: 100vw;
		position: relative;

	}

	.ca-top {
		width: 14.2vw;
		display: flex;
		justify-content: center;
		align-items: center;
		height: 66rpx;
		z-index: 10;
	}

	.cell {
		width: 60rpx;
		height: 60rpx;
		display: flex;
		justify-content: center;
		align-items: center;
		align-content: center;
		border-radius: 30rpx;
	}

	.cell-active {
		background-color: #1E79FF;
		color: #fff;
	}

	.cabg {
		display: flex;
		justify-content: center;
		width: 100vw;
		font-size: 180rpx;
		color: beige;
		position: absolute;
		z-index: 9;
	}

	.picker-view {
		width: 750rpx;
		height: 600rpx;
		margin-top: 20rpx;
	}

	.item {
		height: 50px;
		display: flex;
		align-items: center;
		justify-content: center;
		text-align: center;
	}
</style>