import {
	i18n
} from '../locale/setupI18n';

let permission = {
	permissionList: uni.getStorageSync('permissionList') || [],
	hasP(enCode, menuIds) {
		if (!menuIds) return false
		const list = permission.permissionList.filter(o => o.modelId === menuIds)
		if (!list.length) return false
		const columnList = list[0] && list[0].column ? list[0].column : []
		if (!columnList.length) return false
		const hasPermission = columnList.some(column => column.enCode === enCode)
		if (hasPermission) return true
		return false
	},
	hasFormP(enCode, menuIds) {
		if (!menuIds) return false
		const list = permission.permissionList.filter(o => o.modelId === menuIds)
		if (!list.length) return false
		const formList = list[0] && list[0].form ? list[0].form : []
		if (!formList.length) return false
		const hasPermission = formList.some(form => form.enCode === enCode)
		if (hasPermission) return true
		return false
	},
	hasBtnP(enCode, menuIds) {
		if (!menuIds) return false
		const list = permission.permissionList.filter(o => o.modelId === menuIds)
		if (!list.length) return false
		const btnList = list[0] && list[0].button ? list[0].button : []
		if (!btnList.length) return false
		const hasPermission = btnList.some(btn => btn.enCode === enCode)
		if (hasPermission) return true
		return false
	},
	getPermission(columnData, menuId, getScriptFunc) {
		let btnsList = columnData.btnsList
		let customBtnsList = columnData.customBtnsList
		let columnBtnsList = columnData.columnBtnsList.filter(o => o.show)
		const useBtnPermission = columnData.useBtnPermission
		const useColumnPermission = columnData.useColumnPermission
		const useFormPermission = columnData.useFormPermission
		const useBtnPermissionList = [...btnsList, ...columnBtnsList]
		let btnPermission = [];
		let customBtnsPermission = [];
		let columnPermission = [];
		let formList = [];
		let currentMenu = {}
		let btn_list = ['detail', 'edit', 'add', 'remove', 'batchRemove']
		let labelS = {}
		let enableFunc = {}
		useBtnPermissionList.map((o) => {
			if (btn_list.includes(o.value) && o.show) {
				labelS['btn_' + o.value] = o.labelI18nCode ? i18n.global.t(o.labelI18nCode, o.label) : o
					.label
			}
		})
		let isMenu = permission.permissionList.filter((o) => {
			if (o.modelId === menuId) return currentMenu = o
		})
		//按钮
		if (useBtnPermission) {
			if (customBtnsList && customBtnsList.length) {
				for (let i = 0; i < customBtnsList.length; i++) {
					inner: for (let j = 0; j < currentMenu.button.length; j++) {
						if (customBtnsList[i].value === currentMenu.button[j].enCode) {
							customBtnsPermission.push(customBtnsList[i])
							break inner
						}
					}
				}
			}
			if (!!isMenu.length) {
				for (let i = 0; i < useBtnPermissionList.length; i++) {
					inner: for (let j = 0; j < currentMenu.button.length; j++) {
						if ('btn_' + useBtnPermissionList[i].value === currentMenu.button[j].enCode) {
							btnPermission.push(currentMenu.button[j].enCode)
							break inner
						}
					}
				}
			}
		} else {
			for (let i = 0; i < useBtnPermissionList.length; i++) {
				inner: for (let j = 0; j < btn_list.length; j++) {
					if (useBtnPermissionList[i].show && useBtnPermissionList[i].value === btn_list[j]) {
						btnPermission.push('btn_' + useBtnPermissionList[i].value)
						break inner
					}
				}
			}
			customBtnsPermission = customBtnsList
		}
		// 启用规则
		columnBtnsList.map((o) => {
			enableFunc[o.value] = () => {
				return true
			}
			if (o.event && o.event.enableFunc) {
				const func = getScriptFunc(o.event.enableFunc)
				enableFunc[o.value] = func
			}
		})
		// 启用规则自定义
		customBtnsList.map((o) => {
			enableFunc[o.value] = () => {
				return true
			}
			if (o.event && o.event.enableFunc) {
				const func = getScriptFunc(o.event.enableFunc)
				enableFunc[o.value] = func
			}
		})
		if (useColumnPermission) {
			if (!!isMenu.length) {
				columnData.columnList.forEach((o, i) => {
					currentMenu.column.forEach((m, j) => {
						if (o.prop === m.enCode) {
							columnPermission.push(o)
						}
					})
				})
			}
		} else {
			columnPermission = columnData.columnList
		}

		if (useFormPermission && !!isMenu.length) formList = currentMenu.form
		return {
			labelS,
			btnPermission,
			customBtnsPermission,
			columnPermission,
			formPermission: {
				formList,
				useFormPermission,
				menuId
			},
			enableFunc,
			useBtnPermission
		}
	},
	updatePermissionList() {
		permission.permissionList = uni.getStorageSync('permissionList') || []
	}
}
export default permission