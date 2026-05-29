<template>
	<view class="messageDetail-v u-p-l-20 u-p-r-20">
		<view class="u-flex-col u-border-bottom u-p-b-40 u-m-b-40">
			<text class="u-m-b-16 u-font-32 txt">{{info.title}}</text>
			<view>
				<text class="releaseUser u-font-24 ">{{info.releaseUser}}</text>
				<text
					class="releaseUser u-font-24 u-m-l-16">{{ info.releaseTime?$u.timeFormat(info.releaseTime, 'yyyy-mm-dd hh:MM:ss'):''}}</text>
			</view>
		</view>
		<view class="u-p-b-16 excerpt" v-if="info.excerpt">
			{{info.excerpt}}
		</view>
		<view class="messageDetail-content u-p-b-20 ">
			<mp-html :content="info.bodyText" />
		</view>
		<view class="file-box">
			<view class="file-list u-flex" v-for="(item,index) in fileList" :key="index">
				<view class="file-list-l">
					<u-icon name="attach" color="#969799"></u-icon>
					<text class="fileName">{{item.name}}</text>
				</view>
				<u-icon name="download" color="#969799" @click="openFile(item)"></u-icon>
			</view>
		</view>
	</view>
</template>

<script>
	const imgTypeList = ['png', 'jpg', 'jpeg', 'bmp', 'gif']
	import {
		getMessageDetail
	} from '@/api/message.js'
	import {
		getDownloadUrl
	} from '@/api/common'
	export default {
		data() {
			return {
				info: {},
				fileList: []
			}
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			}
		},
		onLoad(option) {
			this.initDetail(option.id)
		},
		methods: {
			initDetail(id) {
				getMessageDetail(id).then(res => {
					this.info = res.data;
					this.fileList = JSON.parse(this.info.files)
					uni.$emit('initUnReadMsgNum')
				})
			},
			previewImage(item) {
				if (!item.url) return
				const url = this.baseURL + item.url
				uni.previewImage({
					urls: [url],
					current: url,
					success: () => {},
					fail: () => {
						uni.showToast({
							title: '预览图片失败',
							icon: 'none'
						});
					}
				});
			},
			openFile(item) {
				if (item.fileExtension && imgTypeList.includes(item.fileExtension)) return this.previewImage(item)
				// #ifdef MP
				this.previewFile(item)
				// #endif
				// #ifndef MP
				getDownloadUrl('annex', item.fileId).then(res => {
					// #ifdef H5
					window.location.href = this.baseURL + res.data.url + '&name=' + item.name;
					// #endif
					// #ifndef H5
					uni.downloadFile({
						url: this.baseURL + res.data.url + '&name=' + item.name,
						success: function(res) {
							var filePath = res.tempFilePath;
							uni.openDocument({
								filePath: encodeURI(filePath),
								showMenu: true,
								success: function(res) {

								}
							});
						}
					});
					// #endif
				})
				// #endif
			},
			previewFile(item) {
				let url = item.url
				uni.downloadFile({
					url: this.baseURL + url,
					success: (res) => {
						var filePath = res.tempFilePath;
						uni.openDocument({
							filePath: encodeURI(filePath),
							success: (res) => {
								console.log('打开文档成功');
							}
						});
					}
				});
			},
		}
	}
</script>

<style lang="scss">
	.messageDetail-v {
		.excerpt {
			word-break: break-all;
		}

		.txt {
			font-weight: 700;
		}

		.messageDetail-content {
			color: #606266;
			word-break: break-all;
		}

		.releaseUser {
			color: #606266;
		}

		.file-box {
			.file-list {
				margin-top: 20rpx;
				justify-content: space-between;

				.file-list-l {
					.fileName {
						margin-left: 10rpx;
					}
				}
			}
		}
	}
</style>