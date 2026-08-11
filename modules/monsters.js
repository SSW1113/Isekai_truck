import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.179/build/three.module.js';
import { MONSTER_CONFIG } from './config.js';

export function createMonsterSystem(scene, onDefeat) {
    let monsterTypes = {};
    const monsters = [];

    // =============================
    // JSON 로딩
    // =============================
    async function loadData() {
        const response = await fetch('./data/monsters.json');

        if (!response.ok) {
            throw new Error(`몬스터 데이터를 불러오지 못했습니다: ${response.status}`);
        }

        monsterTypes = await response.json();
        console.log('몬스터 데이터 로딩 완료:', monsterTypes);
    }

    // =============================
    // 몬스터 생성
    // =============================
    function spawn(typeId, x, z) {
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

        const monster = {
            typeId,
            mesh,

            wanderAngle: Math.random() * Math.PI * 2,
            nextWanderChange: performance.now() + 1000 + Math.random() * 2000
        };

        monsters.push(monster);

        return monster;
    }

    // =============================
    // 몬스터 제거
    // =============================
    function remove(monster) {
        const index = monsters.indexOf(monster);
        if (index === -1) return;

        scene.remove(monster.mesh);
        monsters.splice(index, 1);
    }

    // 몬스터 AI
    function updateAI(truck) {
        const truckScale = truck.userData.sizeScale ?? 1;

        // 트럭 크기에 따른 추가 인식 거리
        const extraFleeDistance =
            MONSTER_CONFIG.collisionDistance * (truckScale - 1);

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
                const dirX = dx / distance;
                const dirZ = dz / distance;

                mesh.position.x += dirX * type.speed;
                mesh.position.z += dirZ * type.speed;

                continue;
            }

            // 일정 시간마다 배회 방향 변경
            if (now >= monster.nextWanderChange) {
                monster.wanderAngle = Math.random() * Math.PI * 2;
                monster.nextWanderChange = now + 1500 + Math.random() * 2000;
            }

            // 평상시에는 천천히 이동 --> 나중에 json으로 빼도 됨
            const wanderSpeed = type.speed * 0.2;

            mesh.position.x += Math.cos(monster.wanderAngle) * wanderSpeed;
            mesh.position.z += Math.sin(monster.wanderAngle) * wanderSpeed;
        }
    }

    // =============================
    // 트럭 충돌
    // =============================
    function checkCollisions(truck) {
        for (let i = monsters.length - 1; i >= 0; i--) {
            const monster = monsters[i];
            const distance = truck.position.distanceTo(monster.mesh.position);

            const truckScale = truck.userData.sizeScale ?? 1;
            const collisionDistance = MONSTER_CONFIG.collisionDistance * truckScale;

            if (distance < collisionDistance) {
                const type = monsterTypes[monster.typeId];

                scene.remove(monster.mesh);
                monsters.splice(i, 1);

                console.log(`${type.name} 처치!`);

                if (onDefeat) {
                    onDefeat(type);
                }
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
        spawn,
        remove,
        update,
        getMonsters,
        getTypes
    };
}