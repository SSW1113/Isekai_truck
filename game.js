import * as THREE from 'https://cdn.jsdelivr.net/npm/three@0.179/build/three.module.js';

// =============================
// 세로형 게임 컨테이너
// =============================
const container = document.getElementById('game-container');

// =============================W
// Three.js 기본 설정
// =============================
const scene = new THREE.Scene();
scene.background = new THREE.Color(0x87ceeb);

const camera = new THREE.PerspectiveCamera(
    75,
    container.clientWidth / container.clientHeight,
    0.1,
    1000
);

const renderer = new THREE.WebGLRenderer({ antialias: true });
renderer.setSize(container.clientWidth, container.clientHeight);
renderer.setPixelRatio(window.devicePixelRatio);
container.appendChild(renderer.domElement);

// =============================
// 바닥
// =============================
const ground = new THREE.Mesh(
    new THREE.PlaneGeometry(50, 50),
    new THREE.MeshPhongMaterial({ color: 0x3a7a2a })
);
ground.rotation.x = -Math.PI / 2;
scene.add(ground);

// =============================
// 트럭 (임시 모델)
// =============================
const truck = new THREE.Mesh(
    new THREE.BoxGeometry(1.5, 1, 3),
    new THREE.MeshPhongMaterial({ color: 0x3366ff })
);
truck.position.y = 0.5;
scene.add(truck);

// =============================
// 몬스터 생성
// =============================
const monsters = [];

function spawnMonster() {
    const monster = new THREE.Mesh(
        new THREE.SphereGeometry(0.6, 16, 16),
        new THREE.MeshPhongMaterial({ color: 0xff4444 })
    );

    monster.position.set(
        (Math.random() - 0.5) * 30,
        0.6,
        (Math.random() - 0.5) * 30
    );

    scene.add(monster);
    monsters.push(monster);
}

for (let i = 0; i < 10; i++) {
    spawnMonster();
}

// =============================
// 조명
// =============================
const dirLight = new THREE.DirectionalLight(0xffffff, 1);
dirLight.position.set(5, 10, 5);
scene.add(dirLight);

const ambient = new THREE.AmbientLight(0xffffff, 0.4);
scene.add(ambient);

// =============================
// 카메라 설정
// =============================
camera.position.set(0, 18, 0);
camera.lookAt(0, 0, 0);

// =============================
// 가상 조이스틱
// =============================
const joystick = document.getElementById('joystick');
const stick = document.getElementById('stick');

let dragging = false;
let startX = 0;
let startY = 0;

// 입력값
const move = { x: 0, z: 0 };

// =============================
// 트럭 물리값
// =============================
let speed = 0;

const maxSpeed = 0.18;
const friction = 0.90;
// =============================
// 조이스틱 생성
// =============================
container.addEventListener('pointerdown', (e) => {
    dragging = true;

    const rect = container.getBoundingClientRect();

    startX = e.clientX - rect.left;
    startY = e.clientY - rect.top;

    joystick.style.display = 'block';
    joystick.style.left = `${startX}px`;
    joystick.style.top = `${startY}px`;
});

// =============================
// 조이스틱 드래그
// =============================
container.addEventListener('pointermove', (e) => {
    if (!dragging) return;

    const rect = container.getBoundingClientRect();

    let dx = (e.clientX - rect.left) - startX;
    let dy = (e.clientY - rect.top) - startY;

    const dist = Math.hypot(dx, dy);
    const max = 40;

    // 조이스틱 범위 제한
    if (dist > max) {
        dx = (dx / dist) * max;
        dy = (dy / dist) * max;
    }

    stick.style.left = `${35 + dx}px`;
    stick.style.top = `${35 + dy}px`;

    // 조이스틱 방향 벡터
    move.x = dx / max;
    move.z = dy / max;
});

// =============================
// 조이스틱 해제
// =============================
window.addEventListener('pointerup', () => {
    dragging = false;

    joystick.style.display = 'none';

    stick.style.left = '35px';
    stick.style.top = '35px';

    move.x = 0;
    move.z = 0;
});

// =============================
// 게임 루프
// =============================
function animate() {
    requestAnimationFrame(animate);

    // ============================
    // 조이스틱 기반 이동
    // ============================

    // 입력 세기
    const inputLength = Math.hypot(move.x, move.z);

    if (inputLength > 0.05) {
        // 조이스틱 방향 정규화
        const dirX = move.x / inputLength;
        const dirZ = move.z / inputLength;

        // 목표 속도
        const targetSpeed = maxSpeed * inputLength;

        // 부드러운 가속
        speed += (targetSpeed - speed) * 0.12;

        // 이동
        truck.position.x += dirX * speed;
        truck.position.z += dirZ * speed;

        // 트럭이 이동 방향을 바라보게 회전
        const targetRotation = Math.atan2(dirX, dirZ);

        // 부드러운 회전 보간
        let angleDiff = targetRotation - truck.rotation.y;

        while (angleDiff > Math.PI) angleDiff -= Math.PI * 2;
        while (angleDiff < -Math.PI) angleDiff += Math.PI * 2;

        truck.rotation.y += angleDiff * 0.18;

        // ============================
        // 약한 드리프트 효과
        // ============================

        const driftStrength = 0.06;

        truck.position.x += Math.cos(truck.rotation.y) * speed * driftStrength;
        truck.position.z -= Math.sin(truck.rotation.y) * speed * driftStrength;
    } else {
        // 입력 없으면 자연 감속
        speed *= friction;
    }

    // ============================
    // 맵 경계 제한
    // ============================

    const mapLimit = 24;

    truck.position.x = Math.max(-mapLimit, Math.min(mapLimit, truck.position.x));
    truck.position.z = Math.max(-mapLimit, Math.min(mapLimit, truck.position.z));

    // ============================
    // 몬스터 충돌 체크
    // ============================

    for (let i = monsters.length - 1; i >= 0; i--) {
        const monster = monsters[i];

        const distance = truck.position.distanceTo(monster.position);

        if (distance < 1.8) {
            scene.remove(monster);
            monsters.splice(i, 1);

            spawnMonster();

            console.log('몬스터 처치!');
        }
    }

    // ============================
    // 렌더링
    // ============================

    renderer.render(scene, camera);
}

animate();

// =============================
// 반응형 처리
// =============================
window.addEventListener('resize', () => {
    const width = container.clientWidth;
    const height = container.clientHeight;

    camera.aspect = width / height;
    camera.updateProjectionMatrix();

    renderer.setSize(width, height);
});
