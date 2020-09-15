﻿using System;

namespace DotNetDetour
{
	// Token: 0x02000014 RID: 20
	public enum Protection
	{
		// Token: 0x04000041 RID: 65
		PAGE_NOACCESS = 1,
		// Token: 0x04000042 RID: 66
		PAGE_READONLY,
		// Token: 0x04000043 RID: 67
		PAGE_READWRITE = 4,
		// Token: 0x04000044 RID: 68
		PAGE_WRITECOPY = 8,
		// Token: 0x04000045 RID: 69
		PAGE_EXECUTE = 16,
		// Token: 0x04000046 RID: 70
		PAGE_EXECUTE_READ = 32,
		// Token: 0x04000047 RID: 71
		PAGE_EXECUTE_READWRITE = 64,
		// Token: 0x04000048 RID: 72
		PAGE_EXECUTE_WRITECOPY = 128,
		// Token: 0x04000049 RID: 73
		PAGE_GUARD = 256,
		// Token: 0x0400004A RID: 74
		PAGE_NOCACHE = 512,
		// Token: 0x0400004B RID: 75
		PAGE_WRITECOMBINE = 1024
	}
}
