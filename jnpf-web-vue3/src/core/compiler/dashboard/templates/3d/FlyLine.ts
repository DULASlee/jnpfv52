/**
 * 3D 飞线 — 贝塞尔弧线 + 飞行粒子动画
 *
 * 通过 QuadraticBezierCurve3 生成弧形轨迹，粒子沿曲线循环移动。
 * 适用于数字孪生场景中的数据流、物流、人员流动可视化。
 *
 * @jnpf-generated dashboard-3d-flyline v2.0.0
 */

import * as THREE from 'three';

// ============================================================
// Types
// ============================================================

export interface FlyLineConfig {
  /** 起始点 [x, y, z] */
  start: [number, number, number];
  /** 终点 [x, y, z] */
  end: [number, number, number];
  /** 线条颜色，默认 #00d4ff */
  color?: string;
  /** 弧线高度，默认 = 两点距离 × 0.4 */
  height?: number;
  /** 粒子飞行速度 (0~1/帧)，默认 0.003 */
  speed?: number;
  /** 线条宽度，默认 1.5 */
  width?: number;
  /** 粒子半径，默认 0.3 */
  particleRadius?: number;
}

/** 飞线返回对象 */
export interface FlyLineObject extends THREE.Group {
  /** 手动更新粒子动画（当不使用全局渲染循环时调用） */
  update: () => void;
  /** 获取轨迹曲线 */
  getCurve: () => THREE.QuadraticBezierCurve3;
}

// ============================================================
// Helpers
// ============================================================

function vec3FromTuple(t: [number, number, number]): THREE.Vector3 {
  return new THREE.Vector3(t[0], t[1], t[2]);
}

function distance3D(a: [number, number, number], b: [number, number, number]): number {
  const dx = b[0] - a[0];
  const dy = b[1] - a[1];
  const dz = b[2] - a[2];
  return Math.sqrt(dx * dx + dy * dy + dz * dz);
}

// ============================================================
// Create single fly line
// ============================================================

/**
 * 创建单条飞线，返回包含弧线和飞行粒子的 Group。
 *
 * @example
 * const line = createFlyLine({ start: [0,0,0], end: [10,0,10], color: '#ff6b6b' });
 * scene.add(line);
 * // 在渲染循环中调用: line.update();
 */
export function createFlyLine(config: FlyLineConfig): FlyLineObject {
  const { start, end, color = '#00d4ff', speed = 0.003, particleRadius = 0.3 } = config;

  const startVec = vec3FromTuple(start);
  const endVec = vec3FromTuple(end);
  const dist = distance3D(start, end);

  // ── Midpoint with height ──
  const mid = new THREE.Vector3().addVectors(startVec, endVec).multiplyScalar(0.5);
  const arcHeight = config.height ?? dist * 0.4;
  mid.y += arcHeight;

  // ── Bezier curve ──
  const curve = new THREE.QuadraticBezierCurve3(startVec.clone(), mid, endVec.clone());

  // ── Tube / line geometry ──
  const curvePoints = curve.getPoints(64);
  const lineGeom = new THREE.BufferGeometry().setFromPoints(curvePoints);
  const lineMat = new THREE.LineBasicMaterial({
    color: new THREE.Color(color),
    transparent: true,
    opacity: 0.6,
    linewidth: 1, // WebGL renderer typically caps at 1, use width for visual intent
  });
  const line = new THREE.Line(lineGeom, lineMat);

  // ── Dashed overlay for visual richness ──
  const dashPoints = curve.getPoints(128);
  const dashGeom = new THREE.BufferGeometry().setFromPoints(dashPoints);
  const dashMat = new THREE.LineDashedMaterial({
    color: new THREE.Color(color),
    transparent: true,
    opacity: 0.3,
    dashSize: 2,
    gapSize: 1,
  });
  const dashLine = new THREE.Line(dashGeom, dashMat);
  dashLine.computeLineDistances();

  // ── Particle ──
  const particleGeom = new THREE.SphereGeometry(particleRadius, 8, 8);
  const particleMat = new THREE.MeshBasicMaterial({
    color: new THREE.Color(color),
    transparent: true,
    opacity: 0.9,
  });
  const particle = new THREE.Mesh(particleGeom, particleMat);

  // ── Particle glow (larger transparent sphere) ──
  const glowGeom = new THREE.SphereGeometry(particleRadius * 2.5, 8, 8);
  const glowMat = new THREE.MeshBasicMaterial({
    color: new THREE.Color(color),
    transparent: true,
    opacity: 0.25,
  });
  const glow = new THREE.Mesh(glowGeom, glowMat);
  particle.add(glow);

  // Initial particle position
  const initPos = curve.getPoint(0);
  particle.position.copy(initPos);

  // ── Group ──
  const group = new THREE.Group() as FlyLineObject;
  group.name = `flyline-${start.join(',')}-${end.join(',')}`;
  group.add(line);
  group.add(dashLine);
  group.add(particle);

  // ── Animation state ──
  let t = 0;
  const actualSpeed = speed;

  // ── Methods ──
  group.update = () => {
    t += actualSpeed;
    if (t > 1) t -= 1;
    const pt = curve.getPoint(t);
    particle.position.copy(pt);
  };

  group.getCurve = () => curve;

  // Store config for data binding
  group.userData.flyLineConfig = config;

  return group;
}

// ============================================================
// Batch creation
// ============================================================

/**
 * 批量创建飞线，返回 Group 数组。
 *
 * @example
 * const lines = createFlyLineGroup([
 *   { start: [0,0,0], end: [5,0,5], color: '#00d4ff' },
 *   { start: [5,0,5], end: [10,0,0], color: '#ffa940' },
 * ]);
 * lines.forEach(l => scene.add(l));
 * // In render loop: lines.forEach(l => l.update());
 */
export function createFlyLineGroup(configs: FlyLineConfig[]): FlyLineObject[] {
  return configs.map(cfg => createFlyLine(cfg));
}

/**
 * 更新一组飞线的粒子动画（便捷函数，用于渲染循环）。
 */
export function updateFlyLines(lines: FlyLineObject[]): void {
  for (const line of lines) {
    line.update();
  }
}
