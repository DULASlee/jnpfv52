<template>
	<view v-show="show" class="t-wrapper" @touchmove.stop.prevent="moveHandle">
		<view class="t-mask" :class="{active:active}" @click.stop="close"></view>
		<view class="t-box" :class="{active:active}">
			<view class="t-header">
				<view class="t-header-button" @click="close">取消</view>
				<view class="t-header-button" @click="confirm">确认</view>
			</view>
			<view class="t-color__box"
				:style="{ background: 'rgb(' + bgcolor.r + ',' + bgcolor.g + ',' + bgcolor.b + ')'}">
				<view class="t-background boxs" @touchstart="touchstart($event, 0)" @touchmove="touchmove($event, 0)"
					@touchend="touchend($event, 0)">
					<view class="t-color-mask"></view>
					<view class="t-pointer" :style="{ top: site[0].top - 8 + 'px', left: site[0].left - 8 + 'px' }">
					</view>
				</view>
			</view>
			<view class="t-control__box">
				<view class="t-control__color" v-if="colorFormat == 'rgba'">
					<view class="t-control__color-content"
						:style="{ background: 'rgba(' + rgba.r + ',' + rgba.g + ',' + rgba.b + ',' + rgba.a + ')' }">
					</view>
				</view>
				<view class="t-control-box__item">
					<view class="t-controller boxs" @touchstart="touchstart($event, 1)"
						@touchmove="touchmove($event, 1)" @touchend="touchend($event, 1)">
						<view class="t-hue">
							<view class="t-circle" :style="{ left: site[1].left - 12 + 'px' }"></view>
						</view>
					</view>
					<view class="t-controller boxs" @touchstart="touchstart($event, 2)"
						@touchmove="touchmove($event, 2)" @touchend="touchend($event, 2)" v-if="colorFormat == 'rgba'">
						<view class="t-transparency">
							<view class="t-circle" :style="{ left: site[2].left - 12 + 'px' }"></view>
						</view>
					</view>
				</view>
			</view>
			<view class="t-result__box">
				<view class="t-result__item">
					<view class="t-result__box-input">{{colorVal}}</view>
				</view>
				<!-- <template v-else>
					<view class="t-result__item">
						<view class="t-result__box-input">{{rgba.r}}</view>
						<view class="t-result__box-text">R</view>
					</view>
					<view class="t-result__item">
						<view class="t-result__box-input">{{rgba.g}}</view>
						<view class="t-result__box-text">G</view>
					</view>
					<view class="t-result__item">
						<view class="t-result__box-input">{{rgba.b}}</view>
						<view class="t-result__box-text">B</view>
					</view>
					<view class="t-result__item" v-if="colorFormat === 'rgba'">
						<view class="t-result__box-input">{{rgba.a}}</view>
						<view class="t-result__box-text">A</view>
					</view>
				</template> -->
			</view>
			<view class="t-alternative" v-if="isCommonColor">
				<view class="t-alternative__item" v-for="(item,index) in conversion.colorList" :key="index">
					<view class="t-alternative__item-content"
						:style="{ background: 'rgba(' + item.r + ',' + item.g + ',' + item.b + ',' + item.a + ')' }"
						@click="selectColor(item)">
					</view>
				</view>
			</view>
		</view>
	</view>
</template>
<script>
	import conversion from '@/libs/color-typeConversion.js'
	export default {
		props: {
			color: {
				default: ''
			},
			colorFormat: {
				default: 'hex'
			},
			isCommonColor: {
				type: Boolean,
				default: false
			}
		},
		data() {
			return {
				show: false,
				active: false,
				// rgba 颜色
				rgba: {
					r: 0,
					g: 0,
					b: 0,
					a: 1
				},
				// hsb 颜色
				hsb: {
					h: 0,
					s: 0,
					b: 0
				},
				site: [{
					top: 0,
					left: 0
				}, {
					left: 0
				}, {
					left: 0
				}],
				index: 0,
				bgcolor: {
					r: 255,
					g: 0,
					b: 0,
					a: 1
				},
				hex: '#000000',
				hsvList: ['h', 's', 'v'],
				hsvObj: {},
				hslList: ['h', 's', 'l'],
				hslObj: {},
				colorVal: '#000000',
				hsv: '',
				rgbObj: {},
				rgbList: ['r', 'g', 'b'],
				rgbaList: ['r', 'g', 'b', 'a'],
				rgbaObj: {},
				hsl: '',
				conversion: conversion
			};
		},
		created() {

		},
		methods: {
			open() {
				this.show = true;
				this.$nextTick(() => {
					this.init();
					setTimeout(() => {
						this.active = true;
						setTimeout(() => {
							this.getSelectorQuery();
						}, 350)
					}, 50)
				})
			},
			init() {
				if (!this.color) return
				if (this.colorFormat == 'rgb' || this.colorFormat == 'rgba' || this.colorFormat == 'hsv' || this
					.colorFormat == 'hsl') {
					//将字符串括号中的值取出
					var result = this.color.match(/\(([^)]*)\)/)
					result[1].split(',').forEach((o, i) => {
						this.$set(this[this.colorFormat + 'Obj'], this[this.colorFormat + 'List'][i], o)
					})
					if (this.colorFormat == 'rgb' || this.colorFormat == 'rgba') {
						this.rgba = this[this.colorFormat + 'Obj']
						this.hsb = conversion.rgbToHex(this.rgba);
						this.setValue(this.rgba)
					}
					if (this.colorFormat == 'hsv') {
						this.rgba = conversion.hsv2rgb(this[this.colorFormat + 'Obj'].h, this[this.colorFormat + 'Obj'].s,
							this[
								this.colorFormat + 'Obj'].v)
						this.hsb = conversion.rgbToHex(this.rgba);
						this.setValue(this.rgba)
					}
					if (this.colorFormat == 'hsl') {
						this.rgba = conversion.hsl2rgb(parseInt(this.hslObj.h), parseInt(this.hslObj.s), parseInt(this
							.hslObj
							.l))
						this.hsb = conversion.rgbToHex(this.rgba);
						this.setValue(this.rgba)
					}
				} else {
					this.rgba = conversion.hex2rgba(this.color)
					this.hsb = conversion.rgbToHex(this.rgba);
					this.setValue(this.rgba)
				}
			},
			moveHandle() {},
			close() {
				this.active = false;
				this.$nextTick(() => {
					setTimeout(() => {
						this.show = false;
					}, 500)
				})
			},
			confirm() {
				this.$emit('confirm', {
					rgba: this.rgba,
					hex: this.hex,
					colorVal: this.colorVal,
					hsv: this.hsv,
					hsl: this.hsl
				})
				this.close();
			},
			// 常用颜色选择
			selectColor(item) {
				this.setColorBySelect(item)
			},
			touchstart(e, index) {
				const {
					clientX,
					clientY
				} = e.touches[0];
				this.pageX = clientX;
				this.pageY = clientY;
				this.setPosition(clientX, clientY, index);
			},
			touchmove(e, index) {
				const {
					clientX,
					clientY
				} = e.touches[0];
				this.moveX = clientX;
				this.moveY = clientY;
				this.setPosition(clientX, clientY, index);
			},
			touchend(e, index) {},
			/**
			 * 设置位置
			 */
			setPosition(x, y, index) {
				this.index = index;
				const {
					top,
					left,
					width,
					height
				} = this.position[index];
				// 设置最大最小值
				this.site[index].left = Math.max(0, Math.min(parseInt(x - left), width));
				if (index === 0) {
					this.site[index].top = Math.max(0, Math.min(parseInt(y - top), height));
					// 设置颜色
					this.hsb.s = parseInt((100 * this.site[index].left) / width);
					this.hsb.b = parseInt(100 - (100 * this.site[index].top) / height);
					this.setColor();
					this.setValue(this.rgba);
				} else {
					this.setControl(index, this.site[index].left);
				}
			},
			/**
			 * 设置 rgb 颜色
			 */

			setColor() {
				const rgb = conversion.HSBToRGB(this.hsb);
				this.rgba.r = rgb.r;
				this.rgba.g = rgb.g;
				this.rgba.b = rgb.b;
			},
			/**
			 * 设置二进制颜色
			 * @param {Object} rgb
			 */
			setValue(rgb) {
				let hsv = conversion.rgb2hsv(rgb.r, rgb.g, rgb.b)
				let hsl = conversion.rgb2hsl(rgb.r, rgb.g, rgb.b)
				this.hsv = 'hsv(' + hsv.h + ',' + hsv.s + ',' + hsv.v + ')'
				this.hex = '#' + conversion.rgbToHex(rgb);
				this.hsl = 'hsl(' + hsl.h + ',' + hsl.s + ',' + hsl.l + ')'
				if (this.colorFormat == 'hsv') {
					for (let key in hsv) {
						if (key != 'h') {
							hsv[key] += '%'
						}
					}
					this.colorVal = 'hsv(' + hsv.h + ',' + hsv.s + ',' + hsv.v + ')'
				} else if (this.colorFormat == 'hsl') {
					this.colorVal = 'hsl(' + hsl.h + ',' + hsl.s + ',' + hsl.l + ')'
				} else if (this.colorFormat == 'rgba') {
					this.colorVal = this.colorFormat + '(' + rgb.r + ',' + rgb.g + ',' + rgb.b + ',' + rgb.a + ')'
				} else if (this.colorFormat == 'rgb') {
					this.colorVal = this.colorFormat + '(' + rgb.r + ',' + rgb.g + ',' + rgb.b + ')'
				} else {
					this.colorVal = '#' + conversion.rgbToHex(rgb);
				}
			},
			setControl(index, x) {
				const {
					top,
					left,
					width,
					height
				} = this.position[index];

				if (index === 1) {
					this.hsb.h = parseInt((360 * x) / width);
					this.bgcolor = conversion.HSBToRGB({
						h: this.hsb.h,
						s: 100,
						b: 100
					});
					this.setColor()
				} else {
					this.rgba.a = (x / width).toFixed(1);
				}
				this.setValue(this.rgba);
			},
			setColorBySelect(getrgb) {
				const {
					r,
					g,
					b,
					a
				} = getrgb;
				let rgb = {}
				rgb = {
					r: r ? parseInt(r) : 0,
					g: g ? parseInt(g) : 0,
					b: b ? parseInt(b) : 0,
					a: a ? a : 0,
				};
				this.rgba = rgb;
				this.hsb = conversion.rgbToHsb(rgb);
				this.changeViewByHsb();
			},
			changeViewByHsb() {
				const [a, b, c] = this.position;
				this.site[0].left = parseInt(this.hsb.s * a.width / 100);
				this.site[0].top = parseInt((100 - this.hsb.b) * a.height / 100);
				this.setColor(this.hsb.h);
				this.setValue(this.rgba);
				this.bgcolor = conversion.HSBToRGB({
					h: this.hsb.h,
					s: 100,
					b: 100
				});
				this.site[1].left = this.hsb.h / 360 * b.width;
				if (this.colorFormat == 'rgba') {
					this.site[2].left = this.rgba.a * c.width;
				}
			},
			getSelectorQuery() {
				const views = uni.createSelectorQuery().in(this);
				views.selectAll('.boxs').boundingClientRect(data => {
						if (!data || data.length === 0) {
							this.getSelectorQuery()
							return
						}
						this.position = data;
						// this.site[0].top = data[0].height;
						// this.site[0].left = 0;
						// this.site[1].left = data[1].width;
						// this.site[2].left = data[2].width;
						this.setColorBySelect(this.rgba);
					})
					.exec();
			}
		}
	};
</script>

<style>
	.t-wrapper {
		position: fixed;
		top: 0;
		bottom: 0;
		left: 0;
		width: 100%;
		box-sizing: border-box;
		z-index: 9999;
	}

	.t-box {
		width: 100%;
		position: absolute;
		bottom: 0;
		padding: 30rpx 0;
		padding-top: 0;
		background: #fff;
		transition: all 0.3s;
		transform: translateY(100%);
	}

	.t-box.active {
		transform: translateY(0%);
	}

	.t-header {
		display: flex;
		justify-content: space-between;
		width: 100%;
		height: 100rpx;
		border-bottom: 1px #eee solid;
		box-shadow: 1px 0 2px rgba(0, 0, 0, 0.1);
		background: #fff;
	}

	.t-header-button {
		display: flex;
		align-items: center;
		width: 150rpx;
		height: 100rpx;
		font-size: 30rpx;
		color: #666;
		padding-left: 20rpx;
	}

	.t-header-button:last-child {
		justify-content: flex-end;
		padding-right: 20rpx;
	}

	.t-mask {
		position: absolute;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		background: rgba(0, 0, 0, 0.6);
		z-index: -1;
		transition: all 0.3s;
		opacity: 0;
	}

	.t-mask.active {
		opacity: 1;
	}

	.t-color__box {
		position: relative;
		height: 400rpx;
		background: rgb(255, 0, 0);
		overflow: hidden;
		box-sizing: border-box;
		margin: 0 20rpx;
		margin-top: 20rpx;
		box-sizing: border-box;
	}

	.t-background {
		position: absolute;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		background: linear-gradient(to right, #fff, rgba(255, 255, 255, 0));
	}

	.t-color-mask {
		position: absolute;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		width: 100%;
		height: 400rpx;
		background: linear-gradient(to top, #000, rgba(0, 0, 0, 0));
	}

	.t-pointer {
		position: absolute;
		bottom: -8px;
		left: -8px;
		z-index: 2;
		width: 15px;
		height: 15px;
		border: 1px #fff solid;
		border-radius: 50%;
	}

	.t-show-color {
		width: 100rpx;
		height: 50rpx;
	}

	.t-control__box {
		margin-top: 50rpx;
		width: 100%;
		display: flex;
		padding-left: 20rpx;
		box-sizing: border-box;
	}

	.t-control__color {
		flex-shrink: 0;
		width: 100rpx;
		height: 100rpx;
		border-radius: 50%;
		background-color: #fff;
		background-image: linear-gradient(45deg, #eee 25%, transparent 25%, transparent 75%, #eee 75%, #eee),
			linear-gradient(45deg, #eee 25%, transparent 25%, transparent 75%, #eee 75%, #eee);
		background-size: 36rpx 36rpx;
		background-position: 0 0, 18rpx 18rpx;
		border: 1px #eee solid;
		overflow: hidden;
	}

	.t-control__color-content {
		width: 100%;
		height: 100%;
	}

	.t-control-box__item {
		display: flex;
		flex-direction: column;
		justify-content: space-between;
		width: 100%;
		padding: 0 30rpx;
	}

	.t-controller {
		position: relative;
		width: 100%;
		height: 16px;
		background-color: #fff;
		background-image: linear-gradient(45deg, #eee 25%, transparent 25%, transparent 75%, #eee 75%, #eee),
			linear-gradient(45deg, #eee 25%, transparent 25%, transparent 75%, #eee 75%, #eee);
		background-size: 32rpx 32rpx;
		background-position: 0 0, 16rpx 16rpx;
	}

	.t-hue {
		width: 100%;
		height: 100%;
		background: linear-gradient(to right, #f00 0%, #ff0 17%, #0f0 33%, #0ff 50%, #00f 67%, #f0f 83%, #f00 100%);
	}

	.t-transparency {
		width: 100%;
		height: 100%;
		background: linear-gradient(to right, rgba(0, 0, 0, 0) 0%, rgb(0, 0, 0));
	}

	.t-circle {
		position: absolute;
		/* right: -10px; */
		top: -2px;
		width: 20px;
		height: 20px;
		box-sizing: border-box;
		border-radius: 50%;
		background: #fff;
		box-shadow: 0 0 2px 1px rgba(0, 0, 0, 0.1);
	}

	.t-result__box {
		margin-top: 20rpx;
		padding: 10rpx;
		width: 100%;
		display: flex;
		box-sizing: border-box;
	}

	.t-result__item {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		padding: 10rpx;
		width: 100%;
		box-sizing: border-box;
	}

	.t-result__box-input {
		padding: 10rpx 0;
		width: 100%;
		font-size: 28rpx;
		box-shadow: 0 0 1px 1px rgba(0, 0, 0, 0.1);
		color: #999;
		text-align: center;
		background: #fff;
	}

	.t-result__box-text {
		margin-top: 10rpx;
		font-size: 28rpx;
		line-height: 2;
	}

	.t-select {
		flex-shrink: 0;
		width: 150rpx;
		padding: 0 30rpx;
	}

	.t-select .t-result__box-input {
		border-radius: 10rpx;
		border: none;
		color: #999;
		box-shadow: 1px 1px 2px 1px rgba(0, 0, 0, 0.1);
		background: #fff;
	}

	.t-select .t-result__box-input:active {
		box-shadow: 0px 0px 1px 0px rgba(0, 0, 0, 0.1);
	}

	.t-alternative {
		display: flex;
		flex-wrap: wrap;
		/* justify-content: space-between; */
		width: 100%;
		padding-right: 10rpx;
		box-sizing: border-box;
	}

	.t-alternative__item {
		margin-left: 12rpx;
		margin-top: 10rpx;
		width: 50rpx;
		height: 50rpx;
		border-radius: 10rpx;
		background-color: #fff;
		background-image: linear-gradient(45deg, #eee 25%, transparent 25%, transparent 75%, #eee 75%, #eee),
			linear-gradient(45deg, #eee 25%, transparent 25%, transparent 75%, #eee 75%, #eee);
		background-size: 36rpx 36rpx;
		background-position: 0 0, 18rpx 18rpx;
		border: 1px #eee solid;
		overflow: hidden;
	}

	.t-alternative__item-content {
		width: 50rpx;
		height: 50rpx;
		background: rgba(255, 0, 0, 0.5);
	}

	.t-alternative__item:active {
		transition: all 0.3s;
		transform: scale(1.1);
	}
</style>
