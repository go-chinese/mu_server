﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using GameServer.Core.Executor;
using KF.Client;
using Server.Data;
using Server.Tools;
using Tmsk.Contract.Data;

namespace GameServer.Logic.ActivityNew
{
	// Token: 0x0200003F RID: 63
	public class JieriPlatChargeKingEveryDay : Activity
	{
		// Token: 0x060000AD RID: 173 RVA: 0x0000C57C File Offset: 0x0000A77C
		public bool Init()
		{
			try
			{
				GeneralCachingXmlMgr.RemoveCachingXml(Global.GameResPath(this.CfgFile));
				XElement xml = GeneralCachingXmlMgr.GetXElement(Global.GameResPath(this.CfgFile));
				if (null == xml)
				{
					return false;
				}
				XElement args = xml.Element("Activities");
				if (null != args)
				{
					this.FromDate = Global.GetSafeAttributeStr(args, "FromDate");
					this.ToDate = Global.GetSafeAttributeStr(args, "ToDate");
					this.ActivityType = (int)Global.GetSafeAttributeLong(args, "ActivityType");
					this.AwardStartDate = Global.GetSafeAttributeStr(args, "AwardStartDate");
					this.AwardEndDate = Global.GetSafeAttributeStr(args, "AwardEndDate");
				}
				args = xml.Element("GiftList");
				if (null != args)
				{
					IEnumerable<XElement> xmlItems = args.Elements();
					foreach (XElement xmlItem in xmlItems)
					{
						if (null != xmlItem)
						{
							JieriPlatChargeKingEveryDay.ChargeItem ci = new JieriPlatChargeKingEveryDay.ChargeItem();
							ci.Id = (int)Global.GetSafeAttributeLong(xmlItem, "ID");
							ci.Rank = (int)Global.GetSafeAttributeLong(xmlItem, "Ranking");
							ci.Day = (int)Global.GetSafeAttributeLong(xmlItem, "Day");
							ci.NeedChargeYB = (int)Global.GetSafeAttributeLong(xmlItem, "MinYuanBao");
							AwardItem myAwardItem = new AwardItem();
							AwardItem myAwardItem2 = new AwardItem();
							AwardEffectTimeItem timeAwardItem = new AwardEffectTimeItem();
							string goodsIDs = Global.GetSafeAttributeStr(xmlItem, "GoodsOne");
							if (string.IsNullOrEmpty(goodsIDs))
							{
								LogManager.WriteLog(LogTypes.Warning, string.Format("读取{0}配置文件中的物品配置项失败", this.CfgFile), null, true);
							}
							else
							{
								string[] fields = goodsIDs.Split(new char[]
								{
									'|'
								});
								if (fields.Length <= 0)
								{
									LogManager.WriteLog(LogTypes.Warning, string.Format("读取{0}配置文件中的物品配置项失败", this.CfgFile), null, true);
								}
								else
								{
									myAwardItem.GoodsDataList = HuodongCachingMgr.ParseGoodsDataList(fields, "大型节日活动每日平台充值王配置");
								}
							}
							goodsIDs = Global.GetSafeAttributeStr(xmlItem, "GoodsTwo");
							if (!string.IsNullOrEmpty(goodsIDs))
							{
								string[] fields = goodsIDs.Split(new char[]
								{
									'|'
								});
								if (fields.Length <= 0)
								{
									LogManager.WriteLog(LogTypes.Warning, string.Format("读取{0}配置文件中的物品配置项失败", this.CfgFile), null, true);
								}
								else
								{
									myAwardItem2.GoodsDataList = HuodongCachingMgr.ParseGoodsDataList(fields, "大型节日活动每日平台充值王配置2");
								}
							}
							string timeGoods = Global.GetSafeAttributeStr(xmlItem, "GoodsThr");
							string timeList = Global.GetSafeAttributeStr(xmlItem, "EffectiveTime");
							timeAwardItem.Init(timeGoods, timeList, "大型节日每日平台充值王时效性物品活动配置");
							ci.allAwardGoodsOne = myAwardItem;
							ci.occAwardGoodsTwo = myAwardItem2;
							ci.timeAwardGoodsThr = timeAwardItem;
							this.IdVsChargeItemDict[ci.Id] = ci;
							List<JieriPlatChargeKingEveryDay.ChargeItem> chargeItemList;
							if (!this.DayVsChargeItemListDict.TryGetValue(ci.Day, out chargeItemList))
							{
								chargeItemList = new List<JieriPlatChargeKingEveryDay.ChargeItem>();
								chargeItemList.Add(ci);
								this.DayVsChargeItemListDict[ci.Day] = chargeItemList;
							}
							else
							{
								chargeItemList.Add(ci);
							}
							chargeItemList.Sort(delegate(JieriPlatChargeKingEveryDay.ChargeItem left, JieriPlatChargeKingEveryDay.ChargeItem right)
							{
								int result;
								if (left.Rank < right.Rank)
								{
									result = -1;
								}
								else if (left.Rank > right.Rank)
								{
									result = 1;
								}
								else
								{
									result = 0;
								}
								return result;
							});
						}
					}
				}
				base.PredealDateTime();
			}
			catch (Exception ex)
			{
				LogManager.WriteLog(LogTypes.Fatal, string.Format("{0}解析出现异常, {1}", this.CfgFile, ex.Message), null, true);
				return false;
			}
			return true;
		}

		// Token: 0x060000AE RID: 174 RVA: 0x0000C998 File Offset: 0x0000AB98
		public override bool HasEnoughBagSpaceForAwardGoods(GameClient client, int _params)
		{
			JieriPlatChargeKingEveryDay.ChargeItem ci = null;
			bool result;
			if (!this.IdVsChargeItemDict.TryGetValue(_params, out ci))
			{
				result = false;
			}
			else
			{
				AwardItem allItem = ci.allAwardGoodsOne;
				AwardItem occItem = ci.occAwardGoodsTwo;
				AwardEffectTimeItem timeItem = ci.timeAwardGoodsThr;
				int awardCnt = 0;
				if (allItem != null && allItem.GoodsDataList != null)
				{
					awardCnt += allItem.GoodsDataList.Count;
				}
				if (occItem != null && occItem.GoodsDataList != null)
				{
					awardCnt += occItem.GoodsDataList.Count((GoodsData goods) => Global.IsRoleOccupationMatchGoods(client, goods.GoodsID));
				}
				if (timeItem != null)
				{
					awardCnt += timeItem.GoodsCnt();
				}
				result = Global.CanAddGoodsNum(client, awardCnt);
			}
			return result;
		}

		// Token: 0x060000AF RID: 175 RVA: 0x0000CA80 File Offset: 0x0000AC80
		public bool CanGetAnyAward(GameClient client)
		{
			bool result;
			if (GameManager.IsKuaFuServer)
			{
				result = false;
			}
			else if (client == null)
			{
				result = false;
			}
			else if (!this.InAwardTime())
			{
				result = false;
			}
			else
			{
				lock (this.Mutex)
				{
					foreach (JieriPlatChargeKingEveryDay.ChargeItem item in this.IdVsChargeItemDict.Values)
					{
						if (this.CheckCondition(client, item.Id))
						{
							return true;
						}
					}
				}
				result = false;
			}
			return result;
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x0000CB98 File Offset: 0x0000AD98
		public override bool CheckCondition(GameClient client, int extTag)
		{
			bool result;
			try
			{
				JieriPlatChargeKingEveryDay.ChargeItem ci = null;
				if (!this.IdVsChargeItemDict.TryGetValue(extTag, out ci))
				{
					result = false;
				}
				else
				{
					DateTime startTime = DateTime.Parse(this.FromDate);
					DateTime endTime = DateTime.Parse(this.ToDate);
					DateTime now = TimeUtil.NowDateTime();
					lock (this.Mutex)
					{
						if (now < startTime.AddDays((double)ci.Day))
						{
							return false;
						}
						InputKingPaiHangDataEx kfpaihangData = null;
						if (this._kfRankDict == null || !this._kfRankDict.TryGetValue(ci.Day, out kfpaihangData))
						{
							return false;
						}
						DateTime rankTm;
						DateTime.TryParse(kfpaihangData.RankTime, out rankTm);
						if (rankTm < startTime.AddDays((double)ci.Day))
						{
							return false;
						}
						List<InputKingPaiHangData> paihangDataList = null;
						if (this._realRankDict == null || !this._realRankDict.TryGetValue(ci.Day, out paihangDataList))
						{
							return false;
						}
						InputKingPaiHangData myData = paihangDataList.Find((InputKingPaiHangData x) => 0 == string.Compare(x.UserID, client.strUserID, true));
						if (myData == null || myData.PaiHang != ci.Rank)
						{
							return false;
						}
						string huoDongKeyStr = Global.GetHuoDongKeyString(this.FromDate, this.ToDate);
						long hasgettimes = KFCopyRpcClient.getInstance().QueryHuodongAwardUserHist(77, huoDongKeyStr, client.strUserID);
						if (hasgettimes < 0L)
						{
							return false;
						}
						int bitVal = Global.GetBitValue(ci.Day);
						if ((hasgettimes & (long)bitVal) == (long)bitVal)
						{
							return false;
						}
					}
					result = true;
				}
			}
			catch (Exception ex)
			{
				LogManager.WriteLog(LogTypes.Fatal, string.Format("{0}解析出现异常, {1}", this.CfgFile, ex.Message), null, true);
				result = false;
			}
			return result;
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x0000CDF4 File Offset: 0x0000AFF4
		public override bool GiveAward(GameClient client, int _params)
		{
			DateTime startTime = DateTime.Parse(this.FromDate);
			DateTime endTime = DateTime.Parse(this.ToDate);
			JieriPlatChargeKingEveryDay.ChargeItem ci = null;
			bool result;
			if (!this.IdVsChargeItemDict.TryGetValue(_params, out ci))
			{
				result = false;
			}
			else
			{
				string huoDongKeyStr = Global.GetHuoDongKeyString(this.FromDate, this.ToDate);
				int ret = KFCopyRpcClient.getInstance().UpdateHuodongAwardUserHist(77, huoDongKeyStr, client.strUserID, ci.Day);
				if (ret < 0)
				{
					result = false;
				}
				else
				{
					AwardItem allItem = ci.allAwardGoodsOne;
					AwardItem occItem = ci.occAwardGoodsTwo;
					AwardEffectTimeItem timeItem = ci.timeAwardGoodsThr;
					if (!base.GiveAward(client, allItem) || !base.GiveAward(client, occItem) || !base.GiveEffectiveTimeAward(client, timeItem.ToAwardItem()))
					{
						string errLogMsg = string.Format("发送节日每日平台充值王奖励的时候，发送失败，但是已经设置为领取成功, roleid={0}, rolename={1}, id={3}", client.ClientData.RoleID, client.ClientData.RoleName, _params);
						LogManager.WriteLog(LogTypes.Error, errLogMsg, null, true);
						result = false;
					}
					else
					{
						if (client._IconStateMgr.CheckJieRiPCKingEveryDay(client))
						{
							client._IconStateMgr.SendIconStateToClient(client);
						}
						result = true;
					}
				}
			}
			return result;
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x0000CF6C File Offset: 0x0000B16C
		public void HandleCenterPaiHang(int Day, List<InputKingPaiHangData> tmpRankList)
		{
			List<JieriPlatChargeKingEveryDay.ChargeItem> chargeItemList;
			if (this.DayVsChargeItemListDict.TryGetValue(Day, out chargeItemList))
			{
				bool bNeedSort = false;
				int i;
				for (i = 1; i < tmpRankList.Count<InputKingPaiHangData>(); i++)
				{
					if (tmpRankList[i].PaiHangValue > tmpRankList[i - 1].PaiHangValue)
					{
						bNeedSort = true;
						break;
					}
				}
				if (bNeedSort)
				{
					tmpRankList.Sort((InputKingPaiHangData _left, InputKingPaiHangData _right) => _right.PaiHangValue - _left.PaiHangValue);
				}
				tmpRankList.ForEach(delegate(InputKingPaiHangData _item)
				{
					_item.PaiHangValue = Global.TransMoneyToYuanBao(_item.PaiHangValue);
				});
				chargeItemList.ForEach(delegate(JieriPlatChargeKingEveryDay.ChargeItem _item)
				{
					_item.allAwardGoodsOne.MinAwardCondionValue = int.MaxValue;
				});
				int procListIdx = 0;
				i = 0;
				while (i < chargeItemList.Count && procListIdx < tmpRankList.Count)
				{
					if (tmpRankList[procListIdx].PaiHangValue >= chargeItemList[i].NeedChargeYB)
					{
						tmpRankList[procListIdx].PaiHang = chargeItemList[i].Rank;
						procListIdx++;
					}
					i++;
				}
				if (procListIdx < tmpRankList.Count)
				{
					tmpRankList.RemoveRange(procListIdx, tmpRankList.Count - procListIdx);
				}
			}
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x0000D0E0 File Offset: 0x0000B2E0
		public void Update()
		{
			if (!this.InActivityTime() && !this.InAwardTime())
			{
				this._realRankDict.Clear();
				this._kfRankDict.Clear();
			}
			else
			{
				DateTime now = TimeUtil.NowDateTime();
				if (!(now < this.lastUpdateTime.AddSeconds(15.0)))
				{
					this.lastUpdateTime = now;
					DateTime startTime = DateTime.Parse(this.FromDate);
					DateTime endTime = DateTime.Parse(this.ToDate);
					List<InputKingPaiHangDataEx> tmpRankExList = KFCopyRpcClient.getInstance().GetPlatChargeKingEveryDay(startTime, endTime);
					if (tmpRankExList != null)
					{
						for (int dayLoop = 0; dayLoop < tmpRankExList.Count; dayLoop++)
						{
							List<InputKingPaiHangData> tmpRankList = (tmpRankExList[dayLoop] != null) ? tmpRankExList[dayLoop].ListData : null;
							if (null != tmpRankList)
							{
								this.HandleCenterPaiHang(dayLoop + 1, tmpRankList);
								lock (this.Mutex)
								{
									this._realRankDict[dayLoop + 1] = tmpRankList;
									this._kfRankDict[dayLoop + 1] = tmpRankExList[dayLoop];
								}
							}
						}
					}
				}
			}
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x0000D288 File Offset: 0x0000B488
		public JieriPlatChargeKingEverydayData BuildQueryDataForClient(GameClient client)
		{
			DateTime startTime = DateTime.Parse(this.FromDate);
			DateTime endTime = DateTime.Parse(this.ToDate);
			DateTime now = TimeUtil.NowDateTime();
			JieriPlatChargeKingEverydayData myData = new JieriPlatChargeKingEverydayData();
			bool needHasGetTimes = false;
			lock (this.Mutex)
			{
				myData.PaiHangDict = new Dictionary<int, List<InputKingPaiHangData>>(this._realRankDict);
				foreach (KeyValuePair<int, List<InputKingPaiHangData>> kvp in myData.PaiHangDict)
				{
					if (!(now < startTime.AddDays((double)kvp.Key)))
					{
						needHasGetTimes = kvp.Value.Exists((InputKingPaiHangData x) => 0 == string.Compare(x.UserID, client.strUserID, true));
						if (needHasGetTimes)
						{
							break;
						}
					}
				}
			}
			if (needHasGetTimes)
			{
				string huoDongKeyStr = Global.GetHuoDongKeyString(this.FromDate, this.ToDate);
				myData.hasgettimes = KFCopyRpcClient.getInstance().QueryHuodongAwardUserHist(77, huoDongKeyStr, client.strUserID);
			}
			return myData;
		}

		// Token: 0x04000152 RID: 338
		private const int updateIntervalSec = 15;

		// Token: 0x04000153 RID: 339
		private readonly string CfgFile = "Config/JieRiGifts/JieRiMeiRiChongZhiWang.xml";

		// Token: 0x04000154 RID: 340
		private Dictionary<int, List<JieriPlatChargeKingEveryDay.ChargeItem>> DayVsChargeItemListDict = new Dictionary<int, List<JieriPlatChargeKingEveryDay.ChargeItem>>();

		// Token: 0x04000155 RID: 341
		private Dictionary<int, JieriPlatChargeKingEveryDay.ChargeItem> IdVsChargeItemDict = new Dictionary<int, JieriPlatChargeKingEveryDay.ChargeItem>();

		// Token: 0x04000156 RID: 342
		private object Mutex = new object();

		// Token: 0x04000157 RID: 343
		private Dictionary<int, List<InputKingPaiHangData>> _realRankDict = new Dictionary<int, List<InputKingPaiHangData>>();

		// Token: 0x04000158 RID: 344
		private Dictionary<int, InputKingPaiHangDataEx> _kfRankDict = new Dictionary<int, InputKingPaiHangDataEx>();

		// Token: 0x04000159 RID: 345
		private DateTime lastUpdateTime = TimeUtil.NowDateTime().AddSeconds(-30.0);

		// Token: 0x02000040 RID: 64
		private class ChargeItem
		{
			// Token: 0x0400015E RID: 350
			public int Id;

			// Token: 0x0400015F RID: 351
			public int Rank;

			// Token: 0x04000160 RID: 352
			public int Day;

			// Token: 0x04000161 RID: 353
			public int NeedChargeYB;

			// Token: 0x04000162 RID: 354
			public AwardItem allAwardGoodsOne;

			// Token: 0x04000163 RID: 355
			public AwardItem occAwardGoodsTwo;

			// Token: 0x04000164 RID: 356
			public AwardEffectTimeItem timeAwardGoodsThr;
		}
	}
}
