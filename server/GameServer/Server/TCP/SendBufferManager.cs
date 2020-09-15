﻿using System;
using System.Collections.Generic;
using System.Threading;
using GameServer.Logic;
using GameServer.Server;
using Server.Protocol;
using Server.Tools;

namespace Server.TCP
{
	// Token: 0x020008C8 RID: 2248
	public class SendBufferManager
	{
		// Token: 0x17000603 RID: 1539
		// (get) Token: 0x0600402B RID: 16427 RVA: 0x003BB1CC File Offset: 0x003B93CC
		// (set) Token: 0x0600402C RID: 16428 RVA: 0x003BB1E4 File Offset: 0x003B93E4
		public bool Exit
		{
			get
			{
				return this._Exit;
			}
			set
			{
				this._Exit = value;
			}
		}

		// Token: 0x0600402E RID: 16430 RVA: 0x003BB21C File Offset: 0x003B941C
		public bool AddOutPacket(TMSKSocket s, TCPOutPacket tcpOutPacket)
		{
			SendBuffer sendBuffer = s._SendBuffer;
			bool result;
			if (null == sendBuffer)
			{
				result = false;
			}
			else
			{
				int canNotSendReason = -1;
				bool bRet = sendBuffer.CanSend2(s, tcpOutPacket, ref canNotSendReason);
				if (!bRet)
				{
					if (sendBuffer.CanLog(canNotSendReason))
					{
						string failedReason = FullBufferManager.GetErrorStr(canNotSendReason);
						LogManager.WriteLog(LogTypes.Error, string.Format("向客户端{0}发送数据失败, 发送指令:{1}, 大小:{2}, 失败原因:{3}", new object[]
						{
							Global.GetSocketRemoteEndPoint(s, false),
							(TCPGameServerCmds)tcpOutPacket.PacketCmdID,
							tcpOutPacket.PacketDataSize,
							failedReason
						}), null, true);
					}
					Global._FullBufferManager.Add(s, canNotSendReason);
					result = (canNotSendReason == 0);
				}
				else
				{
					if (tcpOutPacket.PacketDataSize > this.MaxOutPacketSize)
					{
						this.MaxOutPacketSize = tcpOutPacket.PacketDataSize;
						this.MaxOutPacketSizeCmdID = (int)tcpOutPacket.PacketCmdID;
					}
					if (!bRet)
					{
						LogManager.WriteLog(LogTypes.Error, string.Format("向客户端{0}发送数据时加入缓存失败, 缓存空间不足, 发送指令:{1}, 大小:{2}", Global.GetSocketRemoteEndPoint(s, false), (TCPGameServerCmds)tcpOutPacket.PacketCmdID, tcpOutPacket.PacketDataSize), null, true);
					}
					result = bRet;
				}
			}
			return result;
		}

		// Token: 0x0600402F RID: 16431 RVA: 0x003BB360 File Offset: 0x003B9560
		public void TrySendAll()
		{
			List<TMSKSocket> lsSocket = new List<TMSKSocket>(2000);
			while (!this._Exit)
			{
				lsSocket.Clear();
				lock (this.BufferDict)
				{
					lsSocket.AddRange(this.BufferDict.Keys);
				}
				int lsSocketCount = lsSocket.Count;
				for (int i = 0; i < lsSocketCount; i++)
				{
					TMSKSocket s = lsSocket[i];
					if (null != s)
					{
						SendBuffer sendBuffer = s._SendBuffer;
						bool bFind = sendBuffer != null;
						if (bFind && null != sendBuffer)
						{
							sendBuffer.ExternalTrySend(s, true, 0);
						}
						if (s.DelayClose > 0 && s.DelayClose-- <= 0)
						{
						}
					}
				}
				Thread.Sleep(20);
			}
		}

		// Token: 0x06004030 RID: 16432 RVA: 0x003BB498 File Offset: 0x003B9698
		public void Remove(TMSKSocket s)
		{
			SendBuffer sendBuffer = null;
			lock (this.BufferDict)
			{
				if (this.BufferDict.TryGetValue(s, out sendBuffer))
				{
					this.BufferDict.Remove(s);
					s._SendBuffer = null;
				}
			}
			if (null != sendBuffer)
			{
				Global._MemoryManager.Push(sendBuffer.MyMemoryBlock);
			}
			Global._FullBufferManager.Remove(s);
		}

		// Token: 0x06004031 RID: 16433 RVA: 0x003BB540 File Offset: 0x003B9740
		public void Add(TMSKSocket s)
		{
			lock (this.BufferDict)
			{
				if (!this.BufferDict.ContainsKey(s))
				{
					SendBuffer sendBuffer = new SendBuffer(0);
					s._SendBuffer = sendBuffer;
					this.BufferDict.Add(s, sendBuffer);
				}
			}
		}

		// Token: 0x06004032 RID: 16434 RVA: 0x003BB5B8 File Offset: 0x003B97B8
		public void OnSendBufferOK(TMSKSocket s)
		{
			SendBuffer sendBuffer = s._SendBuffer;
			if (null != sendBuffer)
			{
				sendBuffer.OnSendOK();
			}
			Global._FullBufferManager.Remove(s);
		}

		// Token: 0x04004F30 RID: 20272
		public int MaxOutPacketSize = 0;

		// Token: 0x04004F31 RID: 20273
		public int MaxOutPacketSizeCmdID = 0;

		// Token: 0x04004F32 RID: 20274
		private Dictionary<TMSKSocket, SendBuffer> BufferDict = new Dictionary<TMSKSocket, SendBuffer>();

		// Token: 0x04004F33 RID: 20275
		private bool _Exit = false;
	}
}
