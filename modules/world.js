import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.179/build/three.module.js';
import { WORLD_CONFIG } from './config.js';

export function createWorld(container) {
    const scene = new THREE.Scene();

    // 하늘과 Fog
    scene.background = new THREE.Color(WORLD_CONFIG.fogColor);
    scene.fog = new THREE.Fog(
        WORLD_CONFIG.fogColor,
        WORLD_CONFIG.baseFogNear,
        WORLD_CONFIG.baseFogFar
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

    // 바닥
    const tileSize = WORLD_CONFIG.tileSize;
    const groundTiles = [];

    const groundGeometry = new THREE.PlaneGeometry(tileSize, tileSize);
    const groundMaterial = new THREE.MeshPhongMaterial({ color: 0x3a7a2a });

    let currentTileX = 0;
    let currentTileZ = 0;
    let currentTileRadius = -1;

    // 필요한 만큼 타일 생성
    function ensureTileCount(count) {
        while (groundTiles.length < count) {
            const tile = new THREE.Mesh(
                groundGeometry,
                groundMaterial
            );

            tile.rotation.x = -Math.PI / 2;
            scene.add(tile);

            groundTiles.push(tile);
        }
    }

    // 타일 배치
    function updateGround(tileX, tileZ, radius) {
        const requiredCount = (radius * 2 + 1) ** 2;

        ensureTileCount(requiredCount);

        let index = 0;

        for (let x = -radius; x <= radius; x++) {
            for (let z = -radius; z <= radius; z++) {
                const tile = groundTiles[index++];

                tile.visible = true;

                tile.position.x = (tileX + x) * tileSize;
                tile.position.z = (tileZ + z) * tileSize;
            }
        }

        // 사용하지 않는 타일 숨김
        for (let i = index; i < groundTiles.length; i++) {
            groundTiles[i].visible = false;
        }
    }

    // 기본 5x5 배치
    updateGround(
        currentTileX,
        currentTileZ,
        WORLD_CONFIG.baseTileRadius
    );

    // 월드 업데이트
    function update(player, camera, zoomMultiplier) {
        // 카메라 줌에 따라 Fog 증가
        const fogMultiplier =
            1 +
            (zoomMultiplier - 1) *
            WORLD_CONFIG.fogScaleStrength;

        scene.fog.near =
            WORLD_CONFIG.baseFogNear * fogMultiplier;

        scene.fog.far =
            WORLD_CONFIG.baseFogFar * fogMultiplier;

        // 현재 플레이어 타일
        const newTileX =
            Math.round(player.position.x / tileSize);

        const newTileZ =
            Math.round(player.position.z / tileSize);

        // 카메라와 트럭의 수평 거리
        const cameraDistance = Math.hypot(
            camera.position.x - player.position.x,
            camera.position.z - player.position.z
        );

        // Fog 끝까지 바닥이 존재하도록 필요한 범위 계산
        const requiredDistance =
            scene.fog.far + cameraDistance;

        let requiredRadius = Math.ceil(
            (requiredDistance - tileSize / 2) / tileSize
        );

        requiredRadius = Math.max(
            requiredRadius,
            WORLD_CONFIG.baseTileRadius
        );

        requiredRadius = Math.min(
            requiredRadius,
            WORLD_CONFIG.maxTileRadius
        );

        // 타일 위치나 범위가 바뀌었을 때만 재배치
        if (
            newTileX === currentTileX &&
            newTileZ === currentTileZ &&
            requiredRadius === currentTileRadius
        ) {
            return;
        }

        currentTileX = newTileX;
        currentTileZ = newTileZ;
        currentTileRadius = requiredRadius;

        updateGround(
            currentTileX,
            currentTileZ,
            currentTileRadius
        );
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

    return {
        scene,
        camera,
        renderer,
        update
    };
}