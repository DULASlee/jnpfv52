/**
 * PoC-B: 飞线生成器
 * 5 条 QuadraticBezierCurve3 动态粒子飞线
 */
import * as THREE from 'three';

export interface FlyLine {
  curve: THREE.QuadraticBezierCurve3;
  particles: THREE.Points;
  progress: number; // 0..1 动画进度
  speed: number;
}

/** 生成 5 条飞线 */
export function createFlyLines(count: number = 5): FlyLine[] {
  const lines: FlyLine[] = [];
  const bounds = 60;

  for (let i = 0; i < count; i++) {
    const start = new THREE.Vector3(
      (Math.random() - 0.5) * bounds * 2,
      5 + Math.random() * 40,
      (Math.random() - 0.5) * bounds * 2,
    );
    const end = new THREE.Vector3(
      (Math.random() - 0.5) * bounds * 2,
      5 + Math.random() * 40,
      (Math.random() - 0.5) * bounds * 2,
    );
    const mid = new THREE.Vector3(
      (start.x + end.x) / 2 + (Math.random() - 0.5) * 60,
      Math.max(start.y, end.y) + 20 + Math.random() * 30,
      (start.z + end.z) / 2 + (Math.random() - 0.5) * 60,
    );

    const curve = new THREE.QuadraticBezierCurve3(start, mid, end);

    // 粒子尾迹 (沿曲线采样 50 个点)
    const particleCount = 50;
    const positions = new Float32Array(particleCount * 3);
    const colors = new Float32Array(particleCount * 3);

    const color = new THREE.Color().setHSL(0.55 + i * 0.08, 1, 0.5);
    for (let j = 0; j < particleCount; j++) {
      const t = j / particleCount;
      const pt = curve.getPoint(t);
      positions[j * 3] = pt.x;
      positions[j * 3 + 1] = pt.y;
      positions[j * 3 + 2] = pt.z;
      colors[j * 3] = color.r;
      colors[j * 3 + 1] = color.g;
      colors[j * 3 + 2] = color.b;
    }

    const geo = new THREE.BufferGeometry();
    geo.setAttribute('position', new THREE.BufferAttribute(positions, 3));
    geo.setAttribute('color', new THREE.BufferAttribute(colors, 3));

    const mat = new THREE.PointsMaterial({
      size: 0.3,
      vertexColors: true,
      blending: THREE.AdditiveBlending,
      depthWrite: false,
      transparent: true,
      opacity: 0.8,
    });

    const particles = new THREE.Points(geo, mat);

    lines.push({
      curve,
      particles,
      progress: Math.random(),
      speed: 0.0003 + Math.random() * 0.0007,
    });
  }

  return lines;
}

/** 更新飞线粒子位置 (每帧调用) */
export function updateFlyLines(lines: FlyLine[], dt: number): void {
  const particleCount = 50;
  for (const line of lines) {
    line.progress += line.speed * dt;
    if (line.progress > 1) line.progress -= 1;

    const positions = line.particles.geometry.attributes.position.array as Float32Array;
    for (let j = 0; j < particleCount; j++) {
      const t = (line.progress + j / particleCount) % 1;
      const pt = line.curve.getPoint(t);
      positions[j * 3] = pt.x;
      positions[j * 3 + 1] = pt.y;
      positions[j * 3 + 2] = pt.z;
    }
    line.particles.geometry.attributes.position.needsUpdate = true;
  }
}
