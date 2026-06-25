<template>
  <div class="login-redirect">
    <p>未登录或 Token 已过期，正在跳转到登录页...</p>
  </div>
</template>
<script>
export default {
  name: 'LoginRedirect',
  created() {
    const loginUrl = import.meta.env.VITE_LOGIN_REDIRECT_URL;
    if (loginUrl) {
      const currentUrl = encodeURIComponent(window.location.href);
      window.location.href = loginUrl + '?redirect=' + currentUrl;
    }
  }
}
</script>
