import { SPAWN_CONFIG } from './config.js';

export function createSpawnSystem(monsterSystem) {
    let lastSpawnTime = 0;

    // =============================
    // 랜덤 몬스터 종류 선택
    // =============================
    function chooseMonsterType() {
        const types = monsterSystem.getTypes();
        const entries = Object.entries(types);

        let totalWeight = 0;

        for (const [, type] of entries) {
            totalWeight += type.spawnWeight ?? 1;
        }

        let random = Math.random() * totalWeight;

        for (const [typeId, type] of entries) {
            random -= type.spawnWeight ?? 1;

            if (random <= 0) {
                return typeId;
            }
        }

        return entries[0][0];
    }

    // =============================
    // 트럭 주변 랜덤 위치
    // =============================
    function getSpawnPosition(truck) {
        const angle = Math.random() * Math.PI * 2;

        const distance =
            SPAWN_CONFIG.minDistance +
            Math.random() * (SPAWN_CONFIG.maxDistance - SPAWN_CONFIG.minDistance);

        return {
            x: truck.position.x + Math.cos(angle) * distance,
            z: truck.position.z + Math.sin(angle) * distance
        };
    }

    // =============================
    // 몬스터 한 마리 생성
    // =============================
    function spawnOne(truck) {
        const typeId = chooseMonsterType();
        const position = getSpawnPosition(truck);

        monsterSystem.spawn(typeId, position.x, position.z);
    }

    // =============================
    // 너무 멀어진 몬스터 제거
    // =============================
    function removeFarMonsters(truck) {
        const monsters = [...monsterSystem.getMonsters()];

        for (const monster of monsters) {
            const dx = monster.mesh.position.x - truck.position.x;
            const dz = monster.mesh.position.z - truck.position.z;
            const distance = Math.hypot(dx, dz);

            if (distance > SPAWN_CONFIG.despawnDistance) {
                monsterSystem.remove(monster);
            }
        }
    }

    // =============================
    // 게임 시작 시 초기 배치
    // =============================
    function fillInitial(truck) {
        while (monsterSystem.getMonsters().length < SPAWN_CONFIG.targetCount) {
            spawnOne(truck);
        }
    }

    // =============================
    // 실시간 스폰 업데이트
    // =============================
    function update(truck) {
        removeFarMonsters(truck);

        const now = performance.now();

        if (now - lastSpawnTime < SPAWN_CONFIG.spawnInterval) return;
        lastSpawnTime = now;

        const currentCount = monsterSystem.getMonsters().length;

        if (currentCount >= SPAWN_CONFIG.targetCount) return;

        const needCount = SPAWN_CONFIG.targetCount - currentCount;
        const spawnCount = Math.min(needCount, SPAWN_CONFIG.spawnPerInterval);

        for (let i = 0; i < spawnCount; i++) {
            spawnOne(truck);
        }
    }

    return {
        fillInitial,
        update
    };
}