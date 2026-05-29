<template>
	<view class="settings-v">
		<u-cell-group class="u-p-l-20 u-p-r-20" :border="false">
			<!-- #ifndef MP-WEIXIN -->
			<u-cell-item :title="$t('app.my.settings.language')" @click="selectLocaleShow=true"
				:title-style="titleStyle"></u-cell-item>
			<!-- #endif -->
			<!-- #ifdef APP-PLUS -->
			<u-cell-item :title="$t('app.my.settings.userAgreement')" @click='openPage(agreement)'
				:title-style="titleStyle"></u-cell-item>
			<u-cell-item :title="$t('app.my.settings.privacyPolicy')" @click='openPage(policy)'
				:title-style="titleStyle"></u-cell-item>
			<!-- #endif -->
			<u-cell-item :title="$t('app.my.settings.changePassword')" @click="modifyPsd('/pages/my/modifyPsd/index')"
				:title-style="titleStyle">
			</u-cell-item>
			<u-cell-item :title="$t('app.my.settings.contact')" @click="modifyPsd('/pages/my/contactUs/index')"
				:title-style="titleStyle">
			</u-cell-item>
			<u-cell-item :title="$t('app.my.settings.About')" @click="modifyPsd('/pages/my/abouts/index')"
				:title-style="titleStyle" :border-bottom="false">
			</u-cell-item>
		</u-cell-group>
		<u-select v-model="selectLocaleShow" :list="localeList" mode="single-column" :default-value="defaultLocale"
			@confirm="localeConfirm"></u-select>
	</view>
</template>

<script>
	import resources from '@/libs/resources.js'
	import {
		useLocale
	} from '@/locale/useLocale';

	export default {
		data() {
			return {
				// #ifdef APP-PLUS
				agreement: resources.userAgreement,
				policy: resources.privacyPolicy,
				// #endif
				titleStyle: {
					color: '#303133'
				},
				localeList: [{
						label: '简体中文',
						value: 'zh-Hans'
					},
					{
						label: '繁体中文',
						value: 'zh-Hant'
					},
					{
						label: 'English',
						value: 'en'
					}
				],
				selectLocaleShow: false,
				defaultLocale: []
			};
		},
		onLoad() {
			this.defaultLocale = [this.localeList.findIndex(o => o.value === uni.getLocale())];
		},
		methods: {
			modifyPsd(path) {
				if (!path) return
				uni.navigateTo({
					url: path
				})
			},
			// #ifdef APP-PLUS
			openPage(url) {
				plus.runtime.openURL(url);
			},
			// #endif
			localeConfirm(e) {
				if (e[0].index === this.defaultLocale[0]) return
				const systemInfo = uni.getSystemInfoSync();
				const isAndroid = systemInfo.platform.toLowerCase() === 'android';
				if (isAndroid) {
					uni.showModal({
						content: '应用此设置将重启App',
						success: (res) => {
							if (res.confirm) {
								this.handleChangeLocale(e[0])
							}
						}
					})
				} else {
					this.handleChangeLocale(e[0])
				}
			},
			handleChangeLocale(e) {
				this.defaultLocale = [e.index];
				const {
					changeLocale
				} = useLocale();
				changeLocale(e.value)
			},
		}
	}
</script>

<style lang="scss">
	page {
		background-color: #f0f2f6;
	}

	:deep(.u-cell) {
		height: 112rpx;
		padding: 20rpx 0;
	}

	.settings-v {
		background-color: #fff;
	}
</style>