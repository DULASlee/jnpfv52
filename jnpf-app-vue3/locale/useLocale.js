import {
	getLangJson
} from '@/api/common';
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
		const res = await getLangJson(getBackLocale(locale));
		if (!res || !res.data) return setLocale(locale, defaultMessage);
		const message = JSON.parse(res.data);
		setLocale(locale, {
			...defaultMessage,
			...message
		})
		return locale;
	}
	async function initLocale() {
		const locale = uni.getLocale()
		changeLocale(locale)
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