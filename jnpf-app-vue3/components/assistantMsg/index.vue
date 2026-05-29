<template>
	<view class="assistantMsg-v">
		<view class="u-p-l-20 u-p-r-20 u-p-t-20" v-if="statusList.length">
			<u-subsection :list="statusList" :current="subsectionIndex" active-color="#2979FF" inactive-color="#999999"
				bg-color="#F2F3F7" font-size="24" :bold="false" @change="subsection"></u-subsection>
		</view>
		<view v-if="tabData.data?.length">
			<view class="u-p-l-20 u-p-r-20 u-p-t-20 u-p-b-20" v-if="subsectionId == 1">
				<text>{{tabData.data}}</text>
			</view>
			<view v-if="subsectionId != 1">
				<view class="u-flex-col list-v">
					<view class="u-flex item" v-for="(item,index) in tabData.data" :key="index">
						<view v-if="subsectionId == 2" class="linkBox">
							<!-- #ifndef APP-HARMONY -->
							<u-link :href="item.urlAddress" under-line>{{item.fullName}}</u-link>
							<!-- #endif -->
							<!-- #ifdef APP-HARMONY -->
							<text @click="jumpLink(item.urlAddress)" class="linkColor">{{item.fullName}}</text>
							<!-- #endif -->
						</view>
						<view class="list-inner u-flex" v-if="subsectionId == 3">
							<view class="u-flex list-inner-box" @click="downLoad(item)">
								<view class="item-icon ">
									<u-image :src="getRecordImg(item.uploaderUrl)" width="84" height="84" />
								</view>
								<view class="u-flex-col r-content ">
									<view class="u-line-1 name">{{item.fileName}}</view>
									<text>
										{{item.fileDate ? $u.timeFormat(item.fileDate, 'yyyy-mm-dd hh:MM:ss'):''}}</text>
								</view>
							</view>
						</view>
						<view class="group-box-inner" v-if="subsectionId == 4">
							<u-cell-group :border="false">
								<u-cell-item :border-bottom="false" :title="item.interfaceName"
									hover-class="cell-hover-class" @click="jump(item)"></u-cell-item>
							</u-cell-group>
						</view>
					</view>
				</view>
			</view>
		</view>
		<view class="u-p-20" v-else>
			<NoData paddingTop="0" backgroundColor="#fff" zIndex="9"></NoData>
		</view>
	</view>
</template>
<script>
	import resources from "@/libs/resources.js";
	import NoData from '@/components/noData'
	const wordTypeList = ['doc', 'docx'];
	const excelTypeList = ['xls', 'xlsx'];
	const pptTypeList = ['ppt', 'pptx'];
	const pdfTypeList = ['pdf'];
	const zipTypeList = ['rar', 'zip', 'arj', 'z', '7z'];
	const txtTypeList = ['txt', 'log'];
	const codeTypeList = ['html', 'cs', 'xml'];
	const imgTypeList = ['png', 'jpg', 'jpeg', 'bmp', 'gif'];
	const videoTypeList = ['avi', 'wmv', 'mpg', 'mpeg', 'mov', 'rm', 'ram', 'swf', 'flv', 'mp4', 'mp3', 'wma', 'avi', 'rm',
		'rmvb', 'flv', 'mpg', 'mkv'
	];
	import {
		packDownload,
	} from "@/api/workFlow/document";
	export default {
		components: {
			NoData
		},
		props: {
			auxiliaryInfo: {
				type: Array,
				default: () => []
			}
		},
		data() {
			return {
				subsectionIndex: 0,
				wordImg: resources.document.wordImg,
				excelImg: resources.document.excelImg,
				pptImg: resources.document.pptImg,
				pdfImg: resources.document.pdfImg,
				rarImg: resources.document.rarImg,
				txtImg: resources.document.txtImg,
				codeImg: resources.document.codeImg,
				imageImg: resources.document.imageImg,
				audioImg: resources.document.audioImg,
				blankImg: resources.document.blankImg,
				folderImg: resources.document.folderImg,
			}
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			},
			statusList() {
				let list = this.auxiliaryInfo.filter(o => o && o.config && o.config.on);
				list = list.map(o => ({
					...o,
					name: o.fullName
				}))
				return list
			},
			subsectionId() {
				return this.statusList[this.subsectionIndex]?.id
			},
			tabData() {
				const configKeyMap = {
					1: 'content',
					2: 'linkList',
					3: 'fileList',
					4: 'dataList'
				};
				const configKey = configKeyMap[this.subsectionId] || null;
				if (configKey) {
					return {
						data: this.statusList[this.subsectionIndex].config[configKey] || (configKey === 'content' ? '' :
						[]),
						show: this.statusList[this.subsectionIndex].config.on
					};
				}
				return {};
			}
		},
		methods: {
			jumpLink(url) {
				if (!url.startsWith("https://")) return this.$u.toast('无效链接')
				uni.navigateTo({
					url: `/pages/workFlow/webView/index?data=${url}`
				})
			},
			getRecordImg(ext) {
				if (!ext) return this.folderImg;
				const match = ext.match(/\.([^\.]+)$/);
				if (match) ext = match[1]
				if (wordTypeList.includes(ext)) return this.wordImg;
				if (excelTypeList.includes(ext)) return this.excelImg;
				if (pptTypeList.includes(ext)) return this.pptImg;
				if (pdfTypeList.includes(ext)) return this.pdfImg;
				if (zipTypeList.includes(ext)) return this.rarImg;
				if (txtTypeList.includes(ext)) return this.txtImg;
				if (codeTypeList.includes(ext)) return this.codeImg;
				if (imgTypeList.includes(ext)) return this.imageImg;
				if (videoTypeList.includes(ext)) return this.audioImg;
				return this.blankImg;
			},
			subsection(e) {
				this.subsectionIndex = e
			},
			jump(item) {
				uni.navigateTo({
					url: "/pages/workFlow/assistantMsg/viewData?data=" + encodeURIComponent(JSON.stringify(item))
				})
			},
			downLoad(item) {
				let data = {
					ids: [item?.id]
				}
				packDownload(data).then(res => {
					// #ifdef H5
					const fileUrl = this.baseURL + res.data.url + '&name=' + encodeURI(res.data.name);
					window.location.href = fileUrl;
					// #endif

					// #ifdef MP
					this.previewFile(res.data)
					// #endif

					// #ifdef APP-PLUS
					this.downloadFile(res.data.url);
					// #endif
				})
			},
			previewFile(item) {
				let fileTypes = ['doc', 'xls', 'ppt', 'pdf', 'docx', 'xlsx', 'pptx']
				let url = item.url
				let fileType = url.split('.')[1]
				if (fileTypes.includes(fileType)) {
					uni.downloadFile({
						url: this.baseURL + url,
						success: (res) => {
							let filePath = res.tempFilePath;
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
			}
		}
	}
</script>

<style lang="scss">
	.list-v {
		.item {
			min-height: 80rpx;
			width: 100%;
			border-bottom: 1rpx solid #d7d7d7;
			padding: 20rpx;

			.linkBox {
				.linkColor {
					color: #007BFF;
				}
			}

			.list-inner {
				width: 100%;

				.list-inner-box {
					width: 100%;

					.item-icon {
						background-color: #FFFFFF;
						border-radius: 8rpx;
					}

					.r-content {
						max-width: 86%;
						margin-left: 14rpx;

						.name {
							text-align: left;
						}
					}
				}
			}

			.group-box-inner {
				width: 100%;

				::v-deep {
					.u-cell {
						padding: 0;
					}
				}
			}
		}
	}
</style>