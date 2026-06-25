<template>
  <div class="totp-verify-page">
    <div class="verify-card">
      <div class="card-header">
        <span class="lock-icon">🔐</span>
        <h2>创始人身份验证</h2>
        <p>请输入 TOTP 验证码以访问控制台</p>
      </div>

      <div class="card-body">
        <div class="code-input-group">
          <input
            v-for="idx in 6"
            :key="idx"
            :ref="(el: any) => { if (el) inputRefs[idx - 1] = el }"
            type="text"
            maxlength="1"
            class="code-input"
            :class="{ error: hasError }"
            :value="codeDigits[idx - 1] || ''"
            @input="(e: Event) => onInput(idx - 1, e)"
            @keydown="(e: KeyboardEvent) => onKeydown(idx - 1, e)"
            @paste="onPaste" />
        </div>

        <p v-if="errorMessage" class="error-msg">{{ errorMessage }}</p>

        <button class="verify-btn" :disabled="codeDigits.join('').length < 6 || verifying" @click="handleVerify">
          {{ verifying ? '验证中...' : '验证' }}
        </button>
      </div>

      <div class="card-footer">
        <p>验证码来自您的身份验证器应用</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { ref, nextTick } from 'vue';
  import { useRouter, useRoute } from 'vue-router';
  import { verifyTotp } from '../api/menu';

  const router = useRouter();
  const route = useRoute();

  const inputRefs = ref<HTMLInputElement[]>([]);
  const codeDigits = ref<string[]>([]);
  const hasError = ref(false);
  const errorMessage = ref('');
  const verifying = ref(false);

  function onInput(idx: number, event: Event) {
    const value = (event.target as HTMLInputElement).value;
    if (!/^\d$/.test(value)) {
      codeDigits.value[idx] = '';
      return;
    }
    codeDigits.value[idx] = value;
    hasError.value = false;
    errorMessage.value = '';

    if (idx < 5) {
      nextTick(() => inputRefs.value[idx + 1]?.focus());
    }
    if (codeDigits.value.join('').length === 6) {
      handleVerify();
    }
  }

  function onKeydown(idx: number, event: KeyboardEvent) {
    if (event.key === 'Backspace' && !codeDigits.value[idx] && idx > 0) {
      codeDigits.value[idx - 1] = '';
      nextTick(() => inputRefs.value[idx - 1]?.focus());
    }
  }

  function onPaste(event: ClipboardEvent) {
    const text = event.clipboardData?.getData('text')?.trim() ?? '';
    if (/^\d{6}$/.test(text)) {
      event.preventDefault();
      for (let i = 0; i < 6; i++) {
        codeDigits.value[i] = text[i];
      }
      handleVerify();
    }
  }

  async function handleVerify() {
    const code = codeDigits.value.join('');
    if (code.length !== 6) return;

    verifying.value = true;
    errorMessage.value = '';

    try {
      const res = await verifyTotp({ code });
      if (res.success) {
        sessionStorage.setItem('founder_totp_verified', 'true');
        const redirect = (route.query.redirect as string) || '/studio/foundry/dashboard';
        router.push(redirect);
      } else {
        hasError.value = true;
        errorMessage.value = res.message || '验证码无效';
        codeDigits.value = [];
        nextTick(() => inputRefs.value[0]?.focus());
      }
    } catch {
      hasError.value = true;
      errorMessage.value = '验证失败';
    } finally {
      verifying.value = false;
    }
  }
</script>

<style scoped lang="less">
  .totp-verify-page {
    display: flex;
    align-items: center;
    justify-content: center;
    min-height: 100vh;
    background: #f5f5f5;
  }

  .verify-card {
    width: 400px;
    background: #fff;
    border-radius: 12px;
    box-shadow: 0 4px 24px rgba(0, 0, 0, 0.08);
    overflow: hidden;

    .card-header {
      text-align: center;
      padding: 32px 24px 16px;

      .lock-icon {
        font-size: 40px;
        display: block;
        margin-bottom: 12px;
      }

      h2 {
        font-size: 20px;
        font-weight: 600;
        color: #1a1a1a;
        margin: 0 0 8px;
      }

      p {
        font-size: 14px;
        color: #888;
        margin: 0;
      }
    }

    .card-body {
      padding: 24px;

      .code-input-group {
        display: flex;
        gap: 8px;
        justify-content: center;
        margin-bottom: 16px;

        .code-input {
          width: 48px;
          height: 56px;
          text-align: center;
          font-size: 24px;
          font-weight: 600;
          border: 2px solid #e0e0e0;
          border-radius: 8px;
          outline: none;
          transition: border-color 0.2s;

          &:focus {
            border-color: #409eff;
          }

          &.error {
            border-color: #f56c6c;
            animation: shake 0.3s;
          }
        }
      }

      .error-msg {
        text-align: center;
        color: #f56c6c;
        font-size: 13px;
        margin: 0 0 16px;
      }

      .verify-btn {
        width: 100%;
        height: 44px;
        background: #409eff;
        color: #fff;
        border: none;
        border-radius: 8px;
        font-size: 15px;
        font-weight: 500;
        cursor: pointer;

        &:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }
      }
    }

    .card-footer {
      padding: 16px 24px 24px;
      text-align: center;
      p {
        font-size: 12px;
        color: #aaa;
        margin: 0;
      }
    }
  }

  @keyframes shake {
    0%,
    100% {
      transform: translateX(0);
    }
    25% {
      transform: translateX(-4px);
    }
    75% {
      transform: translateX(4px);
    }
  }
</style>
