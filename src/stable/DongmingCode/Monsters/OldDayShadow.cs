using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using SnowfallenMod.DongmingCode.Powers;

namespace SnowfallenMod.DongmingCode.Monsters;

public sealed class OldDayShadow : MonsterModel
{
	public override int MinInitialHp => 1;

	public override int MaxInitialHp => 1;

	public override bool ShouldDisappearFromDoom => false;

	protected override string VisualsPath =>
		SceneHelper.GetScenePath("creature_visuals/necrobinder");

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState idle = new("OLD_DAY_SHADOW_IDLE", IdleMove, new HiddenIntent());
		idle.FollowUpState = idle;
		return new MonsterMoveStateMachine([idle], idle);
	}

	private static async Task IdleMove(IReadOnlyList<Creature> creatures)
	{
		foreach (Creature creature in creatures)
		{
			ZhouFuShadowPower power = creature.GetPower<ZhouFuShadowPower>();
			if (power is not null)
			{
				await power.LoseLifeForAction();
			}
		}
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		AnimState idle = new("idle_loop", isLooping: true);
		AnimState cast = new("cast") { NextState = idle };
		AnimState attack = new("attack") { NextState = idle };
		AnimState hurt = new("hurt") { NextState = idle };
		AnimState death = new("die");
		CreatureAnimator animator = new(idle, controller);
		animator.AddAnyState("Idle", idle);
		animator.AddAnyState("Cast", cast);
		animator.AddAnyState("Attack", attack);
		animator.AddAnyState("Hit", hurt);
		animator.AddAnyState("Dead", death);
		return animator;
	}
}
