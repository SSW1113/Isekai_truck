import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.179/build/three.module.js';
import { TRUCK_CONFIG } from './config.js';

export function createTruck(scene) {
    const mesh = new THREE.Mesh(
        new THREE.BoxGeometry(1.5, 1, 3),
        new THREE.MeshPhongMaterial({ color: 0x3366ff })
    );

    mesh.position.y = 0.5;
    scene.add(mesh);

    let speed = 0;
    let lastDirX = 0;
    let lastDirZ = 0;

    function update(move) {
        const inputLength = Math.hypot(move.x, move.z);

        if (inputLength > 0.05) {
            const dirX = move.x / inputLength;
            const dirZ = move.z / inputLength;

            // 회전
            const targetRotation = Math.atan2(dirX, dirZ);
            let angleDiff = targetRotation - mesh.rotation.y;

            while (angleDiff > Math.PI) angleDiff -= Math.PI * 2;
            while (angleDiff < -Math.PI) angleDiff += Math.PI * 2;

            mesh.rotation.y += angleDiff * TRUCK_CONFIG.turnSpeed;

            // 가속
            speed += TRUCK_CONFIG.acceleration * inputLength;
            speed = Math.min(speed, TRUCK_CONFIG.maxSpeed);

            // 진행 방향
            const forwardX = Math.sin(mesh.rotation.y);
            const forwardZ = Math.cos(mesh.rotation.y);

            lastDirX = forwardX;
            lastDirZ = forwardZ;

            mesh.position.x += forwardX * speed;
            mesh.position.z += forwardZ * speed;

        } else {
            // 조이스틱을 놓았을 때 관성
            speed *= TRUCK_CONFIG.friction;

            mesh.position.x += lastDirX * speed;
            mesh.position.z += lastDirZ * speed;

            if (speed < 0.001) speed = 0;
        }
    }

    return { mesh, update };
}