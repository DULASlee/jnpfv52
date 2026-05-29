<template>
	<view class="personalData-v">
		<u-cell-group class="" style="padding: 0 20rpx;" :border-bottom="false" :border="false">
			<u-cell-item title="账户" :value="data.account" :arrow="false" :title-style="titleStyle" />
			<u-cell-item title="所属组织" :value="data.organize" :arrow="false" :title-style="titleStyle" />
			<u-cell-item title="直属主管" :value="data.manager" :arrow="false" :title-style="titleStyle" />
			<u-cell-item title="岗位" :value="data.position" :arrow="false" :title-style="titleStyle" />
			<u-cell-item title="职级" :value="data.ranks" :arrow="false" :title-style="titleStyle" />
			<u-cell-item title="角色" :value="data.roleId" :arrow="false" :title-style="titleStyle" />
			<u-cell-item title="注册时间" :value="data.creatorTime" :arrow="false" :title-style="titleStyle" />
			<u-cell-item title="上次登录" :value="data.prevLogTime" :arrow="false" :title-style="titleStyle" />
			<u-cell-item title="入职时间" :value="data.entryDate" :arrow="false" :title-style="titleStyle"
				:border-bottom="false" />
		</u-cell-group>
	</view>
</template>

<script>
	export default {
		props: {
			accountData: {
				type: Object,
				default: () => ({})
			}
		},
		data() {
			return {
				titleStyle: {
					color: '#303133'
				}
			}
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			},
			data() {
				let obj = {
					...this.accountData
				}
				const {
					creatorTime,
					prevLogTime,
					entryDate
				} = obj
				obj.creatorTime = this.$u.timeFormat(obj.creatorTime, 'yyyy-mm-dd hh:MM') || ''
				obj.prevLogTime = this.$u.timeFormat(obj.prevLogTime, 'yyyy-mm-dd hh:MM') || ''
				obj.entryDate = this.$u.timeFormat(obj.entryDate, 'yyyy-mm-dd hh:MM') || ''
				return obj
			}
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	.personalData-v {
		background-color: #fff;
	}

	:deep(.u-cell) {
		height: 112rpx;
		padding: 20rpx 0;
	}
</style>