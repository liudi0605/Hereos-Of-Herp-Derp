﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkeletonPriest : Enemy
{
    public override void SetBaseProperties()
    {
        base.SetBaseProperties();

        mySpellBook.EnemyLearnAbility("Strike");
        mySpellBook.EnemyLearnAbility("Move");
        mySpellBook.EnemyLearnAbility("Invigorate");
        mySpellBook.EnemyLearnAbility("Healing Light");
        myPassiveManager.ModifyUndead();
        myPassiveManager.ModifyEncouragingAura(10);

    }

    public override IEnumerator StartMyActivationCoroutine()
    {
        Ability strike = mySpellBook.GetAbilityByName("Strike");
        Ability move = mySpellBook.GetAbilityByName("Move");
        Ability invigorate = mySpellBook.GetAbilityByName("Invigorate");
        Ability healingLight = mySpellBook.GetAbilityByName("Healing Light");

        ActionStart:
        while (EventManager.Instance.gameOverEventStarted)
        {
            yield return null;
        }

        if (EntityLogic.IsAbleToTakeActions(this) == false)
        {
            EndMyActivation();
        }
        
        // Invigorate
        else if (EntityLogic.IsTargetInRange(this, GetBestInvigorateTarget(invigorate.abilityRange), invigorate.abilityRange) &&
            EntityLogic.IsAbilityUseable(this, invigorate))
        {
            VisualEffectManager.Instance.CreateStatusEffect(transform.position, "Invigorate");
            yield return new WaitForSeconds(0.5f);

            Action action = AbilityLogic.Instance.PerformInvigorate(this, GetBestInvigorateTarget(invigorate.abilityRange));
            yield return new WaitUntil(() => action.ActionResolved() == true);
            yield return new WaitForSeconds(1f);
            goto ActionStart;
        }

        // Healing Light
        else if (EntityLogic.IsTargetInRange(this, GetBestHealingLightTarget(), healingLight.abilityRange) &&
            GetBestHealingLightTarget().currentHealth < GetBestHealingLightTarget().currentMaxHealth &&
            EntityLogic.IsAbilityUseable(this, healingLight))
        {
            VisualEffectManager.Instance.CreateStatusEffect(transform.position, "Healing Light");
            yield return new WaitForSeconds(0.5f);

            Action action = AbilityLogic.Instance.PerformHealingLight(this, GetBestHealingLightTarget());
            yield return new WaitUntil(() => action.ActionResolved() == true);
            yield return new WaitForSeconds(1f);
            goto ActionStart;
        }        

        // Move towards an ally to give Encouraging presence bonus
        else if (EntityLogic.IsTargetInRange(this, EntityLogic.GetClosestAlly(this, false), EntityLogic.GetTotalMobility(this)) == false &&
            EntityLogic.IsAbleToMove(this) &&
            EntityLogic.IsAbilityUseable(this,move) &&
            EntityLogic.GetBestValidMoveLocationBetweenMeAndTarget(this, EntityLogic.GetClosestAlly(this, false), 1, EntityLogic.GetTotalMobility(this)) != null
            )
        {            
            VisualEffectManager.Instance.CreateStatusEffect(transform.position, "Move");
            yield return new WaitForSeconds(0.5f);
            Tile destination = EntityLogic.GetBestValidMoveLocationBetweenMeAndTarget(this, EntityLogic.GetClosestAlly(this, false), 1, EntityLogic.GetTotalMobility(this));
            Action movementAction = AbilityLogic.Instance.PerformMove(this, destination);
            

            // small delay here in order to seperate the two actions a bit.
            yield return new WaitForSeconds(1f);
            goto ActionStart;
        }

        // Strike
        else if (EntityLogic.IsTargetInRange(this, EntityLogic.GetClosestValidEnemy(this), currentMeleeRange) &&
            EntityLogic.IsAbilityUseable(this, strike))
        {
            SetTargetDefender(EntityLogic.GetClosestValidEnemy(this));
            VisualEffectManager.Instance.CreateStatusEffect(transform.position, "Strike");
            yield return new WaitForSeconds(0.5f);

            AbilityLogic.Instance.PerformStrike(this, myCurrentTarget);

            yield return new WaitForSeconds(1f);
            goto ActionStart;
        }

        EndMyActivation();

    }

    public Enemy GetBestInvigorateTarget(int range)
    {
        Enemy bestTarget = null;

        foreach(Enemy enemy in EnemyManager.Instance.allEnemies)
        {
            if(enemy.currentEnergy < enemy.currentMaxEnergy &&
                EntityLogic.IsTargetInRange(this, enemy, range) &&
                enemy != this)
            {
                bestTarget = enemy;
            }
        }

        if(bestTarget == null)
        {
            Debug.Log("GetBestInvigorateTarget() is null, returning caster as best target");
            bestTarget = this;
        }
        Debug.Log("GetBestInvigorateTarget() return character: " + bestTarget.name);
        return bestTarget;
    }       

    public Enemy GetBestHealingLightTarget()
    {
        Enemy bestTarget = null;
        int lowestHPvalue = 1000;

        foreach (Enemy enemy in EnemyManager.Instance.allEnemies)
        {
            if (enemy.currentHealth < lowestHPvalue &&
                enemy.currentHealth < enemy.currentMaxHealth)
            {
                bestTarget = enemy;
                lowestHPvalue = enemy.currentHealth;
            }
        }

        if(bestTarget == null)
        {
            Debug.Log("GetBestHealingLightTarget() is null, returning caster as best target");
            bestTarget = this;
        }

        return bestTarget;
    }
}
