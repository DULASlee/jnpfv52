<template>
	<view class="dataForm-v">
		<u-popup mode="left" :popup="false" v-model="show" length="auto" @close="close" width="100%">
			<view class="diyTitle u-flex">
				<uni-icons type="back" size="27" class="uni-btn-icon" @click="black"></uni-icons>
				<view class="txt">异常处理</view>
			</view>
			<view class="jnpf-wrap-form u-p-l-20 u-p-r-20">
				<u-form :model="errorDataForm" ref="errorDataForm" :errorType="['toast']" label-position="left"
					:label-width="250" label-align="left">
					<u-form-item prop="errorDataForm" v-for="(item,index) in list" :key="index" :label="item.nodeName"
						required>
						<JnpfUserSelect v-model="errorDataForm.errorRuleUserList[item.nodeCode]" :multiple="true"
							placeholder="请选择异常处理人" />
					</u-form-item>
				</u-form>
				<view class="buttom-actions">
					<u-button class="buttom-btn" @click="cancel">取消</u-button>
					<u-button class="buttom-btn" type="primary" @click="submit">确定</u-button>
				</view>
			</view>
		</u-popup>
	</view>
</template>
<script>
	export default {
		props: {
			// 通过双向绑定控制组件的弹出与收起
		},
		data() {
			return {
				errorDataForm: {
					errorRuleUserList: {},
				},
				list: [],
				show: false,
				query: {}
			};
		},
		methods: {
			init(list, query) {
				this.show = true
				this.list = list
				this.list.map(o => {
					this.$set(this.errorDataForm.errorRuleUserList, o.nodeCode, [])
				})
				this.query = {
					...query
				}
			},
			submit() {
				const hasEmptyUserList = Object.keys(this.errorDataForm.errorRuleUserList).some(rules => {
					return !this.errorDataForm.errorRuleUserList[rules].length;
				});
				if (hasEmptyUserList) return this.$u.toast('异常处理人员不能为空');
				const query = {
					errorRuleUserList: this.errorDataForm.errorRuleUserList,
					...this.query
				}
				this.$emit('submitErrorForm', query);
			},
			cancel() {
				this.close()
			},
			black() {
				this.close()
			},
			close() {
				this.show = false
			}
		}
	};
</script>
<style lang="scss" scoped>
	.dataForm-v {
		.diyTitle {
			height: 80rpx;
			padding: 14rpx 6rpx;
			text-align: center;
			justify-content: flex-start;

			.uniui-back {
				font-size: 27px;
				font-weight: lighter;
			}

			.txt {
				flex: 0.95;
			}
		}
	}
</style>