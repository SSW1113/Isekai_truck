import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.179/build/three.module.js';
import { MONSTER_CONFIG } from './config.js';

export function createMonsterSystem(scene, onDefeat) {
    let monsterTypes = {};
    const monsters = [];

    // 몬스터 데이터 로딩
    async function loadData() {
        const response = await fetch('./data/monsters.json');

        if (!response.ok) {
            throw new Error(`몬스터 데이터를 불러오지 못했습니다: ${response.status}`);
        }

        monsterTypes = await response.json();

        console.log('몬스터 데이터 로딩 완료:', monsterTypes);
    }

    // 몬스터 생성
    function createMonster(typeId, x, z) {
        const type = monsterTypes[typeId];

        if (!type) {
            console.error(`존재하지 않는 몬스터 타입: ${typeId}`);
            return;
        }

        const mesh = new THREE.Mesh(
            new THREE.SphereGeometry(type.size, 16, 16),
            new THREE.MeshPhongMaterial({ color: type.color })
        );

        mesh.position.set(x, type.size, z);
        scene.add(mesh);

        // 몬스터 상태
        const monster = {
            typeId,
            mesh,

            wanderAngle: Math.random() * Math.PI * 2,
            nextWanderChange: performance.now() + 1000 + Math.random() * 2000,

            fleeDirX: 0,
            fleeDirZ: 0,
            hasFleeDirection: false
        };

        monsters.push(monster);

        return monster;
    }

    // 몬스터 제거
    function remove(monster) {
        const index = monsters.indexOf(monster);

        if (index === -1) return;

        scene.remove(monster.mesh);
        monsters.splice(index, 1);
    }

    // 몬스터 AI
    function updateAI(truck) {
        const truckScale = Math.max(truck.scale.x, truck.scale.z);

        // 트럭 크기에 따른 추가 인식 거리
        const extraFleeDistance =
            MONSTER_CONFIG.collisionDistance * (truckScale - 1);

        const collisionDistance =
            MONSTER_CONFIG.collisionDistance * truckScale;

        const directionLockDistance =
            collisionDistance * MONSTER_CONFIG.directionLockMultiplier;

        const now = performance.now();

        for (const monster of monsters) {
            const mesh = monster.mesh;
            const type = monsterTypes[monster.typeId];

            const dx = mesh.position.x - truck.position.x;
            const dz = mesh.position.z - truck.position.z;
            const distance = Math.hypot(dx, dz);

            const fleeDistance = type.fleeDistance + extraFleeDistance;

            // 트럭을 인식하면 도망
            if (distance < fleeDistance && distance > 0.001) {

                // 충분히 멀면 계속 도망 방향 갱신
                if (distance > directionLockDistance || !monster.hasFleeDirection) {
                    monster.fleeDirX = dx / distance;
                    monster.fleeDirZ = dz / distance;
                    monster.hasFleeDirection = true;
                }

                // 가까우면 마지막 도망 방향 유지
                mesh.position.x += monster.fleeDirX * type.speed;
                mesh.position.z += monster.fleeDirZ * type.speed;

                continue;
            }

            monster.hasFleeDirection = false;

            // 배회 방향 변경
            if (now >= monster.nextWanderChange) {
                monster.wanderAngle = Math.random() * Math.PI * 2;
                monster.nextWanderChange = now + 1500 + Math.random() * 2000;
            }

            // 평상시 이동
            const wanderSpeed = type.speed * 0.2;

            mesh.position.x += Math.cos(monster.wanderAngle) * wanderSpeed;
            mesh.position.z += Math.sin(monster.wanderAngle) * wanderSpeed;
        }
    }

    // 트럭과 몬스터 충돌
    function checkCollisions(truck) {
        const truckScale = Math.max(truck.scale.x, truck.scale.z);

        const collisionDistance =
            MONSTER_CONFIG.collisionDistance * truckScale;

        for (let i = monsters.length - 1; i >= 0; i--) {
            const monster = monsters[i];

            const dx = monster.mesh.position.x - truck.position.x;
            const dz = monster.mesh.position.z - truck.position.z;
            const distance = Math.hypot(dx, dz);

            if (distance < collisionDistance) {
                const type = monsterTypes[monster.typeId];

                scene.remove(monster.mesh);
                monsters.splice(i, 1);

                console.log(`${type.name} 처치!`);

                if (onDefeat) onDefeat(type);
            }
        }
    }

    function update(truck) {
        updateAI(truck);
        checkCollisions(truck);
    }

    function getMonsters() {
        return monsters;
    }

    function getTypes() {
        return monsterTypes;
    }

    return {
        loadData,
        createMonster,
        remove,
        update,
        getMonsters,
        getTypes
    };
}