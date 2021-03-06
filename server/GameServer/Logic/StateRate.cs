﻿using System;
using GameServer.Interface;

namespace GameServer.Logic
{
	
	public class StateRate
	{
		
		public static double GetStateDingShengRate(IObject self, IObject obj, double selfBaseRate, double objBaseRate)
		{
			double dSelfRealRate;
			if (self is GameClient)
			{
				dSelfRealRate = selfBaseRate + RoleAlgorithm.GetRoleStateDingSheng(self as GameClient, selfBaseRate);
			}
			else if (self is Monster)
			{
				dSelfRealRate = selfBaseRate + 0.1 * (double)(self as Monster).MonsterInfo.ChangeLifeCount;
			}
			else
			{
				dSelfRealRate = selfBaseRate;
			}
			double dObjRealRate;
			if (obj is GameClient)
			{
				dObjRealRate = objBaseRate + RoleAlgorithm.GetRoleStateDingSheng(obj as GameClient, objBaseRate);
			}
			else if (obj is Monster)
			{
				dObjRealRate = objBaseRate + 0.1 * (double)(obj as Monster).MonsterInfo.ChangeLifeCount;
			}
			else if (obj is FakeRoleItem)
			{
				dObjRealRate = selfBaseRate + 0.1 * (double)(obj as FakeRoleItem).GetFakeRoleData().MyRoleDataMini.ChangeLifeCount;
			}
			else
			{
				dObjRealRate = 0.0;
			}
			return dSelfRealRate - dObjRealRate;
		}

		
		public static double GetStateMoveSpeed(IObject self, IObject obj, double selfBaseRate, double objBaseRate)
		{
			double dSelfRealRate;
			if (self is GameClient)
			{
				dSelfRealRate = selfBaseRate + RoleAlgorithm.GetRoleStateMoveSpeed(self as GameClient, selfBaseRate);
			}
			else if (self is Monster)
			{
				dSelfRealRate = selfBaseRate + 0.1 * (double)(self as Monster).MonsterInfo.ChangeLifeCount;
			}
			else
			{
				dSelfRealRate = selfBaseRate;
			}
			double dObjRealRate;
			if (obj is GameClient)
			{
				dObjRealRate = objBaseRate + RoleAlgorithm.GetRoleStateMoveSpeed(obj as GameClient, objBaseRate);
			}
			else if (obj is Monster)
			{
				dObjRealRate = objBaseRate + 0.1 * (double)(obj as Monster).MonsterInfo.ChangeLifeCount;
			}
			else if (obj is FakeRoleItem)
			{
				dObjRealRate = selfBaseRate + 0.1 * (double)(obj as FakeRoleItem).GetFakeRoleData().MyRoleDataMini.ChangeLifeCount;
			}
			else
			{
				dObjRealRate = 0.0;
			}
			return dSelfRealRate - dObjRealRate;
		}

		
		public static double GetNegativeRate(IObject self, IObject obj, double baseRate, ExtPropIndexes extPropIndex, MagicActionIDs actionId)
		{
			int selfZhuanSheng = 0;
			if (self is GameClient)
			{
				selfZhuanSheng = (self as GameClient).ClientData.ChangeLifeCount;
				baseRate = RoleAlgorithm.GetRoleNegativeRate(self as GameClient, baseRate, extPropIndex);
			}
			else if (self is Monster)
			{
				selfZhuanSheng = (self as Monster).MonsterInfo.ChangeLifeCount;
			}
			int objZhuanSheng = 0;
			if (obj is GameClient)
			{
				if ((obj as GameClient).buffManager.IsBuffEnabled(116))
				{
					return 0.0;
				}
				if (actionId != MagicActionIDs.MU_ADD_JITUI && (obj as GameClient).buffManager.IsBuffEnabled(113))
				{
					return 0.0;
				}
				if (CaiJiLogic.IsCaiJiState(obj as GameClient) && (extPropIndex == ExtPropIndexes.StateDingShen || extPropIndex == ExtPropIndexes.StateMoveSpeed || extPropIndex == ExtPropIndexes.StateJiTui || extPropIndex == ExtPropIndexes.StateHunMi))
				{
					return 0.0;
				}
				objZhuanSheng = (obj as GameClient).ClientData.ChangeLifeCount;
			}
			else if (obj is Monster)
			{
				objZhuanSheng = (obj as Monster).MonsterInfo.ChangeLifeCount;
			}
			else if (obj is FakeRoleItem)
			{
				objZhuanSheng = (obj as FakeRoleItem).GetFakeRoleData().MyRoleDataMini.ChangeLifeCount;
			}
			double result;
			if (selfZhuanSheng > objZhuanSheng)
			{
				result = baseRate + 0.1 * Math.Pow((double)(selfZhuanSheng - objZhuanSheng), 2.0);
			}
			else
			{
				result = baseRate - 0.1 * Math.Pow((double)(selfZhuanSheng - objZhuanSheng), 2.0);
			}
			return result;
		}

		
		public static double GetStateJiTui(IObject self, IObject obj, double selfBaseRate, double objBaseRate)
		{
			double dSelfRealRate;
			if (self is GameClient)
			{
				dSelfRealRate = selfBaseRate + RoleAlgorithm.GetRoleStateJiTui(self as GameClient, selfBaseRate);
			}
			else if (self is Monster)
			{
				dSelfRealRate = selfBaseRate + 0.1 * (double)(self as Monster).MonsterInfo.ChangeLifeCount;
			}
			else
			{
				dSelfRealRate = selfBaseRate;
			}
			double dObjRealRate;
			if (obj is GameClient)
			{
				dObjRealRate = objBaseRate + RoleAlgorithm.GetRoleStateJiTui(obj as GameClient, objBaseRate);
			}
			else if (obj is Monster)
			{
				dObjRealRate = objBaseRate + 0.1 * (double)(obj as Monster).MonsterInfo.ChangeLifeCount;
			}
			else if (obj is FakeRoleItem)
			{
				dObjRealRate = selfBaseRate + 0.1 * (double)(obj as FakeRoleItem).GetFakeRoleData().MyRoleDataMini.ChangeLifeCount;
			}
			else
			{
				dObjRealRate = 0.0;
			}
			return dSelfRealRate - dObjRealRate;
		}

		
		public static double GetStateHunMi(IObject self, IObject obj, double selfBaseRate, double objBaseRate)
		{
			double dSelfRealRate;
			if (self is GameClient)
			{
				dSelfRealRate = selfBaseRate + RoleAlgorithm.GetRoleStateHunMi(self as GameClient, selfBaseRate);
			}
			else if (self is Monster)
			{
				dSelfRealRate = selfBaseRate + 0.1 * (double)(self as Monster).MonsterInfo.ChangeLifeCount;
			}
			else
			{
				dSelfRealRate = selfBaseRate;
			}
			double dObjRealRate;
			if (obj is GameClient)
			{
				dObjRealRate = objBaseRate + RoleAlgorithm.GetRoleStateHunMi(obj as GameClient, objBaseRate);
			}
			else if (obj is Monster)
			{
				dObjRealRate = objBaseRate + 0.1 * (double)(obj as Monster).MonsterInfo.ChangeLifeCount;
			}
			else if (obj is FakeRoleItem)
			{
				dObjRealRate = selfBaseRate + 0.1 * (double)(obj as FakeRoleItem).GetFakeRoleData().MyRoleDataMini.ChangeLifeCount;
			}
			else
			{
				dObjRealRate = 0.0;
			}
			return dSelfRealRate - dObjRealRate;
		}
	}
}
