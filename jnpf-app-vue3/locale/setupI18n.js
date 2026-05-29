import {
	createI18n
} from 'vue-i18n'
import messages from './index'

export let i18n;

function createI18nOptions() {
	const locale = uni.getLocale();
	return {
		locale,
		messages,
		sync: true,
		silentTranslationWarn: true,
		missingWarn: true,
		silentFallbackWarn: true,
	};
}

export function setupI18n(app) {
	const options = createI18nOptions();
	i18n = createI18n(options);
	app.use(i18n);
}