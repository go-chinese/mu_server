﻿using System;
using ProtoBuf;
using Tmsk.Contract;

namespace Server.Data
{
	
	[ProtoContract]
	public class SCClientHeart : IProtoBuffData
	{
		
		public SCClientHeart()
		{
		}

		
		public SCClientHeart(int roleID, int token, int allowTicks)
		{
			this.RoleID = roleID;
			this.RandToken = token;
			this.Ticks = allowTicks;
		}

		
		public int fromBytes(byte[] data, int offset, int count)
		{
			int pos = offset;
			int mycount = 0;
			while (mycount < count)
			{
				int fieldnumber = -1;
				int wt = -1;
				ProtoUtil.GetTag(data, ref pos, ref fieldnumber, ref wt, ref mycount);
				switch (fieldnumber)
				{
				case 1:
					this.RoleID = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount);
					break;
				case 2:
					this.RandToken = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount);
					break;
				case 3:
					this.Ticks = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount);
					break;
				case 4:
					this.ReportCliRealTick = ProtoUtil.LongMemberFromBytes(data, wt, ref pos, ref mycount);
					break;
				default:
					throw new ArgumentException("error!!!");
				}
			}
			return pos;
		}

		
		public byte[] toBytes()
		{
			int total = 0;
			total += ProtoUtil.GetIntSize(this.RoleID, true, 1, true, 0);
			total += ProtoUtil.GetIntSize(this.RandToken, true, 2, true, 0);
			total += ProtoUtil.GetIntSize(this.Ticks, true, 3, true, 0);
			total += ProtoUtil.GetLongSize(this.ReportCliRealTick, true, 4, true, 0L);
			byte[] data = new byte[total];
			int offset = 0;
			ProtoUtil.IntMemberToBytes(data, 1, ref offset, this.RoleID, true, 0);
			ProtoUtil.IntMemberToBytes(data, 2, ref offset, this.RandToken, true, 0);
			ProtoUtil.IntMemberToBytes(data, 3, ref offset, this.Ticks, true, 0);
			ProtoUtil.LongMemberToBytes(data, 4, ref offset, this.ReportCliRealTick, true, 0L);
			return data;
		}

		
		[ProtoMember(1)]
		public int RoleID = 0;

		
		[ProtoMember(2)]
		public int RandToken = 0;

		
		[ProtoMember(3)]
		public int Ticks = 0;

		
		[ProtoMember(4)]
		public long ReportCliRealTick;
	}
}
