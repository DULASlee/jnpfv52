<template>
	<view class="record-v">
		<view class="discuss_box" v-if="commentList.length">
			<view class="u-flex-col discuss_list" v-for="(item,index) in commentList" :key="index">
				<view class="u-flex discuss_txt">
					<view class="discuss_txt_left u-flex">
						<u-avatar :src="baseURL+item.creatorUserHeadIcon"></u-avatar>
						<span class="uName">
							<span class="comment-header-color">{{item.creatorUser}}</span>
							<span v-if="item.replyUser">
								<span class="replyText comment-content-color">回复</span>
								<span class="replyText comment-header-color">{{ item.replyUser }}</span>
								<span class="replyText"> <span class="icon-ym icon-ym-chat"
										@click="openReplyText(item.replyText)"></span></span>
							</span>
						</span>
					</view>
				</view>
				<view class="u-flex-col discuss_content">
					<view class="msg-text">
						<view v-for="(item2,j) in item.text" :key="j">
							<text class="txt comment-content-color" v-if="item2.type=='text'">{{item2.content}}</text>
							<image class="msg-text-emoji" :src="item2.content" v-if="item2.type=='emjio'" />
						</view>
					</view>
					<JnpfUploadImg v-model="item.image" disabled detailed align='left' v-if="item.isDel != 2" />
					<view v-for='(file,f) in item.file' :key="f" class="jnpf-file-item u-type-primary u-flex u-line-1"
						@click="openFile(file)">
						<view class="u-line-1" style="margin-bottom: 10rpx;">
							{{file.name}}
						</view>
					</view>
				</view>
				<view class="u-flex discuss_txt time_button">
					<text
						class="discuss_txt_left u-flex comment-creator-time">{{$u.timeFormat(item.creatorTime,'yyyy-mm-dd hh:MM:ss')}}</text>
					<view>
						<text v-if="item.isDel == 1" class="del" @click.stop="delComment(item.id, index)">删除</text>
						<text v-if="item.isDel != 2" class="reply" @click.stop="handleReply(item.id)">回复</text>
					</view>
				</view>
				<u-divider half-width="100%" :margin-top="32" :margin-bottom="32" :use-slot="false"
					v-if="index != commentList.length-1" />
			</view>
		</view>
		<NoData v-else paddingTop="60" backgroundColor="#fff"></NoData>
	</view>
</template>

<script>
	import {
		getCommentList,
		delComment
	} from '@/api/workFlow/flowEngine'
	import {
		getDownloadUrl
	} from '@/api/common'
	import NoData from '@/components/noData'
	import {
		emojiList,
		imagesMap
	} from '../../flowBefore/emoji'
	export default {
		components: {
			NoData
		},
		props: {
			taskId: {
				type: [String, Number],
				default: ''
			},
		},
		data() {
			return {
				emojiList: emojiList,
				commentList: []
			}
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			}
		},
		methods: {
			getCommentList() {
				let query = {
					currentPage: 1,
					pageSize: 100000,
					sort: 'desc',
					sidx: '',
					taskId: this.taskId
				}
				getCommentList(query).then(res => {
					this.commentList = [];
					const list = res.data.list.map((o) => {
						o.image = JSON.parse(o.image)
						o.file = JSON.parse(o.file)
						o.text = this.replaceEmoji(o.text)
						return o
					})
					this.commentList = this.commentList.concat(list);
				}).catch((err) => {})
			},
			openReplyText(replyText) {
				uni.showModal({
					content: replyText,
					showCancel: false,
					success: res => {}
				})
			},
			replaceEmoji(str) { //替换表情符号为图片
				if (!str) return ''
				let replacedStr = str.replace(/\[([^(\]|\[)]*)\]/g, item => 'jnpfjnpf' + item + 'jnpfjnpf');
				let strArr = replacedStr.split(/jnpfjnpfjnpfjnpf|jnpfjnpf/g)
				strArr = strArr.filter(o => o)
				let contentList = []
				for (let i = 0; i < strArr.length; i++) {
					let item = {
						content: strArr[i],
						type: 'emjio'
					}
					if (/\[([^(\]|\[)]*)\]/.test(strArr[i])) {
						let content = ''
						for (let j = 0; j < this.emojiList.length; j++) {
							let row = this.emojiList[j];
							if (row.alt == strArr[i]) {
								content = this.getEmojiUrl(row.url)
								break
							}
						}
						item = {
							content: content,
							type: 'emjio'
						}
					} else {
						item = {
							content: strArr[i],
							type: 'text'
						}
					}
					contentList.push(item)
				}
				return contentList
			},
			getEmojiUrl(url) {
				return imagesMap[url.replace('.', '')]
			},
			delComment(id, i) {
				uni.showModal({
					title: '提示',
					content: '确定删除?',
					success: (res) => {
						if (res.confirm) {
							delComment(id).then(res => {
								this.getCommentList()
								this.commentList.splice(i, 1)
							})
						}
					}
				})
			},
			handleReply(id) {
				this.$emit('handleReply', id)
			},
			// 流程评论
			doPreviewImage(url) {
				let images;
				this.commentList.forEach(o => {
					if (o.image.length > 0) {
						images = o.image.map(item => this.baseURL + item.url)
					}
				})
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
			openFile(item) {
				// #ifdef MP
				this.previewFile(item)
				// #endif
				// #ifdef H5 || APP
				getDownloadUrl('annex', item.fileId).then(res => {
					// #ifdef H5
					window.location.href = this.baseURL + res.data.url + '&name=' + item.name;
					// #endif
					// #ifdef APP
					uni.downloadFile({
						url: this.baseURL + res.data.url + '&name=' + item.name,
						success: function(res) {
							var filePath = res.tempFilePath;
							uni.openDocument({
								filePath: filePath,
								showMenu: true,
								success: function(res) {}
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
							success: (res) => {}
						});
					}
				});
			}
			// 流程评论end
		}
	}
</script>

<style lang="scss">
	.record-v {
		height: 100%;
		background-color: #fff;


		.msg-text {
			border-radius: 4rpx 30rpx 30rpx 30rpx;
			padding: 16rpx 32rpx;
			display: flex;
			align-items: center;
			font-size: 14px;
			line-height: 17px;
			font-family: PingFang SC;
			word-break: break-word;
			flex-wrap: wrap;
			background-color: #fff;

			.msg-text-emoji {
				vertical-align: top;
				width: 48rpx;
				height: 48rpx;
				display: inline-block;
			}
		}

		.discuss_box {
			padding: 20rpx;

			.discuss_list {

				.time_button {
					padding-left: 110rpx;
					margin-top: 20rpx;
					padding-right: 10rpx;
				}

				.discuss_txt {
					width: 100%;
					justify-content: space-between;

					.discuss_txt_left {
						.uName {
							margin-left: 8px;
							font-size: 14px;
							font-family: PingFang SC;
							line-height: 17rpx;

							.replyText {
								margin-left: 8px;
							}
						}
					}

					.del {
						color: red;
						margin-left: 20px;
					}

					.reply {
						margin-left: 20px;
					}

				}

				.discuss_content {
					font-size: 12px;
					padding-left: 70rpx;

					.img_box {
						margin: 40rpx 0;
					}
				}
			}
		}
	}
</style>