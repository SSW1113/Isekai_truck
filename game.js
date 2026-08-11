import { createWorld } from './modules/world.js';
import { createTruck } from './modules/truck.js';
import { createJoystick } from './modules/input.js';
import { createMonsterSystem } from './modules/monsters.js';
import { createSpawnSystem } from './modules/spawn.js';
import { createCameraController } from './modules/camera.js';
import { createPlayer } from './modules/player.js';
import { createPlayerHUD, createUpgradeUI } from './modules/ui.js';

const container = document.getElementById('game-container');

// 월드
const world = createWorld(container);
const { scene, camera, renderer } = world;

// 입력
const move = createJoystick(container);

// 트럭
const truck = createTruck(scene);

// 플레이어
const player = createPlayer();

// UI
const playerHUD = createPlayerHUD();
playerHUD.update(player.getState());

const upgradeUI = createUpgradeUI(player, truck, () => {
    playerHUD.update(player.getState());
});

// 몬스터
const monsterSystem = createMonsterSystem(scene, (type) => {
    const expGain = type.exp ?? 0;
    const soulGain = type.soul ?? 0;

    const result = player.addRewards(expGain, soulGain);

    console.log(`경험치 +${expGain}, 영혼 +${soulGain}`);

    playerHUD.update(result.state);
    upgradeUI.update();
});

// 스폰
const spawnSystem = createSpawnSystem(monsterSystem);

// 카메라
const cameraController = createCameraController(camera, truck.mesh);

// 게임 루프
function animate() {
    requestAnimationFrame(animate);

    if (!upgradeUI.isOpen()) {
        truck.update(move);

        // 카메라 줌 상태 계산
        const zoomMultiplier = cameraController.update();

        // 줌에 맞춰 Fog와 타일 범위 조절
        world.update(
            truck.mesh,
            camera,
            zoomMultiplier
        );

        monsterSystem.update(truck.mesh);
        spawnSystem.update(truck.mesh);
    }

    renderer.render(scene, camera);
}

// 게임 초기화
async function initGame() {
    try {
        await monsterSystem.loadData();

        spawnSystem.fillInitial(truck.mesh);

        animate();
    } catch (error) {
        console.error('게임 초기화 실패:', error);
    }
}

initGame();