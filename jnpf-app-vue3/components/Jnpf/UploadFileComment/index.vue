<template>
	<view class="jnpf-file">
		<view class="jnpf-file-box" :style="{textAlign:align}">
			<view class="jnpf-file-box-line">
				<lsj-upload :ref="lsjUpload" :childId="childId" :width="width" :height="height" :option="option"
					:size="fileSize" :formats="getFormats" :instantly="instantly" @uploadEnd="onuploadEnd"
					:lsjUpload="lsjUpload">
					<text class="u-m-r-20 icon-ym icon-ym-generator-menu"></text>
					<text>上传文件</text>
				</lsj-upload>
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
			accept: {
				type: [Number, String],
				default: ''
			},
			parentId: {
				type: [Number, String],
				default: 0
			},
			pathType: {
				type: String,
				default: 'defaultPath'
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
				option: {
					parentId: 0
				},
				params: {
					pathType: this.pathType,
					isAccount: this.isAccount,
					folder: this.folder,
					sortRule: (this.sortRule || []).join(),
					timeFormat: this.timeFormat
				},
				// 选择文件后是否立即自动上传，true=选择后立即上传
				instantly: true,
				size: 30,
				list: [],
				deletable: false,
				childId: 'upload' + this.$u.guid(3, false, 2),
				lsjUpload: 'lsjUpload' + this.$u.guid(3, false, 2),
				width: '638rpx',
				height: '60rpx',
				parId: 0
			}
		},
		watch: {
			parentId: {
				handler(val) {
					this.parId = val
					this.option.parentId = val
				},
				immediate: true
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
				url: this.baseURL + `/api/extend/Document/Uploader`, //替换为你的接口地址
				name: 'file',
				header: {
					'Authorization': token,
					'uid': '27682',
					'client': 'app',
					'accountid': 'DP',
				},
				data: this.params,
				parentId: this.parId
			}
		},
		methods: {
			// 某文件上传结束回调(成功失败都回调)
			onuploadEnd(item) {
				if (item['responseText']) {
					let response = JSON.parse(item.responseText)
					this.$emit('callback', response)
				}
			}
		}
	}
</script>

<style lang="scss" scoped>
	.jnpf-file {
		width: 100%;

		.jnpf-file-box {
			text-align: left !important;

			.jnpf-file-box-line {
				height: 1.9rem !important;
				line-height: 1.9rem;

				.lsj-file {
					.hFile {
						input {
							height: 23px !important;
						}
					}
				}
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