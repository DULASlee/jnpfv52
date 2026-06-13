/**
 * 3D 电子围栏 — 地面填充 + 围墙墙壁 + 状态切换
 *
 * 使用 Shape + ShapeGeometry 绘制半透明地面区域，
 * PlaneGeometry 绘制立体围墙，支持 normal/alarm 双状态颜色。
 *
 * @jnpf-generated dashboard-3d-fence v2.0.0
 */

import * as THREE from 'three';

// ============================================================
// Types
// ============================================================

export type FenceStatus = 'normal' | 'alarm';

export interface FenceConfig {
  /** 唯一标识 */
  id: string;
  /** 名称 */
  name: string;
  /** 闭合多边形顶点（至少 3 个点），[x, y, z] 数组 */
  points: [number, number, number][];
  /** 围墙高度，默认 5 */
  height?: number;
  /** 正常状态颜色，默认 #00d4ff */
  color?: string;
  /** 告警状态颜色，默认 #ff4560 */
  alarmColor?: string;
  /** 初始状态，默认 normal */
  status?: FenceStatus;
  /** 填充透明度 0~1，默认 0.3 */
  opacity?: number;
}

export interface FenceObject extends THREE.Group {
  /** 获取围栏配置 */
  getConfig: () => FenceConfig;
}

// ============================================================
// Helpers
// ============================================================

function vec2FromPoint(p: [number, number, number]): THREE.Vector2 {
  // XZ plane → Vector2(x, z)
  return new THREE.Vector2(p[0], p[2]);
}

function vec3FromPoint(p: [number, number, number]): THREE.Vector3 {
  return new THREE.Vector3(p[0], p[1], p[2]);
}

// ============================================================
// Ground fill
// ============================================================

function createGround(points: [number, number, number][], color: string, opacity: number): THREE.Mesh {
  const shape = new THREE.Shape();
  const start = vec2FromPoint(points[0]);
  shape.moveTo(start.x, start.y);

  for (let i = 1; i < points.length; i++) {
    const pt = vec2FromPoint(points[i]);
    shape.lineTo(pt.x, pt.y);
  }
  shape.closePath();

  const geom = new THREE.ShapeGeometry(shape);
  const mat = new THREE.MeshBasicMaterial({
    color: new THREE.Color(color),
    transparent: true,
    opacity: opacity * 0.5,
    side: THREE.DoubleSide,
    depthWrite: false,
  });

  const mesh = new THREE.Mesh(geom, mat);
  mesh.rotation.x = -Math.PI / 2; // Lay flat on XZ plane
  mesh.name = 'fence-ground';
  return mesh;
}

// ============================================================
// Walls
// ============================================================

function createWalls(points: [number, number, number][], height: number, color: string, opacity: number): THREE.Group {
  const wallGroup = new THREE.Group();
  wallGroup.name = 'fence-walls';

  const wallMat = new THREE.MeshBasicMaterial({
    color: new THREE.Color(color),
    transparent: true,
    opacity,
    side: THREE.DoubleSide,
    depthWrite: true,
  });

  const n = points.length;
  for (let i = 0; i < n; i++) {
    const p1 = vec3FromPoint(points[i]);
    const p2 = vec3FromPoint(points[(i + 1) % n]); // Wrap to close

    const dx = p2.x - p1.x;
    const dz = p2.z - p1.z;
    const wallWidth = Math.sqrt(dx * dx + dz * dz);

    // Skip degenerate walls
    if (wallWidth < 0.001) continue;

    const wallGeom = new THREE.PlaneGeometry(wallWidth, height);

    // Position at midpoint between p1 and p2
    const midX = (p1.x + p2.x) / 2;
    const midZ = (p1.z + p2.z) / 2;

    const wall = new THREE.Mesh(wallGeom, wallMat.clone());
    wall.position.set(midX, height / 2 + (p1.y + p2.y) / 2, midZ);

    // Rotate to align with the segment direction
    wall.rotation.y = Math.atan2(dz, dx);
    // Flip to face outward
    wall.rotation.y += Math.PI / 2;

    wall.name = `fence-wall-${i}`;
    wall.userData.wallIndex = i;
    wallGroup.add(wall);
  }

  return wallGroup;
}

// ============================================================
// Top edge line
// ============================================================

function createTopLine(points: [number, number, number][], height: number, color: string): THREE.Line {
  const topPoints = points.map(p => new THREE.Vector3(p[0], p[1] + height, p[2]));
  // Close the loop
  topPoints.push(topPoints[0].clone());

  const geom = new THREE.BufferGeometry().setFromPoints(topPoints);
  const mat = new THREE.LineBasicMaterial({
    color: new THREE.Color(color),
    transparent: true,
    opacity: 0.8,
  });

  const line = new THREE.Line(geom, mat);
  line.name = 'fence-top-line';
  return line;
}

// ============================================================
// Public API
// ============================================================

/**
 * 创建电子围栏，返回包含地面、墙壁和顶部边缘线的 Group。
 *
 * @example
 * const fence = createFence({
 *   id: 'zone1',
 *   name: '安全区域',
 *   points: [[0,0,0], [10,0,0], [10,0,8], [0,0,8]],
 *   height: 4,
 *   color: '#00ff88',
 * });
 * scene.add(fence);
 */
export function createFence(config: FenceConfig): FenceObject {
  const { points, height = 5, color = '#00d4ff', alarmColor = '#ff4560', status = 'normal', opacity = 0.3 } = config;

  if (points.length < 3) {
    throw new Error(`Fence "${config.id}" requires at least 3 points, got ${points.length}`);
  }

  const group = new THREE.Group() as FenceObject;
  group.name = `fence-${config.id}`;

  const currentColor = status === 'alarm' ? alarmColor : color;

  // Ground fill
  const ground = createGround(points, currentColor, opacity);
  group.add(ground);

  // Walls
  const walls = createWalls(points, height, currentColor, opacity);
  group.add(walls);

  // Top edge line
  const topLine = createTopLine(points, height, currentColor);
  group.add(topLine);

  // Store config
  group.userData.fenceConfig = { ...config, status, color, alarmColor, height, opacity };

  // Methods
  group.getConfig = () => group.userData.fenceConfig as FenceConfig;

  return group;
}

/**
 * 切换围栏状态（normal ↔ alarm），自动更新所有子 Mesh 颜色。
 */
export function updateFenceStatus(fence: FenceObject, status: FenceStatus): void {
  const config = fence.userData.fenceConfig as FenceConfig & { status: FenceStatus };
  if (!config || config.status === status) return;

  config.status = status;
  fence.userData.fenceConfig = config;

  const targetColor = status === 'alarm' ? config.alarmColor! : config.color!;
  const newColor = new THREE.Color(targetColor);

  // Update all child meshes and lines
  fence.traverse(child => {
    if (child instanceof THREE.Mesh && child.material) {
      const materials = Array.isArray(child.material) ? child.material : [child.material];
      for (const mat of materials) {
        if (mat instanceof THREE.MeshBasicMaterial && mat.color) {
          mat.color.copy(newColor);
        }
      }
    }
    if (child instanceof THREE.Line && child.material) {
      const materials = Array.isArray(child.material) ? child.material : [child.material];
      for (const mat of materials) {
        if (mat instanceof THREE.LineBasicMaterial && mat.color) {
          mat.color.copy(newColor);
        }
      }
    }
  });
}

/**
 * 根据 ID 在 scene 中查找围栏。
 */
export function findFence(scene: THREE.Scene, id: string): FenceObject | undefined {
  const found = scene.getObjectByName(`fence-${id}`);
  return found ? (found as FenceObject) : undefined;
}
