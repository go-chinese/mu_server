﻿using System;
using System.Collections.Generic;
using System.Text;
using GameDBServer.DB;
using GameDBServer.Server;
using Server.Data;
using Server.Protocol;
using Server.Tools;

namespace GameDBServer.Logic
{
	// Token: 0x02000140 RID: 320
	internal class GroupMailManager
	{
		// Token: 0x06000552 RID: 1362 RVA: 0x0002CE9C File Offset: 0x0002B09C
		public static void ResetData()
		{
			lock (GroupMailManager.GroupMailDataDict_Mutex)
			{
				GroupMailManager.LastMaxGroupMailID = 0;
				GroupMailManager.GroupMailDataDict.Clear();
			}
		}

		// Token: 0x06000553 RID: 1363 RVA: 0x0002CEF4 File Offset: 0x0002B0F4
		public static void ScanLastGroupMails(DBManager dbMgr)
		{
			long nowTicks = DateTime.Now.Ticks / 10000L;
			if (nowTicks - GroupMailManager.LastScanGroupMailTicks >= 30000L)
			{
				GroupMailManager.LastScanGroupMailTicks = nowTicks;
				List<GroupMailData> GroupMailList = DBQuery.ScanNewGroupMailFromTable(dbMgr, GroupMailManager.LastMaxGroupMailID);
				if (GroupMailList != null && GroupMailList.Count > 0)
				{
					foreach (GroupMailData item in GroupMailList)
					{
						GroupMailManager.AddGroupMailData(item);
						if (item.GMailID > GroupMailManager.LastMaxGroupMailID)
						{
							GroupMailManager.LastMaxGroupMailID = item.GMailID;
						}
					}
					string gmCmdData = string.Format("-notifygmail ", new object[0]);
					ChatMsgManager.AddGMCmdChatMsg(-1, gmCmdData);
				}
			}
		}

		// Token: 0x06000554 RID: 1364 RVA: 0x0002CFEC File Offset: 0x0002B1EC
		private static void AddGroupMailData(GroupMailData gmailData)
		{
			lock (GroupMailManager.GroupMailDataDict_Mutex)
			{
				GroupMailManager.GroupMailDataDict[gmailData.GMailID] = gmailData;
			}
		}

		// Token: 0x06000555 RID: 1365 RVA: 0x0002D044 File Offset: 0x0002B244
		private static List<GroupMailData> GetGroupMailList(int beginID)
		{
			List<GroupMailData> GroupMailList = null;
			foreach (KeyValuePair<int, GroupMailData> item in GroupMailManager.GroupMailDataDict)
			{
				if (item.Value.GMailID > beginID)
				{
					if (null == GroupMailList)
					{
						GroupMailList = new List<GroupMailData>();
					}
					GroupMailList.Add(item.Value);
				}
			}
			return GroupMailList;
		}

		// Token: 0x06000556 RID: 1366 RVA: 0x0002D0DC File Offset: 0x0002B2DC
		public static TCPProcessCmdResults RequestNewGroupMailList(DBManager dbMgr, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
		{
			tcpOutPacket = null;
			string cmdData = null;
			try
			{
				cmdData = new UTF8Encoding().GetString(data, 0, count);
			}
			catch (Exception)
			{
				LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}", (TCPGameServerCmds)nID), null, true);
				tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "0", 30767);
				return TCPProcessCmdResults.RESULT_DATA;
			}
			try
			{
				string[] fields = cmdData.Split(new char[]
				{
					':'
				});
				if (fields.Length != 1)
				{
					LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}", (TCPGameServerCmds)nID, fields.Length, cmdData), null, true);
					tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "0", 30767);
					return TCPProcessCmdResults.RESULT_DATA;
				}
				int beginID = Convert.ToInt32(fields[0]);
				List<GroupMailData> GroupMailList = GroupMailManager.GetGroupMailList(beginID);
				byte[] retBytes = DataHelper.ObjectToBytes<List<GroupMailData>>(GroupMailList);
				tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, retBytes, 0, retBytes.Length, nID);
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "", false, false);
			}
			return TCPProcessCmdResults.RESULT_DATA;
		}

		// Token: 0x06000557 RID: 1367 RVA: 0x0002D200 File Offset: 0x0002B400
		public static TCPProcessCmdResults ModifyGMailRecord(DBManager dbMgr, TCPOutPacketPool pool, int nID, byte[] data, int count, out TCPOutPacket tcpOutPacket)
		{
			tcpOutPacket = null;
			string cmdData = null;
			try
			{
				cmdData = new UTF8Encoding().GetString(data, 0, count);
			}
			catch (Exception)
			{
				LogManager.WriteLog(LogTypes.Error, string.Format("解析指令字符串错误, CMD={0}", (TCPGameServerCmds)nID), null, true);
				tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "0", 30767);
				return TCPProcessCmdResults.RESULT_DATA;
			}
			try
			{
				string[] fields = cmdData.Split(new char[]
				{
					':'
				});
				if (fields.Length != 3)
				{
					LogManager.WriteLog(LogTypes.Error, string.Format("指令参数个数错误, CMD={0}, Recv={1}, CmdData={2}", (TCPGameServerCmds)nID, fields.Length, cmdData), null, true);
					tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "0", 30767);
					return TCPProcessCmdResults.RESULT_DATA;
				}
				int roleID = Convert.ToInt32(fields[0]);
				int gmailID = Convert.ToInt32(fields[1]);
				int mailID = Convert.ToInt32(fields[2]);
				DBRoleInfo dbRoleInfo = dbMgr.GetDBRoleInfo(ref roleID);
				if (null == dbRoleInfo)
				{
					LogManager.WriteLog(LogTypes.Error, string.Format("发起请求的角色不存在，CMD={0}, RoleID={1}", (TCPGameServerCmds)nID, roleID), null, true);
					tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, "0", 30767);
					return TCPProcessCmdResults.RESULT_DATA;
				}
				int result = DBWriter.ModifyGMailRecord(dbMgr, roleID, gmailID, mailID);
				if (result > 0)
				{
					lock (dbRoleInfo)
					{
						if (null == dbRoleInfo.GroupMailRecordList)
						{
							dbRoleInfo.GroupMailRecordList = new List<int>();
						}
						if (dbRoleInfo.GroupMailRecordList.IndexOf(gmailID) < 0)
						{
							dbRoleInfo.GroupMailRecordList.Add(gmailID);
						}
					}
				}
				string strcmd = string.Format("{0}", result);
				tcpOutPacket = TCPOutPacket.MakeTCPOutPacket(pool, strcmd, nID);
				return TCPProcessCmdResults.RESULT_DATA;
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "", false, false);
			}
			return TCPProcessCmdResults.RESULT_DATA;
		}

		// Token: 0x0400080C RID: 2060
		private static long LastScanGroupMailTicks = DateTime.Now.Ticks / 10000L;

		// Token: 0x0400080D RID: 2061
		private static int LastMaxGroupMailID = 0;

		// Token: 0x0400080E RID: 2062
		private static object GroupMailDataDict_Mutex = new object();

		// Token: 0x0400080F RID: 2063
		private static Dictionary<int, GroupMailData> GroupMailDataDict = new Dictionary<int, GroupMailData>();
	}
}
