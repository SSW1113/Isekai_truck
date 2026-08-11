import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.179/build/three.module.js';
import { TRUCK_CONFIG } from './config.js';

export function createTruck(scene) {
    const mesh = new THREE.Mesh(
        new THREE.BoxGeometry(1.5, 1, 3),
        new THREE.MeshPhongMaterial({ color: 0x3366ff })
    );

    mesh.position.y = 0.5;
    mesh.userData.sizeScale = 1;

    scene.add(mesh);

    let speed = 0;
    let lastDirX = 0, lastDirZ = 0;

    let speedLevel = 0;
    let sizeLevel = 0;

    let maxSpeed = TRUCK_CONFIG.baseMaxSpeed;

    // 트럭 이동
    function update(move) {
        const inputLength = Math.hypot(move.x, move.z);

        if (inputLength > 0.05) {
            const dirX = move.x / inputLength;
            const dirZ = move.z / inputLength;

            // 목표 방향으로 천천히 회전
            const targetRotation = Math.atan2(dirX, dirZ);
            let angleDiff = targetRotation - mesh.rotation.y;

            while (angleDiff > Math.PI) angleDiff -= Math.PI * 2;
            while (angleDiff < -Math.PI) angleDiff += Math.PI * 2;

            mesh.rotation.y += angleDiff * TRUCK_CONFIG.turnSpeed;

            // 가속
            speed += TRUCK_CONFIG.acceleration * inputLength;
            speed = Math.min(speed, maxSpeed);

            // 트럭이 바라보는 방향으로 이동
            const forwardX = Math.sin(mesh.rotation.y);
            const forwardZ = Math.cos(mesh.rotation.y);

            lastDirX = forwardX;
            lastDirZ = forwardZ;

            mesh.position.x += forwardX * speed;
            mesh.position.z += forwardZ * speed;
        } else {
            // 조이스틱을 놓으면 관성
            speed *= TRUCK_CONFIG.friction;

            mesh.position.x += lastDirX * speed;
            mesh.position.z += lastDirZ * speed;

            if (speed < 0.001) speed = 0;
        }
    }

    // 속도 업그레이드
    function upgradeSpeed() {
        speedLevel++;

        maxSpeed =
            TRUCK_CONFIG.baseMaxSpeed +
            speedLevel * TRUCK_CONFIG.speedPerUpgrade;
    }

    // 크기 업그레이드
    function upgradeSize() {
        sizeLevel++;

        const scale =
            1 + sizeLevel * TRUCK_CONFIG.sizePerUpgrade;

        mesh.scale.setScalar(scale);
        mesh.position.y = 0.5 * scale;

        mesh.userData.sizeScale = scale;
    }

    function getStats() {
        return {
            speedLevel,
            sizeLevel,
            maxSpeed,
            sizeScale: mesh.userData.sizeScale
        };
    }

    return {
        mesh,
        update,
        upgradeSpeed,
        upgradeSize,
        getStats
    };
}