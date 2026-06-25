<!-- @jnpf-generated dashboard-3d-scene v2.0.0 -->
<template>
  <div ref="containerRef" class="three-scene-container"></div>
</template>

<script setup lang="ts">
  /**
   * 3D 场景核心组件 — Three.js 基础渲染器
   *
   * 提供：场景初始化、相机、光源、轨道控制、渲染循环、窗口自适应、Raycaster 点击交互。
   * 通过 defineExpose 暴露场景操作方法，供外部添加/移除 3D 对象。
   *
   * @version 2.0.0
   * @license VIP
   */
  import { ref, onMounted, onUnmounted } from 'vue';
  import * as THREE from 'three';
  import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';

  // ============================================================
  // Props
  // ============================================================

  const props = withDefaults(
    defineProps<{
      /** 场景背景色，默认 #000000 */
      backgroundColor?: string;
      /** 相机初始位置 [x, y, z]，默认 [10, 10, 10] */
      cameraPosition?: [number, number, number];
      /** 环境光强度 0~1，默认 0.6 */
      ambientIntensity?: number;
      /** 方向光位置 [x, y, z]，默认 [50, 50, 50] */
      directionalLightPosition?: [number, number, number];
      /** 是否启用轨道控制，默认 true */
      enableControls?: boolean;
      /** 地面网格辅助线尺寸，默认 100。设 0 禁用 */
      gridSize?: number;
    }>(),
    {
      backgroundColor: '#000000',
      cameraPosition: () => [10, 10, 10],
      ambientIntensity: 0.6,
      directionalLightPosition: () => [50, 50, 50],
      enableControls: true,
      gridSize: 100,
    },
  );

  // ============================================================
  // Emits
  // ============================================================

  const emit = defineEmits<{
    /** 点击 3D 物体时触发，payload 为 Intersection 或 null */
    click: [intersection: THREE.Intersection | null];
    /** 场景就绪时触发，传入 scene/camera/renderer 引用 */
    ready: [payload: { scene: THREE.Scene; camera: THREE.PerspectiveCamera; renderer: THREE.WebGLRenderer }];
  }>();

  // ============================================================
  // Reactive refs (non-reactive Three.js objects stored in plain refs)
  // ============================================================

  const containerRef = ref<HTMLElement>();

  let scene: THREE.Scene;
  let camera: THREE.PerspectiveCamera;
  let renderer: THREE.WebGLRenderer;
  let controls: OrbitControls | null = null;
  let animationId = 0;
  let ambientLight: THREE.AmbientLight;
  let directionalLight: THREE.DirectionalLight;

  // ============================================================
  // Init
  // ============================================================

  function initScene(): void {
    if (!containerRef.value) return;

    // ── Scene ──
    scene = new THREE.Scene();
    scene.background = new THREE.Color(props.backgroundColor);

    // ── Camera ──
    const aspect = containerRef.value.clientWidth / (containerRef.value.clientHeight || 1);
    camera = new THREE.PerspectiveCamera(60, aspect, 0.1, 10000);
    camera.position.set(props.cameraPosition[0], props.cameraPosition[1], props.cameraPosition[2]);
    camera.lookAt(0, 0, 0);

    // ── Renderer ──
    renderer = new THREE.WebGLRenderer({ antialias: true, alpha: true });
    renderer.setSize(containerRef.value.clientWidth, containerRef.value.clientHeight);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.shadowMap.enabled = true;
    containerRef.value.appendChild(renderer.domElement);

    // ── Lights ──
    ambientLight = new THREE.AmbientLight(0x888888, props.ambientIntensity);
    scene.add(ambientLight);

    directionalLight = new THREE.DirectionalLight(0xffffff, 0.8);
    directionalLight.position.set(props.directionalLightPosition[0], props.directionalLightPosition[1], props.directionalLightPosition[2]);
    directionalLight.castShadow = true;
    directionalLight.shadow.mapSize.width = 1024;
    directionalLight.shadow.mapSize.height = 1024;
    directionalLight.shadow.camera.near = 0.5;
    directionalLight.shadow.camera.far = 500;
    scene.add(directionalLight);

    // ── Controls ──
    if (props.enableControls !== false) {
      controls = new OrbitControls(camera, renderer.domElement);
      controls.enableDamping = true;
      controls.dampingFactor = 0.05;
      controls.target.set(0, 0, 0);
      controls.update();
    }

    // ── Grid Helper ──
    if (props.gridSize > 0) {
      const grid = new THREE.GridHelper(props.gridSize, 50, 0x1e3a5f, 0x1e3a5f);
      scene.add(grid);
    }

    // ── Click Listener (Raycaster) ──
    containerRef.value.addEventListener('click', handleClick);
    containerRef.value.addEventListener('pointerdown', handlePointerDown);

    // ── Resize ──
    window.addEventListener('resize', handleResize);

    // ── Ready event ──
    emit('ready', { scene, camera, renderer });

    // ── Start loop ──
    animate();
  }

  // ============================================================
  // Animation Loop
  // ============================================================

  function animate(): void {
    animationId = requestAnimationFrame(animate);
    controls?.update();
    renderer.render(scene, camera);
  }

  // ============================================================
  // Resize
  // ============================================================

  function handleResize(): void {
    if (!containerRef.value) return;
    const w = containerRef.value.clientWidth;
    const h = containerRef.value.clientHeight;
    if (w === 0 || h === 0) return;

    camera.aspect = w / h;
    camera.updateProjectionMatrix();
    renderer.setSize(w, h);
  }

  // ============================================================
  // Raycaster Click
  // ============================================================

  let pointerDown = false;

  function handlePointerDown(): void {
    pointerDown = true;
  }

  function handleClick(event: MouseEvent): void {
    // Only treat as click if it was a press+release (no drag)
    if (!pointerDown) return;
    pointerDown = false;

    if (!containerRef.value) return;

    const rect = containerRef.value.getBoundingClientRect();
    const mouse = new THREE.Vector2(((event.clientX - rect.left) / rect.width) * 2 - 1, -((event.clientY - rect.top) / rect.height) * 2 + 1);

    const raycaster = new THREE.Raycaster();
    raycaster.setFromCamera(mouse, camera);
    const intersects = raycaster.intersectObjects(scene.children, true);

    emit('click', intersects.length > 0 ? intersects[0] : null);
  }

  // ============================================================
  // Public API — defineExpose
  // ============================================================

  function getScene(): THREE.Scene {
    return scene;
  }

  function getCamera(): THREE.PerspectiveCamera {
    return camera;
  }

  function getRenderer(): THREE.WebGLRenderer {
    return renderer;
  }

  function addObject(obj: THREE.Object3D): void {
    scene.add(obj);
  }

  function removeObject(obj: THREE.Object3D): void {
    scene.remove(obj);
  }

  function findObject(name: string): THREE.Object3D | undefined {
    return scene.getObjectByName(name);
  }

  defineExpose({
    getScene,
    getCamera,
    getRenderer,
    addObject,
    removeObject,
    findObject,
  });

  // ============================================================
  // Lifecycle
  // ============================================================

  onMounted(() => {
    initScene();
  });

  onUnmounted(() => {
    if (animationId) cancelAnimationFrame(animationId);
    window.removeEventListener('resize', handleResize);
    if (containerRef.value) {
      containerRef.value.removeEventListener('click', handleClick);
      containerRef.value.removeEventListener('pointerdown', handlePointerDown);
    }
    controls?.dispose();
    controls = null;
    renderer?.dispose();
    // Remove renderer canvas from DOM
    if (renderer?.domElement && containerRef.value) {
      containerRef.value.removeChild(renderer.domElement);
    }
  });
</script>

<style scoped>
  .three-scene-container {
    width: 100%;
    height: 100%;
    overflow: hidden;
    position: relative;
  }
</style>
