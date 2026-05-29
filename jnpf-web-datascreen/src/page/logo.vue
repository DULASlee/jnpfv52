<template>
  <div class="logo">
    <img :src="`${publicPath}img/jnpf.png`" />
  </div>
</template>

<script>
export default {
  data() {
    return {
      publicPath: import.meta.env.VITE_APP_BASE,
    };
  },
}
</script>

<style>
.logo {
  padding: 25px 10px;
}
.logo img {
  width: 100%;
}
</style>