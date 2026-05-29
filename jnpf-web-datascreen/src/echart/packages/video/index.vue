<template>
  <div :class="[b(),className]" :style="styleSizeName" @mouseenter="handleMouseEnter"
    @mouseleave="handleMouseLeave" @dblclick="handleDblClick" ref="main" @click="handleClick">
    <video :style="styleChartName" muted :width="width" :height="height" :src="mappingValue"
      v-bind="params" :poster="poster" style="object-fit: fill" :key="key">
    </video>
    <img :src="computedImgUrl(option.poster)" v-if="option.poster" alt="" :style="styleSizeName"
      :class="b('img')">
  </div>
</template>

<script>
import create from "../../create";
export default create({
  name: "video",
  data() {
    return {
      key: 0
    };
  },
  computed: {
    poster() {
      return this.option.poster ? '-' : ''
    },
    params() {
      let result = {}
      if (this.option.controls) result.controls = "controls"
      if (this.option.loop) result.loop = "loop"
      if (this.option.autoplay) result.autoplay = "autoplay"
      this.key = +new Date()
      return result
    }
  },
  props: {
    option: {
      type: Object,
      default: () => {
        return {};
      }
    }
  }
});
</script>


