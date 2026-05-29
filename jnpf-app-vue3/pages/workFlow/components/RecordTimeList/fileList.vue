<template>
	<view class="" style="padding: 20rpx 0;">
		<view v-for='(item,index) in fileList' :key="index"
			class="jnpf-file-item u-type-primary u-flex u-line-1 u-m-t-10" @tap='downLoad(item)'>
			<view class="jnpf-file-item-txt u-line-1">{{item.name}}</view>
		</view>
	</view>
</template>
<script>
	const imgTypeList = ['png', 'jpg', 'jpeg', 'bmp', 'gif']
	import {
		getDownloadUrl
	} from '@/api/common'
	export default {
		props: {
			fileList: {
				type: Array,
				default: () => []
			}
		},
		data() {
			return {

			}
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			}
		},
		methods: {
			downLoad(item) {
				if (item.fileExtension && imgTypeList.includes(item.fileExtension)) return this.previewImage(item)
				// #ifdef MP
				this.previewFile(item)
				// #endif
				// #ifndef MP
				getDownloadUrl('annex', item.fileId).then(res => {
					const fileUrl = this.baseURL + res.data.url + '&name=' + item.name;
					// #ifdef H5
					window.location.href = fileUrl;
					// #endif
					// #ifdef APP-PLUS
					this.downloadFile(res.data.url);
					// #endif
				})
				// #endif
			},
			previewFile(item) {
				let fileTypes = ['doc', 'xls', 'ppt', 'pdf', 'docx', 'xlsx', 'pptx']
				let url = item.url
				let fileType = url.split('.')[1]
				if (fileTypes.includes(fileType)) {
					uni.downloadFile({
						url: this.baseURL + url,
						success: (res) => {
							var filePath = res.tempFilePath;
							uni.openDocument({
								filePath: encodeURI(filePath),
								showMenu: true,
								fileType: fileType,
								success: (res) => {
									console.log('打开文档成功');
								},
								fail(err) {
									console.log('小程序', err);
								}
							});
						}
					});
				} else {
					this.$u.toast(
						'该文件类型无法打开'
					)
				}
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
			downloadFile(url) {
				uni.downloadFile({
					url: this.baseURL + url,
					success: res => {
						if (res.statusCode === 200) {
							uni.saveFile({
								tempFilePath: res.tempFilePath,
								success: red => {
									uni.showToast({
										icon: 'none',
										mask: true,
										title: '文件已保存：' + red.savedFilePath, //保存路径
										duration: 3000,
									});
									setTimeout(() => {
										uni.openDocument({
											filePath: red.savedFilePath,
											success: ress => {},
											fail(err) {}
										});
									}, 500)
								}
							});
						}
					}
				});
			},
		}
	}
</script>

<style>
</style>