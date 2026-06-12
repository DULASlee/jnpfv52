<template>
  <div class="swiper">
    <el-drawer title="屏幕轮播" append-to-body class="avue-dialog" v-model="box" direction="rtl">
      <div class="swiper__content">
        <el-input placeholder="请输入大屏ID多个大屏用','间隔" v-model="value1">
          <template #append>
            <span @click="goMenu2">预览轮播大屏</span>
          </template>
        </el-input>
        <br /> <br />
        <el-input placeholder="请输入大屏ID" v-model="value">
          <template #append>
            <span @click="goMenu1">预览单html大屏</span>
          </template>
        </el-input>
      </div>
    </el-drawer>
  </div>

</template>
<script>
import { getObj } from '@/api/visual'
import { getToken } from '@/utils/auth'
import { uuid } from '@/utils/utils'
export default {
  data() {
    return {
      box: false,
      value: '',
      value1: ''
    }
  },
  methods: {
    goMenu1() {
      if (!this.value) return this.$message.error("请输入大屏ID");
      window.open(
        "/DataV/view.html?id=" +
        this.value +
        "&token=" +
        getToken() +
        "&isDev=" +
        (process.env.NODE_ENV === "development" ? "1" : "")
      );
    },
    goMenu2() {
      if (!this.value1) {
        this.$message.error('请输入大屏ID多个大屏用", "间隔');
        return;
      }
      window.open(
        "/DataV/swiper.html?id=" +
        this.value1 +
        "&token=" +
        getToken() +
        "&isDev=" +
        (process.env.NODE_ENV === "development" ? "1" : "")
      );
    },
  }
}
</script>
<style lang="scss" scoped>
.swiper {
  &__content {
    padding: 20px;
  }
}
</style>