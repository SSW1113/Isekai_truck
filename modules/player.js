import { PLAYER_CONFIG } from './config.js';

export function createPlayer() {
    let level = PLAYER_CONFIG.startLevel;
    let exp = PLAYER_CONFIG.startExp;
    let soul = PLAYER_CONFIG.startSoul;
    let upgradePoints = 0;

    function getRequiredExp() {
        return Math.round(
            PLAYER_CONFIG.baseRequiredExp * Math.pow(level, PLAYER_CONFIG.expGrowth)
        );
    }

    function addRewards(expGain = 0, soulGain = 0) {
        exp += expGain;
        soul += soulGain;

        let levelUpCount = 0;

        while (exp >= getRequiredExp()) {
            exp -= getRequiredExp();
            level++;
            levelUpCount++;
        }

        if (levelUpCount > 0) {
            const gainedPoints = levelUpCount * PLAYER_CONFIG.upgradePointPerLevel;
            upgradePoints += gainedPoints;

            console.log(`레벨 업! Lv.${level} / 업그레이드 포인트 +${gainedPoints}`);
        }

        return { levelUpCount, state: getState() };
    }

    function spendUpgradePoint() {
        if (upgradePoints <= 0) return false;

        upgradePoints--;
        return true;
    }

    function getState() {
        return {
            level,
            exp,
            requiredExp: getRequiredExp(),
            soul,
            upgradePoints
        };
    }

    return { addRewards, spendUpgradePoint, getState };
}