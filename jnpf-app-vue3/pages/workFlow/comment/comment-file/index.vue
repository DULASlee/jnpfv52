<template>
	<view class="jnpf-file">
		<view class="jnpf-file-box" :style="{textAlign:align}">
			<view v-if="!detailed &&!disabled " class="jnpf-file-box-line">
				<CommentLsjUpload :ref="lsjUpload" :childId="childId" :width="width" :height="height" :option="option"
					:size="fileSize" :formats="getFormats" :instantly="instantly" @uploadEnd="onuploadEnd"
					:lsjUpload="lsjUpload" v-if="!disabled"
					:currentCount="currentCount">
					<view class="icon-ym icon-ym-comment-file" size="mini"></view>
				</CommentLsjUpload>
				<view class="icon-ym icon-ym-comment-file" size="mini" v-else @click="onCountOver"></view>
			</view>
			<view class="icon-ym icon-ym-comment-file" size="mini" v-if="disabled" @click="onCountOver"></view>
			<view class="tipText u-p-l-20">
				{{tipText}}
			</view>
		</view>
	</view>
</template>

<script>
	import CommentLsjUpload from './lsj-upload/lsj-upload.vue'

	import {
		getDownloadUrl
	} from '@/api/common'

	const imgTypeList = ['png', 'jpg', 'jpeg', 'bmp', 'gif']
	export default {
		components: {
			CommentLsjUpload
		},
		name: 'jnpf-upload-img',
		inheritAttrs: false,
		props: {
			modelValue: {
				type: Array,
				default: () => ([])
			},
			limit: {
				type: [Number, String],
				default: 9
			},
			fileSize: {
				type: Number,
				default: 10
			},
			sizeUnit: {
				type: String,
				default: 'MB'
			},
			accept: {
				type: String,
				default: ''
			},
			pathType: {
				type: String,
				default: 'defaultPath'
			},
			tipText: {
				type: String,
				default: ''
			},
			isAccount: {
				type: Number,
				default: 0
			},
			folder: {
				type: String,
				default: ''
			},
			vModel: {
				type: String,
				default: ''
			},
			detailed: {
				type: Boolean,
				default: false
			},
			align: {
				type: String,
				default: 'right'
			},
			currentCount: {
				type: Number,
				default: 0
			}
		},
		data() {
			return {
				percent: '',
				fileList: [],
				// 上传接口参数
				option: {},
				params: {
					pathType: this.pathType,
					isAccount: this.isAccount,
					folder: this.folder
				},
				// 选择文件后是否立即自动上传，true=选择后立即上传
				instantly: true,
				size: 30,
				list: [],
				deletable: false,
				childId: 'upload' + this.$u.guid(3, false, 2),
				lsjUpload: 'lsjUpload' + this.$u.guid(3, false, 2),
				width: '48rpx',
				height: '48rpx',
				disabled: false
			}
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			},
			comUploadUrl() {
				return this.define.comUploadUrl
			},
			getFormats() {
				let formats = this.accept
				formats = formats.replace("image/*", 'png,jpg,jpeg,bmp,gif,webp,psd,svg,tiff')
				formats = formats.replace("video/*", 'avi,wmv,mpg,mpeg,mov,rm,ram,swf,flv,mp4,wma,rm,rmvb,flv,mpg,mkv')
				formats = formats.replace("audio/*", 'mp3,wav,aif,midi,m4a')
				return formats
			},
		},
		created() {
			const token = uni.getStorageSync('token')
			this.option = {
				url: this.baseURL + '/api/file/Uploader/annex',
				name: 'file',
				header: {
					'Authorization': token,
					'uid': '27682',
					'client': 'app',
					'accountid': 'DP',
				},
				data: this.params
			}
		},
		watch: {
			modelValue: {
				handler(val) {
					this.fileList = JSON.parse(JSON.stringify(val));
				},
				immediate: true
			}
		},
		methods: {
			onCountOver() {
				uni.showToast({
					title: `最多可以上传${this.limit}个文件`,
					icon: 'none'
				});
				return;
			},
			// 某文件上传结束回调(成功失败都回调)
			onuploadEnd(item) {
				if (this.currentCount >= this.limit) {
					// this.disabled = true;
					return this.$u.toast('最多可以上传' + this.limit + '个文件')
				}
				if (item['responseText']) {
					let response = JSON.parse(item.responseText)
					let count = this.fileList.length
					if (count >= this.limit) {
						// this.disabled = true;
						return this.$u.toast('最多可以上传' + this.limit + '个文件')
					}
					if (response.code != 200) return this.$u.toast(response.msg)
					this.fileList.push({
						name: item.name,
						fileId: response.data.name,
						url: response.data.url,
						fileExtension: response.data.fileExtension,
						fileSize: response.data.fileSize
					})
					this.$emit('update:modelValue', this.fileList)
					this.$emit('change', this.fileList)
				}
				this.$forceUpdate();
			},
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
			// 移除某个文件
			delFile(files) {
				this.fileList = files
				// if(this.fileList.length >= this.limit) {
				// 	this.disabled = true
				// }else{
				// 	this.disabled = false
				// }
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

<style lang="scss" scoped>
	.icon-ym {
		font-size: 48rpx;
	}

	.jnpf-file {
		width: 100%;

		.jnpf-file-box {

			.jnpf-file-box-line {
				height: 70rpx;
				align-items: center;
				display: flex;
			}

			.tipText {
				color: #606266;
				word-break: break-all;
				line-height: 48rpx;
			}

			.jnpf-file-item {
				justify-content: space-between;
				flex-direction: row;

				.jnpf-file-item-txt {
					width: 230rpx;
					flex: auto;
				}

				.showLeft {
					text-align: left;
				}

				.closeBox {
					height: 60rpx;
					align-items: flex-end;
					justify-content: space-evenly;
					flex: 0.2;

					.closeTxt {
						width: 36rpx;
						height: 36rpx;
						border-radius: 50%;
						background-color: #fa3534;
						color: #FFFFFF;
						font-size: 20rpx;
						align-items: center;
						justify-content: center;
						line-height: 36rpx;
					}
				}
			}
		}
	}
</style>
