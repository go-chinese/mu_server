﻿using System;
using ProtoBuf;
using Tmsk.Contract;

namespace Server.Data
{
	// Token: 0x0200059A RID: 1434
	[ProtoContract]
	public class SpriteLifeChangeData : IProtoBuffData
	{
		// Token: 0x06001A31 RID: 6705 RVA: 0x00193BE4 File Offset: 0x00191DE4
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
					this.roleID = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount);
					break;
				case 2:
					this.lifeV = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount);
					break;
				case 3:
					this.magicV = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount);
					break;
				case 4:
					this.currentLifeV = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount);
					break;
				case 5:
					this.currentMagicV = ProtoUtil.IntMemberFromBytes(data, wt, ref pos, ref mycount);
					break;
				case 6:
					this.ArmorV = ProtoUtil.LongMemberFromBytes(data, wt, ref pos, ref mycount);
					break;
				case 7:
					this.currentArmorV = ProtoUtil.LongMemberFromBytes(data, wt, ref pos, ref mycount);
					break;
				default:
					throw new ArgumentException("error!!!");
				}
			}
			return pos;
		}

		// Token: 0x06001A32 RID: 6706 RVA: 0x00193CE4 File Offset: 0x00191EE4
		public byte[] toBytes()
		{
			int total = 0;
			total += ProtoUtil.GetIntSize(this.roleID, true, 1, true, 0);
			total += ProtoUtil.GetIntSize(this.lifeV, true, 2, true, 0);
			total += ProtoUtil.GetIntSize(this.magicV, true, 3, true, 0);
			total += ProtoUtil.GetIntSize(this.currentLifeV, true, 4, true, 0);
			total += ProtoUtil.GetIntSize(this.currentMagicV, true, 5, true, 0);
			total += ProtoUtil.GetLongSize(this.ArmorV, true, 6, true, 0L);
			total += ProtoUtil.GetLongSize(this.currentArmorV, true, 7, true, 0L);
			byte[] data = new byte[total];
			int offset = 0;
			ProtoUtil.IntMemberToBytes(data, 1, ref offset, this.roleID, true, 0);
			ProtoUtil.IntMemberToBytes(data, 2, ref offset, this.lifeV, true, 0);
			ProtoUtil.IntMemberToBytes(data, 3, ref offset, this.magicV, true, 0);
			ProtoUtil.IntMemberToBytes(data, 4, ref offset, this.currentLifeV, true, 0);
			ProtoUtil.IntMemberToBytes(data, 5, ref offset, this.currentMagicV, true, 0);
			ProtoUtil.LongMemberToBytes(data, 6, ref offset, this.ArmorV, true, 0L);
			ProtoUtil.LongMemberToBytes(data, 7, ref offset, this.currentArmorV, true, 0L);
			return data;
		}

		// Token: 0x04002862 RID: 10338
		[ProtoMember(1)]
		public int roleID;

		// Token: 0x04002863 RID: 10339
		[ProtoMember(2)]
		public int lifeV;

		// Token: 0x04002864 RID: 10340
		[ProtoMember(3)]
		public int magicV;

		// Token: 0x04002865 RID: 10341
		[ProtoMember(4)]
		public int currentLifeV;

		// Token: 0x04002866 RID: 10342
		[ProtoMember(5)]
		public int currentMagicV;

		// Token: 0x04002867 RID: 10343
		[ProtoMember(6)]
		public long ArmorV;

		// Token: 0x04002868 RID: 10344
		[ProtoMember(7)]
		public long currentArmorV;
	}
}
