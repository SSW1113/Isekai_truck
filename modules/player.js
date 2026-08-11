import { PLAYER_CONFIG } from './config.js';

export function createPlayer() {
    let level = PLAYER_CONFIG.startLevel;
    let exp = PLAYER_CONFIG.startExp;
    let soul = PLAYER_CONFIG.startSoul;
    let upgradePoints = 0;

    // 다음 레벨 필요 경험치
    function getRequiredExp() {
        return Math.round(
            PLAYER_CONFIG.baseRequiredExp *
            Math.pow(level, PLAYER_CONFIG.expGrowth)
        );
    }

    // 경험치와 영혼 획득
    function addRewards(expGain = 0, soulGain = 0) {
        exp += expGain;
        soul += soulGain;

        let levelUpCount = 0;

        // 여러 레벨이 한 번에 오르는 경우 처리
        while (exp >= getRequiredExp()) {
            const requiredExp = getRequiredExp();

            exp -= requiredExp;
            level++;
            levelUpCount++;
        }

        // 레벨업 포인트 지급
        if (levelUpCount > 0) {
            const gainedPoints =
                levelUpCount * PLAYER_CONFIG.upgradePointPerLevel;

            upgradePoints += gainedPoints;

            console.log(
                `레벨 업! Lv.${level} / 업그레이드 포인트 +${gainedPoints}`
            );
        }

        return {
            levelUpCount,
            state: getState()
        };
    }

    // 업그레이드 포인트 사용
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

    return {
        addRewards,
        spendUpgradePoint,
        getState
    };
}