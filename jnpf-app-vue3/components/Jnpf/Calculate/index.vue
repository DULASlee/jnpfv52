<template>
	<view class="jnpf-calculation jnpf-calculation-right">
		<u-input input-align='right' v-model="innerValue" disabled placeholder='0.00' />
		<view class="tips" v-if="isAmountChinese">{{rmbText}}</view>
	</view>
</template>
<script>
	/**
	 * 中缀转后缀（逆波兰 Reverse Polish Notation）
	 * @param {Array} exps - 中缀表达式数组
	 */
	const toRPN = exps => {
		const s1 = [] // 符号栈
		const s2 = [] // 输出栈
		const getTopVal = (stack) => stack.length > 0 ? stack[stack.length - 1] : null
		const levelCompare = (c1, c2) => {
			const getIndex = c => ['+-', '×÷', '()'].findIndex(t => t.includes(c))
			return getIndex(c1) - getIndex(c2)
		}
		exps.forEach(t => {
			if (typeof t === 'string' && Number.isNaN(Number(t))) { // 是符号
				if (t === '(') {
					s1.push(t)
				} else if (t === ')') {
					let popVal
					do {
						popVal = s1.pop()
						popVal !== '(' && s2.push(popVal)
					} while (s1.length && popVal !== '(')
				} else {
					let topVal = getTopVal(s1)
					if (!topVal) { // s1 为空 直接push
						s1.push(t)
					} else {
						while (topVal && topVal !== '(' && levelCompare(topVal, t) >= 0) { // 优先级 >= t 弹出到s2
							s2.push(s1.pop())
							topVal = getTopVal(s1)
						}
						s1.push(t)
					}
				}
				return
			}
			s2.push(t) // 数字直接入栈
		})
		while (s1.length) {
			s2.push(s1.pop())
		}
		return s2
	}
	const calcRPN = rpnExps => {
		rpnExps = rpnExps.concat()
		const calc = (x, y, type) => {
			let a1 = Number(x),
				a2 = Number(y)
			switch (type) {
				case '+':
					return a1 + a2;
				case '-':
					return a1 - a2;
				case '×':
					return a1 * a2;
				case '÷':
					return a1 / a2;
			}
		}
		for (let i = 2; i < rpnExps.length; i++) {
			if ('+-×÷'.includes(rpnExps[i])) {
				let val = calc(rpnExps[i - 2], rpnExps[i - 1], rpnExps[i])
				rpnExps.splice(i - 2, 3, val)
				i = i - 2
			}
		}
		return rpnExps[0]
	}
	const mergeNumberOfExps = expressions => {
		const res = []
		const isNumChar = n => /^[\d|\.]$/.test(n)
		for (let i = 0; i < expressions.length; i++) {
			if (i > 0 && isNumChar(expressions[i - 1]) && isNumChar(expressions[i])) {
				res[res.length - 1] += expressions[i]
				continue
			}
			res.push(expressions[i])
		}
		return res
	}
	export default {
		name: 'jnpf-calculation',
		props: {
			modelValue: {
				type: [String, Number],
				default: ''
			},
			thousands: {
				type: Boolean,
				default: false
			},
			precision: {
				default: 0
			},
			isAmountChinese: {
				type: Boolean,
				default: false
			},
			expression: {
				type: Array,
				default: []
			},
			config: {
				type: Object,
				default: {}
			},
			formData: {
				type: Object,
				default: {}
			},
			rowIndex: {
				type: [String, Number],
				default: ''
			},
			roundType: {
				type: [String, Number],
				default: 1
			}
		},
		data() {
			return {
				innerValue: '',
				RPN_EXP: toRPN(mergeNumberOfExps(this.expression)),
				rmbText: '',
				subValue: 0
			}
		},
		watch: {
			formData: {
				handler(val, oldVal) {
					setTimeout(() => {
						this.execRPN()
					}, 0)
				},
				deep: true,
				immediate: true
			},
			modelValue: {
				handler(val, oldVal) {
					this.innerValue = val
				},
				deep: true,
				immediate: true
			},
		},
		methods: {
			getRoundValue(val) {
				const precision = this.precision || 0;
				let truncatedNumber
				if (this.roundType == 2) {
					if (precision === 0) Math.trunc(val);
					const factor = Math.pow(10, precision);
					truncatedNumber = Math.trunc(val * factor) / factor;
					return truncatedNumber
				}
				if (this.roundType == 3) return Math.ceil(val)
				if (this.roundType == 4) return Math.floor(val);
				return val.toFixed(precision)
			},
			/**
			 * 计算表达式
			 */
			execRPN() {
				const temp = this.RPN_EXP.map(t => typeof t === 'object' ? this.getFormVal(t.__vModel__) : t)
				this.setValue(temp)
				this.subValue = JSON.parse(JSON.stringify(this.innerValue))
				if (isNaN(this.innerValue)) this.innerValue = 0
				this.rmbText = this.jnpf.getAmountChinese(Number(this.subValue) || 0)
				this.$emit('update:modelValue', this.subValue)
				if (this.thousands) this.innerValue = this.numFormat(this.innerValue)
			},
			setValue(temp) {
				let result = calcRPN(temp);
				if (isNaN(result) || !isFinite(result)) {
					this.innerValue = 0;
				} else {
					let num = Number(result);
					if (this.roundType == 2) {
						this.innerValue = num;
					} else {
						this.innerValue = Number(num.toFixed(this.precision ||
							0));
					}
				}
				this.innerValue = this.getRoundValue(parseFloat(this.innerValue));
			},
			/**
			 * 千分符
			 */
			numFormat(num) {
				num = num.toString().split("."); // 分隔小数点
				let arr = num[0].split("").reverse(); // 转换成字符数组并且倒序排列
				let res = [];
				for (let i = 0, len = arr.length; i < len; i++) {
					if (i % 3 === 0 && i !== 0) res.push(","); // 添加分隔符
					res.push(arr[i]);
				}
				res.reverse(); // 再次倒序成为正确的顺序
				if (num[1]) { // 如果有小数的话添加小数部分
					res = res.join("").concat("." + num[1]);
				} else {
					res = res.join("");
				}
				return res
			},
			/**
			 * 获取指定组件的值
			 */
			getFormVal(vModel) {
				try {
					if (vModel.indexOf('.') > -1) {
						let [tableVModel, cmpVModel] = vModel.split('.');
						if (typeof this.rowIndex === 'number') {
							if (!Array.isArray(this.formData[tableVModel]) || this.formData[tableVModel].length < this
								.rowIndex + 1) return 0;
							return this.formData[tableVModel][this.rowIndex][cmpVModel] || 0;
						} else {
							if (!this.formData[tableVModel].length) return 0;
							return this.formData[tableVModel].reduce((sum, c) => (c[cmpVModel] ? Number(c[cmpVModel]) :
								0) + sum, 0);
						}
					}
					return this.formData[vModel] || 0
				} catch (error) {
					console.warn('计算公式出错, 可能包含无效的组件值', error)
					return 0
				}
			},
		}
	}
</script>

<style lang="scss" scoped>
	.jnpf-calculation {
		width: 100%;

		&.jnpf-calculation-right {
			text-align: right;
		}

		.tips {
			color: #999999;
			line-height: 40rpx;
		}
	}
</style>