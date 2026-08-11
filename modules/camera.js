import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.179/build/three.module.js';
import { CAMERA_CONFIG } from './config.js';

export function createCameraController(camera, truck) {
    camera.position.set(
        CAMERA_CONFIG.x,
        CAMERA_CONFIG.y,
        CAMERA_CONFIG.z
    );

    // 카메라 시선 고정
    camera.lookAt(0, 4, -2);

    const offset = new THREE.Vector3(
        CAMERA_CONFIG.x,
        CAMERA_CONFIG.y,
        CAMERA_CONFIG.z
    );

    let currentZoomMultiplier = 1;

    function update() {
        const truckScale = truck.scale.x;

        // 실제 트럭 크기에 따른 목표 줌 배율
        const growth = Math.max(
            0,
            truckScale - CAMERA_CONFIG.zoomStartScale
        );

        const targetZoomMultiplier = Math.min(
            1 + growth * CAMERA_CONFIG.zoomStrength,
            CAMERA_CONFIG.maxZoomMultiplier
        );

        // 줌 변화도 부드럽게
        currentZoomMultiplier +=
            (targetZoomMultiplier - currentZoomMultiplier) *
            CAMERA_CONFIG.followSpeed;

        const targetX =
            truck.position.x + offset.x * currentZoomMultiplier;

        const targetY =
            truck.position.y + offset.y * currentZoomMultiplier;

        const targetZ =
            truck.position.z + offset.z * currentZoomMultiplier;

        // 트럭 추적
        camera.position.x +=
            (targetX - camera.position.x) * CAMERA_CONFIG.followSpeed;

        camera.position.y +=
            (targetY - camera.position.y) * CAMERA_CONFIG.followSpeed;

        camera.position.z +=
            (targetZ - camera.position.z) * CAMERA_CONFIG.followSpeed;

        return currentZoomMultiplier;
    }

    return { update };
}