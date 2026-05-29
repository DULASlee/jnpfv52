import {
	getDocumentList,
	packDownload,
	resetFileName,
	batchDelete,
	trash,
	recovery,
	trashDelete,
	folderTree,
	folderMove,
	addFolder,
	shareFolder,
	shareFolderList,
	shareTome,
	shareUser,
	shareAdjustment,
	cancelShare,
	fileDetail
} from "@/api/workFlow/document";
import resources from "@/libs/resources.js";
import MescrollMixin from "@/uni_modules/mescroll-uni/components/mescroll-uni/mescroll-mixins.js";
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
const previewTypeList = [...wordTypeList, ...excelTypeList, ...pptTypeList, ...pdfTypeList];
export default {
	mixins: [MescrollMixin],
	data() {
		return {
			usersSelectValue: '',
			isDetail: true,
			showApply: false,
			folderTreeList: [],
			modalValue: '',
			keyword: '',
			current: 0,
			show: false,
			slide: '',
			slide2: '',
			changeStyle: true,
			checkedAll: false,
			parentId: 0,
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
			downOption: {
				use: true,
				auto: true,
			},
			upOption: {
				page: {
					num: 0,
					size: 50,
					time: null,
				},
				empty: {
					use: true,
					icon: resources.message.nodata,
					tip: this.$t('common.noData'),
					fixed: false,
					top: "560rpx",
				},
				textNoMore: this.$t('app.apply.noMoreData'),
				toTop: {
					bottom: 200,
					right: 80
				}
			},
			selectFolder: {},
			moveId: '',
			selectFiles: [],
			modalType: 'restName',
			showAddSelect: false,
			selector: [{
					fullName: '新建文件夹',
					id: 1,
					icon: 'icon-ym icon-ym-add-folder'
				},
				{
					fullName: '上传文件',
					id: 2,
					icon: 'icon-ym icon-ym-generator-menu'
				}
			],
			isDetail: false
		}
	},
	computed: {
		baseURL() {
			return this.define.baseURL
		},
		modalTitle() {
			return this.modalType === 'restName' ? '重命名文件' : '新建文件夹'
		}
	},
	methods: {
		onCallback(e) {
			this.$u.toast(e.msg)
			setTimeout(() => {
				this.showAddSelect = false
				this.resetList()
			}, 1000)
		},
		addSelect(item) {
			if (item == 'add') {
				this.showAddSelect = false
				this.$refs.inputDialog.open('center')
				this.modalType = 'addFolder'
			}
		},
		addFolder() {
			this.modalValue = ''
			this.showAddSelect = true
		},
		shareSubmit(e) {
			let method = this.current === 1 ? shareAdjustment : shareFolder;
			let data = {
				ids: this.selectFiles,
				userIds: e
			}
			method(data).then(res => {
				this.resetList()
			})
		},
		upCallback(page) {
			let method;
			switch (this.current) {
				case 1:
					method = shareFolderList
					break;
				case 3:
					method = trash
					break;
				case 2:
					method = shareTome
					break;
				default:
					method = getDocumentList
					break;
			}
			let query = {
				keyword: this.keyword,
				parentId: this.parentId
			};
			method(query).then(res => {
				this.documentList = [];
				this.selectFiles = []
				const list = res.data.list.map(o => ({
					...o,
					time: o.deleteTime || o.creatorTime || o.shareTime
				}));
				this.documentList = this.documentList.concat(list);
				this.mescroll.endSuccess(list.length);
			})
		},
		downLoad(id) {
			let data = {
				ids: id ? id : this.selectFiles
			}
			packDownload(data).then(res => {
				// #ifdef H5
				const fileUrl = this.baseURL + res.data.url + '&name=' + encodeURI(res.data.name);
				window.location.href = fileUrl;
				// #endif

				// #ifdef MP
				this.previewFile(res.data)
				// #endif

				// #ifdef APP
				this.downloadFile(res.data.url);
				// #endif
			})
		},
		handelClick(item) {
			this.moveId = item.id == '-1' ? 0 : item.id
		},
		checkboxChange(e) {
			if (e.length) {
				this.slide = 'slide-up'
				this.slide2 = 'slide-up2'
				this.show = true
			} else {
				this.slide = 'slide-down'
				this.slide2 = 'slide-down2'
			}
			this.selectOperation(e)
		},
		change(e) {
			this.current = e
			this.parentId = 0
			this.resetList()
		},
		bottomfun(type) {
			if (type === 'down') this.downLoad()
			if (type === 'restName') {
				fileDetail(this.selectFiles[0]).then(res => {
					this.modalValue = res?.data?.fullName || ''
					this.modalType = 'restName'
					this.$refs.inputDialog.open()
				})
			}
			if (type === 'checkAll') this.checkedAllFun()
			if (type === 'revert') this.recoveryOrDelete(type)
			if (type === 'delete') this.recoveryOrDelete(type)
			if (type === 'share') {
				if (this.current == 1) return this.shareUser()
				this.usersSelectValue = ''
				this.$nextTick(() => {
					this.$refs.JnpfUsersSelect.openSelect()
				})
			}
			if (type === 'shareCancel') return this.cancelShare()
			if (type === 'move') this.getFolderTree()
			if (type === 'cancel') {
				this.selectFiles = []
				this.selectOperation(this.selectFiles)
			}
		},
		cancelShare() {
			uni.showModal({
				title: '提示',
				content: '您确定要取消共享, 是否继续?',
				success: (res) => {
					if (res.confirm) {
						cancelShare({
							ids: this.selectFiles
						}).then(res => {
							this.$u.toast(res.msg)
							this.resetList()
						})
					}
				}
			});
		},
		shareUser() {
			shareUser(this.selectFiles[0]).then(res => {
				let list = res.data.list || []
				const ids = list.map(item => item.shareUserId);
				this.usersSelectValue = ids
				this.$nextTick(() => {
					this.$refs.JnpfUsersSelect.openSelect()
				})
			})
		},
		folderMove() {
			let data = {
				ids: this.selectFiles,
				id: this.moveId
			}
			folderMove(data).then(res => {
				this.selectFiles = []
				this.close()
				this.resetList()
			})
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
		iconClick() {
			if (this.documentList.length) this.changeStyle = !this.changeStyle
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
		close() {
			this.showApply = false
		},
		checkedAllFun() {
			this.checkedAll = !this.checkedAll
			this.selectFiles = [];
			this.documentList.forEach(o => {
				if (this.checkedAll) {
					this.$set(o, 'checked', true)
					this.selectFiles.push(o.id);
				} else {
					this.$set(o, 'checked', false)
					this.selectFiles = [];
				}
			})
		},
		goDetail(e) {
			if (e.type == 0 && this.current != 3) {
				if (!this.isDetail) {
					let item = {
						current: this.current,
						changeStyle: this.changeStyle,
						...e
					}
					this.selectFiles = []
					this.selectOperation()
					uni.navigateTo({
						url: './detail?config=' + JSON.stringify(item),
					});
				} else {
					this.parentId = e.id
					this.setTitle(e.fullName)
					this.pushTreeStack(e);
					this.resetList()
				}
			} else {
				if (imgTypeList.includes(e.isPreview)) {
					const images = this.baseURL + e.uploaderUrl;
					uni.previewImage({
						urls: [images],
						success: () => {},
						fail: () => {
							uni.showToast({
								title: '预览图片失败',
								icon: 'none'
							});
						}
					});
				}
				if (this.current !== 3) this.downLoad([e.id])
			}
		},
		selectOperation(value) {
			let items = this.documentList;
			this.selectFiles = value || [];
			for (let i = 0, lenI = items.length; i < lenI; ++i) {
				const item = items[i]
				if (this.selectFiles.includes(item.id)) {
					this.$set(item, 'checked', true)
				} else {
					this.$set(item, 'checked', false)
				}
			}
		},
		getRecordImg(ext) {
			if (!ext) return this.folderImg;
			if (ext) ext = ext.replace('.', '');
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
		getFolderTree() {
			let data = {
				ids: this.selectFiles
			}
			folderTree(data).then(res => {
				this.showApply = true
				this.folderTreeList = JSON.parse(JSON.stringify(res.data.list)) || []
				const loop = (list, parent) => {
					list.forEach(o => {
						o.icon = 'icon-ym icon-ym-folder';
						if (o && o.children && Array.isArray(o.children)) {
							loop(o.children, o)
						}
					})
				}
				loop(this.folderTreeList)
			})
		},
		recoveryOrDelete(type) {
			let data = {
				ids: this.selectFiles
			}
			let content = '确定要还原选中的文件吗'
			let method = recovery
			if (type !== 'revert') {
				content = '删除后，放入回收站!'
				method = batchDelete
				if (type === 'delete' && this.current == 3) {
					content = '删除后数据无法恢复'
					method = trashDelete
				}
			}
			uni.showModal({
				title: '提示',
				content,
				success: (res) => {
					if (res.confirm) {
						method(data).then(res => {
							this.$u.toast(res.msg)
							setTimeout(() => {
								this.$nextTick(() => {
									this.documentList = [];
									this.selectFiles = []
									this.mescroll.resetUpScroll();
								})
							}, 1000)
						})
					}
				}
			});
		},
		/* 新建文件 */
		handleAddFolder() {
			let item = {
				id: '',
				parentId: this.parentId,
				type: 0,
				fullName: this.modalValue
			}
			addFolder(item).then(res => {
				this.modalType = 'restName'
				this.closeDialog()
				this.resetList()
			})
		},
		/* 重命名 */
		closeDialog() {
			this.$refs.inputDialog.close()
		},
		restName(e) {
			let txt = this.modalType === 'addFolder' ? '文件夹名称不能为空' : '文件名不能为空'
			if (!this.modalValue) return this.$u.toast(txt)
			if (this.modalType === 'addFolder') return this.handleAddFolder()
			if (this.modalType === 'restName') return this.handleRestName()
		},
		handleRestName() {
			let item = {}
			this.documentList.forEach(o => {
				if (o.id === this.selectFiles[0]) item = {
					id: o.id,
					parentId: this.parentId,
					type: o.type,
					fullName: this.modalValue
				}
			})
			resetFileName(item).then(res => {
				this.selectFiles = []
				this.closeDialog()
				this.resetList()
			})
		},
		resetList() {
			this.$nextTick(() => {
				this.selectFiles = []
				this.documentList = [];
				this.mescroll.resetUpScroll();
			})
		},
	}
}