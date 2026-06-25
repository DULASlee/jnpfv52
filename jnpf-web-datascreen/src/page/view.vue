<template>
  <div class="build views" :style="viewStyle">
    <container :id="id" :target="target" :option="option" ref="container"></container>
  </div>
</template>
<script>
import init from '@/mixins/'
import { getToken } from "@/utils/auth";
export default {
  props: {
    target: String,
    option: Object,
    id: [String, Number]
  },
  mixins: [init],
  data() {
    return {
      viewStyle: {}
    }
  },
  created() {
    if (!getToken()) this.$router.push('/login');
  },
}
</script>