import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.179/build/three.module.js';
import { WORLD_CONFIG } from './config.js';

export function createWorld(container) {
    const scene = new THREE.Scene();

    // 하늘과 안개
    scene.background = new THREE.Color(WORLD_CONFIG.fogColor);
    scene.fog = new THREE.Fog(
        WORLD_CONFIG.fogColor,
        WORLD_CONFIG.fogNear,
        WORLD_CONFIG.fogFar
    );

    // 카메라
    const camera = new THREE.PerspectiveCamera(
        75,
        container.clientWidth / container.clientHeight,
        0.1,
        1000
    );

    // 렌더러
    const renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setSize(container.clientWidth, container.clientHeight);
    renderer.setPixelRatio(window.devicePixelRatio);
    container.appendChild(renderer.domElement);

    // 무한 바닥
    const tileSize = WORLD_CONFIG.tileSize;
    const tileRadius = WORLD_CONFIG.tileRadius;

    const groundTiles = [];
    const groundGeometry = new THREE.PlaneGeometry(tileSize, tileSize);
    const groundMaterial = new THREE.MeshPhongMaterial({ color: 0x3a7a2a });

    for (let x = -tileRadius; x <= tileRadius; x++) {
        for (let z = -tileRadius; z <= tileRadius; z++) {
            const tile = new THREE.Mesh(groundGeometry, groundMaterial);

            tile.rotation.x = -Math.PI / 2;
            tile.position.set(x * tileSize, 0, z * tileSize);

            scene.add(tile);
            groundTiles.push(tile);
        }
    }

    let currentTileX = 0;
    let currentTileZ = 0;

    // 플레이어가 새로운 타일로 넘어가면 바닥 재배치
    function update(player) {
        const newTileX = Math.round(player.position.x / tileSize);
        const newTileZ = Math.round(player.position.z / tileSize);

        if (newTileX === currentTileX && newTileZ === currentTileZ) return;

        currentTileX = newTileX;
        currentTileZ = newTileZ;

        let index = 0;

        for (let x = -tileRadius; x <= tileRadius; x++) {
            for (let z = -tileRadius; z <= tileRadius; z++) {
                const tile = groundTiles[index++];

                tile.position.x = (currentTileX + x) * tileSize;
                tile.position.z = (currentTileZ + z) * tileSize;
            }
        }
    }

    // 조명
    const dirLight = new THREE.DirectionalLight(0xffffff, 1);
    dirLight.position.set(5, 10, 5);
    scene.add(dirLight);

    const ambient = new THREE.AmbientLight(0xffffff, 0.4);
    scene.add(ambient);

    // 화면 크기 변경
    window.addEventListener('resize', () => {
        const width = container.clientWidth;
        const height = container.clientHeight;

        camera.aspect = width / height;
        camera.updateProjectionMatrix();
        renderer.setSize(width, height);
    });

    return { scene, camera, renderer, update };
}