/**
 * 3D POI 标注 — CSS2D 标签
 *
 * 在 3D 场景中以 DOM 标签形式标注兴趣点，支持图标、状态颜色、
 * 弹出信息窗口和点击事件。
 *
 * @jnpf-generated dashboard-3d-poi v2.0.0
 */

import * as THREE from 'three';
import { CSS2DObject } from 'three/examples/jsm/renderers/CSS2DRenderer.js';

// ============================================================
// Types
// ============================================================

/** POI 图标类型 */
export type POIIconType = 'device' | 'camera' | 'person' | 'alarm' | 'custom';

/** POI 状态 */
export type POIStatus = 'normal' | 'warning' | 'alarm';

export interface POIConfig {
  /** 唯一标识 */
  id: string;
  /** 显示名称 */
  name: string;
  /** 3D 世界坐标 [x, y, z] */
  position: [number, number, number];
  /** 图标类型 */
  icon: POIIconType;
  /** 自定义图标 URL（icon=custom 时使用） */
  iconUrl?: string;
  /** 状态，控制颜色 */
  status?: POIStatus;
  /** 弹出信息窗口配置 */
  popup?: {
    /** 弹窗标题 */
    title?: string;
    /** 弹窗内容 HTML */
    content?: string;
  };
  /** 附加数据，用于点击回调 */
  data?: Record<string, unknown>;
}

// ============================================================
// Icon mapping
// ============================================================

/** 图标类型 → Unicode 符号 */
const ICON_SYMBOLS: Record<POIIconType, string> = {
  device: '📡',
  camera: '📷',
  person: '👤',
  alarm: '🚨',
  custom: '📍',
};

/** 状态 → CSS 颜色 */
const STATUS_COLORS: Record<POIStatus, string> = {
  normal: '#00d4ff',
  warning: '#ffa940',
  alarm: '#ff4560',
};

// ============================================================
// DOM element creation
// ============================================================

function createPOIElement(config: POIConfig): HTMLElement {
  const status = config.status || 'normal';
  const color = STATUS_COLORS[status];

  const container = document.createElement('div');
  container.className = `poi-marker poi-${status}`;
  container.setAttribute('data-poi-id', config.id);
  container.style.cssText = `
    display: flex;
    flex-direction: column;
    align-items: center;
    cursor: pointer;
    pointer-events: auto;
    user-select: none;
  `;

  // Icon area
  const iconWrap = document.createElement('div');
  iconWrap.className = 'poi-icon';
  iconWrap.style.cssText = `
    width: 32px;
    height: 32px;
    border-radius: 50%;
    background: ${color}22;
    border: 2px solid ${color};
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 16px;
    color: ${color};
    transition: transform 0.2s ease, box-shadow 0.2s ease;
    box-shadow: 0 0 8px ${color}44;
  `;

  if (config.icon === 'custom' && config.iconUrl) {
    const img = document.createElement('img');
    img.src = config.iconUrl;
    img.style.cssText = 'width: 20px; height: 20px; object-fit: contain;';
    iconWrap.appendChild(img);
  } else {
    iconWrap.textContent = ICON_SYMBOLS[config.icon] || ICON_SYMBOLS.device;
  }

  container.appendChild(iconWrap);

  // Label
  const label = document.createElement('div');
  label.className = 'poi-label';
  label.textContent = config.name;
  label.style.cssText = `
    margin-top: 4px;
    padding: 2px 8px;
    background: rgba(0,0,0,0.75);
    color: #fff;
    font-size: 12px;
    white-space: nowrap;
    border-radius: 4px;
    border: 1px solid ${color}66;
  `;
  container.appendChild(label);

  // Click → CustomEvent
  container.addEventListener('click', e => {
    e.stopPropagation();
    document.dispatchEvent(
      new CustomEvent('poi-click', {
        detail: config,
      }),
    );
  });

  // Hover effect
  container.addEventListener('mouseenter', () => {
    iconWrap.style.transform = 'scale(1.2)';
    iconWrap.style.boxShadow = `0 0 16px ${color}`;
  });
  container.addEventListener('mouseleave', () => {
    iconWrap.style.transform = 'scale(1)';
    iconWrap.style.boxShadow = `0 0 8px ${color}44`;
  });

  return container;
}

// ============================================================
// Public API
// ============================================================

/**
 * 创建单个 POI 标注，返回 CSS2DObject（可直接添加到 scene）。
 *
 * @example
 * const poi = createPOI({ id: 'cam1', name: '摄像头1', position: [5, 2, 0], icon: 'camera', status: 'normal' });
 * scene.add(poi);
 */
export function createPOI(config: POIConfig): CSS2DObject {
  const element = createPOIElement(config);
  const label = new CSS2DObject(element);
  label.name = `poi-${config.id}`;
  label.position.set(config.position[0], config.position[1], config.position[2]);
  label.userData.poiConfig = config;
  return label;
}

/**
 * 批量创建 POI 标注，返回 THREE.Group。
 *
 * @example
 * const group = createPOIGroup([{ id: 'a1', name: 'A', position: [1,0,1], icon: 'device' }]);
 * scene.add(group);
 */
export function createPOIGroup(configs: POIConfig[]): THREE.Group {
  const group = new THREE.Group();
  group.name = 'poi-group';

  for (const config of configs) {
    const poi = createPOI(config);
    group.add(poi);
  }

  return group;
}

/**
 * 更新 POI 状态，自动切换颜色。
 */
export function updatePOIStatus(poi: CSS2DObject, status: POIStatus): void {
  const config = poi.userData.poiConfig as POIConfig | undefined;
  if (!config) return;

  config.status = status;
  poi.userData.poiConfig = config;

  const element = poi.element as HTMLElement;
  const color = STATUS_COLORS[status];
  const iconWrap = element.querySelector('.poi-icon') as HTMLElement;
  const label = element.querySelector('.poi-label') as HTMLElement;

  if (iconWrap) {
    iconWrap.style.background = `${color}22`;
    iconWrap.style.borderColor = color;
    iconWrap.style.color = color;
  }
  if (label) {
    label.style.borderColor = `${color}66`;
  }

  // Update class
  element.className = `poi-marker poi-${status}`;
}

/**
 * 根据 ID 在 group 中查找 POI。
 */
export function findPOIInGroup(group: THREE.Group, id: string): CSS2DObject | undefined {
  for (const child of group.children) {
    if (child.name === `poi-${id}`) {
      return child as CSS2DObject;
    }
  }
  return undefined;
}
