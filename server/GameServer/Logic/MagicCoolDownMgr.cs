﻿using System;
using System.Collections.Generic;
using GameServer.Core.Executor;
using GameServer.Interface;
using Server.Data;

namespace GameServer.Logic
{
	// Token: 0x02000744 RID: 1860
	public class MagicCoolDownMgr
	{
		// Token: 0x06002ED2 RID: 11986 RVA: 0x0029F250 File Offset: 0x0029D450
		public bool SkillCoolDown(int skillID)
		{
			CoolDownItem coolDownItem = null;
			bool result;
			if (!this.SkillCoolDownDict.TryGetValue(skillID, out coolDownItem))
			{
				result = true;
			}
			else
			{
				long ticks = TimeUtil.NOW();
				result = (ticks > coolDownItem.StartTicks + coolDownItem.CDTicks);
			}
			return result;
		}

		// Token: 0x06002ED3 RID: 11987 RVA: 0x0029F2A0 File Offset: 0x0029D4A0
		public void AddSkillCoolDown(IObject attacker, int skillID)
		{
			if (attacker is GameClient)
			{
				this.AddSkillCoolDownForClient(attacker as GameClient, skillID);
			}
			else if (attacker is Monster)
			{
				this.AddSkillCoolDownForMonster(attacker as Monster, skillID);
			}
		}

		// Token: 0x06002ED4 RID: 11988 RVA: 0x0029F2F0 File Offset: 0x0029D4F0
		public void AddSkillCoolDownForClient(GameClient client, int skillID)
		{
			SystemXmlItem systemMagic = null;
			if (GameManager.SystemMagicQuickMgr.MagicItemsDict.TryGetValue(skillID, out systemMagic))
			{
				long nowTicks = TimeUtil.NOW();
				int cdTime = Global.GMax(0, systemMagic.GetIntValue("CDTime", -1));
				int pubCDTime = Global.GMax(0, systemMagic.GetIntValue("PubCDTime", -1));
				if (cdTime <= 0)
				{
					int nParentMagicID = systemMagic.GetIntValue("ParentMagicID", -1);
					if (nParentMagicID > 0)
					{
						if (GameManager.SystemMagicQuickMgr.MagicItemsDict.TryGetValue(nParentMagicID, out systemMagic))
						{
							cdTime = Global.GMax(0, systemMagic.GetIntValue("CDTime", -1));
						}
					}
				}
				long delayTicks = Data.MaxServerClientTimeDiff;
				if (client.ClientData.CurrentMagicCode == skillID)
				{
					delayTicks = nowTicks - client.ClientData.CurrentMagicTicks;
				}
				if (cdTime > 0)
				{
					cdTime *= 1000;
					if (client.ClientData.CurrentMagicCode == skillID && client.ClientData.CurrentMagicCDSubPercent > 0.0)
					{
						cdTime = Convert.ToInt32((double)cdTime * (1.0 - client.ClientData.CurrentMagicCDSubPercent));
						cdTime = (int)Math.Max((long)cdTime, delayTicks);
						int nextMagicID = systemMagic.GetIntValue("NextMagicID", -1);
						if (nextMagicID <= 0)
						{
							client.ClientData.CurrentMagicCDSubPercent = 0.0;
						}
					}
					Global.AddCoolDownItem(this.SkillCoolDownDict, skillID, nowTicks, (long)cdTime - delayTicks);
					if (systemMagic.GetStringValue("HorseSkill") == "1")
					{
						ExtData extData = ExtDataManager.GetClientExtData(client);
						extData.ZuoQiSkillCDTicks = nowTicks + (long)cdTime - delayTicks;
						extData.ZuoQiSkillCdTime = (long)cdTime - delayTicks;
					}
				}
				if (pubCDTime > 0)
				{
					client.ClientData.CurrentMagicActionEndTicks = nowTicks - delayTicks + (long)pubCDTime;
					if (null != client.ClientData.SkillDataList)
					{
						for (int i = 0; i < client.ClientData.SkillDataList.Count; i++)
						{
							SkillData skillData = client.ClientData.SkillDataList[i];
							if (null != skillData)
							{
								Global.AddCoolDownItem(this.SkillCoolDownDict, skillData.SkillID, nowTicks, (long)pubCDTime - delayTicks);
							}
						}
					}
				}
			}
		}

		// Token: 0x06002ED5 RID: 11989 RVA: 0x0029F56C File Offset: 0x0029D76C
		public void AddSkillCoolDownForMonster(Monster monster, int skillID)
		{
			SystemXmlItem systemMagic = null;
			if (GameManager.SystemMagicQuickMgr.MagicItemsDict.TryGetValue(skillID, out systemMagic))
			{
				int cdTime = systemMagic.GetIntValue("CDTime", -1);
				if (cdTime > 0)
				{
					int pubCDTime = systemMagic.GetIntValue("PubCDTime", -1);
					long nowTicks = TimeUtil.NOW();
					Global.AddCoolDownItem(this.SkillCoolDownDict, skillID, nowTicks, (long)(cdTime * 1000));
					if (null != monster.MonsterInfo.SkillIDs)
					{
						for (int i = 0; i < monster.MonsterInfo.SkillIDs.Length; i++)
						{
							if (pubCDTime > 0)
							{
								Global.AddCoolDownItem(this.SkillCoolDownDict, monster.MonsterInfo.SkillIDs[i], nowTicks, (long)pubCDTime);
							}
						}
					}
				}
			}
		}

		// Token: 0x04003C77 RID: 15479
		private Dictionary<int, CoolDownItem> SkillCoolDownDict = new Dictionary<int, CoolDownItem>();
	}
}
