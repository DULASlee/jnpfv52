<template>
	<view class="dynamicModel-form-v jnpf-wrap jnpf-wrap-form" v-if="showPage">
		<uni-nav-bar class='nav' :fixed="true" :statusBar="true" :border="false" height="44">
			<view class="nav-left">
				<view class="nav-left-text">详情</view>
			</view>
		</uni-nav-bar>
		<Parser :formConf="formConf" :formValue="formData" ref="dynamicForm" v-if="!loading" :key="key"
			@toDetail="toDetail" />
		<view class="buttom-actions">
			<u-button class="buttom-btn" @click.stop="jnpf.goBack">取消</u-button>
			<u-button class="buttom-btn" type="primary" @click.stop="handleEdit"
				v-if="btnType === 'btn_edit'&&!this.setting.noShowBtn">编辑
			</u-button>
		</view>
	</view>
</template>

<script>
	import {
		getConfig,
		createModel,
		getDataChange
	} from '@/api/apply/webDesign'
	import Parser from '@/pages/apply/dynamicModel/components/detail/Parser'
	const getFormDataFields = item => {
		const config = item.__config__
		if (!config || !config.jnpfKey) return true
		const jnpfKey = config.jnpfKey
		const list = ["input", "textarea", "inputNumber", "switch", "datePicker", "timePicker", "colorPicker", "rate",
			"slider", "editor", "link", "text", "alert", 'table', "collapse", 'collapseItem', 'tabItem',
			"tab", "row", "card", "groupTitle", "divider", 'location', 'stepItem', 'steps'
		]
		const fieldsSelectList = ["radio", "checkbox", "select", "cascader", "treeSelect"]
		if (list.includes(jnpfKey) || (fieldsSelectList.includes(jnpfKey) && config.dataType === 'static')) return true
		return false
	}
	export default {
		components: {
			Parser
		},
		data() {
			return {
				showPage: false,
				loading: true,
				isPreview: '0',
				modelId: '',
				formConf: {},
				formData: {},
				dataForm: {
					id: '',
					data: ''
				},
				btnType: '',
				formPermissionList: {},
				formList: [],
				encryption: ''
			}
		},
		onLoad(option) {
			let config = JSON.parse(this.jnpf.base64.decode(option.config))
			this.formPermissionList = !config.currentMenu ? [] : JSON.parse(decodeURIComponent(config.currentMenu))
			this.formList = this.formPermissionList.formList
			this.btnType = config.jurisdictionType || ''
			this.modelId = config.modelId;
			this.encryption = config.encryption;
			this.isPreview = config.isPreview || '0';
			this.dataForm.id = config.id || ''
			this.setting = config
			this.getConfigData()
			uni.$on('refresh', () => {
				this.getConfigData()
			})
		},
		beforeDestroy() {
			uni.$off('refresh')
		},
		methods: {
			// 递归过滤
			recursivefilter(arr, value) {
				let newColumn = arr.filter(item => getFormDataFields(item))
				newColumn.forEach(x =>
					x.__config__ && x.__config__.children && Array.isArray(x.__config__.children) && (x
						.__config__.children = this.recursivefilter(x.__config__.children))
				)
				return newColumn
			},
			getConfigData() {
				this.loading = true
				getConfig(this.modelId, this.encryption).then(res => {
					if (res.code !== 200 || !res.data) {
						uni.showToast({
							title: '暂无此页面',
							icon: 'none',
							complete: () => {
								setTimeout(() => {
									uni.navigateBack()
								}, 1500)
							}
						})
						return
					}
					this.formConf = res.data.formData ? JSON.parse(res.data.formData) : {};
					this.beforeInit(this.formConf.fields || []);
					this.showPage = true
					this.key = +new Date()
					this.initData()
				})
			},
			beforeInit(fields) {
				const loop = (list) => {
					for (var index = 0; index < list.length; index++) {
						const config = list[index].__config__;
						if (config.children && config.children.length) loop(config.children);
						if (config.jnpfKey == "tableGrid") {
							let newList = [];
							for (var i = 0; i < config.children.length; i++) {
								let element = config.children[i];
								for (var j = 0; j < element.__config__.children.length; j++) {
									let item = element.__config__.children[j];
									newList.push(...item.__config__.children);
								}
							}
							list.splice(index, 1, ...newList);
						}
					}
				};
				loop(fields);
			},
			initData() {
				this.$nextTick(() => {
					if (this.dataForm.id) {
						let extra = {
							modelId: this.modelId,
							id: this.dataForm.id,
							type: 2
						}
						getDataChange(this.modelId, this.dataForm.id, this.encryption).then(res => {
							this.dataForm = res.data
							if (!this.dataForm.data) return
							this.formData = {
								...JSON.parse(this.dataForm.data),
								id: this.dataForm.id
							}
							let fields = this.recursivefilter(this.formConf.fields)
							this.formConf.fields = fields
							this.fillFormData(fields, this.formData)
							this.initRelationForm(fields)
						})
					} else {
						this.loading = false
					}
					this.key = +new Date()
				})
			},
			fillFormData(form, data) {
				const loop = (list, parent) => {
					for (let i = 0; i < list.length; i++) {
						let item = list[i]
						if (item.__vModel__) {
							if (item.__config__.jnpfKey === 'relationForm' || item.__config__.jnpfKey ===
								'popupSelect') {
								item.__config__.defaultValue = data[item.__vModel__ + '_id']
								this.$set(item, 'name', data[item.__vModel__] || '')
							} else {
								let val = data.hasOwnProperty(item.__vModel__) ? data[item.__vModel__] : item
									.__config__.defaultValue
								if (!item.__config__.custom && item.__config__.defaultCurrent && item.__config__
									.jnpfKey === 'time') val = this.jnpf.toDate(new Date(), item.format)
								item.__config__.defaultValue = val
							}
							if (this.formPermissionList.useFormPermission) {
								let id = item.__config__.isSubTable ? parent.__vModel__ + '-' + item.__vModel__ : item
									.__vModel__
								let noShow = true
								if (this.formList && this.formList.length) {
									noShow = !this.formList.some(o => o.enCode === id)
								}
								noShow = item.__config__.noShow ? item.__config__.noShow : noShow
								this.$set(item.__config__, 'noShow', noShow)
							}
						} else {
							if (['relationFormAttr', 'popupAttr'].includes(item.__config__.jnpfKey)) {
								item.__config__.defaultValue =
									data[item.relationField.split('_jnpfTable_')[0] + '_' + item.showField];
							}
						}
						if (item.__config__ && item.__config__.children && Array.isArray(item.__config__.children)) {
							loop(item.__config__.children, item)
						}
					}
				}
				loop(form)
				this.loading = false
			},
			initRelationForm(componentList) {
				componentList.forEach(cur => {
					const config = cur.__config__
					if (config.jnpfKey == 'relationFormAttr' || config.jnpfKey == 'popupAttr') {
						const relationKey = cur.relationField.split("_jnpfTable_")[0]
						componentList.forEach(item => {
							const noVisibility = Array.isArray(item.__config__.visibility) && !item
								.__config__.visibility.includes('app')
							if ((relationKey == item.__vModel__) && (noVisibility || !!item.__config__
									.noShow)) {
								cur.__config__.noShow = true
							}
						})
					}
					if (cur.__config__.children && cur.__config__.children.length) this.initRelationForm(cur
						.__config__.children)
				})
			},
			toDetail(item) {
				const id = item.__config__.defaultValue
				if (!id) return
				let config = {
					modelId: item.modelId,
					id: id,
					formTitle: '详情',
					noShowBtn: 1,
					noDataLog: 1
				}
				this.$nextTick(() => {
					const url =
						'/pages/apply/dynamicModel/detail?config=' + this.base64.encode(JSON.stringify(config),
							"UTF-8")
					uni.navigateTo({
						url: url
					})
				})
			},
			handleEdit() {
				const currentMenu = encodeURIComponent(JSON.stringify(this.formPermissionList))
				let config = {
					modelId: this.modelId,
					isPreview: this.isPreview,
					id: this.setting.id,
					jurisdictionType: 'btn_edit',
					currentMenu,
					list: this.setting.list,
					index: this.setting.index
				}
				const url = '/pages/apply/dynamicModel/form?config=' + this.base64.encode(JSON.stringify(config), "UTF-8")
				uni.navigateTo({
					url: url
				})
			}
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.nav {
		z-index: 99999;

		:deep(.uni-navbar__content) {
			z-index: 99999;
		}

		:deep(.uni-navbar__header-container) {
			justify-content: center;
		}
	}

	.nav-left {
		max-width: 100%;
		display: flex;
		align-items: center;

		.nav-left-text {
			font-weight: 700;
			font-size: 32rpx;
			flex: 1;
			min-width: 0;
			white-space: nowrap;
			overflow: hidden;
			text-overflow: ellipsis;
		}

		.right-icons {
			font-weight: 700;
			margin-top: 2px;
			margin-left: 4px;
			transition-duration: 0.3s;
		}

		.select-right-icons {
			transform: rotate(-180deg);
		}
	}
</style>