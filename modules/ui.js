export function createPlayerHUD() {
    const levelText = document.getElementById('level-text');
    const expText = document.getElementById('exp-text');
    const expFill = document.getElementById('exp-fill');
    const soulText = document.getElementById('soul-text');

    function update(state) {
        const expPercent = (state.exp / state.requiredExp) * 100;

        levelText.textContent = `Lv. ${state.level}`;
        expText.textContent = `EXP ${state.exp} / ${state.requiredExp}`;
        soulText.textContent = `영혼 ${state.soul}`;

        expFill.style.width = `${Math.min(expPercent, 100)}%`;
    }

    return { update };
}