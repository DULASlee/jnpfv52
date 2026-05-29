import {
	defineStore
} from 'pinia';
import {
	getDictionaryDataAll,
	getOrganizeSelector,
	getDepartmentSelector,
	getGroupSelector,
	getRoleSelector,
	getPositionSelector,
	getUserSelector,
	getUserAll
} from '@/api/common.js'


export const useBaseStore = defineStore({
	id: ' app-base',
	state: () => ({
		dictionaryList: [],
		organizeTree: [],
		departmentTree: [],
		positionTree: [],
		groupTree: [],
		roleTree: [],
		userTree: [],
		userList: [],
		relationData: {},
	}),
	getters: {
		getDictionaryList() {
			return this.dictionaryList
		},
		getRelationData() {
			return this.relationData
		},
	},
	actions: {
		setDictionaryList(dictionaryList) {
			this.dictionaryList = dictionaryList || []
		},
		setOrganizeTree(organizeTree) {
			this.organizeTree = organizeTree
		},
		setDepartmentTree(departmentTree) {
			this.departmentTree = departmentTree
		},
		setPositionTree(positionTree) {
			this.positionTree = positionTree
		},
		setGroupTree(groupTree) {
			this.groupTree = groupTree
		},
		setRoleTree(roleTree) {
			this.roleTree = roleTree
		},
		setUserTree(userTree) {
			this.userTree = userTree
		},
		setUserList(userList) {
			this.userList = userList
		},
		updateRelationData(val) {
			this.relationData = val
		},
		getDictionaryDataAll() {
			return new Promise((resolve, reject) => {
				if (this.dictionaryList.length) {
					resolve(this.dictionaryList)
				} else {
					getDictionaryDataAll().then(res => {
						this.setDictionaryList(res.data.list)
						resolve(res.data.list)
					}).catch(error => {
						reject(error)
					})
				}
			})
		},
		getDictionaryData(info) {
			return new Promise(async resolve => {
				let list = [],
					data = [],
					json = []
				if (!this.dictionaryList.length) {
					list = await this.getDictionaryDataAll()
				} else {
					list = this.dictionaryList
				}
				if (info.sort) {
					data = list.filter(o => o.enCode === info.sort)[0]
					if (!info.id) {
						json = data?.dictionaryList || []
					} else {
						let rowData = [];
						if (!data.isTree) {
							rowData = data.dictionaryList.fliter(o => o.id == info.id)
						} else {
							const findData = list => {
								for (let i = 0; i < list.length; i++) {
									const e = list[i];
									if (e.id == info.id) {
										rowData[0] = e
										break
									}
									if (e.children && e.children.length) {
										findData(e.children)
									}
								}
							}
							findData(data.dictionaryList)
						}
						if (rowData.length) {
							json = rowData[0]
						} else {
							json = {
								id: "",
								fullName: ""
							};
						}
					}
				}
				resolve(json)
			})
		},
		getDicDataSelector(value, key = 'id') {
			return new Promise(async resolve => {
				let list = [],
					data = {},
					json = [];
				if (!this.dictionaryList.length) {
					list = await this.getDictionaryDataAll()
				} else {
					list = this.dictionaryList
				}
				if (!value) return resolve([])
				let arr = list.filter(o => o[key] === value);
				if (!arr.length) return resolve([])
				data = arr[0];
				json = data.dictionaryList;
				resolve(json)
			})
		},
		getOrganizeTree() {
			return new Promise((resolve, reject) => {
				if (!this.organizeTree.length) {
					getOrganizeSelector().then(res => {
						this.setOrganizeTree(res.data.list)
						resolve(res.data.list)
					}).catch(error => {
						reject(error)
					})
				} else {
					resolve(this.organizeTree)
				}
			})
		},
		getDepartmentTree() {
			return new Promise((resolve, reject) => {
				if (!this.departmentTree.length) {
					getDepartmentSelector().then(res => {
						this.setDepartmentTree(res.data.list)
						resolve(res.data.list)
					}).catch(error => {
						reject(error)
					})
				} else {
					resolve(this.departmentTree)
				}
			})
		},
		getPositionTree() {
			return new Promise((resolve, reject) => {
				if (!this.positionTree.length) {
					getPositionSelector().then(res => {
						this.setPositionTree(res.data.list)
						resolve(res.data.list)
					}).catch(error => {
						reject(error)
					})
				} else {
					resolve(this.positionTree)
				}
			})
		},
		getGroupTree() {
			return new Promise((resolve, reject) => {
				if (!this.groupTree.length) {
					getGroupSelector().then(res => {
						this.setGroupTree(res.data)
						resolve(res.data)
					}).catch(error => {
						reject(error)
					})
				} else {
					resolve(this.groupTree)
				}
			})
		},
		getRoleTree() {
			return new Promise((resolve, reject) => {
				if (!this.roleTree.length) {
					getRoleSelector().then(res => {
						this.setRoleTree(res.data.list)
						resolve(res.data.list)
					}).catch(error => {
						reject(error)
					})
				} else {
					resolve(this.roleTree)
				}
			})
		},
		getUserTree() {
			return new Promise((resolve, reject) => {
				if (!this.userTree.length) {
					getUserSelector().then(res => {
						this.setUserTree(res.data.list)
						resolve(res.data.list)
					}).catch(error => {
						reject(error)
					})
				} else {
					resolve(this.userTree)
				}
			})
		},
		getUserList() {
			return new Promise((resolve, reject) => {
				if (!this.userList.length) {
					getUserAll().then(res => {
						this.setUserList(res.data.list)
						resolve(res.data.list)
					}).catch(error => {
						reject(error)
					})
				} else {
					resolve(this.userList)
				}
			})
		},
		getUserInfo(id) {
			return new Promise(async resolve => {
				let list = []
				if (!this.userList.length) {
					list = await this.getUserList()
				} else {
					list = this.userList
				}
				let item = list.filter(o => o.id === id)[0]
				resolve(item || {})
			})
		},
	},
});