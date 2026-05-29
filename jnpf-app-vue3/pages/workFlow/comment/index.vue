<template>
	<view class="comment-v">
		<view class="comment_inner">
			<view class="text_box">
				<div class="u-input-wrapper">
					<u-input :type="textarea.type" v-model="dataForm.text" placeholder="请输入" :height='textarea.height'
						ref="textRef" :focus="textFocus" :border='textarea.border' :maxlength="textarea.maxlength"
						:auto-height="textarea.autoHeight" class="text_input" @input="handleContentChange" />
					<div class="remainingCharacters">{{ dataForm.text.length }}/{{textarea.maxlength}}</div>
				</div>
			</view>
			<view class="box" :style="{ bottom: popupOpenBottom + 'rpx'}">
				<scroll-view :scroll-y="true" style="height: 550rpx;" class="scroll_view" @click="hideDrawer">
					<view class="comment-area">
						<view class="img_box">
							<view class="u-preview-wrap" v-for="(item, index) in dataForm.imgList" :key="index">
								<view class="u-delete-icon" @tap.stop="deleteItem(index)">
									<u-icon class="u-icon" name="close" size="20" color="#ffffff"></u-icon>
								</view>
								<image class="u-preview-image" :src="baseURL+(item.thumbUrl||item.url)"
									mode="aspectFill" @tap.stop="doPreviewImage(baseURL+item.url)"></image>
							</view>
						</view>
						<view v-for='(item,index) in dataForm.file' :key="index"
							class="jnpf-file-item u-type-primary u-flex u-line-1" @tap='downLoad(item)'>
							<view class="jnpf-file-item-txt u-line-1">{{item.name}}</view>
							<view class="closeBox u-flex-col" @click.stop="delFile(index)">
								<text class="icon-ym icon-ym-nav-close closeTxt u-flex"></text>
							</view>
						</view>
					</view>
				</scroll-view>
				<view class="btn_box">
					<view class="u-flex">
						<view class="btn_item">
							<view class="icon-ym icon-ym-roll-call" size="mini" @click="openSelectUser()">
							</view>
						</view>
						<view class="btn_item">
							<view class="icon-ym icon-ym-comment-img"
								v-if="dataForm.imgList && dataForm.imgList.length >= 9"
								@click="clickImgUploadOverCount">
							</view>
							<u-upload :custom-btn="true" :action="comUploadUrl+type" :header="uploadHeaders"
								ref="uUpload" :max-size="10*1024*1024" :max-count="9" :show-upload-list="false"
								:show-progress="false" @on-success="onImgSuccess" @on-change="onImgChange"
								:show-tips="false" @on-oversize="onImgOversize">
								<template #addBtn>
									<view class="slot-btn" hover-class="slot-btn__hover" hover-stay-time="150">
										<view class="icon-ym icon-ym-comment-img"></view>
									</view>
								</template>
							</u-upload>
						</view>
						<!-- #ifndef APP-HARMONY -->
						<view class="btn_item">
							<CommentFile v-model="dataForm.file" :limit="9" :fileSize="10"
								:currentCount="dataForm.file?dataForm.file.length:0" ref="commentFile" />
						</view>
						<!-- #endif -->
						<view class="btn_item">
							<view class="icon-ym icon-ym-emoji" size="mini" @click="chooseEmoji"></view>
						</view>
					</view>
					<view class="btn_item">
						<u-button type="primary" @click="handleClick" :disabled="submitDisabled"
							size="medium">发送</u-button>
					</view>
				</view>
			</view>
		</view>
		<view class="popup-layer u-border-top" :class="popupLayerClass" @touchmove.stop.prevent="discard">
			<swiper class="emoji-swiper" indicator-dots="true" duration="150" v-show="showEmoji">
				<swiper-item v-for="(page,pid) in emojiTree" :key="pid">
					<view v-for="(em,eid) in page" :key="eid" @click="addEmoji(em)" class="emoji-item">
						<image mode="widthFix" :src="getEmojiUrl(em.url)" class="emoji-item-img"></image>
					</view>
				</swiper-item>
			</swiper>
		</view>
		<FlowUserModal v-model="flowUserModalShow" :taskId="taskId" :selectType="'custom'" :multiple="true"
			ref="flowUserModal" @confirm="handleSelectUser" @close="closeFlowUserModal" />
	</view>
</template>
<script>
	import {
		getDownloadUrl
	} from '@/api/common'
	import CommentFile from './comment-file/index.vue'
	import FlowUserModal from './comment-user-select/index.vue'
	import {
		emojiList,
		emojiTree,
		imagesMap
	} from '../flowBefore/emoji'

	export default {
		components: {
			CommentFile,
			FlowUserModal
		},
		data() {
			return {
				dataForm: {
					text: '',
					file: [],
					imgList: [],
				},
				type: 'annexpic',
				deletable: true,
				uploadHeaders: {
					Authorization: this.token
				},
				token: '',
				tabIndex: 0,
				percent: '',
				list: [],
				textarea: {
					type: 'textarea',
					border: true,
					height: 440,
					autoHeight: false,
					maxlength: 500
				},
				taskId: null,
				replyId: null,
				submitDisabled: true,
				selectUserType: '',
				selectionStart: 0,
				flowUserModalShow: false,
				textFocus: true,
				popupLayerClass: '',
				popupOpenBottom: 20,
				showEmoji: false,
				emojiList,
				emojiTree,
			};
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			},
			comUploadUrl() {
				return this.define.comUploadUrl
			},
		},
		onReady() {

		},
		onLoad(e) {
			let data = JSON.parse(decodeURIComponent(e.data))
			this.taskId = data.taskId
			if (data.replyId) {
				this.replyId = data.replyId
				uni.setNavigationBarTitle({
					title: '回复评论'
				});
			}
		},
		watch: {
			dataForm: {
				handler: function(val) {
					this.setSubmitDisabled()
				},
				deep: true,
				immediate: true
			},
		},
		methods: {
			clickImgUploadOverCount() {
				uni.showToast({
					title: '最多可以上传9张图片',
					icon: 'none'
				});
				return false;
			},
			discard() {
				return;
			},
			chooseEmoji() {
				if (this.showEmoji) return this.hideDrawer()
				this.showEmoji = true;
				this.openDrawer();
			},

			addEmoji(em) {
				this.dataForm.text += em.alt;
			},
			getEmojiUrl(url) {
				return imagesMap[url.replace('.', '')]
			},
			openDrawer() {
				this.popupLayerClass = 'showLayer';
				setTimeout(() => {
					this.popupOpenBottom = 315;
				}, 150);
			},
			hideDrawer() {
				this.popupLayerClass = '';
				setTimeout(() => {
					this.showEmoji = false;
					this.popupOpenBottom = 20;
				}, 50);
			},
			getFocus() {
				this.textFocus = false
				setTimeout(() => {
					this.textFocus = true
				}, 50);
			},
			setSubmitDisabled() {
				this.submitDisabled = !this.dataForm.text
			},
			openSelectUser() {
				this.selectUserType = 'btn';
				this.selectionStart = -1;
				this.flowUserModalShow = true
			},
			closeFlowUserModal() {
				this.flowUserModalShow = false
			},
			handleContentChange(value) {
				if (!value || !value.endsWith("@")) {
					return;
				}
				this.selectUserType = 'input';
				this.selectionStart = value.length;
				this.flowUserModalShow = true
			},
			handleSelectUser(data) {
				if (!data.length || !data.length) return;
				let addContent = this.selectUserType === 'btn' ? '@' : '';
				for (let i = 0; i < data.length; i++) {
					let str = (i > 0 ? '@' : '') + `{${data[i].fullName}}`;
					addContent += str;
				}
				if (this.selectionStart === -1) {
					this.dataForm.text += addContent;
					this.textFocus = false
					this.getFocus();
				} else {
					let oldValue = this.dataForm.text;
					let rangeIndex = this.selectionStart + addContent.length;
					this.dataForm.text = oldValue.slice(0, this.selectionStart) + addContent + oldValue.slice(this
						.selectionStart);
					this.textFocus = false
					this.getFocus();
				}
			},
			handleClick() {
				const query = {
					text: this.dataForm.text,
					file: JSON.stringify(this.dataForm.file),
					image: JSON.stringify(this.dataForm.imgList),
					replyId: this.replyId
				}
				uni.$emit('comment', query);
				uni.navigateBack();
			},

			//文件下载
			downLoad(item) {
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
								success: (res) => {},
								fail(err) {}
							});
						}
					});
				} else {
					this.$u.toast(
						'该文件类型无法打开'
					)
				}
			},
			//文件删除
			delFile(index) {
				uni.showModal({
					title: '提示',
					content: '是否删除该文件？',
					success: res => {
						if (res.confirm) {
							this.dataForm.file.splice(index, 1)
							this.$refs.commentFile.delFile(this.dataForm.file);
						} else if (res.cancel) {}
					}
				});
			},
			downloadFile(url) {
				uni.downloadFile({
					url: this.baseURL + url,
					success: res => {
						if (res.statusCode === 200) {
							const filePath = res.tempFilePath;
							uni.openDocument({
								filePath: escape(filePath),
								success: ress => {},
								fail(err) {}
							});
						}
					}
				});
			},
			onImgSuccess(data, index, lists, name) {
				if (data.code == 200) {
					this.dataForm.imgList.push({
						name: lists[index].file.name,
						fileId: data.data.name,
						url: data.data.url,
						thumbUrl: data.data.thumbUrl,
					})
					// this.$emit('input', this.fileList)
				} else {
					lists.splice(index, 1)
					this.$u.toast(data.msg)
				}
			},
			onImgChange(res, index, lists, name) {
				const isTopLimit = lists.length > 9;
				if (isTopLimit) {
					uni.showToast({
						title: '最多可以上传9张图片',
						icon: 'none'
					});
					return false
				}
				const isRightSize = lists[index].file.size < 10 * 1024 * 1024;
				if (!isRightSize) {
					uni.showToast({
						title: '图片大小超过10MB',
						icon: 'none'
					});
					return false;
				}
				/*
				let isAccept = new RegExp('image/!*').test(file.type);
				if (!isAccept) {
				  this.$message({ message: '请上传图片', type: 'error', duration: 1000 })
				  return
				}
				return isRightSize && isAccept;*/
			},
			onImgOversize(res, index, lists, name) {
				uni.showToast({
					title: '图片大小超过10MB',
					icon: 'none'
				});
				return false;
			},
			doPreviewImage(url) {
				const images = this.dataForm.imgList.map(item => this.baseURL + item.url);
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
			},
			deleteItem(index) {
				uni.showModal({
					title: '提示',
					content: '是否删除该图片？',
					success: res => {
						if (res.confirm) {
							this.$refs.uUpload.remove(index);
							this.dataForm.imgList.splice(index, 1)
							// this.$emit('input', this.fileList)
							uni.showToast({
								title: '移除成功',
								icon: 'none'
							});
						}
					}
				});
			}
		}
	}
</script>

<style lang="scss">
	page {
		height: 100%;
	}

	.comment-v {
		width: 100%;
		height: 100%;
		display: flex;
		flex-direction: column;
		margin: 0 auto;
		// .flowBefore-actions{
		// 	width: 90%;
		// 	left: 50%;
		// 	transform: translateX(-50%);
		// 	bottom: 20rpx;
		// 	.buttom-btn{
		// 		border-radius: 10rpx;
		// 	}
		// }

		.uni-textarea-compute {
			height: 470rpx !important;
		}

		.comment_inner {
			display: flex;
			flex-direction: column;
			background-color: #FFFFFF;
			height: 100%;
			padding: 0 30rpx;

			.text_box {
				flex: 0.35;

				.u-input-wrapper {
					border: 1px solid #E7E7E7;
					margin-top: 10px;
				}

				.remainingCharacters {
					width: 99%;
					text-align: right;
				}

				.text_input {
					border: 0px;
				}

				// .input_textarea{
				// 	height: 470rpx !important;

				// }
			}

			.box {
				width: 100%;
				display: flex;
				flex-direction: column;
				position: absolute;

				.scroll_view {
					.comment-area {
						height: 550rpx;
						display: flex;
						flex-direction: column;
						align-items: flex-end;
						justify-content: flex-end;
						margin-bottom: 28rpx;

						.img_box {
							width: 100%;
							display: flex;
							flex-direction: row;
							flex-wrap: wrap;

							.u-preview-wrap {
								width: 110rpx;
								height: 110rpx;
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
									width: 34rpx;
									height: 34rpx;
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

						.jnpf-file-item {
							width: 100%;
							justify-content: space-between;
							flex-direction: row;

							.jnpf-file-item-txt {
								flex: 1;
							}

							.closeBox {
								height: 60rpx;
								justify-content: space-evenly;
								flex: 0.2;

								.closeTxt {
									width: 34rpx;
									height: 34rpx;
									border-radius: 50%;
									background-color: #909194;
									color: #FFFFFF;
									font-size: 22rpx;
									align-items: center;
									justify-content: center;
								}
							}
						}
					}
				}


				.btn_box {
					width: 100%;
					display: flex;
					flex-direction: row;
					// justify-content: flex-start;
					align-items: center;

					.btn_item {
						margin-right: 30rpx;
						margin-left: 10rpx;

						.icon-ym {
							font-size: 48rpx;
						}
					}

					.btn_item:last-child {
						margin-left: auto;
						margin-right: 50rpx;
					}

					.submit_item {
						background-color: red;
					}

					.slot-btn {
						.img_icon {
							width: 80rpx;
							height: 80rpx;
							text-align: center;
							line-height: 80rpx;

							&:before {
								content: "\e987";
								font-size: 60rpx;
								color: #666666;
							}
						}
					}

					.file_icon {
						width: 80rpx;
						height: 80rpx;
						text-align: center;
						line-height: 80rpx;

						&:before {
							font-size: 60rpx;
							color: #666666;
						}
					}
				}
			}
		}
	}

	.popup-layer {
		&.showLayer {
			transform: translate3d(0, -42vw, 0);
		}

		transition: all .15s linear;
		width: 100%;
		height: 42vw;
		padding: 20rpx 2%;
		background-color: #f2f2f2;
		position: fixed;
		z-index: 20;
		top: 100%;

		.emoji-swiper {
			height: 40vw;

			swiper-item {
				display: flex;
				align-content: flex-start;
				flex-wrap: wrap;

				.emoji-item {
					width: 12vw;
					height: 12vw;
					display: flex;
					justify-content: center;
					align-items: center;

					.emoji-item-img {
						width: 8.4vw;
						height: 8.4vw;
					}
				}
			}
		}

		.more-layer {
			width: 100%;
			height: 42vw;

			.list {
				width: 100%;
				display: flex;
				flex-wrap: wrap;

				.box {
					width: 18vw;
					height: 18vw;
					border-radius: 20rpx;
					background-color: #fff;
					display: flex;
					justify-content: center;
					align-items: center;
					margin: 0 3vw 2vw 3vw;

					.icon {
						font-size: 70rpx;
					}
				}
			}
		}
	}
</style>