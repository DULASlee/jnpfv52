<template>
	<view>
		<view class="page_v u-flex-col">
			<view>
				<view v-if="show" v-for="(item,index) in signImg" :key="index" :class="item.isDefault ? 'active' : '' "
					class="lists_box" @longpress="handleTouchStart(item,index)">
					<view class="signImgBox">
						<image :src="item.signImg" mode="scaleToFill" class="signImg"></image>
					</view>
					<view class="icon-checked-box" v-if="item.isDefault">
						<view class="icon-checked">
							<u-icon name="checkbox-mark" color="#fff" size="28"></u-icon>
						</view>
					</view>
					<view class="sign-mask" v-if="!item.isDefault && item.isSet" :id="index">
						<view class="sign-mask-btn">
							<u-button @click.prevent="del(item.id,index)">删除</u-button>
							<u-button type="primary" @click.prevent="setDefault(item.id,index)">设为默认</u-button>
						</view>
					</view>
				</view>
			</view>
			<JnpfSign ref="signRef" @change="signData" :showBtn="false" />
			<NoData v-if="!show" :paddingTop="460"></NoData>
		</view>
		<view class="flowBefore-actions">
			<u-button class="buttom-btn" type="primary" @click='showAction = true'>添加签名</u-button>
		</view>
		<u-action-sheet @click="handleAction" :list="actionList" :tips="{ text: '' , color: '#000' , fontSize: 30 }"
			v-model="showAction">
		</u-action-sheet>
	</view>
</template>
<script>
	import NoData from '@/components/noData'
	import {
		pathToBase64
	} from '@/libs/file.js'
	import {
		getSignImgList,
		createSignImg,
		setDefSignImg,
		delSignImg
	} from '@/api/common'
	export default {
		components: {
			NoData
		},
		data() {
			return {
				value: '',
				show: true,
				signImg: [],
				isSet: false,
				showAction: false,
				actionList: [{
						text: '在线签名',
						id: 1
					},
					{
						text: '图片上传',
						id: 2
					}
				]
			}
		},
		computed: {
			baseURL() {
				return this.define.comUploadUrl
			},
			token() {
				return uni.getStorageSync('token')
			}
		},
		mounted() {
			this.getSignImgList()
		},
		methods: {
			getSignImgList() {
				getSignImgList().then(res => {
					let signList = JSON.parse(JSON.stringify(res.data)) || []
					this.show = signList.length > 0 ? true : false
					this.signImg = signList.map(o => ({
						isSet: false,
						...o
					}))
				})
			},
			signData(e) {
				if (e) {
					let data = {
						'signImg': e,
						'isDefault': 0
					}
					createSignImg(data).then((res) => {
						this.getSignImgList()
					})
				}
			},
			handleTouchStart(item, index) {
				this.signImg.map((o, i) => {
					o.isSet = false
				})
				item.isSet = true
			},
			del(id, index) {
				delSignImg(id, index).then((res) => {
					this.signImg.splice(index, 1)
				})
			},
			setDefault(id, index) {
				let userInfo = uni.getStorageSync('userInfo')
				setDefSignImg(id).then((res) => {
					this.signImg.map((o, i) => {
						o.isDefault = false;
						if (index == i) {
							o.isDefault = true
							o.isSet = false
							userInfo.signImg = o.signImg
							uni.setStorageSync('userInfo', userInfo)
						}
					})

				})
			},
			handleAction(e) {
				if (e == 0) {
					this.$refs.signRef.addSign();
				} else {
					uni.chooseImage({
						count: 1, //默认9
						sizeType: ['original', 'compressed'], //可以指定是原图还是压缩图，默认二者都有
						sourceType: ['album'],
						success: (res) => {
							let tempFilePaths = res.tempFilePaths[0]
							// #ifdef H5
							let isAccept = new RegExp('image/*').test(res.tempFiles[0].type)
							if (!isAccept) return this.$u.toast(`请上传图片`)
							// #endif
							if ((res.tempFiles[0].size / 1024) > 500) return this.$u.toast('操作失败，图片大小超出500K')
							// #ifdef APP-HARMONY
							this.harmony(tempFilePaths)
							// #endif
							// #ifndef APP-HARMONY
							pathToBase64(tempFilePaths).then(base64 => {
								this.signData(base64)
							})
							// #endif
						}
					});
				}
			},
			harmony(tempFilePaths) {
				uni.uploadFile({
					url: this.baseURL + 'imgToBase64',
					filePath: tempFilePaths,
					name: 'file',
					header: {
						'Authorization': this.token
					},
					success: (uploadFileRes) => {
						let res = JSON.parse(uploadFileRes.data)
						this.signData(res.data)
					},
					fail: (err) => {}
				});
			}
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.page_v {
		height: 100%;
		padding: 0 20rpx;

		.active {
			border: 1rpx solid #2979FF;
			color: #2979FF;

			.icon-ym-organization {
				&::before {
					color: #2979FF !important;
				}
			}
		}

		.sign-mask {
			width: 100%;
			height: 200rpx;
			background: rgba(0, 0, 0, .3);
			position: absolute;
			top: 0;
			border-radius: 12rpx;
			display: flex;
			align-items: center;
			flex-direction: column;
			justify-content: center;

			.sign-mask-btn {
				width: 60%;
				display: flex;
			}
		}

		.lists_box {
			width: 100%;
			height: 200rpx;
			border-radius: 8rpx;
			position: relative;
			display: flex;
			flex-direction: column;
			justify-content: center;
			background-color: #FFFFFF;
			margin-bottom: 20rpx;
			overflow: hidden;

			.signImgBox {
				width: 100%;
				height: 100%;
				text-align: center;

				.signImg {
					width: 100%;
					height: 100%;
				}
			}

			.icon-checked-box {
				display: flex;
				width: 140rpx;
				height: 80rpx;
				position: absolute;
				transform: scale(0.9);
				right: -4rpx;
				bottom: -2rpx;
				flex-direction: row;
				align-items: center;


				.icon-checked {
					width: 44rpx;
					height: 44rpx;
					border: 40rpx solid #1890ff;
					border-left: 40rpx solid transparent;
					border-top: 40rpx solid transparent;
					border-bottom-right-radius: 12rpx;
					position: absolute;
					transform: scale(0.95);
					right: -8rpx;
					bottom: -6rpx;
				}
			}
		}
	}
</style>