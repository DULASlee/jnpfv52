<template>
	<view class="jnpf-editor">
		<template v-if="!detailed">
			<view class='toolbar'>
				<view :class="{'ql-active':formats.bold}" class="iconfont icon-zitijiacu" data-name="bold"
					@tap="format">
				</view>
				<view :class="{'ql-active':formats.italic}" class="iconfont icon-zitixieti" data-name="italic"
					@tap="format"></view>
				<view :class="{'ql-active':formats.underline}" class="iconfont icon-zitixiahuaxian"
					data-name="underline" @tap="format"></view>
				<view :class="{'ql-active':formats.strike}" class="iconfont icon-zitishanchuxian" data-name="strike"
					@tap="format"></view>
				<view :class="{'ql-active':formats.align==='left'}" class="iconfont icon-zuoduiqi" data-name="align"
					data-value="left" @tap="format"></view>
				<view :class="{'ql-active':formats.align==='center'}" class="iconfont icon-juzhongduiqi"
					data-name="align" data-value="center" @tap="format"></view>
				<view :class="{'ql-active':formats.align==='right'}" class="iconfont icon-youduiqi" data-name="align"
					data-value="right" @tap="format"></view>
				<view :class="{'ql-active':formats.align==='justify'}" class="iconfont icon-zuoyouduiqi"
					data-name="align" data-value="justify" @tap="format"></view>
				<view :class="{'ql-active':formats.lineHeight}" class="iconfont icon-line-height" data-name="lineHeight"
					data-value="2" @tap="format"></view>
				<view :class="{'ql-active':formats.letterSpacing}" class="iconfont icon-Character-Spacing"
					data-name="letterSpacing" data-value="2em" @tap="format"></view>
				<view :class="{'ql-active':formats.marginTop}" class="iconfont icon-722bianjiqi_duanqianju"
					data-name="marginTop" data-value="20px" @tap="format"></view>
				<view :class="{'ql-active':formats.marginBottom}" class="iconfont icon-723bianjiqi_duanhouju"
					data-name="marginBottom" data-value="20px" @tap="format"></view>
				<view class="iconfont icon-clearedformat" @tap="removeFormat"></view>
				<view :class="{'ql-active':formats.fontFamily}" class="iconfont icon-font" data-name="fontFamily"
					data-value="Pacifico" @tap="format"></view>
				<view :class="{'ql-active':formats.fontSize === '24px'}" class="iconfont icon-fontsize"
					data-name="fontSize" data-value="24px" @tap="format"></view>
				<view :class="{'ql-active':formats.color === '#0000ff'}" class="iconfont icon-text_color"
					data-name="color" data-value="#0000ff" @tap="format"></view>
				<view :class="{'ql-active':formats.backgroundColor === '#00ff00'}" class="iconfont icon-fontbgcolor"
					data-name="backgroundColor" data-value="#00ff00" @tap="format"></view>
				<view class="iconfont icon-date" @tap="insertDate"></view>
				<view class="iconfont icon--checklist" data-name="list" data-value="check" @tap="format"></view>
				<view :class="{'ql-active':formats.list === 'ordered'}" class="iconfont icon-youxupailie"
					data-name="list" data-value="ordered" @tap="format"></view>
				<view :class="{'ql-active':formats.list === 'bullet'}" class="iconfont icon-wuxupailie" data-name="list"
					data-value="bullet" @tap="format"></view>
				<view class="iconfont icon-undo" @tap="undo"></view>
				<view class="iconfont icon-redo" @tap="redo"></view>
				<view class="iconfont icon-outdent" data-name="indent" data-value="-1" @tap="format"></view>
				<view class="iconfont icon-indent" data-name="indent" data-value="+1" @tap="format"></view>
				<view class="iconfont icon-fengexian" @tap="insertDivider"></view>
				<view class="iconfont icon-charutupian" @tap="insertImage"></view>
				<view :class="{'ql-active':formats.header === 1}" class="iconfont icon-format-header-1"
					data-name="header" :data-value="1" @tap="format"></view>
				<view :class="{'ql-active':formats.script === 'sub'}" class="iconfont icon-zitixiabiao"
					data-name="script" data-value="sub" @tap="format"></view>
				<view :class="{'ql-active':formats.script === 'super'}" class="iconfont icon-zitishangbiao"
					data-name="script" data-value="super" @tap="format"></view>
				<view class="iconfont icon-shanchu" @tap="clear"></view>
				<view :class="{'ql-active':formats.direction === 'rtl'}" class="iconfont icon-direction-rtl"
					data-name="direction" data-value="rtl" @tap="format"></view>
			</view>
			<view class="editor-wrapper">
				<editor :id="id" class="ql-container" :placeholder="placeholder" showImgSize showImgToolbar
					showImgResize @statuschange="onStatusChange" :read-only="disabled" @ready="onEditorReady"
					@input="getValue">
				</editor>
			</view>
		</template>
		<view v-else>
			<mp-html class="editor-box" :content="modelValue" />
		</view>
	</view>
</template>

<script>
	export default {
		name: 'jnpf-editor',
		props: {
			modelValue: {
				type: String,
				default: ''
			},
			placeholder: {
				type: String,
				default: '请输入'
			},
			disabled: {
				type: Boolean,
				default: false
			},
			detailed: {
				type: Boolean,
				default: false
			},
		},
		data() {
			return {
				innerValue: '',
				readOnly: false,
				formats: {},
				editorChange: false,
				id: 'jnpf-editor-' + this.jnpf.idGenerator(),
			}
		},
		watch: {
			modelValue(val) {
				if (this.editorChange && val) return
				this.editorChange = false
				this.editorCtx && this.editorCtx.setContents({
					html: val
				})
			}
		},
		onLoad() {
			uni.loadFontFace({
				family: 'Pacifico',
				source: 'url("/Pacifico-Regular.ttf")'
			})
		},
		methods: {
			readOnlyChange() {
				this.readOnly = !this.readOnly
			},
			onEditorReady() {
				// #ifdef APP-PLUS || H5 ||MP-WEIXIN
				uni.createSelectorQuery().in(this).select('#' + this.id).context((res) => {
					this.editorCtx = res.context
					this.editorCtx.setContents({
						html: this.modelValue
					})
				}).exec()
				// #endif
			},
			undo() {
				this.editorCtx.undo()
			},
			redo() {
				this.editorCtx.redo()
			},
			format(e) {
				let {
					name,
					value
				} = e.target.dataset
				if (!name) return
				this.editorCtx.format(name, value)
			},
			onStatusChange(e) {
				const formats = e.detail
				this.formats = formats
			},
			insertDivider() {
				this.editorCtx.insertDivider()
			},
			clear() {
				this.editorCtx.clear()
			},
			removeFormat() {
				this.editorCtx.removeFormat()
			},
			insertDate() {
				const date = new Date()
				const formatDate = `${date.getFullYear()}/${date.getMonth() + 1}/${date.getDate()}`
				this.editorCtx.insertText({
					text: formatDate
				})
			},
			insertImage() {
				uni.chooseImage({
					count: 1,
					success: (res) => {
						this.getImageBase64(res)
					}
				})
			},
			getImageBase64(res) {
				const image = res.tempFilePaths[0]
				// #ifdef MP-WEIXIN
				uni.getFileSystemManager().readFile({
					filePath: image,
					encoding: "base64",
					success: (e) => {
						this.insertImageVal('data:image/jpeg;base64,' + e.data)
					},
				});
				// #endif
				// #ifdef APP-PLUS
				let path = plus.io.convertLocalFileSystemURL(image);
				let fileReader = new plus.io.FileReader();
				fileReader.readAsDataURL(path);
				fileReader.onloadend = (e) => {
					this.insertImageVal(e.target.result);
				}
				// #endif
				// #ifdef H5
				uni.request({
					url: image, //临时路径
					responseType: 'arraybuffer', //设置返回的数据格式为arraybuffer
					success: res => {
						const base64 = wx.arrayBufferToBase64(res.data)
						this.insertImageVal('data:image/jpeg;base64,' + base64);
					},
				})
				// #endif
			},
			insertImageVal(image) {
				this.editorCtx.insertImage({
					src: image,
					alt: '图像',
					success: function() {}
				})
			},
			getValue(e) {
				this.editorChange = true
				const that = this
				this.editorCtx.getContents({
					success: function(res) {
						let val = res.text === '\n' ? '' : res.html
						that.$emit('update:modelValue', val)
					}
				});
			}
		}
	}
</script>

<style lang="scss" scoped>
	@import "./editor-icon.css";

	:deep(.ql-editor) {
		word-break: break-all;
	}

	.jnpf-editor {
		background-color: #fff;

		.iconfont {
			display: inline-block;
			width: 80rpx;
			height: 80rpx;
			cursor: pointer;
			font-size: 20px;
			line-height: 80rpx;
			text-align: center;
		}

		.toolbar {
			height: 240rpx;
			background: #f5f5f5;
			overflow-y: auto;
			box-sizing: border-box;
			border-bottom: 0;
			font-family: 'Helvetica Neue', 'Helvetica', 'Arial', sans-serif;
		}

		.ql-container {
			box-sizing: border-box;
			padding: 20rpx;
			width: 100%;
			height: 400rpx;
			margin-top: 20rpx;
			font-size: 30rpx;
			line-height: 1.5;
		}

		.ql-active {
			color: #06c;
		}
	}
</style>