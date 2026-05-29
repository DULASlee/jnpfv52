<template>
	<view>
		<u-time-line>
			<u-time-line-item nodeTop="2" class="u-p-b-20" v-for="item,index in list" :key="index">
				<template v-slot:node>
					<view class="u-node" :style="{ background: getTimeLineTagColor(item.type) }"></view>
				</template>
				<template v-slot:content>
					<view class="u-font-24 content">
						<view class="start" v-if="item.type == '0'">
							<view class="u-m-b-20 u-p-l-8 log-title u-flex">
								<text>创建</text>
								<text>{{item.creatorTime ? $u.timeFormat(item.creatorTime, 'yyyy-mm-dd hh:MM:ss'):''}}</text>
							</view>
							<view class="u-flex avatar-box" style="background-color: #F5F5F5;">
								<u-avatar :src="baseURL + item.headIcon" size="mini" mode="circle"
									class="avatar"></u-avatar>
								<text class="u-m-l-8">{{item.creatorUserName}}</text>
							</view>
						</view>
						<view class="avatar-block" v-else>
							<view class="u-m-b-20 u-p-l-8 u-flex log-title">
								<text>编辑</text>
								<text>{{item.creatorTime ? $u.timeFormat(item.creatorTime, 'yyyy-mm-dd hh:MM:ss'):''}}</text>
							</view>
							<view class="u-flex avatar-box ">
								<view class="u-flex">
									<u-avatar :src="baseURL + item.headIcon" size="mini" mode="circle"
										class="avatar"></u-avatar>
									<text class="u-m-l-8">{{item.creatorUserName}}</text>
								</view>
								<view>
									<text class="u-m-l-8">{{`有`}}</text>
									<text class="u-m-l-8" style="color: red;">{{item.dataLog.length}}</text>
									<text class="u-m-l-8">处更改</text>
								</view>
							</view>
							<view class="dataLog-child">
								<view class="dataLog-item" v-for="child,i in item.dataLog" :key="i">
									<!-- 子表 -->
									<view class="child-line" v-if="child.jnpfKey === 'table'">
										<view class="field-child">
											<text class="table-name">{{child.fieldName}}：</text>
											<view class="u-flex table-detail">
												<text>已修改</text>
												<view class="u-m-l-20 approver-list-r u-flex" @click="openChild(child)">
													<view class="txt">详情</view>
													<text class="icon-ym icon-ym-right"></text>
												</view>
											</view>
										</view>
									</view>
									<!-- 主表 -->
									<view class="ordinary-field" v-else>
										<view class="u-m-b-10 left">{{child.fieldName}}：</view>
										<view class="right">
											<view class="val" v-if="child.nameModified">
												<text style="color: #efbd47;">已修改</text>
											</view>
											<view class="val" v-else>
												<template v-if="child.jnpfKey === 'sign'">
													<view v-if="child.oldData !== child.newData">
														<text v-if="child.oldData" class="u-m-r-8 txt line-through"
															@click="previewImage(child.oldData,child.jnpfKey)">
															旧签名
														</text>
														<text v-if="child.newData">
															<text>修改为</text>
															<text class="u-m-l-8 txt"
																@click="previewImage(child.newData,child.jnpfKey)">
																新签名
															</text>
														</text>
													</view>
													<text v-if="child.oldData === child.newData"
														@click="previewImage(child.oldData,child.jnpfKey)">签名</text>
												</template>
												<template v-else-if="child.jnpfKey === 'signature'">
													<view v-if="child.oldData !== child.newData">
														<text v-if="child.oldData" class="u-m-r-8 txt line-through"
															@click="previewImage(child.oldData)">
															旧签章
														</text>
														<text v-if="child.newData">
															<text>修改为</text>
															<text class="u-m-l-8 txt"
																@click="previewImage(child.newData)">
																新签章
															</text>
														</text>
													</view>
													<text v-if="child.oldData === child.newData"
														@click="previewImage(child.oldData)">签章</text>
												</template>
												<template v-else>
													<text v-if="child.oldData !== child.newData">
														<text class="line-through">{{child.oldData}}</text>
														<text class="u-m-l-16 u-m-r-16"
															v-if="!child.oldData || child.newData ">修改为</text>
														<text>{{child.newData}}</text>
													</text>
													<text v-if="child.oldData === child.newData">未修改</text>
												</template>
											</view>
										</view>
									</view>
								</view>
							</view>
						</view>
					</view>
				</template>
			</u-time-line-item>
		</u-time-line>
	</view>
	<uni-popup ref="flowStepPopup" background-color="#fff" border-radius="8rpx 8rpx 0 0" :is-mask-click="false">
		<view class="timeLine-popup-content u-flex-col">
			<view class="u-flex head-title notice-warp">
				<text class="text">修改详情</text>
				<text class="text icon-ym icon-ym-fail" @click="popupClose"></text>
			</view>
			<view class="table-content">
				<view class="table-content-inner" v-for="(item,index) in chidData">
					<view class="table-content-title">
						{{item.jnpf_type == '0' ? '新增' : item.jnpf_type == '2' ? '删除' : '编辑'}}
					</view>
					<view class="table-field u-flex-col">
						<view class="u-flex table-field-item" v-for="(field,i) in item.chidField" :key="i"
							:style="{backgroundColor:item.jnpf_type == '0'?'#defee9':item.jnpf_type == '2'?'#ffd2d2':''}">
							<view class="label">{{field.label}}</view>
							<!-- 显示已修改，未修改 -->
							<view class="val" v-if="field.nameModified">
								<text v-if="field.oldData !== field.newData" style="color: #efbd47;"
									:class="item.jnpf_type == '2' ? 'line-through' : ''">已修改</text>
								<text v-if="field.oldData === field.newData"
									:class="item.jnpf_type == '2' ? 'line-through' : ''">{{item.jnpf_type == 0 ? '' : '未修改'}}</text>
							</view>
							<view class="val" v-else>
								<!-- 签名，电子签章 -->
								<template v-if="field.jnpfKey === 'sign'">
									<view v-if="field.oldData !== field.newData" class="val">
										<text v-if="field.oldData" class="u-m-r-8 txt"
											:class="item.jnpf_type == '2' || item.jnpf_type == '1' ? 'line-through' : ''"
											@click="previewImage(field.oldData,field.jnpfKey)">
											旧签名
										</text>
										<template class="" v-if="item.jnpf_type != 2">
											<text v-if="field.newData && field.oldData">修改为</text>
											<text class="u-m-l-8 txt"
												@click="previewImage(field.newData,field.jnpfKey)">
												新签名
											</text>
										</template>
									</view>
									<text @click="previewImage(field.oldData,field.jnpfKey)" class="txt"
										v-if="field.oldData === field.newData">{{item.jnpf_type == 0 ||(!field.oldData && !field.newData)? '' : '签名'}}</text>
								</template>
								<template v-else-if="field.jnpfKey === 'signature'">
									<view v-if="field.oldData !== field.newData" class="val">
										<text v-if="field.oldData" class="u-m-r-8 txt txt-left"
											:class="item.jnpf_type == '2' || item.jnpf_type == '1'  ? 'line-through' : ''"
											@click="previewImage(field.oldData)">
											旧签章
										</text>
										<template class="" v-if="item.jnpf_type != 2">
											<text v-if="field.newData && field.oldData">修改为</text>
											<text class="u-m-l-8 txt text-right" @click="previewImage(field.newData)">
												新签章
											</text>
										</template>
									</view>
									<text @click="previewImage(field.oldData)" class="txt"
										v-if="field.oldData === field.newData">{{item.jnpf_type == 0 ||(!field.oldData && !field.newData) ? '' : '签章'}}</text>
								</template>
								<!-- 签名，电子签章end -->
								<template v-else>
									<view class="u-flex modify-box" v-if="field.oldData !== field.newData ">
										<view class="modify modify-left"
											:style="{'text-align':item.jnpf_type == '2'? 'end' : 'center'}"
											:class=" item.jnpf_type == '2' || item.jnpf_type == '1'   ? 'line-through' : ''"
											v-if="field.oldData">
											{{field.oldData}}
										</view>
										<view v-if="field.newData && field.oldData" class="modify-center">修改为
										</view>
										<view class="modify modify-right"
											:style="{'text-align': item.jnpf_type == '0'? 'end' : 'center'}"
											:class="item.jnpf_type == '2' ? 'line-through' : ''" v-if="field.newData">
											{{field.newData}}
										</view>
									</view>
									<view v-if="field.oldData === field.newData" class="modify-else"
										:class="item.jnpf_type == '2' ? 'line-through' : ''">
										{{field.oldData}}
									</view>
								</template>
							</view>
						</view>
					</view>
				</view>
			</view>
		</view>
	</uni-popup>
</template>
<script>
	import {
		recordList
	} from '@/api/workFlow/flowBefore'


	import {
		useDefineSetting
	} from '@/utils/useDefineSetting';
	export default {
		props: {
			dataLogList: {
				type: Array,
				default: () => []
			}
		},
		data() {
			return {
				useDefine: useDefineSetting(),
				recordList: [],
				chidData: [],
				chidField: [],
				childItem: {}
			}
		},
		computed: {
			baseURL() {
				return this.define.baseURL
			},
			list() {
				let dataLogList = JSON.parse(JSON.stringify(this.dataLogList))
				if (dataLogList.length) {
					let dataLogData = dataLogList.map(o => ({
						...o,
						dataLog: JSON.parse(o.dataLog)
					}))
					return dataLogData
				}
			}
		},
		methods: {
			previewImage(url, jnpfKey) {
				// #ifndef MP
				let baseURL = jnpfKey == 'sign' ? url : (this.baseURL + url)
				uni.previewImage({
					urls: [baseURL]
				});
				// #endif
			},
			openChild(child) {
				this.childItem = child || {}
				let chidData = this.childItem.chidData || [];
				let chidField = this.childItem.chidField || [];
				let data = chidData.map(o => {
					let item = {
						jnpf_type: o.jnpf_type,
						chidField: []
					};
					chidField.forEach(chid => {
						if (o.hasOwnProperty(chid.prop)) {
							let val = o[chid.prop]
							if (chid.jnpfKey === "calculate" && !chid.prop) return
							let newData = {
								...chid,
								newData: val,
								oldData: o[`jnpf_old_${chid.prop}`] ? o[`jnpf_old_${chid.prop}`] : ''
							};
							item.chidField.push(newData);
						} else {
							item.chidField.push({
								...chid,
								newData: '',
								oldData: ''
							});
						}
					});
					return item;
				});
				this.chidData = data
				this.$nextTick(() => {
					this.$refs.flowStepPopup.open('bottom')
				})
			},
			getTimeLineTagColor(status) {
				return status == 0 ? '#08AF28' : '#0177FF';
			},
			popupClose() {
				this.$refs.flowStepPopup.close()
			}
		}
	}
</script>

<style lang="scss">
	.line-through {
		text-decoration: line-through;
	}

	.modify-else {
		overflow: hidden;
		word-break: break-all;
		text-overflow: ellipsis;
	}

	.timeLine-popup-content {
		height: 1200rpx;
		overflow-y: scroll;

		.head-title {
			justify-content: space-between;
			color: #333333;
			padding: 0 20rpx;
			/* #ifdef APP */
			// padding: 20rpx 0;
			/* #endif */
			height: 80rpx;
		}

		.notice-warp {
			top: -2rpx;
			height: 80rpx;
			// border-bottom: 1rpx solid #ececec;
		}

		.table-content {
			padding: 0 20rpx;
			margin-top: 80rpx;

			.table-content-inner {
				margin-bottom: 40rpx;

				.table-content-title {
					min-height: 80rpx;
					background-color: #f5f5f5;
					padding: 16rpx 20rpx;
					border-radius: 8rpx 8rpx 0 0;
					color: #303133;
				}

				.table-field {
					.table-field-item {
						min-height: 80rpx;
						padding: 16rpx 20rpx;
						justify-content: space-between;
						align-items: center;
						border-bottom: 1rpx solid #ececec;
						color: #606266;

						&:last-child {
							border-radius: 0rpx 0rpx 8rpx 8rpx;
						}

						.label {
							flex: 0.3;
						}

						.val {
							font-size: 24rpx;
							text-align: right;
							flex: 0.7;

							.txt {
								color: #4aa8ff;
							}

							.modify-box {
								justify-content: flex-end;

								.modify {
									padding: 8rpx;
									border-radius: 8rpx;
									color: #606266;
								}

								.modify-center {
									width: 100rpx;
									text-align: center;
								}

								.modify-right {
									width: 200rpx;
									word-wrap: break-word;
									background-color: #defee9;
									text-align: center;
								}

								.modify-left {
									width: 200rpx;
									word-wrap: break-word;
									background-color: #ffd2d2;
									text-align: center;
								}
							}
						}
					}
				}
			}
		}
	}

	.u-node {
		width: 20rpx;
		height: 20rpx;
		border-radius: 50%;
	}

	.content {
		.start {
			.log-title {
				justify-content: space-between;
			}

			// box-shadow: 0 6rpx 12rpx rgba(2, 7, 28, 0.16);
			.avatar-box {
				padding: 20rpx;
			}
		}

		.avatar-block {
			.log-title {
				justify-content: space-between;
			}

			border-radius: 8rpx;
			// box-shadow: 0 6rpx 12rpx rgba(2, 7, 28, 0.16);

			.avatar-box {
				justify-content: space-between;
				background-color: #F5F5F5;
				border-bottom: 1rpx solid #ececec;
				padding: 20rpx;
			}

			.dataLog-child {
				background-color: #F5F5F5;
				// padding: 0 20rpx;

				.dataLog-item {
					padding: 20rpx;

					&:last-child {
						border-bottom: none;
					}

					.ordinary-field {
						align-items: flex-start;

						.left {
							flex: 0.2;
						}

						.right {
							flex: 0.8;

							.val {
								font-size: 24rpx;
								flex: 0.8;

								.txt {
									color: #4aa8ff;
								}
							}
						}
					}

					.child-line {
						.field-child {
							justify-content: space-between;

							.table-detail {
								justify-content: space-between;
							}

							.approver-list-r {
								height: 46rpx;

								.txt {
									height: 36rpx;
								}
							}
						}
					}
				}
			}
		}
	}
</style>