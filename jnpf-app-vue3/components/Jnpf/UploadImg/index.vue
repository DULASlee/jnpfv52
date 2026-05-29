<template>
	<view class="jnpf-upload-img">
		<view :class="'jnpf-upload jnpf-upload-'+align" v-if="!simple">
			<template v-if="fileList.length">
				<view class="u-list-item u-preview-wrap" v-for="(item, index) in fileList" :key="index">
					<view v-if="!disabled&&!detailed" class="u-delete-icon" @tap.stop="deleteItem(index)">
						<u-icon class="u-icon" name="close" size="20" color="#ffffff"></u-icon>
					</view>
					<image class="u-preview-image" :src="baseURL+(item.thumbUrl||item.url)" mode="aspectFill"
						@tap.stop="doPreviewImage(baseURL+item.url)"></image>
				</view>
			</template>
			<u-upload v-if="!detailed" width="150" height="150" :action="comUploadUrl+'annexpic'"
				:header="uploadHeaders" :form-data="params" @on-list-change="onListChange" :max-size="maxSize"
				:max-count="realLimit" :show-upload-list="false" :show-progress="false" :deletable="deletable"
				@on-success="onSuccess" @on-error="handleError" ref="uUpload" :file-list="lists" :disabled='disabled'>
			</u-upload>
		</view>
		<view class="tipText u-p-l-20" v-if="!simple">
			{{tipText}}
		</view>
		<view class="text-primary" v-if="simple" @tap.stop="doPreviewImage(baseURL+fileList[0].url)">查看图片</view>
	</view>
</template>

<script>
	const units = {
		KB: 1024,
		MB: 1024 * 1024,
		GB: 1024 * 1024 * 1024
	}
	export default {
		name: 'jnpf-upload-img',
		props: {
			modelValue: {
				type: [Array, String],
				default: () => []
			},
			tipText: {
				type: String,
				default: ''
			},
			limit: {
				type: Number,
				default: 99
			},
			sizeUnit: {
				type: String,
				default: 'MB'
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
			fileSize: {
				type: Number,
				default: 10
			},
			disabled: {
				type: Boolean,
				default: false
			},
			detailed: {
				type: Boolean,
				default: false
			},
			simple: {
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
				fileList: [],
				realLimit: 0,
				deletable: true,
				uploadHeaders: {
					Authorization: ''
				},
				params: {
					pathType: this.pathType,
					isAccount: this.isAccount,
					folder: this.folder,
					sortRule: (this.sortRule || []).join(),
					timeFormat: this.timeFormat,
				},
				lists: [],
				maxSize: ''
			}
		},
		watch: {
			limit(val) {
				this.realLimit = val
			},
			modelValue: {
				immediate: true,
				handler(val) {
					this.fileList = Array.isArray(val) ? JSON.parse(JSON.stringify(val)) : []
				}
			}
		},
		created() {
			this.uploadHeaders.Authorization = uni.getStorageSync('token')
			this.maxSize = this.fileSize ? this.fileSize * units[this.sizeUnit] : 10000000000000
			this.$nextTick(() => {
				this.lists = this.fileList || []
			})
			this.realLimit = this.limit
			if (this.disabled) this.deletable = false
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			},
			comUploadUrl() {
				return this.define.comUploadUrl
			},
		},
		methods: {
			onSuccess(data, index, lists, name) {
				if (data.code == 200) {
					this.fileList.push({
						name: lists[index].file.name,
						fileId: data.data.name,
						url: data.data.url,
						thumbUrl: data.data.thumbUrl,
					})
					this.$emit('update:modelValue', this.fileList)
					this.$emit('change', this.fileList)
				} else {
					lists.splice(index, 1)
					this.$u.toast(data.msg)
				}
			},
			handleError(res, index, lists, name) {
				lists.splice(index, 1)
			},
			deleteItem(index) {
				uni.showModal({
					title: '提示',
					content: '您确定要删除此项吗？',
					success: res => {
						if (res.confirm) {
							this.$refs.uUpload.remove(index);
							this.fileList.splice(index, 1)
							this.$emit('update:modelValue', this.fileList)
							this.$emit('change', this.fileList)
							uni.showToast({
								title: '移除成功',
								icon: 'none'
							});
						}
					}
				});
			},
			onListChange(lists) {
				this.lists = lists || [];
			},
			doPreviewImage(url) {
				const images = this.fileList.map(item => this.baseURL + item.url);
				uni.previewImage({
					urls: images,
					current: url,
					success: () => {},
					fail: () => {
						uni.showToast({
							title: '预览图片失败',
							icon: 'none'
						});
					}
				});
			}
		}
	}
</script>
<style lang="scss" scoped>
	.jnpf-upload-img {
		width: 100%;

		.tipText {
			color: #606266;
			word-break: break-all;
			line-height: 48rpx;
			text-align: right;
		}

		.jnpf-upload {
			width: 100%;
			display: flex;
			flex-wrap: wrap;
			align-items: center;

			&.jnpf-upload-right {
				justify-content: flex-end;
			}

			&.jnpf-upload-left {
				justify-content: flex-start;
			}

			:deep(.u-upload) {
				.u-list-item {
					margin: 0 !important;
				}
			}

			.u-preview-wrap {
				width: 150rpx;
				height: 150rpx;
				border: 1px solid #ebecee;
				overflow: hidden;
				margin: 10rpx;
				background: rgb(244, 245, 246);
				position: relative;
				border-radius: 10rpx;
				/* #ifndef APP-NVUE */
				display: flex;
				/* #endif */
				align-items: center;
				justify-content: center;

				.u-preview-image {
					display: block;
					width: 100%;
					height: 100%;
					border-radius: 10rpx;

				}

				.u-delete-icon {
					position: absolute;
					top: 10rpx;
					right: 10rpx;
					z-index: 10;
					background-color: $u-type-error;
					border-radius: 100rpx;
					width: 44rpx;
					height: 44rpx;
					/* #ifndef APP-NVUE */
					display: flex;
					/* #endif */
					align-items: center;
					justify-content: center;
				}

				.u-icon {
					/* #ifndef APP-NVUE */
					display: flex;
					/* #endif */
					align-items: center;
					justify-content: center;
				}
			}
		}
	}
</style>