export function createJoystick(container) {
    const joystick = document.getElementById('joystick');
    const stick = document.getElementById('stick');

    let dragging = false;
    let startX = 0;
    let startY = 0;

    const move = { x: 0, z: 0 };

    // 조이스틱 시작
    container.addEventListener('pointerdown', (e) => {
        if (e.target.closest('[data-game-ui]')) return;

        dragging = true;

        const rect = container.getBoundingClientRect();
        startX = e.clientX - rect.left;
        startY = e.clientY - rect.top;

        joystick.style.display = 'block';
        joystick.style.left = `${startX}px`;
        joystick.style.top = `${startY}px`;
    });

    // 조이스틱 이동
    container.addEventListener('pointermove', (e) => {
        if (!dragging) return;

        const rect = container.getBoundingClientRect();

        let dx = (e.clientX - rect.left) - startX;
        let dy = (e.clientY - rect.top) - startY;

        const dist = Math.hypot(dx, dy);
        const max = 40;

        if (dist > max) {
            dx = (dx / dist) * max;
            dy = (dy / dist) * max;
        }

        stick.style.left = `${35 + dx}px`;
        stick.style.top = `${35 + dy}px`;

        move.x = dx / max;
        move.z = dy / max;
    });

    // 조이스틱 해제
    window.addEventListener('pointerup', () => {
        dragging = false;

        joystick.style.display = 'none';
        stick.style.left = '35px';
        stick.style.top = '35px';

        move.x = 0;
        move.z = 0;
    });

    return move;
}