import { createWorld } from './modules/world.js';
import { createTruck } from './modules/truck.js';
import { createJoystick } from './modules/input.js';
import { createMonsterSystem } from './modules/monsters.js';
import { createSpawnSystem } from './modules/spawn.js';
import { createCameraController } from './modules/camera.js';
import { createPlayer } from './modules/player.js';
import { createPlayerHUD } from './modules/ui.js';

const container = document.getElementById('game-container');

const world = createWorld(container);
const { scene, camera, renderer } = world;

const player = createPlayer();
const playerHUD = createPlayerHUD();

playerHUD.update(player.getState());

const move = createJoystick(container);
const truck = createTruck(scene);

const monsterSystem = createMonsterSystem(scene, (type) => {
    const expGain = type.exp ?? 0;
    const soulGain = type.soul ?? 0;

    const result = player.addRewards(expGain, soulGain);

    console.log(`경험치 +${expGain}, 영혼 +${soulGain}`);

    playerHUD.update(result.state);
});
const spawnSystem = createSpawnSystem(monsterSystem);

const cameraController = createCameraController(camera, truck.mesh);

// =============================
// 게임 루프
// =============================
function animate() {
    requestAnimationFrame(animate);

    truck.update(move);

    world.update(truck.mesh);

    monsterSystem.update(truck.mesh);
    spawnSystem.update(truck.mesh);

    cameraController.update();

    renderer.render(scene, camera);
}

// =============================
// 게임 초기화
// =============================
async function initGame() {
    try {
        await monsterSystem.loadData();

        // 게임 시작 전에 목표 수만큼 미리 배치
        spawnSystem.fillInitial(truck.mesh);

        animate();

    } catch (error) {
        console.error('게임 초기화 실패:', error);
    }
}

initGame();