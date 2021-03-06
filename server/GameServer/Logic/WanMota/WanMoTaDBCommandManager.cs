﻿using System;
using GameServer.Core.Executor;
using Server.Data;
using Server.Tools;

namespace GameServer.Logic.WanMota
{
	
	public class WanMoTaDBCommandManager
	{
		
		public static int LayerChangeDBCommand(GameClient client, int nLayerCount)
		{
			long lFlushTime = TimeUtil.NOW();
			string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", new object[]
			{
				client.ClientData.RoleID,
				lFlushTime,
				nLayerCount,
				"*",
				"*",
				"*"
			});
			ModifyWanMotaData modifyData = new ModifyWanMotaData
			{
				strParams = strcmd,
				strSweepReward = "*"
			};
			string[] fields = Global.SendToDB<ModifyWanMotaData>(10158, modifyData, client.ServerId);
			int result;
			if (fields == null || fields.Length != 2)
			{
				result = -1;
			}
			else
			{
				result = Convert.ToInt32(fields[1]);
			}
			return result;
		}

		
		public static int SweepBeginDBCommand(GameClient client, int nLayerCount)
		{
			long lBeginTime = TimeUtil.NOW();
			string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", new object[]
			{
				client.ClientData.RoleID,
				"*",
				"*",
				"1",
				"",
				lBeginTime
			});
			ModifyWanMotaData modifyData = new ModifyWanMotaData
			{
				strParams = strcmd,
				strSweepReward = "*"
			};
			string[] fields = Global.SendToDB<ModifyWanMotaData>(10158, modifyData, client.ServerId);
			int result;
			if (fields == null || fields.Length != 2)
			{
				result = -1;
			}
			else
			{
				result = Convert.ToInt32(fields[1]);
			}
			return result;
		}

		
		public static int UpdateSweepAwardDBCommand(GameClient client, int nSweepLayerCount)
		{
			int result;
			if (null == client.ClientData.LayerRewardData)
			{
				result = -1;
			}
			else
			{
				string strLayerReward = "";
				lock (client.ClientData.LayerRewardData)
				{
					if (-1 != nSweepLayerCount)
					{
						byte[] bytes = DataHelper.ObjectToBytes<LayerRewardData>(client.ClientData.LayerRewardData);
						strLayerReward = Convert.ToBase64String(bytes);
					}
				}
				string strcmd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", new object[]
				{
					client.ClientData.RoleID,
					"*",
					"*",
					nSweepLayerCount,
					"*",
					"*"
				});
				ModifyWanMotaData modifyData = new ModifyWanMotaData
				{
					strParams = strcmd,
					strSweepReward = strLayerReward
				};
				string[] fields = Global.SendToDB<ModifyWanMotaData>(10158, modifyData, client.ServerId);
				if (fields == null || fields.Length != 2)
				{
					result = -1;
				}
				else
				{
					client.ClientData.WanMoTaProp.nSweepLayer = nSweepLayerCount;
					client.ClientData.WanMoTaProp.strSweepReward = strLayerReward;
					result = Convert.ToInt32(fields[1]);
				}
			}
			return result;
		}
	}
}
