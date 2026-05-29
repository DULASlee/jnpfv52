<template>
	<view>
		<!-- 卡片 -->
		<template v-if="activeData.show && activeData.jnpfKey === 'card'">
			<view class="u-m-b-20">
				<HCard :cardData="activeData">
					<template #content>
						<view class="card-inner u-p-l-8 u-p-r-8 u-p-t-8">
							<Item v-for="(child, index) in activeData.children" :item="child" :key="index" />
						</view>
					</template>
				</HCard>
			</view>
		</template>
		<!-- 排行榜 -->
		<template v-if="activeData.show && activeData.jnpfKey === 'rankList'">
			<view class="u-m-b-20">
				<HCard :cardData="activeData">
					<template #content>
						<HRankList :config="activeData"></HRankList>
					</template>
				</HCard>
			</view>
		</template>
		<!-- 文本 -->
		<template v-if="activeData.show && activeData.jnpfKey === 'text'">
			<view class="u-m-b-20">
				<HCard :cardData="activeData">
					<template #content>
						<HText :config="activeData"></HText>
					</template>
				</HCard>
			</view>
		</template>
		<!-- 图片 -->
		<template v-if="activeData.show && activeData.jnpfKey === 'image'">
			<view class="u-m-b-20">
				<HCard :cardData="activeData">
					<template #content>
						<HImage :config="activeData"></HImage>
					</template>
				</HCard>
			</view>
		</template>
		<!-- 轮播图 -->
		<template v-if="activeData.show && activeData.jnpfKey === 'carousel'">
			<view class="u-m-b-20">
				<HCard :cardData="activeData">
					<template #content>
						<HCarousel :config="activeData"></HCarousel>
					</template>
				</HCard>
			</view>
		</template>
		<!-- 视频 -->
		<template v-if="activeData.show && activeData.jnpfKey === 'video'">
			<view class="u-m-b-20">
				<HCard :cardData="activeData">
					<template #content>
						<HVideo :config="activeData"></HVideo>
					</template>
				</HCard>
			</view>
		</template>
		<!-- 图表 -->
		<template v-if="activeData.show && chartList.includes(activeData.jnpfKey)">
			<view class="u-m-b-20">
				<HCard :cardData="activeData">
					<template #content>
						<HCharts :config="activeData" :key="key"></HCharts>
					</template>
				</HCard>
			</view>
		</template>
		<!-- 自定义echarts -->
		<template v-if="activeData.show && activeData.jnpfKey === 'customEcharts'">
			<view class="u-m-b-20">
				<HCard :cardData="activeData">
					<template #content>
						<HCustomeCharts :config="activeData" :key="key"></HCustomeCharts>
					</template>
				</HCard>
			</view>
		</template>
		<!-- 我的待办 -->
		<template v-if="activeData.show && activeData.jnpfKey === 'todo'">
			<view class="u-m-b-20">
				<HCard :cardData="activeData">
					<template #content>
						<HTodo :config="activeData" :key="key"></HTodo>
					</template>
				</HCard>
			</view>
		</template>
		<!-- 常用功能 -->
		<template v-if="activeData.show && activeData.jnpfKey === 'dataBoard'">
			<view class="u-m-b-20">
				<HCard :cardData="activeData">
					<template #content>
						<HDataBoard :config="activeData" :key="key"></HDataBoard>
					</template>
				</HCard>
			</view>
		</template>
		<!-- 数据面板 -->
		<template v-if="activeData.show && activeData.jnpfKey === 'commonFunc'">
			<view class="u-m-b-20">
				<HCard :cardData="activeData">
					<template #content>
						<HCommonFunc :config="activeData" :key="key"></HCommonFunc>
					</template>
				</HCard>
			</view>
		</template>
		<!-- 时间轴 -->
		<template v-if="activeData.show && activeData.jnpfKey === 'timeAxis'">
			<view class="u-m-b-20">
				<HCard :cardData="activeData">
					<template #content>
						<HTimeAxis :config="activeData"></HTimeAxis>
					</template>
				</HCard>
			</view>
		</template>
		<!-- 表格 -->
		<template v-if="activeData.show && activeData.jnpfKey === 'tableList'">
			<view class="u-m-b-20">
				<HCard :cardData="activeData">
					<template #content>
						<HTable :config="activeData"></HTable>
					</template>
				</HCard>
			</view>
		</template>
		<!-- 待办事项 -->
		<template v-if="activeData.show && activeData.jnpfKey === 'todoList'">
			<view class="u-m-b-20">
				<HCard :cardData="activeData">
					<template #content>
						<HTodoList :config="activeData"></HTodoList>
					</template>
				</HCard>
			</view>
		</template>
		<!-- 未读邮件 -->
		<template v-if="activeData.show && activeData.jnpfKey === 'email'">
			<view class="u-m-b-20">
				<HCard :cardData="activeData">
					<template #content>
						<HEmail :config="activeData"></HEmail>
					</template>
				</HCard>
			</view>
		</template>
		<!-- 公告通知 -->
		<template v-if="activeData.show && activeData.jnpfKey === 'notice'">
			<view class="u-m-b-20">
				<HCard :cardData="activeData">
					<template #content>
						<HNotice :config="activeData"></HNotice>
					</template>
				</HCard>
			</view>
		</template>
		<!-- 标签 -->
		<template v-if="activeData.show && activeData.jnpfKey === 'tab'">
			<view class="u-m-b-20" style="background-color: #ffffff;">
				<u-tabs :list="activeData.children" name="title" :is-scroll="activeData.children.length>3"
					v-model="tabCurrent" @change="onTabChange" :show-bar="!!activeData.type" :class="tabsClass"
					:inactive-color="activeData.type==='border-card'?' #9ea1a6':'#303133'"
					:active-item-style='activeItemStyle' :bg-color="activeData.type==='border-card'?'#f5f7fa':'#fff'">
				</u-tabs>
				<view v-for="(item,i) in activeData.children" :key='i' class="tab-inner u-p-8">
					<view v-show="i == tabCurrent">
						<Item v-for="(child, index) in item.children" :item="child" :key="key" />
					</view>
				</view>
			</view>
		</template>
	</view>
</template>
<script>
	import HCard from './HCard'
	import HDataBoard from './HDataBoard'
	import HTable from './HTable'
	import HNotice from './HNotice'
	import HEmail from './HEmail'
	import HTodoList from './HTodoList'
	import HCharts from './HCharts'
	import HCustomeCharts from './HCustomeCharts'
	import HRankList from './HRankList'
	import HImage from './HImage'
	import HCarousel from './HCarousel'
	import HText from './HText'
	import HVideo from './HVideo'
	import HTodo from './HTodo'
	import HCommonFunc from './HCommonFunc'
	import HTimeAxis from './HTimeAxis'
	const chartList = ['barChart', 'lineChart', 'pieChart', 'radarChart', 'mapChart'];
	export default {
		name: 'Item',
		props: {
			item: {
				type: Object,
				default: () => ({})
			}
		},
		components: {
			HCard,
			HDataBoard,
			HTable,
			HNotice,
			HEmail,
			HTodoList,
			HCharts,
			HRankList,
			HImage,
			HCarousel,
			HCustomeCharts,
			HText,
			HVideo,
			HTodo,
			HCommonFunc,
			HTimeAxis
		},
		data() {
			return {
				cardData: {},
				current: 0,
				tabCurrent: 0,
				key: +new Date(),
				tabsClass: '',
				activeItemStyle: {
					'background-color': '#fff',
				},
				chartList
			}
		},
		computed: {
			activeData() {
				const activeData = this.item;
				if (activeData.titleI18nCode) activeData.title = this.$t(activeData.titleI18nCode, activeData.title);
				if (activeData.card.cardRightBtnI18nCode) activeData.card.cardRightBtn =
					this.$t(activeData.card.cardRightBtnI18nCode, activeData.card.cardRightBtn);
				if (activeData.jnpfKey === 'tab') {
					for (let i = 0; i < activeData.children.length; i++) {
						if (activeData.children[i].titleI18nCode) activeData.children[i].title =
							this.$t(activeData.children[i].titleI18nCode, activeData.children[i].title);
					}
				}
				if (['todo', 'commonFunc', 'dataBoard'].includes(activeData.jnpfKey)) {
					for (let i = 0; i < activeData.option.defaultValue.length; i++) {
						const e = activeData.option.defaultValue[i];
						if (e.fullNameI18nCode) e.fullName = this.$t(e.fullNameI18nCode, e.fullName);
					}
				}
				if (activeData.jnpfKey === 'carousel') {
					for (let i = 0; i < activeData.option.defaultValue.length; i++) {
						const e = activeData.option.defaultValue[i];
						if (e.textDefaultValueI18nCode) {
							e.textDefaultValue = this.$t(e.textDefaultValueI18nCode, e.textDefaultValue);
						}
					}
				}
				if (activeData.jnpfKey === 'text' && activeData.dataType === 'static' && activeData.option
					.defaultValueI18nCode) {
					activeData.option.defaultValue =
						this.$t(activeData.option.defaultValueI18nCode, activeData.option.defaultValue);
				}
				if (activeData.jnpfKey === 'image' && activeData.option.textDefaultValueI18nCode) {
					activeData.option.textDefaultValue =
						this.$t(activeData.option.textDefaultValueI18nCode, activeData.option.textDefaultValue);
				}
				if (['tableList', 'notice'].includes(activeData.jnpfKey)) {
					for (let i = 0; i < activeData.option.columnData.length; i++) {
						const e = activeData.option.columnData[i];
						if (e.fullNameI18nCode) e.fullName = this.$t(e.fullNameI18nCode, e.fullName);
					}
				}
				if (activeData.jnpfKey === 'rankList') {
					for (let i = 0; i < activeData.option.columnOptions.length; i++) {
						const e = activeData.option.columnOptions[i];
						if (e.labelI18nCode) e.label = this.$t(e.labelI18nCode, e.label);
					}
				}
				if (chartList.includes(activeData.jnpfKey)) {
					if (activeData.option.titleTextI18nCode) activeData.option.titleText =
						this.$t(activeData.option.titleTextI18nCode, activeData.option.titleText);
					if (activeData.option.titleSubtextI18nCode) activeData.option.titleSubtext =
						this.$t(activeData.option.titleSubtextI18nCode, activeData.option.titleSubtext);
					if (activeData.option.xAxisNameI18nCode) activeData.option.xAxisName =
						this.$t(activeData.option.xAxisNameI18nCode, activeData.option.xAxisName);
					if (activeData.option.yAxisNameI18nCode) activeData.option.yAxisName =
						this.$t(activeData.option.yAxisNameI18nCode, activeData.option.yAxisName);
				}
				return activeData
			}
		},
		created() {
			if (this.item.jnpfKey === 'tab') {
				const list = this.item.children
				for (let i = 0; i < list.length; i++) {
					if (this.item.active == list[i].name) {
						this.tabCurrent = i
						break
					}
				}
				if (this.item.type === "border-card" || this.item.type === "card") {
					this.tabsClass = 'htabs'
				}
			}
		},
		methods: {
			change(index) {
				this.current = index;
			},
			onTabChange(index) {
				if (this.tabCurrent === index) return
				this.tabCurrent = index;
				this.key = +new Date()
			},
		}
	}
</script>

<style lang="scss">
</style>