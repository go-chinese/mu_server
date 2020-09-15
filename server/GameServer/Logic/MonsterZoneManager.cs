﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Xml.Linq;
using GameServer.Logic.JingJiChang;
using Server.Protocol;
using Server.TCP;
using Server.Tools;
using Tmsk.Contract;

namespace GameServer.Logic
{
	// Token: 0x02000767 RID: 1895
	public class MonsterZoneManager
	{
		// Token: 0x0600308C RID: 12428 RVA: 0x002B2328 File Offset: 0x002B0528
		public MonsterZoneManager()
		{
			for (int i = 0; i < this.WaitingAddDynamicMonsterQueue.Length; i++)
			{
				if (null == this.WaitingAddDynamicMonsterQueue[i])
				{
					this.WaitingAddDynamicMonsterQueue[i] = new Queue<MonsterZoneQueueItem>();
				}
			}
		}

		// Token: 0x17000393 RID: 915
		// (get) Token: 0x0600308D RID: 12429 RVA: 0x002B2418 File Offset: 0x002B0618
		// (set) Token: 0x0600308E RID: 12430 RVA: 0x002B242F File Offset: 0x002B062F
		public Dictionary<int, Monster> DictDynamicMonsterSeed { get; set; }

		// Token: 0x17000394 RID: 916
		// (get) Token: 0x0600308F RID: 12431 RVA: 0x002B2438 File Offset: 0x002B0638
		// (set) Token: 0x06003090 RID: 12432 RVA: 0x002B2484 File Offset: 0x002B0684
		public XElement AllMonstersXml
		{
			get
			{
				XElement allMonstersXml;
				lock (this._allMonsterXmlMutex)
				{
					allMonstersXml = this._allMonstersXml;
				}
				return allMonstersXml;
			}
			set
			{
				lock (this._allMonsterXmlMutex)
				{
					this._allMonstersXml = value;
				}
			}
		}

		// Token: 0x06003091 RID: 12433 RVA: 0x002B24D0 File Offset: 0x002B06D0
		public void LoadAllMonsterXml()
		{
			XElement tmpXml = null;
			try
			{
				tmpXml = XElement.Load(Global.GameResPath("Config/Monsters.xml"));
			}
			catch (Exception ex)
			{
			}
			if (tmpXml != null)
			{
				this.AllMonstersXml = tmpXml;
			}
		}

		// Token: 0x06003092 RID: 12434 RVA: 0x002B251C File Offset: 0x002B071C
		private void AddMap2MonsterZoneDict(MonsterZone monsterZone)
		{
			List<MonsterZone> monsterZoneList = null;
			if (this.Map2MonsterZoneDict.TryGetValue(monsterZone.MapCode, out monsterZoneList))
			{
				monsterZoneList.Add(monsterZone);
			}
			else
			{
				monsterZoneList = new List<MonsterZone>();
				this.Map2MonsterZoneDict[monsterZone.MapCode] = monsterZoneList;
				monsterZoneList.Add(monsterZone);
			}
		}

		// Token: 0x06003093 RID: 12435 RVA: 0x002B2574 File Offset: 0x002B0774
		private List<BirthTimePoint> ParseBirthTimePoints(string s)
		{
			List<BirthTimePoint> result;
			if (string.IsNullOrEmpty(s))
			{
				result = null;
			}
			else
			{
				string[] fields = s.Split(new char[]
				{
					'|'
				});
				if (fields.Length <= 0)
				{
					result = null;
				}
				else
				{
					List<BirthTimePoint> list = new List<BirthTimePoint>();
					for (int i = 0; i < fields.Length; i++)
					{
						if (!string.IsNullOrEmpty(fields[i]))
						{
							string[] fields2 = fields[i].Split(new char[]
							{
								':'
							});
							if (fields2.Length == 2)
							{
								string str = fields2[0].TrimStart(new char[]
								{
									'0'
								});
								string str2 = fields2[1].TrimStart(new char[]
								{
									'0'
								});
								BirthTimePoint birthTimePoint = new BirthTimePoint
								{
									BirthHour = Global.SafeConvertToInt32(str),
									BirthMinute = Global.SafeConvertToInt32(str2)
								};
								list.Add(birthTimePoint);
							}
						}
					}
					result = ((list.Count > 0) ? list : null);
				}
			}
			return result;
		}

		// Token: 0x06003094 RID: 12436 RVA: 0x002B26A0 File Offset: 0x002B08A0
		public void AddMapMonsters(int mapCode, GameMap gameMap)
		{
			this.AddDynamicMonsterZone(mapCode);
			string fileName = string.Format("Map/{0}/Monsters.xml", mapCode);
			XElement xml = null;
			try
			{
				xml = XElement.Load(Global.ResPath(fileName));
			}
			catch (Exception)
			{
				throw new Exception(string.Format("加载地图怪物配置文件:{0}, 失败。没有找到相关XML配置文件!", fileName));
			}
			IEnumerable<XElement> monsterItems = xml.Elements("Monsters").Elements<XElement>();
			if (null != monsterItems)
			{
				bool isFuBenMap = FuBenManager.IsFuBenMap(mapCode);
				foreach (XElement monsterItem in monsterItems)
				{
					string timePoints = Global.GetSafeAttributeStr(monsterItem, "TimePoints");
					int configBirthType = (int)Global.GetSafeAttributeLong(monsterItem, "BirthType");
					int realBirthType = configBirthType;
					string realTimePoints = timePoints;
					int spawnMonstersAfterKaiFuDays = 0;
					int spawnMonstersDays = 0;
					List<BirthTimeForDayOfWeek> CreateMonstersDayOfWeek = new List<BirthTimeForDayOfWeek>();
					List<BirthTimePoint> birthTimePointList = null;
					if (4 == configBirthType || 5 == configBirthType || 6 == configBirthType)
					{
						string[] arr = timePoints.Split(new char[]
						{
							';'
						});
						if (4 != arr.Length)
						{
							throw new Exception(string.Format("地图{0}的类型4的刷怪配置参数个数不对!!!!", mapCode));
						}
						spawnMonstersAfterKaiFuDays = int.Parse(arr[0]);
						spawnMonstersDays = int.Parse(arr[1]);
						realBirthType = int.Parse(arr[2]);
						realTimePoints = arr[3];
						if (1 != realBirthType && 0 != realBirthType)
						{
							throw new Exception(string.Format("地图{0}的类型4的刷怪配置子类型不对!!!!", mapCode));
						}
					}
					if (7 == configBirthType)
					{
						string[] arrTime = timePoints.Split(new char[]
						{
							'|'
						});
						if (arrTime.Length > 0)
						{
							int nIndex = 0;
							while (nIndex < arrTime.Length)
							{
								string sTimePoint = arrTime[nIndex];
								if (sTimePoint != null)
								{
									string[] sTime = sTimePoint.Split(new char[]
									{
										','
									});
									if (sTime != null && sTime.Length == 2)
									{
										int nDayOfWeek = int.Parse(sTime[0]);
										string sTimeString = sTime[1];
										if (nDayOfWeek != -1 && !string.IsNullOrEmpty(sTimeString))
										{
											string[] fields2 = sTimeString.Split(new char[]
											{
												':'
											});
											if (fields2.Length == 2)
											{
												string str = fields2[0].TrimStart(new char[]
												{
													'0'
												});
												string str2 = fields2[1].TrimStart(new char[]
												{
													'0'
												});
												BirthTimePoint birthTimePoint = new BirthTimePoint
												{
													BirthHour = Global.SafeConvertToInt32(str),
													BirthMinute = Global.SafeConvertToInt32(str2)
												};
												CreateMonstersDayOfWeek.Add(new BirthTimeForDayOfWeek
												{
													BirthDayOfWeek = nDayOfWeek,
													BirthTime = birthTimePoint
												});
											}
										}
									}
								}
								IL_2E5:
								nIndex++;
								continue;
								goto IL_2E5;
							}
						}
					}
					else
					{
						birthTimePointList = this.ParseBirthTimePoints(realTimePoints);
					}
					MonsterZone monsterZone = new MonsterZone
					{
						MapCode = mapCode,
						ID = (int)Global.GetSafeAttributeLong(monsterItem, "ID"),
						Code = (int)Global.GetSafeAttributeLong(monsterItem, "Code"),
						ToX = (int)Global.GetSafeAttributeLong(monsterItem, "X") / gameMap.MapGridWidth,
						ToY = (int)Global.GetSafeAttributeLong(monsterItem, "Y") / gameMap.MapGridHeight,
						Radius = (int)Global.GetSafeAttributeLong(monsterItem, "Radius") / gameMap.MapGridWidth,
						TotalNum = (int)Global.GetSafeAttributeLong(monsterItem, "Num"),
						Timeslot = (int)Global.GetSafeAttributeLong(monsterItem, "Timeslot"),
						IsFuBenMap = isFuBenMap,
						BirthType = realBirthType,
						ConfigBirthType = configBirthType,
						SpawnMonstersAfterKaiFuDays = spawnMonstersAfterKaiFuDays,
						SpawnMonstersDays = spawnMonstersDays,
						SpawnMonstersDayOfWeek = CreateMonstersDayOfWeek,
						BirthTimePointList = birthTimePointList,
						BirthRate = (int)(Global.GetSafeAttributeDouble(monsterItem, "BirthRate") * 10000.0)
					};
					XAttribute attrib = monsterItem.Attribute("PursuitRadius");
					if (null != attrib)
					{
						monsterZone.PursuitRadius = (int)Global.GetSafeAttributeLong(monsterItem, "PursuitRadius");
					}
					else
					{
						monsterZone.PursuitRadius = (int)Global.GetSafeAttributeLong(monsterItem, "Radius");
					}
					lock (this.InitMonsterZoneMutex)
					{
						this.MonsterZoneList.Add(monsterZone);
						if (isFuBenMap)
						{
							this.FuBenMonsterZoneList.Add(monsterZone);
						}
						this.AddMap2MonsterZoneDict(monsterZone);
					}
					monsterZone.LoadStaticMonsterInfo_2();
					monsterZone.LoadMonsters();
				}
			}
		}

		// Token: 0x06003095 RID: 12437 RVA: 0x002B2C08 File Offset: 0x002B0E08
		public void RunMapMonsters(SocketListener sl, TCPOutPacketPool pool)
		{
			for (int i = 0; i < this.MonsterZoneList.Count; i++)
			{
				this.MonsterZoneList[i].ReloadMonsters(sl, pool);
			}
			for (int i = 0; i < this.FuBenMonsterZoneList.Count; i++)
			{
				this.FuBenMonsterZoneList[i].DestroyDeadMonsters(true);
			}
			List<MonsterZone> monsterZoneList = this.MonsterDynamicZoneDict.Values.ToList<MonsterZone>();
			for (int i = 0; i < monsterZoneList.Count; i++)
			{
				monsterZoneList[i].DestroyDeadDynamicMonsters();
			}
		}

		// Token: 0x06003096 RID: 12438 RVA: 0x002B2CAC File Offset: 0x002B0EAC
		public void RunMapDynamicMonsters(SocketListener sl, TCPOutPacketPool pool)
		{
			for (int i = 0; i < MonsterZoneManager.MaxRunQueueNum; i++)
			{
				if (!this.RunAddCopyMapMonsters())
				{
					break;
				}
			}
			for (int i = 0; i < MonsterZoneManager.MaxRunQueueNum; i++)
			{
				if (!this.RunDestroyCopyMapMonsters())
				{
					break;
				}
			}
			for (int i = 0; i < MonsterZoneManager.MaxRunQueueNum; i++)
			{
				if (!this.RunReloadCopyMapMonsters())
				{
					break;
				}
			}
			for (int i = 0; i < MonsterZoneManager.MaxRunQueueNum; i++)
			{
				if (!this.RunReloadNormalMapMonsters())
				{
					break;
				}
			}
			for (int i = 0; i < MonsterZoneManager.MaxRunAddDynamicMonstersQueueNum; i++)
			{
				if (!this.RunAddRobots())
				{
					break;
				}
			}
			int Count = 0;
			int loop_Count = 0;
			while (Count < MonsterZoneManager.MaxRunAddDynamicMonstersQueueNum)
			{
				for (int i = 0; i < 10; i++)
				{
					loop_Count++;
					if (this.RunAddDynamicMonsters(i))
					{
						Count++;
					}
				}
				if (loop_Count >= MonsterZoneManager.MaxRunQueueNum)
				{
					break;
				}
			}
		}

		// Token: 0x06003097 RID: 12439 RVA: 0x002B2DB8 File Offset: 0x002B0FB8
		public int WaitingAddFuBenMonsterQueueCount()
		{
			int count;
			lock (this.WaitingAddFuBenMonsterQueue)
			{
				count = this.WaitingAddFuBenMonsterQueue.Count;
			}
			return count;
		}

		// Token: 0x06003098 RID: 12440 RVA: 0x002B2E0C File Offset: 0x002B100C
		private bool RunAddCopyMapMonsters()
		{
			MonsterZoneQueueItem monsterZoneQueueItem = null;
			lock (this.WaitingAddFuBenMonsterQueue)
			{
				if (this.WaitingAddFuBenMonsterQueue.Count > 0)
				{
					monsterZoneQueueItem = this.WaitingAddFuBenMonsterQueue.Dequeue();
				}
			}
			bool result;
			if (null != monsterZoneQueueItem)
			{
				monsterZoneQueueItem.MyMonsterZone.LoadCopyMapMonsters(monsterZoneQueueItem.CopyMapID);
				result = true;
			}
			else
			{
				result = false;
			}
			return result;
		}

		// Token: 0x06003099 RID: 12441 RVA: 0x002B2EA0 File Offset: 0x002B10A0
		public int WaitingDestroyFuBenMonsterQueueCount()
		{
			int count;
			lock (this.WaitingDestroyFuBenMonsterQueue)
			{
				count = this.WaitingDestroyFuBenMonsterQueue.Count;
			}
			return count;
		}

		// Token: 0x0600309A RID: 12442 RVA: 0x002B2EF4 File Offset: 0x002B10F4
		private bool RunDestroyCopyMapMonsters()
		{
			MonsterZoneQueueItem monsterZoneQueueItem = null;
			lock (this.WaitingDestroyFuBenMonsterQueue)
			{
				if (this.WaitingDestroyFuBenMonsterQueue.Count > 0)
				{
					monsterZoneQueueItem = this.WaitingDestroyFuBenMonsterQueue.Dequeue();
				}
			}
			bool result;
			if (null != monsterZoneQueueItem)
			{
				monsterZoneQueueItem.MyMonsterZone.ClearCopyMapMonsters(monsterZoneQueueItem.CopyMapID);
				result = true;
			}
			else
			{
				result = false;
			}
			return result;
		}

		// Token: 0x0600309B RID: 12443 RVA: 0x002B2F88 File Offset: 0x002B1188
		public int WaitingReloadFuBenMonsterQueueCount()
		{
			int count;
			lock (this.WaitingReloadFuBenMonsterQueue)
			{
				count = this.WaitingReloadFuBenMonsterQueue.Count;
			}
			return count;
		}

		// Token: 0x0600309C RID: 12444 RVA: 0x002B2FDC File Offset: 0x002B11DC
		private bool RunReloadCopyMapMonsters()
		{
			MonsterZoneQueueItem monsterZoneQueueItem = null;
			lock (this.WaitingReloadFuBenMonsterQueue)
			{
				if (this.WaitingReloadFuBenMonsterQueue.Count > 0)
				{
					monsterZoneQueueItem = this.WaitingReloadFuBenMonsterQueue.Dequeue();
				}
			}
			bool result;
			if (null != monsterZoneQueueItem)
			{
				monsterZoneQueueItem.MyMonsterZone.ReloadCopyMapMonsters(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, monsterZoneQueueItem.CopyMapID);
				result = true;
			}
			else
			{
				result = false;
			}
			return result;
		}

		// Token: 0x0600309D RID: 12445 RVA: 0x002B3084 File Offset: 0x002B1284
		public void AddCopyMapMonsters(int mapCode, int copyMapID)
		{
			List<MonsterZone> monsterZoneList = null;
			if (this.Map2MonsterZoneDict.TryGetValue(mapCode, out monsterZoneList))
			{
				for (int i = 0; i < monsterZoneList.Count; i++)
				{
					lock (this.WaitingAddFuBenMonsterQueue)
					{
						this.WaitingAddFuBenMonsterQueue.Enqueue(new MonsterZoneQueueItem
						{
							CopyMapID = copyMapID,
							BirthCount = 0,
							MyMonsterZone = monsterZoneList[i]
						});
					}
				}
			}
		}

		// Token: 0x0600309E RID: 12446 RVA: 0x002B3130 File Offset: 0x002B1330
		public void DestroyCopyMapMonsters(int mapCode, int copyMapID)
		{
			List<MonsterZone> monsterZoneList = null;
			if (this.Map2MonsterZoneDict.TryGetValue(mapCode, out monsterZoneList))
			{
				for (int i = 0; i < monsterZoneList.Count; i++)
				{
					lock (this.WaitingDestroyFuBenMonsterQueue)
					{
						this.WaitingDestroyFuBenMonsterQueue.Enqueue(new MonsterZoneQueueItem
						{
							CopyMapID = copyMapID,
							BirthCount = 0,
							MyMonsterZone = monsterZoneList[i]
						});
					}
				}
			}
		}

		// Token: 0x0600309F RID: 12447 RVA: 0x002B31DC File Offset: 0x002B13DC
		public void ReloadCopyMapMonsters(int mapCode, int copyMapID)
		{
			List<MonsterZone> monsterZoneList = null;
			if (this.Map2MonsterZoneDict.TryGetValue(mapCode, out monsterZoneList))
			{
				for (int i = 0; i < monsterZoneList.Count; i++)
				{
					lock (this.WaitingReloadFuBenMonsterQueue)
					{
						this.WaitingReloadFuBenMonsterQueue.Enqueue(new MonsterZoneQueueItem
						{
							CopyMapID = copyMapID,
							BirthCount = 0,
							MyMonsterZone = monsterZoneList[i]
						});
					}
				}
			}
		}

		// Token: 0x060030A0 RID: 12448 RVA: 0x002B3288 File Offset: 0x002B1488
		public int GetMapTotalMonsterNum(int mapCode, MonsterTypes monsterType, bool excludePets = true)
		{
			List<MonsterZone> monsterZoneList = null;
			int result;
			if (!this.Map2MonsterZoneDict.TryGetValue(mapCode, out monsterZoneList))
			{
				result = 0;
			}
			else
			{
				int totalNum = 0;
				int i = 0;
				while (i < monsterZoneList.Count)
				{
					if (MonsterTypes.None == monsterType)
					{
						goto IL_47;
					}
					if (monsterZoneList[i].MonsterType == monsterType)
					{
						goto IL_47;
					}
					IL_76:
					i++;
					continue;
					IL_47:
					if (excludePets && monsterZoneList[i].IsDynamicZone())
					{
						goto IL_76;
					}
					totalNum += monsterZoneList[i].TotalNum;
					goto IL_76;
				}
				result = totalNum;
			}
			return result;
		}

		// Token: 0x060030A1 RID: 12449 RVA: 0x002B3324 File Offset: 0x002B1524
		public int GetMapMonsterNum(int mapCode, int nMonsterID)
		{
			List<MonsterZone> monsterZoneList = null;
			int result;
			if (!this.Map2MonsterZoneDict.TryGetValue(mapCode, out monsterZoneList))
			{
				result = 0;
			}
			else
			{
				int nCount = 0;
				for (int i = 0; i < monsterZoneList.Count; i++)
				{
					MonsterStaticInfo monsterInfo = monsterZoneList[i].GetMonsterInfo();
					if (monsterInfo != null)
					{
						if (monsterInfo.ExtensionID == nMonsterID)
						{
							nCount += monsterZoneList[i].TotalNum;
						}
					}
				}
				result = nCount;
			}
			return result;
		}

		// Token: 0x060030A2 RID: 12450 RVA: 0x002B33B0 File Offset: 0x002B15B0
		public bool GetMonsterBirthPoint(int mapCode, int nMonsterID, out int posX, out int posY, out int radis)
		{
			posX = 0;
			posY = 0;
			radis = 0;
			GameMap gameMap = null;
			bool result;
			if (!GameManager.MapMgr.DictMaps.TryGetValue(mapCode, out gameMap))
			{
				result = false;
			}
			else
			{
				List<MonsterZone> monsterZoneList = null;
				if (!this.Map2MonsterZoneDict.TryGetValue(mapCode, out monsterZoneList))
				{
					result = false;
				}
				else
				{
					for (int i = 0; i < monsterZoneList.Count; i++)
					{
						MonsterZone zone = monsterZoneList[i];
						if (zone.Code == nMonsterID)
						{
							Point p = Global.GridToPixel(mapCode, (double)zone.ToX, (double)zone.ToY);
							posX = (int)p.X;
							posY = (int)p.Y;
							return true;
						}
					}
					result = false;
				}
			}
			return result;
		}

		// Token: 0x060030A3 RID: 12451 RVA: 0x002B3474 File Offset: 0x002B1674
		private bool RunReloadNormalMapMonsters()
		{
			MonsterZoneQueueItem monsterZoneQueueItem = null;
			lock (this.WaitingReloadNormalMapMonsterQueue)
			{
				if (this.WaitingReloadNormalMapMonsterQueue.Count > 0)
				{
					monsterZoneQueueItem = this.WaitingReloadNormalMapMonsterQueue.Dequeue();
				}
			}
			bool result;
			if (null != monsterZoneQueueItem)
			{
				monsterZoneQueueItem.MyMonsterZone.ReloadNormalMapMonsters(Global._TCPManager.MySocketListener, Global._TCPManager.TcpOutPacketPool, monsterZoneQueueItem.BirthCount);
				result = true;
			}
			else
			{
				result = false;
			}
			return result;
		}

		// Token: 0x060030A4 RID: 12452 RVA: 0x002B351C File Offset: 0x002B171C
		public void ReloadNormalMapMonsters(int mapCode, int birthCount)
		{
			List<MonsterZone> monsterZoneList = null;
			if (this.Map2MonsterZoneDict.TryGetValue(mapCode, out monsterZoneList))
			{
				for (int i = 0; i < monsterZoneList.Count; i++)
				{
					lock (this.WaitingReloadNormalMapMonsterQueue)
					{
						this.WaitingReloadNormalMapMonsterQueue.Enqueue(new MonsterZoneQueueItem
						{
							CopyMapID = -1,
							BirthCount = birthCount,
							MyMonsterZone = monsterZoneList[i]
						});
					}
				}
			}
		}

		// Token: 0x060030A5 RID: 12453 RVA: 0x002B35C8 File Offset: 0x002B17C8
		public MonsterZone GetDynamicMonsterZone(int mapCode)
		{
			MonsterZone zone = null;
			MonsterZone result;
			if (this.MonsterDynamicZoneDict.TryGetValue(mapCode, out zone))
			{
				result = zone;
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060030A6 RID: 12454 RVA: 0x002B35F8 File Offset: 0x002B17F8
		public void AddDynamicMonsterZone(int mapCode)
		{
			bool isFuBenMap = FuBenManager.IsFuBenMap(mapCode);
			MonsterZone monsterZone = new MonsterZone
			{
				MapCode = mapCode,
				ID = this.MonsterDynamicZoneDict.Count + 10000,
				Code = -1,
				ToX = -1,
				ToY = -1,
				Radius = 300,
				TotalNum = 0,
				Timeslot = 1,
				IsFuBenMap = isFuBenMap,
				BirthType = 3,
				ConfigBirthType = -1,
				BirthTimePointList = null,
				BirthRate = 10000
			};
			monsterZone.PursuitRadius = 0;
			lock (this.InitMonsterZoneMutex)
			{
				this.MonsterDynamicZoneDict.Add(mapCode, monsterZone);
				this.MonsterZoneList.Add(monsterZone);
				if (isFuBenMap)
				{
					this.FuBenMonsterZoneList.Add(monsterZone);
				}
				this.AddMap2MonsterZoneDict(monsterZone);
			}
		}

		// Token: 0x060030A7 RID: 12455 RVA: 0x002B3714 File Offset: 0x002B1914
		private void InitDynamicMonsterSeedByMonserID(int monsterID)
		{
			MonsterZone monsterZone = new MonsterZone();
			Monster myMonster = null;
			if (!this._DictDynamicMonsterSeed.TryGetValue(monsterID, out myMonster) || null == myMonster)
			{
				int ID = 1;
				lock (this._DictDynamicMonsterSeed)
				{
					ID = this._DictDynamicMonsterSeed.Count + 1;
				}
				monsterZone.MapCode = 1;
				monsterZone.ID = ID;
				monsterZone.Code = monsterID;
				monsterZone.LoadStaticMonsterInfo();
				myMonster = monsterZone.LoadDynamicMonsterSeed();
				lock (this._DictDynamicMonsterSeed)
				{
					if (!this._DictDynamicMonsterSeed.ContainsKey(monsterID))
					{
						this._DictDynamicMonsterSeed.Add(monsterID, myMonster);
					}
				}
			}
		}

		// Token: 0x060030A8 RID: 12456 RVA: 0x002B381C File Offset: 0x002B1A1C
		public Monster GetDynamicMonsterSeed(int monsterID)
		{
			Monster monster = null;
			lock (this._DictDynamicMonsterSeed)
			{
				if (this._DictDynamicMonsterSeed.TryGetValue(monsterID, out monster))
				{
					return monster;
				}
			}
			try
			{
				this.InitDynamicMonsterSeedByMonserID(monsterID);
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "InitDynamicMonsterSeed()", false, false);
			}
			lock (this._DictDynamicMonsterSeed)
			{
				this._DictDynamicMonsterSeed.TryGetValue(monsterID, out monster);
			}
			return monster;
		}

		// Token: 0x060030A9 RID: 12457 RVA: 0x002B3900 File Offset: 0x002B1B00
		public Monster GetMonsterByMonsterID(int monsterID)
		{
			return this.GetDynamicMonsterSeed(monsterID);
		}

		// Token: 0x060030AA RID: 12458 RVA: 0x002B391C File Offset: 0x002B1B1C
		public void AddDynamicRobot(int mapCode, Robot robot, int copyMapID = -1, int addNum = 1, int gridX = 0, int gridY = 0, int radius = 3, int pursuitRadius = 0, SceneUIClasses managerType = SceneUIClasses.Normal, object tag = null)
		{
			this.TraceAllDynamicMonsters();
			MonsterZone monsterZone = null;
			if (this.MonsterDynamicZoneDict.TryGetValue(mapCode, out monsterZone))
			{
				robot.MonsterZoneNode = monsterZone;
				lock (this.WaitingReloadRobotQueue)
				{
					this.WaitingReloadRobotQueue.Enqueue(new MonsterZoneQueueItem
					{
						CopyMapID = copyMapID,
						BirthCount = addNum,
						MyMonsterZone = monsterZone,
						seedMonster = robot,
						ToX = gridX,
						ToY = gridY,
						Radius = radius,
						PursuitRadius = pursuitRadius,
						Tag = tag,
						ManagerType = managerType
					});
				}
			}
		}

		// Token: 0x060030AB RID: 12459 RVA: 0x002B39EC File Offset: 0x002B1BEC
		public Monster AddDynamicMonsters(int mapCode, int monsterID, int copyMapID = -1, int addNum = 1, int gridX = 0, int gridY = 0, int radius = 3, int pursuitRadius = 0, SceneUIClasses managerType = SceneUIClasses.Normal, object tag = null, MonsterFlags flags = null)
		{
			this.TraceAllDynamicMonsters();
			MonsterZone monsterZone = null;
			Monster result;
			if (!this.MonsterDynamicZoneDict.TryGetValue(mapCode, out monsterZone))
			{
				result = null;
			}
			else
			{
				Monster seedMonster = this.GetDynamicMonsterSeed(monsterID);
				if (null == seedMonster)
				{
					result = null;
				}
				else
				{
					int index;
					if (copyMapID >= 0)
					{
						index = Global.Clamp(copyMapID % 10, 0, 9);
					}
					else
					{
						index = Global.Clamp(mapCode % 10, 0, 9);
					}
					lock (this.WaitingAddDynamicMonsterQueue)
					{
						this.WaitingAddDynamicMonsterQueue[index].Enqueue(new MonsterZoneQueueItem
						{
							CopyMapID = copyMapID,
							BirthCount = addNum,
							MyMonsterZone = monsterZone,
							seedMonster = seedMonster,
							ToX = gridX,
							ToY = gridY,
							Radius = radius,
							PursuitRadius = pursuitRadius,
							Tag = tag,
							ManagerType = managerType,
							Flags = flags
						});
					}
					result = seedMonster;
				}
			}
			return result;
		}

		// Token: 0x060030AC RID: 12460 RVA: 0x002B3B18 File Offset: 0x002B1D18
		public bool CallDynamicMonstersOwnedByRole(GameClient client, int monsterID, int magicLevel, int SurvivalTime, int callAsType = 1001, int callNum = 1, int pursuitRadius = 0)
		{
			this.TraceAllDynamicMonsters();
			int mapCode = client.ClientData.MapCode;
			int copyMapID = client.ClientData.CopyMapID;
			Point grid = client.CurrentGrid;
			int gridX = (int)grid.X;
			int gridY = (int)grid.Y;
			int radius = 3;
			MonsterZone monsterZone = null;
			bool result;
			if (!this.MonsterDynamicZoneDict.TryGetValue(mapCode, out monsterZone))
			{
				result = false;
			}
			else
			{
				Monster seedMonster = this.GetDynamicMonsterSeed(monsterID);
				if (null == seedMonster)
				{
					result = false;
				}
				else
				{
					Monster realSeedMonster = seedMonster.Clone();
					realSeedMonster.MonsterInfo = realSeedMonster.MonsterInfo.Clone();
					realSeedMonster.MonsterType = callAsType;
					realSeedMonster.OwnerClient = client;
					if (callAsType == 1001)
					{
						Global.RecalcDSMonsterProps(client, realSeedMonster, magicLevel, SurvivalTime);
					}
					int index;
					if (client.ClientData.CopyMapID >= 0)
					{
						index = Global.Clamp(client.ClientData.CopyMapID % 10, 0, 9);
					}
					else
					{
						index = Global.Clamp(client.ClientData.MapCode % 10, 0, 9);
					}
					lock (this.WaitingAddDynamicMonsterQueue)
					{
						this.WaitingAddDynamicMonsterQueue[index].Enqueue(new MonsterZoneQueueItem
						{
							CopyMapID = copyMapID,
							BirthCount = callNum,
							MyMonsterZone = monsterZone,
							seedMonster = realSeedMonster,
							ToX = gridX,
							ToY = gridY,
							Radius = radius,
							PursuitRadius = pursuitRadius
						});
					}
					result = true;
				}
			}
			return result;
		}

		// Token: 0x060030AD RID: 12461 RVA: 0x002B3CE0 File Offset: 0x002B1EE0
		public bool CallDynamicMonstersOwnedByMonster(Monster owner, int monsterID, int magicLevel, int SurvivalTime, int callAsType = 1001, int callNum = 1, int pursuitRadius = 0)
		{
			this.TraceAllDynamicMonsters();
			int mapCode = owner.CurrentMapCode;
			int copyMapID = owner.CopyMapID;
			Point grid = owner.CurrentGrid;
			int gridX = (int)grid.X;
			int gridY = (int)grid.Y;
			int radius = 3;
			MonsterZone monsterZone = this.GetDynamicMonsterZone(mapCode);
			bool result;
			if (null == monsterZone)
			{
				result = false;
			}
			else
			{
				Monster seedMonster = this.GetDynamicMonsterSeed(monsterID);
				if (null == seedMonster)
				{
					result = false;
				}
				else
				{
					Monster realSeedMonster = seedMonster.Clone();
					realSeedMonster.MonsterInfo = realSeedMonster.MonsterInfo.Clone();
					realSeedMonster.MonsterType = callAsType;
					realSeedMonster.OwnerMonster = owner;
					if (callAsType == 1001)
					{
						Global.RecalcDSMonsterProps(owner, realSeedMonster, magicLevel, SurvivalTime);
					}
					int index;
					if (owner.CurrentCopyMapID >= 0)
					{
						index = Global.Clamp(owner.CurrentCopyMapID % 10, 0, 9);
					}
					else
					{
						index = Global.Clamp(owner.CurrentCopyMapID % 10, 0, 9);
					}
					lock (this.WaitingAddDynamicMonsterQueue)
					{
						this.WaitingAddDynamicMonsterQueue[index].Enqueue(new MonsterZoneQueueItem
						{
							CopyMapID = copyMapID,
							BirthCount = callNum,
							MyMonsterZone = monsterZone,
							seedMonster = realSeedMonster,
							ToX = gridX,
							ToY = gridY,
							Radius = radius,
							PursuitRadius = pursuitRadius
						});
					}
					result = true;
				}
			}
			return result;
		}

		// Token: 0x060030AE RID: 12462 RVA: 0x002B3E90 File Offset: 0x002B2090
		private bool RunAddDynamicMonsters(int index)
		{
			bool result;
			if (index < 0 || index >= 10)
			{
				result = false;
			}
			else
			{
				MonsterZoneQueueItem monsterZoneQueueItem = null;
				lock (this.WaitingAddDynamicMonsterQueue)
				{
					if (this.WaitingAddDynamicMonsterQueue[index].Count > 0)
					{
						monsterZoneQueueItem = this.WaitingAddDynamicMonsterQueue[index].Dequeue();
					}
				}
				if (null != monsterZoneQueueItem)
				{
					monsterZoneQueueItem.MyMonsterZone.LoadDynamicMonsters(monsterZoneQueueItem);
					result = true;
				}
				else
				{
					result = false;
				}
			}
			return result;
		}

		// Token: 0x060030AF RID: 12463 RVA: 0x002B3F34 File Offset: 0x002B2134
		private bool RunAddRobots()
		{
			MonsterZoneQueueItem monsterZoneQueueItem = null;
			lock (this.WaitingReloadRobotQueue)
			{
				if (this.WaitingReloadRobotQueue.Count > 0)
				{
					monsterZoneQueueItem = this.WaitingReloadRobotQueue.Dequeue();
				}
			}
			bool result;
			if (null != monsterZoneQueueItem)
			{
				monsterZoneQueueItem.MyMonsterZone.LoadDynamicRobot(monsterZoneQueueItem);
				result = true;
			}
			else
			{
				result = false;
			}
			return result;
		}

		// Token: 0x060030B0 RID: 12464 RVA: 0x002B3FC4 File Offset: 0x002B21C4
		protected void TraceAllDynamicMonsters()
		{
		}

		// Token: 0x060030B1 RID: 12465 RVA: 0x002B3FC8 File Offset: 0x002B21C8
		public List<MonsterZone> GetMonsterZoneListByMapCode(int mapCode)
		{
			List<MonsterZone> list = null;
			this.Map2MonsterZoneDict.TryGetValue(mapCode, out list);
			return list;
		}

		// Token: 0x060030B2 RID: 12466 RVA: 0x002B3FEC File Offset: 0x002B21EC
		public List<MonsterZone> GetMonsterZoneByMapCodeAndMonsterID(int mapCode, int monsterID)
		{
			List<MonsterZone> list2 = new List<MonsterZone>();
			List<MonsterZone> list3 = this.GetMonsterZoneListByMapCode(mapCode);
			List<MonsterZone> result;
			if (null == list3)
			{
				result = list2;
			}
			else
			{
				for (int i = 0; i < list3.Count; i++)
				{
					if (monsterID == list3[i].Code)
					{
						list2.Add(list3[i]);
					}
				}
				result = list2;
			}
			return result;
		}

		// Token: 0x060030B3 RID: 12467 RVA: 0x002B4060 File Offset: 0x002B2260
		public Point GetMonsterPointByMapCodeAndMonsterID(int mapCode, int monsterID)
		{
			Point pt = new Point(-1.0, -1.0);
			List<MonsterZone> monsterZoneList = this.GetMonsterZoneByMapCodeAndMonsterID(mapCode, monsterID);
			Point result;
			if (monsterZoneList == null || monsterZoneList.Count <= 0)
			{
				result = pt;
			}
			else
			{
				List<Point> ptList = new List<Point>();
				for (int i = 0; i < monsterZoneList.Count; i++)
				{
					Point targetPoint = Global.GetMapPointByGridXY(ObjectTypes.OT_CLIENT, monsterZoneList[i].MapCode, monsterZoneList[i].ToX, monsterZoneList[i].ToY, monsterZoneList[i].Radius, 0, false);
					ptList.Add(targetPoint);
				}
				if (ptList.Count <= 0)
				{
					result = new Point(-1.0, -1.0);
				}
				else
				{
					result = ptList[Global.GetRandomNumber(0, ptList.Count)];
				}
			}
			return result;
		}

		// Token: 0x04003D4D RID: 15693
		private const int Max_WaitingAddDynamicMonsterQueneCount = 10;

		// Token: 0x04003D4E RID: 15694
		public static int MaxRunQueueNum = 100;

		// Token: 0x04003D4F RID: 15695
		public static int MaxWaitingRunQueueNum = 200;

		// Token: 0x04003D50 RID: 15696
		public static int MaxRunAddDynamicMonstersQueueNum = 30;

		// Token: 0x04003D51 RID: 15697
		private Dictionary<int, MonsterZone> MonsterDynamicZoneDict = new Dictionary<int, MonsterZone>(100);

		// Token: 0x04003D52 RID: 15698
		private List<MonsterZone> MonsterZoneList = new List<MonsterZone>(100);

		// Token: 0x04003D53 RID: 15699
		private List<MonsterZone> FuBenMonsterZoneList = new List<MonsterZone>(100);

		// Token: 0x04003D54 RID: 15700
		private Dictionary<int, List<MonsterZone>> Map2MonsterZoneDict = new Dictionary<int, List<MonsterZone>>(100);

		// Token: 0x04003D55 RID: 15701
		private Queue<MonsterZoneQueueItem> WaitingAddFuBenMonsterQueue = new Queue<MonsterZoneQueueItem>();

		// Token: 0x04003D56 RID: 15702
		private Queue<MonsterZoneQueueItem> WaitingDestroyFuBenMonsterQueue = new Queue<MonsterZoneQueueItem>();

		// Token: 0x04003D57 RID: 15703
		private Queue<MonsterZoneQueueItem> WaitingReloadFuBenMonsterQueue = new Queue<MonsterZoneQueueItem>();

		// Token: 0x04003D58 RID: 15704
		private Queue<MonsterZoneQueueItem> WaitingReloadNormalMapMonsterQueue = new Queue<MonsterZoneQueueItem>();

		// Token: 0x04003D59 RID: 15705
		private Queue<MonsterZoneQueueItem>[] WaitingAddDynamicMonsterQueue = new Queue<MonsterZoneQueueItem>[10];

		// Token: 0x04003D5A RID: 15706
		private Queue<MonsterZoneQueueItem> WaitingReloadRobotQueue = new Queue<MonsterZoneQueueItem>();

		// Token: 0x04003D5B RID: 15707
		private Dictionary<int, Monster> _DictDynamicMonsterSeed = new Dictionary<int, Monster>();

		// Token: 0x04003D5C RID: 15708
		private XElement _allMonstersXml = null;

		// Token: 0x04003D5D RID: 15709
		private object _allMonsterXmlMutex = new object();

		// Token: 0x04003D5E RID: 15710
		private object InitMonsterZoneMutex = new object();
	}
}
