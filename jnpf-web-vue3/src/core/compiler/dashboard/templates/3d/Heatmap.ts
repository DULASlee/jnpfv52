/**
 * 3D 热力图 — 柱状 / 平面双模式
 *
 * 柱状模式：CylinderGeometry 高度映射数据值。
 * 平面模式：CircleGeometry 半透明水平圆盘。
 * 颜色从冷色渐变到暖色，使用 lerp 插值。
 *
 * @jnpf-generated dashboard-3d-heatmap v2.0.0
 */

import * as THREE from 'three';

// ============================================================
// Types
// ============================================================

export interface HeatmapPoint {
  /** 3D 世界坐标 [x, y, z] */
  position: [number, number, number];
  /** 数据值 0~1 */
  value: number;
  /** 可选标签 */
  label?: string;
}

export interface HeatmapConfig {
  /** 唯一标识 */
  id: string;
  /** 数据点数组 */
  points: HeatmapPoint[];
  /** 最大值对应高度（柱状模式），默认 10 */
  maxHeight?: number;
  /** 最小值对应高度，默认 0.1（避免零高度不可见） */
  minHeight?: number;
  /** 冷色（低值），默认 #00d4ff */
  coldColor?: string;
  /** 暖色（高值），默认 #ff4560 */
  hotColor?: string;
  /** true=柱状，false=平面，默认 true */
  barMode?: boolean;
  /** 柱状模式柱子半径，默认 0.3 */
  barRadius?: number;
  /** 平面模式圆盘半径，默认 0.5 */
  planeRadius?: number;
  /** 透明度 0~1，默认 0.8 */
  opacity?: number;
}

// ============================================================
// Color interpolation
// ============================================================

function lerpColor(cold: THREE.Color, hot: THREE.Color, t: number): THREE.Color {
  return cold.clone().lerp(hot, Math.max(0, Math.min(1, t)));
}

// ============================================================
// Bar mode
// ============================================================

function createBars(config: HeatmapConfig): THREE.Group {
  const { points, maxHeight = 10, minHeight = 0.1, coldColor = '#00d4ff', hotColor = '#ff4560', barRadius = 0.3, opacity = 0.8 } = config;

  const cold = new THREE.Color(coldColor);
  const hot = new THREE.Color(hotColor);
  const group = new THREE.Group();
  group.name = `heatmap-${config.id}-bars`;

  for (let i = 0; i < points.length; i++) {
    const pt = points[i];
    const t = Math.max(0, Math.min(1, pt.value));
    const h = minHeight + (maxHeight - minHeight) * t;
    const color = lerpColor(cold, hot, t);

    const geom = new THREE.CylinderGeometry(barRadius, barRadius, h, 16);
    const mat = new THREE.MeshStandardMaterial({
      color,
      transparent: true,
      opacity,
      roughness: 0.4,
      metalness: 0.1,
    });

    const bar = new THREE.Mesh(geom, mat);
    bar.position.set(pt.position[0], pt.position[1] + h / 2, pt.position[2]);
    bar.name = `heatmap-bar-${i}`;
    bar.userData.heatmapValue = pt.value;
    bar.userData.label = pt.label;

    // Enable shadow
    bar.castShadow = true;
    bar.receiveShadow = true;

    group.add(bar);
  }

  return group;
}

// ============================================================
// Plane mode
// ============================================================

function createPlanes(config: HeatmapConfig): THREE.Group {
  const { points, coldColor = '#00d4ff', hotColor = '#ff4560', planeRadius = 0.5, opacity = 0.6 } = config;

  const cold = new THREE.Color(coldColor);
  const hot = new THREE.Color(hotColor);
  const group = new THREE.Group();
  group.name = `heatmap-${config.id}-planes`;

  for (let i = 0; i < points.length; i++) {
    const pt = points[i];
    const t = Math.max(0, Math.min(1, pt.value));
    const color = lerpColor(cold, hot, t);

    const geom = new THREE.CircleGeometry(planeRadius, 32);
    const mat = new THREE.MeshBasicMaterial({
      color,
      transparent: true,
      opacity: opacity * (0.3 + t * 0.7), // Higher value = more opaque
      side: THREE.DoubleSide,
      depthWrite: false,
    });

    const plane = new THREE.Mesh(geom, mat);
    plane.position.set(pt.position[0], pt.position[1] + 0.05, pt.position[2]);
    plane.rotation.x = -Math.PI / 2; // Lay flat on XZ plane
    plane.name = `heatmap-plane-${i}`;
    plane.userData.heatmapValue = pt.value;
    plane.userData.label = pt.label;

    group.add(plane);
  }

  return group;
}

// ============================================================
// Public API
// ============================================================

/**
 * 创建 3D 热力图。
 *
 * @example
 * // Bar mode
 * const hm = createHeatmap({
 *   id: 'temp', points: [{ position: [0,0,0], value: 0.8 }, { position: [2,0,0], value: 0.3 }],
 *   barMode: true,
 * });
 * scene.add(hm);
 *
 * @example
 * // Plane mode
 * const hm = createHeatmap({
 *   id: 'density', points: [{ position: [0,0,0], value: 0.6 }],
 *   barMode: false,
 * });
 * scene.add(hm);
 */
export function createHeatmap(config: HeatmapConfig): THREE.Group {
  if (!config.points || config.points.length === 0) {
    throw new Error(`Heatmap "${config.id}" requires at least 1 data point`);
  }

  const group = config.barMode !== false ? createBars(config) : createPlanes(config);
  group.name = `heatmap-${config.id}`;
  group.userData.heatmapConfig = config;
  return group;
}

/**
 * 更新热力图数据（替换所有点）。
 */
export function updateHeatmap(group: THREE.Group, points: HeatmapPoint[]): void {
  const config = group.userData.heatmapConfig as HeatmapConfig;
  if (!config) return;

  // Remove old children
  while (group.children.length > 0) {
    const child = group.children[0];
    if (child instanceof THREE.Mesh) {
      child.geometry?.dispose();
      if (Array.isArray(child.material)) {
        child.material.forEach(m => m.dispose());
      } else {
        child.material?.dispose();
      }
    }
    group.remove(child);
  }

  // Rebuild with new points
  const newConfig = { ...config, points };
  group.userData.heatmapConfig = newConfig;

  const newGroup = config.barMode !== false ? createBars(newConfig) : createPlanes(newConfig);
  while (newGroup.children.length > 0) {
    group.add(newGroup.children[0]);
  }
}
