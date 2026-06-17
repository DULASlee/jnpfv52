<template>
  <div class="attachment-upload">
    <div class="drop-zone" :class="{ dragging }" @dragover.prevent="dragging = true" @dragleave.prevent="dragging = false" @drop.prevent="handleDrop">
      <span class="upload-icon">📎</span>
      <span>拖拽文件到此处，或 <a @click="triggerInput">点击上传</a></span>
      <span class="hint">支持 PDF / Word / 图片 / 语音文件</span>
      <input ref="fileInput" type="file" multiple :accept="accept" style="display: none" @change="handleFiles" />
    </div>
    <div v-if="files.length" class="file-list">
      <div v-for="(f, i) in files" :key="i" class="file-item">
        <span class="file-icon">{{ getFileIcon(f.name) }}</span>
        <span class="file-name">{{ f.name }}</span>
        <span class="file-size">{{ formatSize(f.size) }}</span>
        <span class="remove" @click="removeFile(i)">✕</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { ref } from 'vue';

  const accept = '.pdf,.doc,.docx,.txt,.jpg,.jpeg,.png,.gif,.bmp,.mp3,.wav,.mp4';
  const files = ref<File[]>([]);
  const dragging = ref(false);
  const fileInput = ref<HTMLInputElement | null>(null);

  const emit = defineEmits<{ 'update:files': [files: File[]] }>();

  function triggerInput() {
    fileInput.value?.click();
  }

  function handleDrop(e: DragEvent) {
    dragging.value = false;
    if (e.dataTransfer?.files) addFiles(e.dataTransfer.files);
  }

  function handleFiles(e: Event) {
    const input = e.target as HTMLInputElement;
    if (input.files) addFiles(input.files);
  }

  function addFiles(fileList: FileList) {
    for (let i = 0; i < fileList.length; i++) {
      if (!files.value.find(f => f.name === fileList[i].name)) {
        files.value.push(fileList[i]);
      }
    }
    emit('update:files', files.value);
  }

  function removeFile(index: number) {
    files.value.splice(index, 1);
    emit('update:files', files.value);
  }

  function getFileIcon(name: string): string {
    const ext = name.split('.').pop()?.toLowerCase() || '';
    if (['pdf'].includes(ext)) return '📕';
    if (['doc', 'docx'].includes(ext)) return '📘';
    if (['jpg', 'jpeg', 'png', 'gif', 'bmp'].includes(ext)) return '🖼️';
    if (['mp3', 'wav'].includes(ext)) return '🎵';
    if (['mp4'].includes(ext)) return '🎬';
    return '📄';
  }

  function formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes}B`;
    if (bytes < 1048576) return `${(bytes / 1024).toFixed(1)}KB`;
    return `${(bytes / 1048576).toFixed(1)}MB`;
  }
</script>

<style scoped lang="less">
  .attachment-upload {
    .drop-zone {
      border: 2px dashed #d9d9d9;
      border-radius: 8px;
      padding: 24px;
      text-align: center;
      color: #888;
      font-size: 13px;
      transition: all 0.2s;
      cursor: pointer;

      &.dragging,
      &:hover {
        border-color: #1890ff;
        background: #e6f7ff;
      }

      .upload-icon {
        font-size: 24px;
        display: block;
        margin-bottom: 8px;
      }
      a {
        color: #1890ff;
        cursor: pointer;
      }
      .hint {
        display: block;
        font-size: 11px;
        color: #bbb;
        margin-top: 4px;
      }
    }

    .file-list {
      margin-top: 8px;
      display: flex;
      flex-wrap: wrap;
      gap: 6px;

      .file-item {
        display: flex;
        align-items: center;
        gap: 4px;
        background: #f5f5f5;
        padding: 4px 8px;
        border-radius: 4px;
        font-size: 12px;

        .file-size {
          color: #888;
        }
        .remove {
          cursor: pointer;
          color: #ff4d4f;
        }
      }
    }
  }
</style>
