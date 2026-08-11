// =============================
// 트럭 설정
// =============================
export const TRUCK_CONFIG = {
    baseMaxSpeed: 0.12,
    acceleration: 0.001,
    friction: 0.94,
    turnSpeed: 0.03,

    speedPerUpgrade: 0.01,
    sizePerUpgrade: 0.12
};

export const PLAYER_CONFIG = {
    startLevel: 1,
    startExp: 0,
    startSoul: 0,

    baseRequiredExp: 100,
    expGrowth: 1.5,

    upgradePointPerLevel: 1
};

// =============================
// 월드 설정
// =============================
export const WORLD_CONFIG = {
    tileSize: 50,
    tileRadius: 2,       // 중심 기준 좌우 2칸 = 5x5

    fogColor: 0x87ceeb,
    fogNear: 55,
    fogFar: 90
};

// =============================
// 카메라 설정
// =============================
export const CAMERA_CONFIG = {
    x: 0,
    y: 20,
    z: 12,
    followSpeed: 0.08
};

// =============================
// 몬스터 설정
// =============================
export const MONSTER_CONFIG = {
    collisionDistance: 1.8,
};

// =============================
// 몬스터 스폰 설정
// =============================
export const SPAWN_CONFIG = {
    targetCount: 100,

    minDistance: 35,
    maxDistance: 70,
    despawnDistance: 80,

    spawnInterval: 100,
    spawnPerInterval: 1
};