<template>
  <div class="studio-sidebar">
    <div class="top-tabs">
      <div v-for="menu in topMenus" :key="menu.Id" class="tab-item" :class="{ active: activeTopMenu === menu.Id }" @click="selectTopMenu(menu.Id)">
        <span class="tab-name">{{ menu.Name }}</span>
        <span v-if="menu.BadgeCount > 0" class="badge-dot">
          {{ menu.BadgeCount > 99 ? '99+' : menu.BadgeCount }}
        </span>
      </div>
    </div>
    <div class="sub-menus">
      <template v-for="group in currentSubMenus" :key="group.Id">
        <div v-if="group.Children?.length" class="menu-group">
          <div class="group-title" :class="{ expanded: expandedGroups.includes(group.Id) }" @click="toggleGroup(group.Id)">
            <span>{{ group.Name }}</span>
            <span class="arrow">›</span>
          </div>
          <transition name="slide">
            <div v-show="expandedGroups.includes(group.Id)" class="group-items">
              <router-link v-for="item in group.Children" :key="item.Id" :to="item.Url!" class="menu-item" :class="{ active: route.path === item.Url }">
                <span>{{ item.Name }}</span>
                <span v-if="item.BadgeCount > 0" class="badge-dot small">
                  {{ item.BadgeCount > 99 ? '99+' : item.BadgeCount }}
                </span>
              </router-link>
            </div>
          </transition>
        </div>
        <router-link v-else-if="group.Url" :key="'link-' + group.Id" :to="group.Url" class="menu-item standalone" :class="{ active: route.path === group.Url }">
          <span>{{ group.Name }}</span>
          <span v-if="group.BadgeCount > 0" class="badge-dot">
            {{ group.BadgeCount > 99 ? '99+' : group.BadgeCount }}
          </span>
        </router-link>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
  import { ref, computed, watch } from 'vue';
  import { useRoute } from 'vue-router';
  import { useStudioMenuStore } from '../store/studio-menu';

  const route = useRoute();
  const menuStore = useStudioMenuStore();
  const activeTopMenu = ref<number>(0);
  const expandedGroups = ref<number[]>([]);
  const topMenus = computed(() => menuStore.topMenus);
  const currentSubMenus = computed(() => {
    if (!activeTopMenu.value) return [];
    return menuStore.getChildren(activeTopMenu.value);
  });

  watch(
    () => route.path,
    path => {
      for (const top of menuStore.topMenus) {
        const children = menuStore.getChildren(top.Id);
        for (const group of children) {
          if (group.Children?.some((c: any) => c.Url === path)) {
            activeTopMenu.value = top.Id;
            if (!expandedGroups.value.includes(group.Id)) expandedGroups.value.push(group.Id);
            return;
          }
          if (group.Url === path) {
            activeTopMenu.value = top.Id;
            return;
          }
        }
      }
    },
    { immediate: true },
  );

  if (!activeTopMenu.value && topMenus.value.length) activeTopMenu.value = topMenus.value[0].Id;

  function selectTopMenu(id: number) {
    activeTopMenu.value = id;
  }
  function toggleGroup(id: number) {
    const idx = expandedGroups.value.indexOf(id);
    if (idx >= 0) expandedGroups.value.splice(idx, 1);
    else expandedGroups.value.push(id);
  }
</script>

<style scoped lang="less">
  .studio-sidebar {
    width: 240px;
    height: 100vh;
    display: flex;
    flex-direction: column;
    background: #fff;
    border-right: 1px solid #f0f0f0;
  }
  .top-tabs {
    display: flex;
    flex-wrap: wrap;
    border-bottom: 1px solid #f0f0f0;
    padding: 8px 4px;
    gap: 4px;
  }
  .top-tabs .tab-item {
    position: relative;
    display: flex;
    align-items: center;
    gap: 6px;
    padding: 8px 12px;
    border-radius: 6px;
    cursor: pointer;
    font-size: 13px;
    color: #333;
    transition: all 0.2s;
  }
  .top-tabs .tab-item:hover {
    background: #f5f5f5;
  }
  .top-tabs .tab-item.active {
    background: #e6f7ff;
    color: #1890ff;
    font-weight: 600;
  }
  .top-tabs .tab-item .tab-name {
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    max-width: 80px;
  }
  .sub-menus {
    flex: 1;
    overflow-y: auto;
    padding: 8px 0;
  }
  .sub-menus::-webkit-scrollbar {
    width: 4px;
  }
  .sub-menus::-webkit-scrollbar-thumb {
    background: #d9d9d9;
    border-radius: 2px;
  }
  .menu-group .group-title {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 10px 16px;
    cursor: pointer;
    font-size: 13px;
    color: #333;
  }
  .menu-group .group-title:hover {
    background: #fafafa;
  }
  .menu-group .group-title .arrow {
    margin-left: auto;
    transition: transform 0.2s;
  }
  .menu-group .group-title.expanded .arrow {
    transform: rotate(90deg);
  }
  .menu-group .group-items .menu-item {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 9px 16px 9px 40px;
    font-size: 13px;
    color: #333;
    text-decoration: none;
  }
  .menu-group .group-items .menu-item:hover {
    background: #fafafa;
    color: #1890ff;
  }
  .menu-group .group-items .menu-item.active {
    background: #e6f7ff;
    color: #1890ff;
    font-weight: 500;
    border-right: 3px solid #1890ff;
  }
  .menu-item.standalone {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 10px 16px;
    font-size: 13px;
    color: #333;
    text-decoration: none;
  }
  .menu-item.standalone:hover {
    background: #fafafa;
    color: #1890ff;
  }
  .menu-item.standalone.active {
    background: #e6f7ff;
    color: #1890ff;
    font-weight: 500;
    border-right: 3px solid #1890ff;
  }
  .badge-dot {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    min-width: 18px;
    height: 18px;
    padding: 0 5px;
    border-radius: 9px;
    background: #ff4d4f;
    color: #fff;
    font-size: 11px;
    font-weight: 600;
  }
  .badge-dot.small {
    min-width: 16px;
    height: 16px;
    font-size: 10px;
  }
  .slide-enter-active,
  .slide-leave-active {
    transition: all 0.2s ease;
    overflow: hidden;
  }
  .slide-enter-from,
  .slide-leave-to {
    opacity: 0;
    max-height: 0;
  }
</style>
