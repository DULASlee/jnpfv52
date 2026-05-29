<template>
	<view class="jnpf-file">
		<view class="jnpf-file-box" :style="{textAlign:align}">
			<view v-if="!detailed &&!disabled " class="jnpf-file-box-line">
				<lsj-upload :ref="lsjUpload" :childId="childId" :width="width" :height="height" :option="option"
					:size="fileSize" :formats="getFormats" :instantly="instantly" @uploadEnd="onuploadEnd"
					:lsjUpload="lsjUpload" v-if="!disabled" :sizeUnit="sizeUnit">
					<!-- #ifndef MP-WEIXIN -->
					<view>
						<u-button size="mini">上传附件</u-button>
					</view>
					<!-- #endif -->
				</lsj-upload>
			</view>
			<u-button size="mini" v-if="disabled">上传附件</u-button>
			<view v-for='(item,index) in fileList' :key="index" class="jnpf-file-item u-type-primary u-flex u-line-1"
				@tap='downLoad(item)'>
				<view class="jnpf-file-item-txt u-line-1" v-if="item.fileSize">
					{{item.name+'（'+`${jnpf.toFileSize(item.fileSize)}`+' ）'}}
				</view>
				<view class="jnpf-file-item-txt u-line-1" v-else>{{item.name}}</view>
				<view class="closeBox u-flex-col" @click.stop="delFile(index)" v-if="!detailed && !disabled">
					<text class="icon-ym icon-ym-nav-close closeTxt u-flex"></text>
				</view>
			</view>
			<view class="tipText u-p-l-20">
				{{tipText}}
			</view>
		</view>
	</view>
</template>

<script>
	import {
		getDownloadUrl
	} from '@/api/common'

	const imgTypeList = ['png', 'jpg', 'jpeg', 'bmp', 'gif']
	export default {
		name: 'jnpf-upload-img',
		inheritAttrs: false,
		props: {
			modelValue: {
				type: Array,
				default: () => ([])
			},
			disabled: {
				type: Boolean,
				default: false
			},
			limit: {
				type: [Number, String],
				default: 9
			},
			fileSize: {
				type: Number,
				default: 5
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
			sortRule: {
				type: Array,
				default: () => ([])
			},
			timeFormat: {
				type: String,
				default: ''
			},
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
					folder: this.folder,
					sortRule: (this.sortRule || []).join(),
					timeFormat: this.timeFormat,
				},
				// 选择文件后是否立即自动上传，true=选择后立即上传
				instantly: true,
				size: 30,
				list: [],
				deletable: false,
				childId: 'upload' + this.$u.guid(3, false, 2),
				lsjUpload: 'lsjUpload' + this.$u.guid(3, false, 2),
				width: '140rpx',
				height: '70rpx',
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
			// 某文件上传结束回调(成功失败都回调)
			onuploadEnd(item) {
				if (item['responseText']) {
					let response = JSON.parse(item.responseText)
					if (this.fileList.length >= this.limit) return this.$u.toast('已达最大上传数量')
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
			delFile(index) {
				uni.showModal({
					title: '提示',
					content: '是否删除该文件？',
					success: res => {
						if (res.confirm) {
							this.fileList.splice(index, 1)
							this.$emit('update:modelValue', this.fileList)
							this.$emit('change', this.fileList)
							this.fileList.length >= this.fileCount ? this.deletable = true : this.deletable =
								false
						} else if (res.cancel) {}
					}
				});
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
	.jnpf-file {
		width: 100%;

		.jnpf-file-box {

			.jnpf-file-box-line {
				height: 70rpx;
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