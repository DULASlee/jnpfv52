<template>
	<web-view :src="fileUrl"></web-view>
</template>

<script>
	import {
		getDownloadUrl
	} from '@/api/common'
	export default {
		data() {
			return {
				fileUrl: ''
			}
		},
		onLoad(e) {
			this.fileUrl = e.data
		}
	}
</script>