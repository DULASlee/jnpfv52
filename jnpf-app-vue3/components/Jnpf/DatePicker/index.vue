<template>
	<view class="jnpf-date-time">
		<selectBox v-model="innerValue" :placeholder="placeholder" @openSelect="openSelect" :select-open="selectShow">
		</selectBox>
		<Select v-if="selectShow" v-model="selectShow" mode="time" :defaultTime="defaultTime" :params="params"
			:startDate="startDate" :endDate="endDate" :format='format' @close="handleClose" @confirm="handleConfirm" />
	</view>
</template>

<script>
	import Select from './Select.vue';
	import selectBox from '@/components/selectBox'
	export default {
		name: 'jnpf-dateTime',
		components: {
			Select,
			selectBox
		},
		props: {
			scene: {
				type: String,
				default: 'form'
			},
			inputType: {
				type: String,
				default: 'select'
			},
			modelValue: {
				type: [String, Number],
				default: ''
			},
			placeholder: {
				type: String,
				default: '请选择'
			},
			disabled: {
				type: Boolean,
				default: false
			},
			type: {
				type: String,
				default: 'date'
			},
			startTime: {
				type: [String, Number],
				default: 0
			},
			selectType: {
				type: String,
				default: ''
			},
			endTime: {
				type: [String, Number],
				default: 0
			},
			format: {
				type: String,
				default: 'yyyy-MM-dd HH:mm:ss'
			}
		},
		data() {
			return {
				startDate: '',
				endDate: '',
				params: {
					year: true,
					month: true,
					day: true,
					hour: true,
					minute: true,
					second: true,
					timestamp: true
				},
				defaultTime: '',
				selectShow: false,
				innerValue: '',
				startTimestamp: -25140,
				endTimestamp: 7289625599000,
				formatObj: {
					'yyyy': 'yyyy',
					'yyyy-MM': 'yyyy-mm',
					'yyyy-MM-dd': 'yyyy-mm-dd',
					'yyyy-MM-dd HH:mm': 'yyyy-mm-dd hh:MM',
					'yyyy-MM-dd HH:mm:ss': 'yyyy-mm-dd hh:MM:ss',
					'HH:mm:ss': 'hh:MM:ss',
					"HH:mm": "hh:MM",
					'YYYY': 'yyyy',
					'YYYY-MM': 'yyyy-mm',
					'YYYY-MM-DD': 'yyyy-mm-dd',
					'YYYY-MM-DD HH:mm': 'yyyy-mm-dd hh:MM',
					'YYYY-MM-DD HH:mm:ss': 'yyyy-mm-dd hh:MM:ss',
				}
			}
		},
		watch: {
			modelValue(val) {
				this.setDefault()
			},
			startTime(val) {
				this.setMode()
			},
			endTime(val) {
				this.setMode()
			}
		},
		created() {
			this.setMode()
			this.setDefault()
		},
		methods: {
			setMode() {
				let str = this.formatObj[this.format] || 'yyyy-mm-dd hh:MM:ss'
				let formatArr = str.trim().split(" ")
				let startYear = '970'
				if (this.type === 'time') {
					let t = formatArr[0].split(":") || []
					this.params = {
						...this.params,
						year: false,
						month: false,
						day: false,
						hour: t.includes('hh'),
						minute: t.includes('MM'),
						second: t.includes('ss'),
					}
					this.startDate = this.startTime ? this.getYearDate() + ' ' + this.startTime : this.getYearDate() +
						' ' + "00:00:00"
					this.endDate = this.endTime ? this.getYearDate() + ' ' + this.endTime : this.getYearDate() + ' ' +
						"23:59:59"
				} else {
					let y = formatArr[0] ? formatArr[0].split("-") : []
					let t = formatArr[1] ? formatArr[1].split(":") : []
					this.params = {
						...this.params,
						year: y.includes('yyyy'),
						month: y.includes('mm'),
						day: y.includes('dd'),
						hour: t.includes('hh'),
						minute: t.includes('MM'),
						second: t.includes('ss'),
					}
					// #ifdef APP
					const sys = uni.getSystemInfoSync()
					let platform = sys.platform
					startYear = platform === 'ios' ? '1899' : '970'
					// #endif
					this.startDate = this.startTime ? this.$u.timeFormat(this.startTime, str) : startYear + '-1-1 00:00:00'
					this.endDate = this.endTime ? this.$u.timeFormat(this.endTime, str) : '2500-12-31 23:59:59'
				}
			},
			getYearDate() {
				let date = new Date();
				let year = date.getFullYear()
				let month = date.getMonth() + 1
				let day = date.getDate()
				return year + '-' + month + '-' + day
			},
			setDefault() {
				if (!this.modelValue) return this.innerValue = ''
				if (this.type === 'time') {
					let valueArr = this.modelValue.split(':')
					let formatArr = this.formatObj[this.format].split(':')
					this.innerValue = this.modelValue
					if (valueArr.length != formatArr.length) this.innerValue = valueArr[0] + ':' + valueArr[1]
					this.defaultTime = this.getYearDate() + ' ' + this.modelValue
				} else {
					const format = 'yyyy-mm-dd hh:MM:ss'
					this.innerValue = this.$u.timeFormat(this.modelValue, this.formatObj[this.format])
					this.defaultTime = this.$u.timeFormat(this.modelValue, format)
				}
			},
			openSelect() {
				uni.hideKeyboard()
				if (this.disabled) return
				if (new Date(this.startDate).getTime() > new Date(this.endDate).getTime()) return this
					.$u.toast('开始时间不能大于结束时间')
				this.selectShow = true
			},
			handleConfirm(e) {
				let newFormat = this.format
				let timeType = newFormat === 'yyyy' ? '/01/01 00:00:00' : newFormat === 'yyyy-MM' ? '/01 00:00:00' :
					newFormat === 'yyyy-MM-dd' ?
					' 00:00:00' : ''
				this.innerValue = ''
				if (this.params.year) this.innerValue += e.year
				if (this.params.month) this.innerValue += '-' + e.month
				if (this.params.day) this.innerValue += '-' + e.day
				if (this.params.hour) this.innerValue += (this.type === 'time' ? '' : ' ') + e.hour
				if (this.params.minute) this.innerValue += ':' + e.minute
				if (this.params.second) this.innerValue += ':' + e.second
				const value = this.type === 'time' ? this.innerValue : e.timestamp
				if (this.modelValue === value) return
				this.$emit('update:modelValue', value)
				this.$emit('change', value, this.selectType)
			},
			handleClose() {
				this.selectShow = false
			}
		}
	}
</script>
<style lang="scss" scoped>
	.jnpf-date-time {
		width: 100%;
	}
</style>