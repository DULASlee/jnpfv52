<template>
	<view class="jnpf-location">
		<u-button class="jnpf-location-btn" @click="handleLocation" v-if="!detailed" size="mini">
			<u-icon class="jnpf-location-icon" name="map" />
			{{ innerValue.fullAddress ||errTitle ? $t('app.apply.location.relocation') : $t('app.apply.location.location') }}
		</u-button>
		<view class="location-card" v-if="innerValue.fullAddress">
			<view class="location-card-info" @click="getLocation">
				<image class="location-card-static-map" :src="staticMapUrl" v-if="enableLocationScope" />
				<view class="location-card-address">{{ innerValue.fullAddress }}</view>
				<u-icon name="close-circle-fill" v-if="!detailed && !disabled && clearable" @click="handleClear" />
			</view>
		</view>
		<view v-if="errTitle" class="errTitle">{{errTitle}}</view>
	</view>
</template>
<script>
	import {
		getAddress
	} from '@/api/common.js'
	const defaultValue = {
		lat: '',
		lng: '',
		name: '',
		fullAddress: '',
	};
	export default {
		name: 'jnpf-location',
		props: {
			modelValue: {
				type: String,
				default: ''
			},
			autoLocation: {
				type: Boolean,
				default: false
			},
			enableLocationScope: {
				type: Boolean,
				default: false
			},
			adjustmentScope: {
				type: Number,
				default: 500
			},
			enableDesktopLocation: {
				type: Boolean,
				default: false
			},
			locationScope: {
				type: Array,
				default: []
			},
			disabled: {
				type: Boolean,
				default: false
			},
			detailed: {
				type: Boolean,
				default: false
			},
			clearable: {
				type: Boolean,
				default: false
			},
		},
		data() {
			return {
				innerValue: '',
				errTitle: '',
				emitKey: ''
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					this.innerValue = val ? JSON.parse(val) : defaultValue
				},
				immediate: true,
			},
		},
		computed: {
			staticMapUrl() {
				if (!this.enableLocationScope) return ' ';
				const location = this.innerValue.lng + ',' + this.innerValue.lat;
				const url =
					`${this.define.baseURL}/api/system/Location/staticmap?location=${location}&zoom=19&size=80*80&key=${this.define.aMapWebKey}`;
				return url;
			}
		},
		created() {
			this.errTitle = ''
			this.handleAutoLocation()
			this.handleListen()
		},
		methods: {
			handleListen() {
				this.emitKey = 'location' + this.jnpf.idGenerator()
				uni.$on(this.emitKey, data => {
					this.handleConfirm(data)
				})
			},
			handleLocation(val) {
				if (this.disabled || this.detailed) return
				const data = {
					adjustmentScope: this.adjustmentScope,
					enableLocationScope: this.enableLocationScope,
					enableDesktopLocation: this.enableDesktopLocation,
					locationScope: this.locationScope,
					emitKey: this.emitKey
				}
				uni.navigateTo({
					url: '/pages/apply/location/index?data=' + JSON.stringify(data)
				})
			},
			handleAutoLocation() {
				if (!this.autoLocation || this.innerValue.fullAddress || this.detailed) return;
				uni.getLocation({
					type: 'gcj02',
					isHighAccuracy: true,
					success: (e) => {
						const getAddressFun = () => {
							const query = {
								location: e.longitude + ',' + e.latitude,
								key: this.define.aMapWebKey
							}
							getAddress(query).then(res => {
								const data = res.data.regeocode.addressComponent;
								this.innerValue = {
									pName: data.province,
									cName: data.city,
									adName: data.district,
									address: data.streetNumber.street + data.streetNumber
										.number,
									name: res.data.regeocode.formatted_address,
									lng: e.longitude,
									lat: e.latitude,
									fullAddress: res.data.regeocode.formatted_address,
								};
								this.$emit('update:modelValue', JSON.stringify(this.innerValue));
								this.$emit('change', JSON.stringify(this.innerValue));
							}).catch(() => {
								this.handelError()
							})
						}
						if (this.enableDesktopLocation && this.locationScope.length) {
							let list = [];
							for (let i = 0; i < this.locationScope.length; i++) {
								const o = this.locationScope[i];
								const discount = this.jnpf.getDistance(o.lat, o.lng, e.latitude, e
									.longitude) || 0;
								list.push(discount > o.radius);
							}
							if (list.every(o => o === true)) return;
							getAddressFun()
						} else {
							getAddressFun()
						}

					},
					fail: (err) => {
						this.handelError()
					}
				});
			},
			handleConfirm(item) {
				this.innerValue = item ? JSON.parse(item) : defaultValue
				this.errTitle = ''
				this.onchange()
			},
			handelError() {
				this.errTitle = '定位失败，请检查网络畅通、定位开启后重试'
			},
			handleClear() {
				this.innerValue = defaultValue;
				this.$emit('update:modelValue', '');
				this.$emit('change', '');
			},
			onchange() {
				let innerValue = this.$u.deepClone(this.innerValue)
				this.$emit('update:modelValue', JSON.stringify(innerValue))
				this.$emit('change', JSON.stringify(innerValue))
			},
			openMap() {
				uni.openLocation({
					latitude: Number(this.innerValue.lat),
					longitude: Number(this.innerValue.lng),
					name: this.innerValue.name,
					address: this.innerValue.address,
					success: () => {},
					fail: function(error) {
						console.log(error)
					}
				});
			},
			getLocation() {
				if (this.detailed) return this.openMap()
				if (this.enableLocationScope) this.handleLocation()
			}
		}
	}
</script>
<style lang="scss">
	.jnpf-location {
		width: 100%;
		display: flex;
		flex-wrap: wrap;
		justify-content: flex-end;

		.jnpf-location-btn {
			margin: unset;

			.jnpf-location-icon {
				font-size: 28rpx;
				padding-right: 2px;
			}
		}

		.location-card {
			display: flex;
			align-items: center;
			margin-top: 16rpx;
			background: #f2f2f6;
			padding: 16rpx;
			border-radius: 16rpx;
			justify-content: space-between;

			.location-card-info {
				flex: 1;
				display: flex;
				align-items: center;

				.location-card-static-map {
					width: 96rpx;
					height: 96rpx;
					margin-right: 8rpx;
					flex-shrink: 0;
				}

				.location-card-address {
					line-height: 1.5;
					padding: 0 8rpx;
					word-break: normal;
					white-space: normal;
				}
			}

			.location-card-actions {
				color: rgb(135, 143, 149);
				cursor: pointer;
				flex-shrink: 0;
			}
		}

		.errTitle {
			color: $u-type-error
		}
	}
</style>