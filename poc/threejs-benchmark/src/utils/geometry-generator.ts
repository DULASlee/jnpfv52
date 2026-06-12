/**
 * PoC-B: 程序化几何体生成器
 * 目标: 10 万三角面混合场景 (建筑 + 地形 + 设备)
 */
import * as THREE from 'three';

export interface GeoStats {
  totalFaces: number;
  buildings: number;
  terrainSegments: number;
  equipment: number;
}

/** 生成混合场景几何体，达到 targetFaces 面数 */
export function generateMixedGeometry(targetFaces: number = 100_000): {
  group: THREE.Group;
  stats: GeoStats;
} {
  const group = new THREE.Group();
  const stats: GeoStats = { totalFaces: 0, buildings: 0, terrainSegments: 0, equipment: 0 };

  // ── 1. 建筑群 (BoxGeometry, ~40% 面数) ──
  const buildingFaces = Math.floor(targetFaces * 0.4);
  const buildingCount = 60;
  const facesPerBuilding = Math.floor(buildingFaces / buildingCount);
  const buildingSeg = Math.max(1, Math.floor(Math.sqrt(facesPerBuilding / 12))); // BoxGeometry = 12 triangles

  const buildingMat = new THREE.MeshPhongMaterial({
    color: 0x4a90d9,
    flatShading: true,
    specular: 0x111111,
    shininess: 30,
  });

  for (let i = 0; i < buildingCount; i++) {
    const w = 2 + Math.random() * 4;
    const h = 5 + Math.random() * 30;
    const d = 2 + Math.random() * 4;
    const geo = new THREE.BoxGeometry(w, h, d, buildingSeg, buildingSeg, buildingSeg);
    const mesh = new THREE.Mesh(geo, buildingMat);
    mesh.position.set(
      (Math.random() - 0.5) * 120,
      h / 2,
      (Math.random() - 0.5) * 120,
    );
    mesh.castShadow = true;
    mesh.receiveShadow = true;
    group.add(mesh);
    stats.totalFaces += geo.index ? geo.index.count / 3 : geo.attributes.position.count / 3;
    stats.buildings++;
  }

  // ── 2. 地形 (PlaneGeometry, ~35% 面数) ──
  const terrainFaces = Math.floor(targetFaces * 0.35);
  const terrainSeg = Math.floor(Math.sqrt(terrainFaces / 2)); // PlaneGeometry = 2 triangles per segment
  const terrainGeo = new THREE.PlaneGeometry(200, 200, terrainSeg, terrainSeg);
  // 地形起伏
  const posAttr = terrainGeo.attributes.position;
  for (let i = 0; i < posAttr.count; i++) {
    const x = posAttr.getX(i);
    const y = posAttr.getY(i);
    posAttr.setZ(i, Math.sin(x * 0.05) * Math.cos(y * 0.05) * 8 + Math.random() * 2);
  }
  terrainGeo.computeVertexNormals();

  const terrainMat = new THREE.MeshPhongMaterial({
    color: 0x3a7d44,
    flatShading: true,
    side: THREE.DoubleSide,
  });
  const terrain = new THREE.Mesh(terrainGeo, terrainMat);
  terrain.rotation.x = -Math.PI / 2;
  terrain.position.y = -1;
  terrain.receiveShadow = true;
  group.add(terrain);
  stats.totalFaces += terrainGeo.index
    ? terrainGeo.index.count / 3
    : terrainGeo.attributes.position.count / 3;
  stats.terrainSegments = terrainSeg * terrainSeg;

  // ── 3. 设备模型 (SphereGeometry + CylinderGeometry, ~25% 面数) ──
  const equipFaces = Math.floor(targetFaces * 0.25);
  const equipCount = 80;
  const facesPerEquip = Math.floor(equipFaces / equipCount);
  const sphereSeg = Math.max(4, Math.floor(Math.sqrt(facesPerEquip / 8))); // ~8 triangles per segment²

  const equipColors = [0xff6b35, 0xf7c948, 0x5bc0eb, 0x9b5de5, 0x00f5d4];
  for (let i = 0; i < equipCount; i++) {
    const color = equipColors[Math.floor(Math.random() * equipColors.length)];
    const mat = new THREE.MeshPhongMaterial({ color, flatShading: true });

    const geo =
      Math.random() > 0.5
        ? new THREE.SphereGeometry(0.8 + Math.random() * 2, sphereSeg, sphereSeg)
        : new THREE.CylinderGeometry(0.5 + Math.random() * 1.5, 0.5 + Math.random() * 1.5, 2 + Math.random() * 6, sphereSeg);

    const mesh = new THREE.Mesh(geo, mat);
    mesh.position.set(
      (Math.random() - 0.5) * 140,
      Math.random() * 3,
      (Math.random() - 0.5) * 140,
    );
    mesh.castShadow = true;
    group.add(mesh);
    stats.totalFaces += geo.index ? geo.index.count / 3 : geo.attributes.position.count / 3;
    stats.equipment++;
  }

  return { group, stats };
}
