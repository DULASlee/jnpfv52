<template>
	<view class="jnpf-date-range">
		<JnpfDatePicker v-if="type=='date'" v-model="startValue" :placeholder="placeholder" :disabled="disabled"
			inputType="text" scene="searchList" :format="format" @change="change" :defaultTime="startDefaultTime"
			selectType='start' :key="key" ref="dateTime" />
		<JnpfTimePicker v-else v-model="startValue" :placeholder="placeholder" :disabled="disabled" inputType="text"
			scene="searchList" :format="format" @change="change" :defaultTime="startDefaultTime" selectType='start'
			:key="key" ref="dateTime" />
		<view class="u-p-l-10 u-p-r-10">
			至
		</view>
		<JnpfDatePicker v-if="type=='date'" v-model="endValue" :placeholder="placeholder" :disabled="disabled"
			inputType="text" scene="searchList" :format="format" @change="change" :defaultTime="endDefaultTime"
			selectType='end' :key="key+1" ref="dateTime" />
		<JnpfTimePicker v-else v-model="endValue" :placeholder="placeholder" :disabled="disabled" inputType="text"
			scene="searchList" :format="format" @change="change" :defaultTime="endDefaultTime" selectType='end'
			:key="key+1" ref="dateTime" />
	</view>
</template>
<script>
	export default {
		name: 'jnpf-date-range',
		props: {
			modelValue: {
				type: Array,
				default: () => []
			},
			placeholder: {
				type: String,
				default: '请选择日期范围'
			},
			disabled: {
				type: Boolean,
				default: false
			},
			format: {
				type: String,
				default: 'yyyy-MM-dd HH:mm:ss'
			},
			type: {
				type: String,
				default: 'date'
			}
		},
		data() {
			return {
				startDefaultTime: '',
				endDefaultTime: '',
				startValue: '',
				endValue: '',
				datetimerange: [],
				datetimerangeObj: {},
				key: +new Date()
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					if (Array.isArray(val) && val.length) {
						this.startValue = val[0]
						this.endValue = val[1]
					} else {
						this.startValue = ''
						this.endValue = ''
						this.datetimerangeObj = {}
					}
				},
				immediate: true
			},
		},
		methods: {
			change(e, type) {
				this.datetimerange = []
				if (type == 'start') {
					const format = 'yyyy-mm-dd hh:MM:ss'
					this.$set(this.datetimerangeObj, type, e)
					this.$refs.dateTime.defaultTime = this.type == 'time' ? this.datetimerangeObj['start'] : this.$u
						.timeFormat(this.datetimerangeObj['start'], format)
					this.handelValue()
				} else {
					this.$set(this.datetimerangeObj, type, e)
					this.handelValue()
				}
			},
			handelValue() {
				this.datetimerange.unshift(this.datetimerangeObj['start'])
				this.datetimerange.push(this.datetimerangeObj['end'])
				if (this.datetimerange[0] > this.datetimerange[1]) {
					this.$u.toast('开始不能大于结束')
					setTimeout(() => {
						this.startValue = ""
						this.endValue = ""
						this.datetimerangeObj = {}
						this.datetimerange = []
						this.key = +new Date()
					}, 500)
					return
				}
				this.$emit('update:modelValue', this.datetimerange)
			}
		}
	}
</script>