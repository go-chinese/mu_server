﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Server.Data;
using Server.Tools;
using Tmsk.Tools.Tools;

namespace GameServer.Logic
{
	
	public class GoodsUtil
	{
		
		public static void LoadConfig()
		{
			GoodsUtil.LoadGetGoodsXml();
			int[] list = GameManager.systemParamsList.GetParamValueIntArrayByName("HorseEquipBarOpen", ',');
			int[] list2 = GameManager.systemParamsList.GetParamValueIntArrayByName("HorseEquipMeltingOpen", ',');
			lock (GoodsUtil.GoodsTypeInfoDict)
			{
				for (int i = 40; i <= 45; i++)
				{
					GoodsTypeInfo typeInfo;
					if (GoodsUtil.GoodsTypeInfoDict.TryGetValue(i, out typeInfo))
					{
						typeInfo.Categriory = 40;
						typeInfo.GoodsType = 2;
						int idx = i - 40;
						if (list != null && idx < list.Length && list[idx] == 1)
						{
							typeInfo.IsEquip = true;
						}
						for (int j = 0; j < 15; j++)
						{
							if (list2 != null && j < list2.Length && list2[j] == 1)
							{
								typeInfo.Operations[j] = true;
								if (j == 10)
								{
									typeInfo.OperationsTypeList[j] = new List<int>();
									for (int k = 40; k <= 45; k++)
									{
										typeInfo.OperationsTypeList[j].Add(k);
									}
								}
								else
								{
									typeInfo.OperationsTypeList[j] = new List<int>
									{
										i
									};
								}
							}
						}
					}
				}
				for (int i = 11; i <= 21; i++)
				{
					GoodsTypeInfo typeInfo;
					if (GoodsUtil.GoodsTypeInfoDict.TryGetValue(i, out typeInfo))
					{
						typeInfo.Categriory = i;
						typeInfo.GoodsType = 1;
						typeInfo.IsEquip = true;
						for (int j = 0; j < 15; j++)
						{
							typeInfo.Operations[j] = true;
							typeInfo.OperationsTypeList[j] = new List<int>();
							for (int k = 11; k <= 21; k++)
							{
								typeInfo.OperationsTypeList[j].Add(k);
							}
							if (j == 10)
							{
								for (int k = 0; k <= 6; k++)
								{
									typeInfo.OperationsTypeList[j].Add(k);
								}
							}
						}
					}
				}
				for (int i = 0; i <= 6; i++)
				{
					GoodsTypeInfo typeInfo;
					if (GoodsUtil.GoodsTypeInfoDict.TryGetValue(i, out typeInfo))
					{
						typeInfo.Categriory = i;
						typeInfo.GoodsType = 1;
						typeInfo.IsEquip = true;
						for (int j = 0; j < 15; j++)
						{
							typeInfo.Operations[j] = true;
							if (j == 10)
							{
								typeInfo.OperationsTypeList[j] = new List<int>();
								for (int k = 0; k <= 6; k++)
								{
									typeInfo.OperationsTypeList[j].Add(k);
								}
								for (int k = 11; k <= 21; k++)
								{
									typeInfo.OperationsTypeList[j].Add(k);
								}
							}
							else
							{
								typeInfo.OperationsTypeList[j] = new List<int>
								{
									i
								};
							}
						}
					}
				}
				for (int i = 37; i <= 38; i++)
				{
					GoodsTypeInfo typeInfo;
					if (GoodsUtil.GoodsTypeInfoDict.TryGetValue(i, out typeInfo))
					{
						typeInfo.Categriory = i;
						typeInfo.GoodsType = 1;
						typeInfo.IsEquip = true;
						for (int j = 0; j < 15; j++)
						{
							typeInfo.Operations[j] = true;
							typeInfo.OperationsTypeList[j] = new List<int>();
							for (int k = 37; k <= 38; k++)
							{
								typeInfo.OperationsTypeList[j].Add(k);
							}
						}
					}
				}
				for (int i = 30; i <= 36; i++)
				{
					GoodsTypeInfo typeInfo;
					if (GoodsUtil.GoodsTypeInfoDict.TryGetValue(i, out typeInfo))
					{
						typeInfo.Categriory = i;
						typeInfo.GoodsType = 1;
						typeInfo.IsEquip = true;
						for (int j = 100; j < 101; j++)
						{
							typeInfo.Operations[j] = true;
						}
					}
				}
			}
		}

		
		public static void LoadGetGoodsXml()
		{
			string xmlFileName = Global.GameResPath("Config/GetGoods.xml");
			Dictionary<int, int> dict = new Dictionary<int, int>();
			try
			{
				XElement xmlFile = ConfigHelper.Load(xmlFileName);
				if (null != xmlFile)
				{
					IEnumerable<XElement> xmls = ConfigHelper.GetXElements(xmlFile, "GetGoods");
					if (null != xmls)
					{
						foreach (XElement xml in xmls)
						{
							int id = (int)ConfigHelper.GetElementAttributeValueLong(xml, "ID", 0L);
							int goodsId = (int)ConfigHelper.GetElementAttributeValueLong(xml, "Goods", 0L);
							dict[id] = goodsId;
						}
					}
				}
			}
			catch (Exception ex)
			{
				LogManager.WriteLog(LogTypes.Exception, ex.ToString(), null, true);
			}
			GoodsUtil.GetGoodsIdDict = dict;
		}

		
		public static bool CanEquip(int type, int site)
		{
			lock (GoodsUtil.GoodsTypeInfoDict)
			{
				GoodsTypeInfo info;
				if (GoodsUtil.GoodsTypeInfoDict.TryGetValue(type, out info) && info.UsingSite == site)
				{
					return info.IsEquip;
				}
			}
			return false;
		}

		
		public static bool IsEquip(int goodsId)
		{
			int type = Global.GetGoodsCatetoriy(goodsId);
			lock (GoodsUtil.GoodsTypeInfoDict)
			{
				GoodsTypeInfo info;
				if (GoodsUtil.GoodsTypeInfoDict.TryGetValue(type, out info))
				{
					return info.IsEquip;
				}
			}
			return false;
		}

		
		public static GoodsTypeInfo GetGoodsTypeInfo(int type)
		{
			lock (GoodsUtil.GoodsTypeInfoDict)
			{
				GoodsTypeInfo info;
				if (GoodsUtil.GoodsTypeInfoDict.TryGetValue(type, out info))
				{
					return info;
				}
			}
			return GoodsTypeInfo.Empty;
		}

		
		public static GoodsTypeInfo GetGoodsTypeInfoByGoodsId(int goodsId)
		{
			int type = Global.GetGoodsCatetoriy(goodsId);
			return GoodsUtil.GetGoodsTypeInfo(type);
		}

		
		public static bool CanUpgrade(int type, int op)
		{
			lock (GoodsUtil.GoodsTypeInfoDict)
			{
				GoodsTypeInfo info;
				if (GoodsUtil.GoodsTypeInfoDict.TryGetValue(type, out info))
				{
					return info.Operations[op];
				}
			}
			return false;
		}

		
		public static int CanUpgradeInhert(int type1, int type2, int op)
		{
			int ret = -200;
			lock (GoodsUtil.GoodsTypeInfoDict)
			{
				GoodsTypeInfo info;
				GoodsTypeInfo info2;
				if (GoodsUtil.GoodsTypeInfoDict.TryGetValue(type1, out info) && GoodsUtil.GoodsTypeInfoDict.TryGetValue(type2, out info2))
				{
					if (info.Operations[op] && info2.Operations[op])
					{
						if (info.OperationsTypeList[op] == null || info.OperationsTypeList[op].Contains(type2))
						{
							return 0;
						}
						ret = -201;
					}
				}
			}
			return ret;
		}

		
		public static bool IsZuoQiEquip(int goodsId)
		{
			int type = Global.GetGoodsCatetoriy(goodsId);
			lock (GoodsUtil.GoodsTypeInfoDict)
			{
				GoodsTypeInfo info;
				if (GoodsUtil.GoodsTypeInfoDict.TryGetValue(type, out info))
				{
					return info.GoodsType == 2;
				}
			}
			return false;
		}

		
		public static bool IsVisiableEquip(int goodsId)
		{
			int type = Global.GetGoodsCatetoriy(goodsId);
			lock (GoodsUtil.GoodsTypeInfoDict)
			{
				GoodsTypeInfo info;
				if (GoodsUtil.GoodsTypeInfoDict.TryGetValue(type, out info))
				{
					return info.GoodsType == 1;
				}
			}
			return true;
		}

		
		public static int GetResGoodsID(MoneyTypes type)
		{
			Dictionary<int, int> dict = GoodsUtil.GetGoodsIdDict;
			if (null != dict)
			{
				lock (dict)
				{
					int goodsId;
					if (dict.TryGetValue((int)type, out goodsId))
					{
						return goodsId;
					}
				}
			}
			return 0;
		}

		
		public static string FormatUpdateDBGoodsStr(params object[] args)
		{
			string result;
			if (args.Length != 24)
			{
				LogManager.WriteLog(LogTypes.Error, string.Format("FormatUpdateDBGoodsStr, 参数个数不对{0}", args.Length), null, true);
				result = null;
			}
			else
			{
				result = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}:{9}:{10}:{11}:{12}:{13}:{14}:{15}:{16}:{17}:{18}:{19}:{20}:{21}:{22}:{23}", args);
			}
			return result;
		}

		
		
		
		private static Dictionary<int, int> logItemDict
		{
			get
			{
				Dictionary<int, int> logItemDict_storage;
				lock (GoodsUtil._logItemLock)
				{
					logItemDict_storage = GoodsUtil._logItemDict_storage;
				}
				return logItemDict_storage;
			}
			set
			{
				lock (GoodsUtil._logItemLock)
				{
					GoodsUtil._logItemDict_storage = value;
				}
			}
		}

		
		public static void LoadItemLogMark()
		{
			int[] arrMark = GameManager.systemParamsList.GetParamValueIntArrayByName("LogGoods", ',');
			Dictionary<int, int> tmpDict = new Dictionary<int, int>();
			if (arrMark != null && arrMark.Length > 0)
			{
				for (int i = 0; i < arrMark.Length; i++)
				{
					tmpDict.Add(arrMark[i], 1);
				}
			}
			GoodsUtil.logItemDict = tmpDict;
		}

		
		public static string ModifyGoodsLogName(GoodsData goodsData)
		{
			return Global.ModifyGoodsLogName(goodsData);
		}

		
		public static bool CheckHasGoodsList(GameClient client, List<List<int>> needGoods, bool notBind)
		{
			for (int i = 0; i < needGoods.Count; i++)
			{
				if (needGoods[i].Count >= 2)
				{
					int goodsId = needGoods[i][0];
					int costCount = needGoods[i][1];
					int haveGoodsCnt = notBind ? Global.GetTotalNotBindGoodsCountByID(client, goodsId) : Global.GetTotalGoodsCountByID(client, goodsId);
					if (haveGoodsCnt < costCount)
					{
						return false;
					}
				}
			}
			return true;
		}

		
		public static bool CostGoodsList(GameClient client, List<List<int>> needGoods, bool notBind, ref string strCostList, string logMsg)
		{
			bool result = true;
			bool bUsedBinding_just_placeholder = false;
			bool bUsedTimeLimited_just_placeholder = false;
			for (int i = 0; i < needGoods.Count; i++)
			{
				if (needGoods[i].Count >= 2)
				{
					int goodsId = needGoods[i][0];
					int costCount = needGoods[i][1];
					if (notBind)
					{
						if (!GameManager.ClientMgr.NotifyUseNotBindGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, goodsId, costCount, false, out bUsedBinding_just_placeholder, out bUsedTimeLimited_just_placeholder, false))
						{
							LogManager.WriteLog(LogTypes.Error, string.Format("{0},消耗{1}个GoodsID={2}的非绑定物品失败", logMsg, costCount, goodsId), null, true);
							result = false;
							goto IL_15A;
						}
					}
					else if (!GameManager.ClientMgr.NotifyUseGoods(Global._TCPManager.MySocketListener, Global._TCPManager.tcpClientPool, Global._TCPManager.TcpOutPacketPool, client, goodsId, costCount, false, out bUsedBinding_just_placeholder, out bUsedTimeLimited_just_placeholder, false))
					{
						LogManager.WriteLog(LogTypes.Error, string.Format("{0},消耗{1}个GoodsID={2}的物品失败", logMsg, costCount, goodsId), null, true);
						result = false;
						goto IL_15A;
					}
					if (strCostList != null)
					{
						GoodsData goodsDataCost = new GoodsData
						{
							GoodsID = goodsId,
							GCount = costCount
						};
						strCostList += EventLogManager.NewGoodsDataPropString(goodsDataCost);
					}
				}
				IL_15A:;
			}
			return result;
		}

		
		public static GoodsData GetGoodsDataBySite(GameClient client, int id, int site)
		{
			if (site == 1)
			{
				if (null == client.ClientData.MeditateGoodsDataList)
				{
					return null;
				}
				lock (client.ClientData.MeditateGoodsDataList)
				{
					return client.ClientData.MeditateGoodsDataList.FirstOrDefault((GoodsData x) => x.Id == id);
				}
			}
			return null;
		}

		
		public static bool RemoveGoodsDataBySite(GameClient client, GoodsData goodsData, int site)
		{
			if (site != 1)
			{
				if (site == 12000)
				{
					return ZuoQiManager.RemoveStoreGoodsData(client, goodsData);
				}
				if (site != 13000)
				{
					return false;
				}
			}
			else
			{
				if (null == client.ClientData.MeditateGoodsDataList)
				{
					return false;
				}
				lock (client.ClientData.MeditateGoodsDataList)
				{
					return client.ClientData.MeditateGoodsDataList.Remove(goodsData);
				}
			}
			return ZuoQiManager.RemoveEquipGoodsData(client, goodsData);
		}

		
		public static bool DestoryGoodsBySystem(GameClient client, GoodsData goodsData)
		{
			string cmdData;
			if (goodsData.Using > 0)
			{
				cmdData = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", new object[]
				{
					client.ClientData.RoleID,
					2,
					goodsData.Id,
					goodsData.GoodsID,
					0,
					goodsData.Site,
					goodsData.GCount,
					goodsData.BagIndex,
					""
				});
				Global.ModifyGoodsByCmdParams(client, cmdData, "客户端修改", null);
			}
			cmdData = string.Format("{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}:{8}", new object[]
			{
				client.ClientData.RoleID,
				6,
				goodsData.Id,
				goodsData.GoodsID,
				0,
				goodsData.Site,
				goodsData.GCount,
				goodsData.BagIndex,
				""
			});
			Global.ModifyGoodsByCmdParams(client, cmdData, "客户端修改", null);
			return true;
		}

		
		public static int AddGoodsDBCommand(GameClient client, GoodsData goodsData, bool useOldGrid, int newHint, string goodsFromWhere, bool onLine = true)
		{
			int dbRet = 0;
			int goodsNum = goodsData.GCount;
			int goodsID = goodsData.GoodsID;
			int gridNum = Global.GetGoodsGridNumByID(goodsID);
			gridNum = Global.GMax(gridNum, 1);
			int result;
			if (goodsNum <= 0)
			{
				result = 0;
			}
			else
			{
				int addCount = (goodsNum - 1) / gridNum + 1;
				for (int i = 0; i < addCount; i++)
				{
					int thisTimeNum = gridNum;
					if (i >= addCount - 1 && goodsNum % gridNum > 0)
					{
						thisTimeNum = goodsNum % gridNum;
					}
					dbRet = Global.AddGoodsDBCommand_Hook(Global._TCPManager.TcpOutPacketPool, client, goodsID, thisTimeNum, goodsData.Quality, goodsData.Props, goodsData.Forge_level, goodsData.Binding, goodsData.Site, goodsData.Jewellist, useOldGrid, newHint, goodsFromWhere, false, goodsData.Endtime, goodsData.AddPropIndex, goodsData.BornIndex, goodsData.Lucky, goodsData.Strong, goodsData.ExcellenceInfo, goodsData.AppendPropLev, goodsData.ChangeLifeLevForEquip, false, goodsData.WashProps, goodsData.ElementhrtsProps, "1900-01-01 12:00:00", goodsData.JuHunID, onLine);
					if (dbRet < 0)
					{
						return dbRet;
					}
				}
				result = dbRet;
			}
			return result;
		}

		
		public static GoodsData AddGoodsDataToBag(GameClient client, int id, int goodsID, int forgeLevel, int quality, int goodsNum, int binding, int site, string jewelList, string startTime, string endTime, int addPropIndex, int bornIndex, int lucky, int strong, int ExcellenceProperty, int nAppendPropLev, int nEquipChangeLife, int bagIndex = 0, List<int> washProps = null)
		{
			GoodsData gd = new GoodsData
			{
				Id = id,
				GoodsID = goodsID,
				Using = 0,
				Forge_level = forgeLevel,
				Starttime = startTime,
				Endtime = endTime,
				Site = site,
				Quality = quality,
				Props = "",
				GCount = goodsNum,
				Binding = 1,
				Jewellist = jewelList,
				BagIndex = bagIndex,
				AddPropIndex = addPropIndex,
				BornIndex = bornIndex,
				Lucky = lucky,
				Strong = strong,
				ExcellenceInfo = ExcellenceProperty,
				AppendPropLev = nAppendPropLev,
				ChangeLifeLevForEquip = nEquipChangeLife,
				WashProps = washProps
			};
			if (site == 1)
			{
				if (null == client.ClientData.MeditateGoodsDataList)
				{
					client.ClientData.MeditateGoodsDataList = new List<GoodsData>();
				}
				lock (client.ClientData.MeditateGoodsDataList)
				{
					client.ClientData.MeditateGoodsDataList.Add(gd);
				}
			}
			return gd;
		}

		
		public static int GetIdleSlotOfBag(GameClient client, int site)
		{
			int idelPos = 0;
			List<GoodsData> list = null;
			if (site == 1)
			{
				list = client.ClientData.FuWenGoodsDataList;
			}
			int result;
			if (null == list)
			{
				result = idelPos;
			}
			else
			{
				List<int> usedBagIndex = new List<int>();
				for (int i = 0; i < list.Count; i++)
				{
					if (usedBagIndex.IndexOf(list[i].BagIndex) < 0)
					{
						usedBagIndex.Add(list[i].BagIndex);
					}
				}
				int totalCount = GoodsUtil.GetMaxBagCount(site);
				for (int j = 0; j < totalCount; j++)
				{
					if (usedBagIndex.IndexOf(j) < 0)
					{
						idelPos = j;
						break;
					}
				}
				result = idelPos;
			}
			return result;
		}

		
		public static bool CanAddGoodsNum(GameClient client, int num, int site)
		{
			return client != null && num > 0;
		}

		
		public static int GetMaxBagCount(int site)
		{
			return 1000;
		}

		
		public static GoodsData GetResGoodsData(MoneyTypes type, int gcount)
		{
			GoodsData result;
			if (gcount <= 0)
			{
				result = null;
			}
			else
			{
				int goodsID = GoodsUtil.GetResGoodsID(type);
				if (goodsID == 0)
				{
					result = null;
				}
				else
				{
					GoodsData goodsData = new GoodsData
					{
						GoodsID = goodsID,
						GCount = gcount
					};
					result = goodsData;
				}
			}
			return result;
		}

		
		public static List<GoodsData> GetGoodsListBySiteFromDB(GameClient client, int site)
		{
			List<GoodsData> result;
			if (null == client)
			{
				result = null;
			}
			else if (!Array.Exists<SaleGoodsConsts>(typeof(SaleGoodsConsts).GetEnumValues() as SaleGoodsConsts[], (SaleGoodsConsts _p) => _p == (SaleGoodsConsts)site))
			{
				result = null;
			}
			else
			{
				string strDBcmd = StringUtil.substitute("{0}:{1}", new object[]
				{
					client.ClientData.RoleID,
					site
				});
				result = Global.sendToDB<List<GoodsData>, string>(204, strDBcmd, client.ServerId);
			}
			return result;
		}

		
		public static int GetMeditateBagGoodsCnt(GameClient client)
		{
			if (null == client.ClientData.MeditateGoodsDataList)
			{
				client.ClientData.MeditateGoodsDataList = GoodsUtil.GetGoodsListBySiteFromDB(client, 1);
			}
			if (null == client.ClientData.MeditateGoodsDataList)
			{
				client.ClientData.MeditateGoodsDataList = new List<GoodsData>();
			}
			return client.ClientData.MeditateGoodsDataList.Count;
		}

		
		public static void ProcessMeditateGoods(GameClient client)
		{
			int GetRewardTime = Global.GetMingXiangGoodsInterval(client);
			int givenGoodsCnt = GoodsUtil.GetMeditateBagGoodsCnt(client);
			int totalTime = client.ClientData.MeditateTime;
			int leaveTime = Global.GMax(0, totalTime - givenGoodsCnt * GetRewardTime);
			if (leaveTime >= GetRewardTime)
			{
				int cnt = leaveTime / GetRewardTime;
				int addCount = 0;
				for (int i = 0; i < cnt; i++)
				{
					if (givenGoodsCnt + 1 > Data.OfflineRW_ItemLimit)
					{
						LogManager.WriteLog(LogTypes.Info, string.Format("角色冥想背包超过{2}了,角色ID = {0} ，角色roleid = {1}", client.strUserID, client.ClientData.RoleID, Data.OfflineRW_ItemLimit), null, true);
						break;
					}
					if (null != GoodsUtil.GiveOneMeditateGood(client))
					{
						givenGoodsCnt++;
						addCount++;
					}
				}
				LogManager.WriteLog(LogTypes.Info, string.Format("玩家登陆的时候,冥想背包物品添加{3}个,现有{2}个,角色ID = {0} ，角色roleid = {1}", new object[]
				{
					client.strUserID,
					client.ClientData.RoleID,
					givenGoodsCnt,
					addCount
				}), null, true);
			}
		}

		
		public static long GetLastGiveMeditateTime(GameClient client)
		{
			return (long)(GoodsUtil.GetMeditateBagGoodsCnt(client) * Global.GetMingXiangGoodsInterval(client));
		}

		
		public static GoodsData GiveOneMeditateGood(GameClient client)
		{
			int packageID = Global.GetMingXiangPackageID(client);
			GoodsData result;
			if (0 == packageID)
			{
				result = null;
			}
			else
			{
				List<GoodsData> AwardGoodsList = GoodsBaoXiang.FetchGoodListBaseFallPacketID(client, packageID, 1, FallAlgorithm.BaoXiang);
				if (AwardGoodsList == null || AwardGoodsList.Count == 0)
				{
					result = null;
				}
				else
				{
					GoodsData tmpGoodsData = AwardGoodsList[0];
					tmpGoodsData.Site = 1;
					int dbRet = GoodsUtil.AddGoodsDBCommand(client, tmpGoodsData, false, 0, "冥想", true);
					tmpGoodsData.Id = dbRet;
					int totalTime = Global.GetRoleParamsInt32FromDB(client, "MeditateTime");
					EventLogManager.AddRoleMeditateEvent(client, (long)(totalTime / 1000), GoodsUtil.GetMeditateBagGoodsCnt(client), Global.NewGoodsDataPropString(tmpGoodsData));
					result = tmpGoodsData;
				}
			}
			return result;
		}

		
		public static bool MoveGoodsSite(GameClient client, GoodsData goodsData, int newSite)
		{
			throw new NotImplementedException();
		}

		
		private static Dictionary<int, GoodsTypeInfo> GoodsTypeInfoDict = new Dictionary<int, GoodsTypeInfo>
		{
			{
				0,
				new GoodsTypeInfo()
			},
			{
				1,
				new GoodsTypeInfo()
			},
			{
				2,
				new GoodsTypeInfo()
			},
			{
				3,
				new GoodsTypeInfo()
			},
			{
				4,
				new GoodsTypeInfo()
			},
			{
				5,
				new GoodsTypeInfo()
			},
			{
				6,
				new GoodsTypeInfo()
			},
			{
				11,
				new GoodsTypeInfo()
			},
			{
				12,
				new GoodsTypeInfo()
			},
			{
				13,
				new GoodsTypeInfo()
			},
			{
				14,
				new GoodsTypeInfo()
			},
			{
				15,
				new GoodsTypeInfo()
			},
			{
				16,
				new GoodsTypeInfo()
			},
			{
				17,
				new GoodsTypeInfo()
			},
			{
				18,
				new GoodsTypeInfo()
			},
			{
				19,
				new GoodsTypeInfo()
			},
			{
				20,
				new GoodsTypeInfo()
			},
			{
				21,
				new GoodsTypeInfo()
			},
			{
				24,
				new GoodsTypeInfo
				{
					FashionGoods = true,
					GoodsType = 3,
					UsingSite = 6000
				}
			},
			{
				25,
				new GoodsTypeInfo
				{
					FashionGoods = true,
					GoodsType = 3,
					UsingSite = 6000
				}
			},
			{
				26,
				new GoodsTypeInfo
				{
					FashionGoods = true,
					GoodsType = 3,
					UsingSite = 6000
				}
			},
			{
				27,
				new GoodsTypeInfo
				{
					FashionGoods = true,
					GoodsType = 3,
					UsingSite = 6000
				}
			},
			{
				28,
				new GoodsTypeInfo
				{
					FashionGoods = true,
					GoodsType = 3,
					UsingSite = 6000
				}
			},
			{
				40,
				new GoodsTypeInfo()
			},
			{
				41,
				new GoodsTypeInfo()
			},
			{
				42,
				new GoodsTypeInfo()
			},
			{
				43,
				new GoodsTypeInfo()
			},
			{
				44,
				new GoodsTypeInfo()
			},
			{
				45,
				new GoodsTypeInfo()
			},
			{
				30,
				new GoodsTypeInfo()
			},
			{
				31,
				new GoodsTypeInfo()
			},
			{
				32,
				new GoodsTypeInfo()
			},
			{
				33,
				new GoodsTypeInfo()
			},
			{
				34,
				new GoodsTypeInfo()
			},
			{
				35,
				new GoodsTypeInfo()
			},
			{
				36,
				new GoodsTypeInfo()
			},
			{
				37,
				new GoodsTypeInfo()
			},
			{
				38,
				new GoodsTypeInfo()
			}
		};

		
		public static Dictionary<int, int> GetGoodsIdDict = new Dictionary<int, int>();

		
		private static object _logItemLock = new object();

		
		private static Dictionary<int, int> _logItemDict_storage = null;
	}
}
