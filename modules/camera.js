import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.179/build/three.module.js';
import { CAMERA_CONFIG } from './config.js';

export function createCameraController(camera, truck) {
    camera.position.set(CAMERA_CONFIG.x, CAMERA_CONFIG.y, CAMERA_CONFIG.z);
    camera.lookAt(0, 8, 0);

    const offset = new THREE.Vector3(
        CAMERA_CONFIG.x,
        CAMERA_CONFIG.y,
        CAMERA_CONFIG.z
    );

    function update() {
        const targetX = truck.position.x + offset.x;
        const targetY = truck.position.y + offset.y;
        const targetZ = truck.position.z + offset.z;
        const speed = CAMERA_CONFIG.followSpeed;

        camera.position.x += (targetX - camera.position.x) * speed;
        camera.position.y += (targetY - camera.position.y) * speed;
        camera.position.z += (targetZ - camera.position.z) * speed;
    }

    return { update };
}