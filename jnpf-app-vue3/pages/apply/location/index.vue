<template>
	<view class="jnpf-location-map">
		<u-top-tips ref="uTips" />
		<view class="content">
			<view class="user-select u-flex-col">
				<view class="user-select-search">
					<u-search :placeholder="$t('common.searchText')" v-model="keyword" height="72" :show-action="false" bg-color="#f0f2f6"
						shape="square" @change='search'>
					</u-search>
				</view>
			</view>
		</view>
		<view class="header">
			<view class="map-container">
				<map class='map' id="maps" :latitude="location.latitude" :longitude="location.longitude"
					:circles="circles" :polygons="polygons" :scale='15' @regionchange="regionChange">
					<!-- #ifdef H5 -->
					<cover-image class="map-marker h5-map-marker" src="/static/image/mark.png" />
					<!-- #endif -->
					<!-- #ifndef H5-->
					<cover-image class="map-marker" src="/static/image/mark.png" />
					<!-- #endif -->
					<cover-view class="map-locate" @click="handleLocate">
						<cover-image v-if="!locateLoading" src="/static/image/locate.png" />
						<cover-image v-else class="map-locate-img" src="/static/image/waite.png" />
					</cover-view>
				</map>
			</view>
		</view>
		<view class="around-contain">
			<scroll-view style="height:100%" id="scroll-view-h" class="scroll-view2" :refresher-enabled="false"
				:refresher-threshold="50" :scroll-with-animation='true' @scrolltolower="handleScrollToLower"
				:scroll-y="true">
				<radio-group class="around-contain-item" v-for="(item,index) in list" :key="index" v-if="list.length"
					@change="onSelectValueChange(item,index)">
					<label class="u-radio-label">
						<radio class="u-radio" :value="item.id" :checked="item.id === selectId" />
						<view class="around-item-title-box">
							<view class="around-item-title u-line-1"> {{ item.name }}</view>
							<view class="around-item-sub-title u-line-1"> {{ item.address }}</view>
						</view>
					</label>
				</radio-group>
				<u-loading class="loading" mode="circle" size="44" v-if="loading" />
				<view v-if="!loading&&!list.length" class="nodata u-flex-col">
					<image :src="noDataIcon" mode="widthFix" class="noDataIcon" />
					{{$t('common.noData')}}
				</view>
			</scroll-view>
		</view>
		<view class="jnpf-bottom-actions">
			<u-button class="buttom-btn" @click="close()">{{$t('common.cancelText')}}</u-button>
			<u-button class="buttom-btn" type="primary" @click.stop="handleConfirm()">{{$t('common.okText')}}</u-button>
		</view>
	</view>
</template>

<script>
	import resources from '@/libs/resources.js'
	import {
		getAroundList,
		getTextList
	} from '@/api/common.js'
	export default {
		data() {
			return {
				loading: false,
				noDataIcon: resources.message.nodata,
				tabWidth: 150,
				tabIndex: 0,
				keyword: '',
				location: {
					longitude: 116.404, // 经度
					latitude: 39.915, // 纬度
				},
				circles: [],
				list: [],
				pagination: {
					currentPage: 1,
					pageSize: 50
				},
				total: 0,
				currentLocation: {},
				selectId: '',
				selectItem: {},
				enableLocation: '',
				showPopup: false,
				locateLoading: false,
				polygons: [],
				enableLocationScope: false,
				adjustmentScope: 500,
				enableDesktopLocation: false,
				locationScope: [],
				emitKey: '',
				// #ifdef APP
				dragLoading: false
				// #endif
			};
		},
		onLoad(e) {
			const data = e.data ? JSON.parse(e.data) : {}
			this.enableLocationScope = data.enableLocationScope || false
			this.adjustmentScope = data.adjustmentScope || 500
			this.enableDesktopLocation = data.enableDesktopLocation || false
			this.locationScope = data.locationScope || []
			this.emitKey = data.emitKey
			this.init()
		},
		methods: {
			init() {
				this.circles = []
				this.polygons = []
				this.selectId = ''
				this.list = []
				this.locateLoading = false
				// #ifdef APP
				this.dragLoading = false
				// #endif
				this.getLocation()
			},
			getLocation() {
				this.loading = true;
				uni.getLocation({
					type: 'gcj02',
					isHighAccuracy: true,
					success: (res) => {
						this.location.longitude = res.longitude // 经度
						this.location.latitude = res.latitude // 纬度
						//查询附近位置
						this.getList()
						//添加可选区域圆形
						this.handelCircle();
						//添加微调区域圆形
						this.handleScopeCircle();
					},
					fail: (err) => {
						//查询附近位置
						this.getList()
						//添加可选区域圆形
						this.handelCircle();
						//添加微调区域圆形
						this.handleScopeCircle();
					}
				});
			},
			handleGetCenter() {
				this.mapContext = uni.createMapContext("maps", this);
				this.mapContext.getCenterLocation({
					type: 'gcj02',
					geocode: true,
					isHighAccuracy: true,
					altitude: true,
					success: (res) => {
						this.location.longitude = res.longitude
						this.location.latitude = res.latitude
						if (this.enableLocationScope) {
							const discount = this.jnpf.getDistance(this.currentLocation.latitude, this
								.currentLocation.longitude, this.location.latitude, this.location.longitude
							) || 0;
							if (discount > (this.adjustmentScope || 500)) return this.$refs.uTips.show({
								title: '超出微调范围',
								type: 'warning',
							});
						}
						this.getList()
					}
				})
			},
			handelCircle() {
				if (!this.enableDesktopLocation || !this.locationScope.length) return;
				for (let i = 0; i < this.locationScope.length; i++) {
					const o = this.locationScope[i];
					if (!o.lng || !o.lat || !o.radius) continue;
					o.longitude = o.lng
					o.latitude = o.lat
					this.addCircle({
						...o,
						color: '#54d65e99',
						fillColor: '#54d65e66',
					});
				}
			},
			handleScopeCircle() {
				if (!this.enableLocationScope) return;
				this.currentLocation = this.$u.deepClone(this.location);
				this.addCircle({
					...this.location,
					radius: this.adjustmentScope || 500,
					color: '#1890ff99',
					fillColor: '#1890ff66'
				});
			},
			addCircle(o) {
				// #ifdef H5
				this.polygons.push({
					points: this.CreateSimpleCircle(o.latitude, o.longitude, o.radius, 100),
					strokeColor: o.color,
					fillColor: o.fillColor,
					strokeWidth: 1
				})
				// #endif
				// #ifndef H5
				this.circles.push({
					...o,
					strokeWidth: 1,
				})
				// #endif
			},
			// #ifdef H5
			CreateSimpleCircle(lat, lng, radius, pointCount) {
				var km = radius / 1000;
				var a = km < 5 ? 0.01 : km < 50 ? 0.1 : km < 500 ? 1 : 10;
				var b = this.getCircleDistance(lng, lat, lng + a, lat);
				var c = this.getCircleDistance(lng, lat, lng, lat + a);
				var rb = radius / b * a;
				var rc = radius / c * a;
				var arr = [];
				var n = 0,
					step = 360.0 / pointCount,
					N = 360 - step / 2; //注意浮点数±0.000000001的差异
				for (var i = 0; n < N; i++, n += step) {
					var x = lng + rb * Math.cos(n * Math.PI / 180);
					var y = lat + rc * Math.sin(n * Math.PI / 180);
					arr[i] = {
						latitude: y,
						longitude: x
					}
				}
				arr.push({
					latitude: arr[0].latitude,
					longitude: arr[0].longitude
				});
				return arr;
			},
			getCircleDistance(lng1, lat1, lng2, lat2) {
				var d = Math.PI / 180;
				var f = lat1 * d,
					h = lat2 * d;
				var i = lng2 * d - lng1 * d;
				var e = (1 - Math.cos(h - f) + (1 - Math.cos(i)) * Math.cos(f) * Math.cos(h)) / 2;
				return 2 * 6378137 * Math.asin(Math.sqrt(e));
			},
			// #endif
			regionChange(e) {
				// #ifdef APP
				if (this.dragLoading) return (this.dragLoading = false);
				this.list = [];
				this.handleGetCenter()
				// #endif
				// #ifndef APP
				if (e.detail.causedBy == 'drag' && e.type == 'end') {
					this.list = [];
					this.handleGetCenter()
				}
				// #endif
			},
			handleScrollToLower() {
				if (this.pagination.pageSize * this.pagination.currentPage < this.total) {
					this.pagination.currentPage = this.pagination.currentPage + 1;
					this.getList()
				} else {
					this.$u.toast('没有更多信息啦！')
				}
			},
			getList() {
				this.loading = true;
				const query = {
					key: this.define.aMapWebKey,
					location: this.location.longitude + ',' + this.location.latitude,
					radius: -1,
					offset: this.pagination.pageSize,
					page: this.pagination.currentPage,
				};
				getAroundList(query).then(res => {
					this.handleResult(res)
				}).catch(() => {
					this.loading = false;
				})
			},
			handleResult(res) {
				this.loading = false;
				if (res.data.status == '1') {
					this.list = [...this.list, ...res.data.pois || []];
					this.total = Number(res.data.count || 0)
				} else {
					this.$u.toast(res.data.info)
				}
			},
			onSelectValueChange(item, index) {
				// #ifdef APP
				this.dragLoading = true
				// #endif
				this.selectStatus = true
				this.selectId = item.id
				this.selectItem = item
				const [longitude, latitude] = (item.location || '').split(',');
				this.location = {
					longitude,
					latitude
				};
				// #ifdef APP
				setTimeout(() => {
					this.dragLoading = false
				}, 800)
				// #endif
			},
			handleConfirm() {
				if (!this.selectId) return this.$u.toast('请选择地址')
				const data = this.selectItem
				const [lng, lat] = data.location.split(',');
				if (this.enableLocationScope) {
					const discount = this.jnpf.getDistance(this.currentLocation.latitude, this.currentLocation
						.longitude, lat,
						lng) || 0;
					if (discount > (this.adjustmentScope || 500)) return this.$refs.uTips.show({
						title: '超出微调范围',
						type: 'warning',
					});
				}
				//判断可选范围
				if (this.enableDesktopLocation && this.locationScope.length) {
					let list = [];
					for (let i = 0; i < this.locationScope.length; i++) {
						const o = this.locationScope[i];
						const discount = this.jnpf.getDistance(o.lat, o.lng, lat, lng) || 0;
						list.push(discount > o.radius);
					}
					if (list.every(o => o === true)) return this.$refs.uTips.show({
						title: '超出规定范围',
						type: 'warning',
					});
				}
				const address = data.address && data.address.length ? data.address : '';
				//台湾、北京、上海、重庆、深圳地址特殊处理
				let fullAddress = data.pname + data.cityname + data.adname + address + data.name;
				if (data.pname == data.cityname) fullAddress = data.pname + data.adname + address + data.name;
				if (data.pname == data.cityname && data.pname == data.adname) fullAddress = data.pname + address +
					data.name;
				this.innerValue = {
					pName: data.pname,
					cName: data.cityname,
					adName: data.adname,
					address,
					name: data.name,
					lng,
					lat,
					fullAddress,
				};
				uni.$emit(this.emitKey, JSON.stringify(this.innerValue))
				this.close();
			},
			close() {
				uni.navigateBack({
					delta: 1
				});
			},
			getDistance(lat1, lon1, lat2, lon2) {
				const toRadians = (degrees) => {
					return degrees * (Math.PI / 180);
				}
				const R = 6371;
				const dLat = toRadians(lat2 - lat1);
				const dLon = toRadians(lon2 - lon1);
				const a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
					Math.cos(toRadians(lat1)) * Math.cos(toRadians(lat2)) *
					Math.sin(dLon / 2) * Math.sin(dLon / 2);
				const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
				const distance = R * c;
				return distance * 1000;
			},
			search() {
				// 节流,避免输入过快多次请求
				this.searchTimer && clearTimeout(this.searchTimer)
				this.searchTimer = setTimeout(() => {
					this.list = [];
					this.pagination.currentPage = 1
					this.keyword ? this.handleSearch() : this.getList();
				}, 300)
			},
			handleSearch() {
				this.loading = true;
				const query = {
					key: this.define.aMapWebKey,
					keywords: this.keyword,
					radius: this.enableLocationScope ? this.adjustmentScope || 500 : -1,
					offset: this.pagination.pageSize,
					page: this.pagination.currentPage,
				};
				getTextList(query).then(res => {
					this.handleResult(res);
				});
			},
			handleLocate() {
				if (this.locateLoading) return
				this.locateLoading = true
				uni.getLocation({
					type: 'gcj02',
					isHighAccuracy: true,
					success: (res) => {
						this.locateLoading = false
						if (!res.longitude || !res.latitude) return
						this.mapContext = uni.createMapContext("maps", this);
						this.mapContext.moveToLocation({
							longitude: res.longitude,
							latitude: res.latitude,
						})
					},
					fail: (res) => {
						this.locateLoading = false
						this.$u.toast('获取定位失败')
					}
				})
			},
		}
	};
</script>

<style scoped lang="scss">
	.jnpf-location-map {
		/* #ifdef H5 */
		height: calc(100vh - 44px);
		/* #endif */
		/* #ifndef H5 */
		height: 100vh;
		/* #endif */

		display: flex;
		flex-direction: column;

		.header {
			.map-container {
				position: relative;
				padding: 0rpx 20rpx;

				.map {
					width: 100%;
					height: 600rpx;
				}

				.map-marker {
					width: 38rpx;
					height: 64rpx;
					position: absolute;
					top: 50%;
					left: 50%;
					transform: translate(-50%, calc(-50% - 20rpx));
					z-index: 9999;
				}

				.h5-map-marker {
					transform: translate(-50%, calc(-50% - 30rpx));
				}

				.map-locate {
					position: absolute;
					bottom: 10px;
					right: 10px;
					height: 24px;
					width: 24px;
					padding: 4px;
					background-color: #fff;
					border-radius: 50%;
					box-shadow: 0 0 5px silver;
					z-index: 999;

					.map-locate-img {
						-webkit-animation: rotate 2s linear infinite;
					}

					@keyframes rotate {
						0% {
							-webkit-transform: rotate(0deg);
						}

						25% {
							-webkit-transform: rotate(90deg);
						}

						50% {
							-webkit-transform: rotate(180deg);
						}

						75% {
							-webkit-transform: rotate(270deg);
						}

						100% {
							-webkit-transform: rotate(1turn);
						}
					}
				}
			}
		}

		.content {
			width: 100%;

			.user-select {
				.user-select-search {
					padding: 0rpx 20rpx;
					margin: 20rpx 0;
				}
			}
		}

		.around-contain {
			flex: 1;
			width: 100%;
			overflow: hidden;

			.loading {
				display: flex;
				justify-content: center;
				margin: 250rpx auto 0;
			}

			.around-contain-item {
				display: flex;
				align-items: center;
				padding: 10rpx 0;
				height: 60px;
				line-height: 22px;
				border-bottom: 1px solid #f2f2f6;

				.u-radio-label {
					width: 100%;
					display: flex;
					align-items: center;

					//  #ifdef MP
					:deep(.u-radio) {
						margin: 0 16rpx 0 20rpx;

					}

					// #endif
					//  #ifndef MP
					:deep(.uni-radio-input) {
						margin: 0 16rpx 0 20rpx;
					}

					// #endif
					.around-item-title-box {
						flex: 1;
						min-width: 0;
						padding-right: 16rpx;

						.around-item-title {
							font-size: 30rpx;
							color: #171a1d;
						}

						.around-item-sub-title {
							font-size: 28rpx;
							color: #b9babb;
							padding-top: 8rpx;
						}
					}
				}
			}
		}
	}
</style>