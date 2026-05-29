<template>
	<u-divider half-width="200" height="80">{{content}}</u-divider>
</template>
<script>
	export default {
		name: 'jnpf-divider',
		props: {
			content: {
				default: ''
			}
		},
	}
</script>