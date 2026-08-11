import { PLAYER_CONFIG } from './config.js';

export function createPlayer() {
    let level = PLAYER_CONFIG.startLevel;
    let exp = PLAYER_CONFIG.startExp;
    let soul = PLAYER_CONFIG.startSoul;

    // 현재 레벨에서 다음 레벨까지 필요한 경험치
    function getRequiredExp() {
    return Math.round(
        PLAYER_CONFIG.baseRequiredExp * Math.pow(level, PLAYER_CONFIG.expGrowth)
    );
}

    // 경험치 + 영혼 획득
    function addRewards(expGain = 0, soulGain = 0) {
        exp += expGain;
        soul += soulGain;

        let levelUpCount = 0;

        // 한 번에 많은 경험치를 받아 여러 번 레벨업하는 경우도 처리
        while (exp >= getRequiredExp()) {
            const requiredExp = getRequiredExp();

            exp -= requiredExp;
            level++;
            levelUpCount++;
        }

        if (levelUpCount > 0) {
            console.log(`레벨 업! 현재 레벨: ${level}`);
        }

        return {
            levelUpCount,
            state: getState()
        };
    }

    function getState() {
        return {
            level,
            exp,
            requiredExp: getRequiredExp(),
            soul
        };
    }

    return {
        addRewards,
        getState
    };
}