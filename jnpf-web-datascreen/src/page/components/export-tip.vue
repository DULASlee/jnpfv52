<template>
  <el-dialog :title="`【${item.title}】打包部署`" v-model="visible" :close-on-click-modal="false"
    class="avue-dialog" width="50%">
    <div class="avue-tip" v-loading="loading" v-bind="$loadingParams">
      <div class="item">
        <div class="header">
          第一步：下载部署容器(必须)
        </div>
        <div class="content">
          <div style="width:100%">
            <el-button type="primary" icon="el-icon-suitcase"
              @click="handleContent">下载容器</el-button>
          </div>
          <p>下载部署容器。配置 Nginx，参考如下：</p>
          <div style="width:100%">
            <p v-highlight>
            <pre><code>
            location / {
              root /;
              index index.html;
              try_files $uri $uri/ /index.html;
            }
            </code> </pre>
            </p>
          </div>
        </div>
      </div>
      <div class="item">
        <div class="header">
          第二步：下载大屏配置文件
        </div>
        <div class="content">
          <div style="width:100%">
            <el-button type="primary" icon="el-icon-download"
              @click="handleExport">下载大屏配置文件</el-button>
          </div>
          <p>1.【本地文件】下载大屏配置文件。将大屏配置文件view.js 放入部署容器根目录。</p>
          <p>2.【云端加载】无需下载大屏配置文件，访问第一步下载的容器中index.html文件,url中带大屏参数/index.html?id={{item.id}}</p>
        </div>
      </div>

      <div class="item">
        <div class="header">
          iframe嵌入
        </div>
        <div class="content">
          <p>嵌入地址：<a :href="url" target="_blank">{{url}}</a></p>
        </div>
      </div>
    </div>
  </el-dialog>
</template>

<script>
import { getObj } from '@/api/visual'
export default {
  data() {
    return {
      url: '',
      loading: false,
      visible: false,
      item: {}
    }
  },
  methods: {
    handleOpen(item = {}) {
      this.item = item;
      this.visible = true
      this.url = location.origin + '/view.html?id=' + this.item.id
    },
    handleContent() {
      location.href = '/index.zip'
    },
    handleExport() {
      this.loading = true
      getObj(this.item.id).then(res => {
        const data = res.data.data;
        let mode = {
          detail: JSON.parse(data.config.detail),
          component: JSON.parse(data.config.component)
        }
        const blob = new Blob([`//将大屏配置文件view.js 放入部署容器根目录 \n const option =${JSON.stringify(mode, null, 4)}`], { type: 'text/plain;charset=utf-8' });
        saveAs(blob, "view.js");
        this.loading = false
        this.$message.success('大屏导出成功')
      }).catch(err => {
        console.log(err)
        this.$message.error('大屏导出失败')
        this.loading = false
      })
    }
  }
}
</script>
