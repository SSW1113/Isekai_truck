// 플레이어 HUD
export function createPlayerHUD() {
    const levelText = document.getElementById('level-text');
    const expText = document.getElementById('exp-text');
    const expFill = document.getElementById('exp-fill');
    const soulText = document.getElementById('soul-text');
    const pointText = document.getElementById('point-text');

    function update(state) {
        const expPercent =
            (state.exp / state.requiredExp) * 100;

        levelText.textContent = `Lv. ${state.level}`;
        expText.textContent =
            `EXP ${state.exp} / ${state.requiredExp}`;

        soulText.textContent = `영혼 ${state.soul}`;
        pointText.textContent =
            `포인트 ${state.upgradePoints}`;

        expFill.style.width =
            `${Math.min(expPercent, 100)}%`;
    }

    return { update };
}


// 트럭 업그레이드 UI
export function createUpgradeUI(player, truck, onChange) {
    const panel = document.getElementById('upgrade-panel');

    const openButton = document.getElementById('upgrade-open');
    const closeButton = document.getElementById('upgrade-close');

    const pointText = document.getElementById('upgrade-point-text');

    const speedButton = document.getElementById('speed-upgrade');
    const sizeButton = document.getElementById('size-upgrade');

    const speedLevel = document.getElementById('speed-level');
    const sizeLevel = document.getElementById('size-level');

    const speedStat = document.getElementById('speed-stat');
    const sizeStat = document.getElementById('size-stat');

    // 업그레이드 정보 갱신
    function update() {
        const playerState = player.getState();
        const truckStats = truck.getStats();

        pointText.textContent =
            `남은 포인트: ${playerState.upgradePoints}`;

        speedLevel.textContent =
            `Lv.${truckStats.speedLevel}`;

        sizeLevel.textContent =
            `Lv.${truckStats.sizeLevel}`;

        speedStat.textContent =
            `최대 속도: ${truckStats.maxSpeed.toFixed(3)}`;

        sizeStat.textContent =
            `트럭 크기: ${Math.round(truckStats.sizeScale * 100)}%`;

        const disabled = playerState.upgradePoints <= 0;

        speedButton.disabled = disabled;
        sizeButton.disabled = disabled;
    }

    // 팝업 열기
    function open() {
        update();
        panel.classList.add('open');
    }

    // 팝업 닫기
    function close() {
        panel.classList.remove('open');
    }

    // 속도 업그레이드
    speedButton.addEventListener('click', () => {
        if (!player.spendUpgradePoint()) return;

        truck.upgradeSpeed();

        update();
        onChange();
    });

    // 크기 업그레이드
    sizeButton.addEventListener('click', () => {
        if (!player.spendUpgradePoint()) return;

        truck.upgradeSize();

        update();
        onChange();
    });

    openButton.addEventListener('click', open);
    closeButton.addEventListener('click', close);

    function isOpen() {
        return panel.classList.contains('open');
    }

    return {
        update,
        isOpen
    };
}