<script setup lang="ts">
  /**
   * 材料上传组件（增量版）
   *
   * 关键设计：
   *   - 已上传成功的文件保留 serverId，重新提交时不重复上传
   *   - 仅上传 status === 'pending' 的新增文件
   *   - submitMaterials 调用时自动过滤
   */

  import { ref, computed } from 'vue';
  import { uploadMaterials } from '/@/api/studio/ai';

  interface UploadedFile {
    raw: File;
    fileName: string;
    status: 'pending' | 'uploaded' | 'error';
    serverId?: string;
    serverUrl?: string;
    errorMessage?: string;
  }

  const files = ref<UploadedFile[]>([]);

  // 只取待上传的文件
  const pendingFiles = computed(() => files.value.filter(f => f.status === 'pending').map(f => f.raw));

  // 已上传文件的 serverId 列表
  const uploadedServerIds = computed(() => files.value.filter(f => f.status === 'uploaded' && f.serverId).map(f => f.serverId!));

  function handleFilesSelected(newFiles: File[]) {
    for (const file of newFiles) {
      // 按文件名+大小去重
      const exists = files.value.some(f => f.fileName === file.name && f.raw.size === file.size);
      if (!exists) {
        files.value.push({
          raw: file,
          fileName: file.name,
          status: 'pending',
        });
      }
    }
  }

  function removeFile(index: number) {
    files.value.splice(index, 1);
  }

  /**
   * 预上传：在 submitMaterials 之前调用
   * 将 pending 文件上传到服务端，标记为 uploaded
   * 返回新上传的 serverId 列表
   */
  async function preUpload(pipelineId: string): Promise<string[]> {
    const newServerIds: string[] = [];

    for (const file of files.value) {
      if (file.status !== 'pending') continue;

      try {
        file.status = 'uploaded'; // 乐观标记
        const result: any = await uploadMaterials(pipelineId, { files: [file.raw] });
        file.serverId = result?.serverId || result?.data?.serverId;
        if (file.serverId) {
          newServerIds.push(file.serverId);
        }
      } catch (err: any) {
        file.status = 'error';
        file.errorMessage = err.message || '上传失败';
      }
    }

    return newServerIds;
  }

  defineExpose({
    files,
    pendingFiles,
    uploadedServerIds,
    handleFilesSelected,
    preUpload,
    removeFile,
  });
</script>
