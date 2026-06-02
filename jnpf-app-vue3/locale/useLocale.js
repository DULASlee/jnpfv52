import {
	i18n
} from './setupI18n';
import messages from './index'


export function useLocale() {
	function getBackLocale(locale) {
		const backLocale = locale || uni.getLocale()
		if (backLocale === 'zh-Hans') return 'zh-CN'
		if (backLocale === 'zh-Hant') return 'zh-TW'
		if (backLocale === 'en') return 'en-US'
		return backLocale
	}
	async function changeLocale(locale) {
		const defaultMessage = messages[locale] || {}
		// v5.2 后端无 /api/system/BaseLang/LangJson，演示联调仅用本地语言包
		setLocale(locale, defaultMessage);
		return locale;
	}
	async function initLocale() {
		const locale = uni.getLocale()
		await changeLocale(locale)
	}

	function setLocale(locale, message) {
		const globalI18n = i18n.global;
		globalI18n.setLocaleMessage(locale, message);
		globalI18n.locale = locale
		uni.setLocale(locale)
	}

	return {
		changeLocale,
		initLocale,
		setLocale,
		getBackLocale,
	};
}