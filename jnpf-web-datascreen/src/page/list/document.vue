<template>
  <div class="document">
    <div class="item">
      <p class="avue-tip-title">功能</p>
      <el-button type="primary" icon="el-icon-menu" @click="$refs.swiper.box=true">屏幕轮播</el-button>
    </div>
    <swiper ref="swiper"></swiper>
    <control ref="control"></control>
  </div>
</template>

<script>
import control from './control.vue'
import swiper from './swiper.vue'
export default {
  components: {
    control,
    swiper
  },
};
</script>

<style lang="scss">
.document {
  padding: 30px;
  .item {
    margin-bottom: 20px;
  }
  .avue-tip-title {
    font-size: 18px;
    margin-bottom: 5px;
  }
}
</style>
