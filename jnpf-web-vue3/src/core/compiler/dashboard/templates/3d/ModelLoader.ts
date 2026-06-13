/**
 * 3D 模型加载器 — glTF / GLB / OBJ
 *
 * 支持 Draco 压缩解压、URL 缓存、变换应用。
 * 所有加载方法返回 Promise<THREE.Object3D>。
 *
 * @jnpf-generated dashboard-3d-modelloader v2.0.0
 */

import * as THREE from 'three';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';
import { DRACOLoader } from 'three/examples/jsm/loaders/DRACOLoader.js';
import { OBJLoader } from 'three/examples/jsm/loaders/OBJLoader.js';

// ============================================================
// Types
// ============================================================

export interface LoadOptions {
  /** 模型文件 URL（支持 .gltf / .glb / .obj） */
  url: string;
  /** 可选名称，加载后设置 object.name */
  name?: string;
  /** 统一缩放，默认 1 */
  scale?: number;
  /** 位置偏移 [x, y, z]，默认 [0, 0, 0] */
  position?: [number, number, number];
  /** 欧拉旋转 [x, y, z]（弧度），默认 [0, 0, 0] */
  rotation?: [number, number, number];
  /** 是否投射阴影，默认 false */
  castShadow?: boolean;
  /** 加载进度回调 */
  onProgress?: (loaded: number, total: number) => void;
  /** 加载失败回调 */
  onError?: (error: unknown) => void;
}

// ============================================================
// Loader singletons (lazy initialized)
// ============================================================

let dracoLoader: DRACOLoader | null = null;
let gltfLoader: GLTFLoader | null = null;
let objLoader: OBJLoader | null = null;

/** Draco WASM 解码器 CDN 路径 */
const DRACO_DECODER_PATH = 'https://www.gstatic.com/draco/versioned/decoders/1.5.6/';

function getDracoLoader(): DRACOLoader {
  if (!dracoLoader) {
    dracoLoader = new DRACOLoader();
    dracoLoader.setDecoderPath(DRACO_DECODER_PATH);
  }
  return dracoLoader;
}

function getGLTFLoader(): GLTFLoader {
  if (!gltfLoader) {
    gltfLoader = new GLTFLoader();
    gltfLoader.setDRACOLoader(getDracoLoader());
  }
  return gltfLoader;
}

function getOBJLoader(): OBJLoader {
  if (!objLoader) {
    objLoader = new OBJLoader();
  }
  return objLoader;
}

// ============================================================
// Cache
// ============================================================

/** URL → 原始 Object3D 缓存。缓存命中时返回 clone() 并重新应用变换。 */
const cache = new Map<string, THREE.Object3D>();

export function clearCache(): void {
  cache.clear();
}

export function getCacheSize(): number {
  return cache.size;
}

// ============================================================
// File type detection
// ============================================================

function getFileExtension(url: string): string {
  // Strip query params and hash
  const clean = url.split('?')[0].split('#')[0];
  const dot = clean.lastIndexOf('.');
  return dot >= 0 ? clean.slice(dot).toLowerCase() : '';
}

// ============================================================
// Transform application
// ============================================================

function applyTransforms(obj: THREE.Object3D, options: LoadOptions): void {
  if (options.name) {
    obj.name = options.name;
  }

  if (options.scale !== undefined) {
    obj.scale.setScalar(options.scale);
  }

  if (options.position) {
    obj.position.set(options.position[0], options.position[1], options.position[2]);
  }

  if (options.rotation) {
    obj.rotation.set(options.rotation[0], options.rotation[1], options.rotation[2]);
  }

  if (options.castShadow) {
    obj.traverse(child => {
      if (child instanceof THREE.Mesh) {
        child.castShadow = true;
        child.receiveShadow = true;
      }
    });
  }
}

// ============================================================
// Main loader
// ============================================================

/**
 * 加载 3D 模型。
 *
 * 支持格式：glTF (.gltf)、GLB (.glb)、OBJ (.obj)。
 * 自动缓存：同一 URL 第二次请求直接返回 clone。
 *
 * @example
 * const robot = await loadModel({ url: '/models/robot.glb', scale: 0.5, castShadow: true });
 * scene.add(robot);
 */
export async function loadModel(options: LoadOptions): Promise<THREE.Object3D> {
  const { url, onProgress, onError } = options;

  // ── Cache hit: clone and re-apply transforms ──
  const cached = cache.get(url);
  if (cached) {
    const clone = cached.clone(true);
    applyTransforms(clone, options);
    return clone;
  }

  const ext = getFileExtension(url);

  try {
    let result: THREE.Object3D;

    if (ext === '.obj') {
      result = await loadOBJ(url, onProgress);
    } else {
      // .gltf / .glb / default → GLTFLoader
      result = await loadGLTF(url, onProgress);
    }

    // Cache the original (before transform)
    cache.set(url, result.clone(true));

    applyTransforms(result, options);
    return result;
  } catch (err) {
    onError?.(err);
    throw err;
  }
}

// ============================================================
// Format-specific loaders
// ============================================================

function loadGLTF(url: string, onProgress?: (loaded: number, total: number) => void): Promise<THREE.Object3D> {
  return new Promise((resolve, reject) => {
    const loader = getGLTFLoader();
    loader.load(
      url,
      gltf => {
        resolve(gltf.scene);
      },
      progress => {
        if (progress.total > 0) {
          onProgress?.(progress.loaded, progress.total);
        }
      },
      error => {
        reject(new Error(`GLTF load failed: ${url} — ${(error as Error).message || error}`));
      },
    );
  });
}

function loadOBJ(url: string, onProgress?: (loaded: number, total: number) => void): Promise<THREE.Object3D> {
  return new Promise((resolve, reject) => {
    const loader = getOBJLoader();
    loader.load(
      url,
      obj => {
        resolve(obj);
      },
      progress => {
        if (progress.total > 0) {
          onProgress?.(progress.loaded, progress.total);
        }
      },
      error => {
        reject(new Error(`OBJ load failed: ${url} — ${(error as Error).message || error}`));
      },
    );
  });
}

// ============================================================
// Batch loading
// ============================================================

/**
 * 批量加载模型。所有加载并行进行，任一失败则整体 reject。
 */
export async function loadModels(optionsList: LoadOptions[]): Promise<THREE.Object3D[]> {
  return Promise.all(optionsList.map(opts => loadModel(opts)));
}
