/**
 * Studio 菜单状态管理 (Sprint 1)
 * API 返回 PascalCase，前端直接用 PascalCase
 */
import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { getUserMenus } from '../api/menu';

export interface StudioMenuItem {
  Id: number;
  ParentId: number;
  Name: string;
  Icon?: string;
  Url?: string;
  Sort: number;
  Comment?: string;
  DataScope: string;
  ExpandPhase: string;
  BadgeCount: number;
  Children: StudioMenuItem[];
}

export const useStudioMenuStore = defineStore('studio-menu', () => {
  const menus = ref<StudioMenuItem[]>([]);
  const loading = ref(false);
  const loaded = ref(false);

  const topMenus = computed(() => menus.value.filter(m => m.ParentId === 0).sort((a, b) => a.Sort - b.Sort));

  function getChildren(parentId: number): StudioMenuItem[] {
    const parent = menus.value.find(m => m.Id === parentId);
    return parent?.Children ?? [];
  }

  const totalBadgeCount = computed(() => {
    let count = 0;
    const walk = (items: StudioMenuItem[]) => {
      for (const item of items) {
        count += item.BadgeCount;
        if (item.Children?.length) walk(item.Children);
      }
    };
    walk(menus.value);
    return count;
  });

  async function loadMenus() {
    if (loaded.value) return;
    loading.value = true;
    try {
      const data = await getUserMenus();
      menus.value = (data as any)?.data ?? data ?? [];
      loaded.value = true;
    } finally {
      loading.value = false;
    }
  }

  async function refreshMenus() {
    loaded.value = false;
    await loadMenus();
  }

  loadMenus();

  return { menus, loading, loaded, topMenus, totalBadgeCount, getChildren, loadMenus, refreshMenus };
});
