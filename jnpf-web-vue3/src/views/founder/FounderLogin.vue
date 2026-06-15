<template>
  <div class="founder-login">
    <div class="totp-card">
      <div class="card-header">
        <SafetyCertificateOutlined class="header-icon" />
        <h2>创始人二次认证</h2>
        <p>请输入 6 位 TOTP 验证码以访问创始人功能</p>
      </div>

      <a-form :model="form" layout="vertical" @finish="handleVerify">
        <a-form-item label="管理员邮箱" name="email" :rules="[{ required: true, type: 'email', message: '请输入有效邮箱' }]">
          <a-input v-model:value="form.email" placeholder="founder@example.com" size="large" :disabled="verifying" />
        </a-form-item>

        <a-form-item label="TOTP 验证码" name="code" :rules="[{ required: true, pattern: /^\d{6}$/, message: '请输入 6 位数字' }]">
          <a-input v-model:value="form.code" placeholder="000000" size="large" maxlength="6" :disabled="verifying" @press-enter="handleVerify" />
        </a-form-item>

        <a-button type="primary" html-type="submit" size="large" block :loading="verifying" :disabled="!isFormValid"> 验证并进入 </a-button>
      </a-form>

      <div v-if="errorMsg" class="error-msg">
        <a-alert type="error" :message="errorMsg" show-icon closable @close="errorMsg = ''" />
      </div>

      <div class="card-footer">
        <a-button type="link" @click="handleBack">返回首页</a-button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { ref, reactive, computed } from 'vue';
  import { useRouter, useRoute } from 'vue-router';
  import { SafetyCertificateOutlined } from '@ant-design/icons-vue';
  import { verifyTotp, setFounderToken } from '/@/api/founder/index';

  defineOptions({ name: 'FounderLogin' });

  const router = useRouter();
  const route = useRoute();

  const form = reactive({ email: '', code: '' });
  const verifying = ref(false);
  const errorMsg = ref('');

  const isFormValid = computed(() => form.email.includes('@') && /^\d{6}$/.test(form.code));

  const handleVerify = async () => {
    if (!isFormValid.value) return;
    verifying.value = true;
    errorMsg.value = '';

    try {
      const res = await verifyTotp(form.email, Number(form.code));
      const token = res.data?.token || res.data?.data?.token;
      if (!token) {
        errorMsg.value = '验证失败：未收到有效 Token';
        return;
      }

      // 写入 founder token 到 localStorage（API 层使用）
      setFounderToken(token);

      // 写入 TOTP session（路由守卫使用）
      const session = {
        token,
        expiresAt: Date.now() + 30 * 60 * 1000,
      };
      localStorage.setItem('founder_totp_session', JSON.stringify(session));

      // 跳转回原目标路径
      const redirect = (route.query.redirect as string) || '/studio/founder/console';
      router.push(redirect);
    } catch {
      errorMsg.value = '验证失败，请检查邮箱和验证码是否正确';
    } finally {
      verifying.value = false;
    }
  };

  const handleBack = () => {
    router.push('/');
  };
</script>

<style lang="less" scoped>
  .founder-login {
    display: flex;
    justify-content: center;
    align-items: center;
    min-height: 100vh;
    background: #f0f2f5;

    .totp-card {
      width: 400px;
      padding: 40px;
      background: #fff;
      border-radius: 8px;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);

      .card-header {
        text-align: center;
        margin-bottom: 32px;

        .header-icon {
          font-size: 48px;
          color: #cf1322;
          margin-bottom: 16px;
        }

        h2 {
          font-size: 20px;
          font-weight: 600;
          margin-bottom: 8px;
        }

        p {
          color: #8c8c8c;
          font-size: 14px;
        }
      }

      .error-msg {
        margin-top: 16px;
      }

      .card-footer {
        text-align: center;
        margin-top: 16px;
      }
    }
  }
</style>
