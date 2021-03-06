﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
using GameDBServer.Core;
using GameDBServer.Core.GameEvent;
using GameDBServer.Core.GameEvent.EventOjectImpl;
using GameDBServer.DB;
using GameDBServer.Logic;
using GameDBServer.Logic.Ten;
using GameDBServer.Logic.UserReturn;
using GameDBServer.Server;
using MySQLDriverCS;
using Nhiredis;
using Server.Data;
using Server.Tools;
using Tmsk.Tools;

namespace GameDBServer
{
	
	public class Program
	{
		
		[DllImport("kernel32.dll")]
		private static extern bool SetConsoleCtrlHandler(Program.ControlCtrlDelegate HandlerRoutine, bool Add);

		
		public static bool HandlerRoutine(int CtrlType)
		{
			switch (CtrlType)
			{
			}
			return true;
		}

		
		[DllImport("user32.dll")]
		private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

		
		[DllImport("user32.dll")]
		private static extern IntPtr GetSystemMenu(IntPtr hWnd, IntPtr bRevert);

		
		[DllImport("user32.dll")]
		private static extern IntPtr RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);

		
		private static void HideCloseBtn()
		{
			Console.Title = "游戏数据库服务器";
			IntPtr windowHandle = Program.FindWindow(null, Console.Title);
			IntPtr closeMenu = Program.GetSystemMenu(windowHandle, IntPtr.Zero);
			uint SC_CLOSE = 61536U;
			Program.RemoveMenu(closeMenu, SC_CLOSE, 0U);
		}

		
		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			try
			{
				Exception exception = e.ExceptionObject as Exception;
				DataHelper.WriteFormatExceptionLog(exception, "CurrentDomain_UnhandledException", UnhandedException.ShowErrMsgBox, true);
			}
			catch
			{
			}
		}

		
		private static void ExceptionHook()
		{
			AppDomain.CurrentDomain.UnhandledException += Program.CurrentDomain_UnhandledException;
		}

		
		public static void DeleteFile(string strFileName)
		{
			string strFullFileName = Directory.GetCurrentDirectory() + "\\" + strFileName;
			if (File.Exists(strFullFileName))
			{
				FileInfo fi = new FileInfo(strFullFileName);
				if (fi.Attributes.ToString().IndexOf("ReadOnly") != -1)
				{
					fi.Attributes = FileAttributes.Normal;
				}
				File.Delete(strFullFileName);
			}
		}

		
		public static void WritePIDToFile(string strFile)
		{
			string strFileName = Directory.GetCurrentDirectory() + "\\" + strFile;
			Process processes = Process.GetCurrentProcess();
			int nPID = processes.Id;
			File.WriteAllText(strFileName, string.Concat(nPID));
		}

		
		public static int GetServerPIDFromFile()
		{
			string strFileName = Directory.GetCurrentDirectory() + "\\GameServerStop.txt";
			int result;
			if (File.Exists(strFileName))
			{
				string str = File.ReadAllText(strFileName);
				result = int.Parse(str);
			}
			else
			{
				result = 0;
			}
			return result;
		}

		
		private static void Main(string[] args)
		{
			if (args.Length > 0)
			{
				Program.cmdLineARGS = args;
			}
			Program.DeleteFile("Start.txt");
			Program.DeleteFile("Stop.txt");
			Program.DeleteFile("GameServerStop.txt");
			Program.HideCloseBtn();
			Program.SetConsoleCtrlHandler(Program.newDelegate, true);
			try
			{
				if (Console.WindowWidth < 88)
				{
					Console.BufferWidth = 88;
					Console.WindowWidth = 88;
				}
			}
			catch
			{
			}
			Program.ExceptionHook();
			Program.InitCommonCmd();
			Global.CheckCodes();
			Program.OnStartServer();
			Program.ShowCmdHelpInfo(null);
			Program.WritePIDToFile("Start.txt");
			Thread thread = new Thread(new ParameterizedThreadStart(Program.ConsoleInputThread));
			thread.IsBackground = true;
			thread.Start();
			while (!Program.NeedExitServer || !Program.ServerConsole.MustCloseNow || Program.ServerConsole.MainDispatcherWorker.IsBusy)
			{
				Thread.Sleep(1000);
			}
			thread.Abort();
		}

		
		public static void ConsoleInputThread(object obj)
		{
			string cmd = Console.ReadLine();
			while (!Program.NeedExitServer)
			{
				if (cmd == null || 0 == cmd.CompareTo("exit"))
				{
					if (Program.ServerConsole.CanExit())
					{
						Console.WriteLine("确认退出吗(输入 y 将立即退出)？");
						cmd = Console.ReadLine();
						if (0 == cmd.CompareTo("y"))
						{
							break;
						}
					}
				}
				else
				{
					Program.ParseInputCmd(cmd);
				}
				cmd = Console.ReadLine();
			}
			Program.OnExitServer();
		}

		
		private static void ParseInputCmd(string cmd)
		{
			Program.CmdCallback cb = null;
			if (Program.CmdDict.TryGetValue(cmd, out cb) && null != cb)
			{
				cb(cmd);
			}
			else
			{
				Console.WriteLine("未知命令,输入 help 查看具体命令信息");
			}
		}

		
		private static void OnStartServer()
		{
			Program.ServerConsole.InitServer();
			Console.Title = string.Format("游戏数据库服务器{0}区@{1}@{2}", GameDBManager.ZoneID, Program.GetVersionDateTime(), Program.ProgramExtName);
		}

		
		private static void OnExitServer()
		{
			Program.ServerConsole.ExitServer();
		}

		
		public static void Exit()
		{
			Program.NeedExitServer = true;
		}

		
		private static void InitCommonCmd()
		{
			Program.CmdDict.Add("help", new Program.CmdCallback(Program.ShowCmdHelpInfo));
			Program.CmdDict.Add("gc", new Program.CmdCallback(Program.GarbageCollect));
			Program.CmdDict.Add("append lipinma", new Program.CmdCallback(Program.AppendLiPinMaCmd));
			Program.CmdDict.Add("load names", new Program.CmdCallback(Program.LoadNamesCmd));
			Program.CmdDict.Add("show baseinfo", new Program.CmdCallback(Program.ShowServerBaseInfo));
			Program.CmdDict.Add("show tcpinfo", new Program.CmdCallback(Program.ServerConsole.ShowServerTCPInfo));
		}

		
		private static void ShowCmdHelpInfo(string cmd = null)
		{
			Console.WriteLine(string.Format("游戏数据库服务器", new object[0]));
			Console.WriteLine("输入 help， 显示帮助信息");
			Console.WriteLine("输入 exit， 然后输入y退出？");
			Console.WriteLine("输入 gc， 执行垃圾回收");
			Console.WriteLine("输入 append lipinma， 从文件追加礼品码");
			Console.WriteLine("输入 load names， 从目录加载名字");
			Console.WriteLine("输入 show baseinfo， 查看基础运行信息");
			Console.WriteLine("输入 show tcpinfo， 查看指令消耗信息");
		}

		
		private static void GarbageCollect(string cmd = null)
		{
			try
			{
				GC.Collect();
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "GarbageCollect()", false, false);
			}
		}

		
		private static void AppendLiPinMaCmd(string cmd = null)
		{
			try
			{
				Program.ServerConsole.AppendLiPinMa();
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "ShowDBConnectInfo()", false, false);
			}
		}

		
		private void AppendLiPinMa()
		{
			if (!this.updateLiPinMaWorker.IsBusy)
			{
				this.toAppendLiPinMa = true;
				this.updateLiPinMaWorker.RunWorkerAsync();
			}
		}

		
		private static void LoadNamesCmd(string cmd = null)
		{
			try
			{
				Program.ServerConsole.LoadNames();
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "ShowDBConnectInfo()", false, false);
			}
		}

		
		private void LoadNames()
		{
			if (!this.updatePreNamesWorker.IsBusy)
			{
				this.updatePreNamesWorker.RunWorkerAsync();
			}
		}

		
		private static void ShowServerBaseInfo(string cmd = null)
		{
			string info = string.Format("已经连接的登录服务器连接数:{0}", Program.ServerConsole.TotalConnections);
			Console.WriteLine(info);
			info = string.Format("数据库连接池个数: {0}", Program.ServerConsole._DBManger.GetMaxConnsCount());
			Console.WriteLine(info);
			info = string.Format("用户缓存: {0}, 角色缓存: {1}", Program.ServerConsole._DBManger.dbUserMgr.GetUserInfoCount(), Program.ServerConsole._DBManger.DBRoleMgr.GetRoleInfoCount());
			Console.WriteLine(info);
		}

		
		private void ShowServerTCPInfo(string cmd = null)
		{
			bool clear = cmd.Contains("/c");
			bool detail = cmd.Contains("/d");
			string info = string.Format("总接收字节: {0:0.00} MB", (double)this._TCPManager.MySocketListener.TotalBytesReadSize / 1048576.0);
			Console.WriteLine(info);
			info = string.Format("总发送字节: {0:0.00} MB", (double)this._TCPManager.MySocketListener.TotalBytesWriteSize / 1048576.0);
			Console.WriteLine(info);
			info = string.Format("指令处理平均耗时（毫秒）{0}", (TCPManager.processCmdNum != 0L) ? TimeUtil.TimeMS(TCPManager.processTotalTime / TCPManager.processCmdNum, 2) : 0.0);
			Console.WriteLine(info);
			info = string.Format("指令处理耗时详情", new object[0]);
			Console.WriteLine(info);
			int count = 0;
			lock (TCPManager.cmdMoniter)
			{
				foreach (PorcessCmdMoniter i in TCPManager.cmdMoniter.Values)
				{
					Console.ForegroundColor = count % 5 + ConsoleColor.Green;
					if (detail)
					{
						if (count++ == 0)
						{
							info = string.Format("{0, -48}{1, 6}{2, 7}{3, 7}{4, 7}{5, 7}", new object[]
							{
								"消息",
								"已处理次数",
								"平均处理时长",
								"总计消耗时长",
								"最大处理时长",
								"最大等待时长"
							});
							Console.WriteLine(info);
						}
						info = string.Format("{0, -50}{1, 11}{2, 13:0.##}{3, 13:0.##}{4, 13:0.##}{5, 13:0.##}", new object[]
						{
							(TCPGameServerCmds)i.cmd,
							i.processNum,
							TimeUtil.TimeMS(i.avgProcessTime(), 2),
							TimeUtil.TimeMS(i.processTotalTime, 2),
							TimeUtil.TimeMS(i.processMaxTime, 2),
							TimeUtil.TimeMS(i.maxWaitProcessTime, 2)
						});
						Console.WriteLine(info);
					}
					else
					{
						if (count++ == 0)
						{
							info = string.Format("{0, -48}{1, 6}{2, 7}{3, 7}", new object[]
							{
								"消息",
								"已处理次数",
								"平均处理时长",
								"总计消耗时长"
							});
							Console.WriteLine(info);
						}
						info = string.Format("{0, -50}{1, 11}{2, 13:0.##}{3, 13:0.##}", new object[]
						{
							(TCPGameServerCmds)i.cmd,
							i.processNum,
							TimeUtil.TimeMS(i.avgProcessTime(), 2),
							TimeUtil.TimeMS(i.processTotalTime, 2)
						});
						Console.WriteLine(info);
					}
					if (clear)
					{
						i.maxWaitProcessTime = 0L;
						i.processMaxTime = 0L;
						i.processNum = 0;
						i.processTotalTime = 0L;
						i.waitProcessTotalTime = 0L;
					}
				}
				Console.ForegroundColor = ConsoleColor.White;
			}
		}

		
		
		
		public int TotalConnections
		{
			get
			{
				return this._TotalConnections;
			}
			set
			{
				this._TotalConnections = value;
			}
		}

		
		private void closingTimer_Tick(object sender, EventArgs e)
		{
			try
			{
				string title = "";
				this.ClosingCounter -= 200;
				if (this.ClosingCounter <= 0)
				{
					this.MustCloseNow = true;
				}
				else
				{
					int counter = this.ClosingCounter / 200;
					title = string.Format("游戏数据库服务器{0} 关闭中, 倒计时:{1}", GameDBManager.ZoneID, counter);
				}
				Console.Title = title;
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "closingTimer_Tick", false, false);
			}
		}

		
		private void MainDispatcherWorker_DoWork(object sender, EventArgs e)
		{
			long startTicks = DateTime.Now.Ticks / 10000L;
			long endTicks = DateTime.Now.Ticks / 10000L;
			int maxSleepMs = 1000;
			for (;;)
			{
				try
				{
					startTicks = DateTime.Now.Ticks / 10000L;
					this.ExecuteBackgroundWorkers(null, EventArgs.Empty);
					if (Program.NeedExitServer)
					{
						maxSleepMs = 200;
						this.closingTimer_Tick(null, null);
						if (this.MustCloseNow)
						{
							break;
						}
					}
					endTicks = DateTime.Now.Ticks / 10000L;
					int sleepMs = (int)Math.Max(1L, (long)maxSleepMs - (endTicks - startTicks));
					Thread.Sleep(sleepMs);
					if (0 != Program.GetServerPIDFromFile())
					{
						Program.OnExitServer();
					}
				}
				catch (Exception ex)
				{
					DataHelper.WriteFormatExceptionLog(ex, "MainDispatcherWorker_DoWork", false, false);
				}
			}
			Console.WriteLine("主循环线程退出，回车退出系统");
			if (0 != Program.GetServerPIDFromFile())
			{
				Program.WritePIDToFile("Stop.txt");
			}
		}

		
		private void UserReturnCheckWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				SingletonTemplate<UserReturnManager>.Instance().ScanLastUserReturn(this._DBManger);
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "updateFacebook_DoWork", false, false);
			}
		}

		
		private void updateGiftCode_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				GiftCodeNewManager.ScanLastGroup(this._DBManger);
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "updateGiftCode_DoWork", false, false);
			}
		}

		
		private void ShowInfoTicks(object sender, EventArgs e)
		{
			try
			{
				this.ExecuteBackgroundWorkers(null, EventArgs.Empty);
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "ShowInfoTicks", false, false);
			}
		}

		
		private static void InitProgramExtName()
		{
			Program.ProgramExtName = AppDomain.CurrentDomain.BaseDirectory;
		}

		
		public void InitServer()
		{
			Program.InitProgramExtName();
			Console.WriteLine("正在初始化语言文件");
			Global.LoadLangDict();
			if (!IpLibrary.loadIpLibrary("qqwry.dat"))
			{
				throw new Exception(string.Format("启动时加载IP库: {0} 失败", "qqwry.dat"));
			}
			XElement xml = null;
			Console.WriteLine("正在初始化系统配置文件");
			try
			{
				xml = XElement.Load("AppConfig.xml");
			}
			catch (Exception)
			{
				throw new Exception(string.Format("启动时加载xml文件: {0} 失败", "AppConfig.xml"));
			}
			LogManager.LogTypeToWrite = (LogTypes)Global.GetSafeAttributeLong(xml, "Server", "LogType");
			GameDBManager.SystemServerSQLEvents.EventLevel = (EventLevels)Global.GetSafeAttributeLong(xml, "Server", "EventLevel");
			int dbLog = Math.Max(0, (int)Global.GetSafeAttributeLong(xml, "DBLog", "DBLogEnable"));
			GameDBManager.ZoneID = (int)Global.GetSafeAttributeLong(xml, "Zone", "ID");
			string startURL = null;
			string chargeKey = "";
			string serverKey = "";
			try
			{
				startURL = Global.GetSafeAttributeStr(xml, "Zone", "URL");
				chargeKey = Global.GetSafeAttributeStr(xml, "Platform", "GameKey");
				serverKey = Global.GetSafeAttributeStr(xml, "Platform", "ServerKey");
			}
			catch (Exception ex)
			{
				LogManager.WriteLog(LogTypes.Fatal, "读取AppConfig.xml配置文件出错" + ex.Message, ex, true);
			}
			if (!string.IsNullOrEmpty(startURL))
			{
				GetCDBInfoReq cdbServerReq = new GetCDBInfoReq();
				cdbServerReq.PTID = Global.GetSafeAttributeStr(xml, "Zone", "PTID");
				cdbServerReq.ServerID = GameDBManager.ZoneID.ToString();
				cdbServerReq.Gamecode = Global.GetSafeAttributeStr(xml, "Zone", "GameCode");
				byte[] responseData = null;
				byte[] clientBytes = DataHelper.ObjectToBytes<GetCDBInfoReq>(cdbServerReq);
				for (int i = 1; i <= 5; i++)
				{
					responseData = WebHelper.RequestByPost(startURL, clientBytes, 5000, 30000);
					if (responseData != null)
					{
						break;
					}
					Console.WriteLine("第{0}次获取启动信息失败，2秒后重试", i);
					Thread.Sleep(2000);
				}
				if (responseData == null)
				{
					throw new Exception(string.Format("请求启动信息失败", new object[0]));
				}
				ServerDBInfo databaseInfo = DataHelper.BytesToObject<ServerDBInfo>(responseData, 0, responseData.Length);
				if (databaseInfo == null)
				{
					throw new Exception(string.Format("请求启动信息 解析失败", new object[0]));
				}
				GameDBManager.serverDBInfo = databaseInfo;
				GameDBManager.serverDBInfo.userName = StringEncrypt.Decrypt(GameDBManager.serverDBInfo.userName, "eabcix675u49,/", "3&3i4x4^+-0");
				GameDBManager.serverDBInfo.uPassword = StringEncrypt.Decrypt(GameDBManager.serverDBInfo.uPassword, "eabcix675u49,/", "3&3i4x4^+-0");
				if (GameDBManager.serverDBInfo.InternalIP != Global.GetInternalIP())
				{
					throw new Exception(string.Format("请求启动信息 内网IP不匹配", new object[0]));
				}
				string tmpName = string.Format("{0}_game_{1}", cdbServerReq.Gamecode, cdbServerReq.ServerID);
				if (tmpName != GameDBManager.serverDBInfo.dbName)
				{
					throw new Exception(string.Format("请求启动信息 dbName不匹配", new object[0]));
				}
			}
			else
			{
				GameDBManager.serverDBInfo = new ServerDBInfo();
				GameDBManager.serverDBInfo.userName = StringEncrypt.Decrypt(Global.GetSafeAttributeStr(xml, "Database", "uname"), "eabcix675u49,/", "3&3i4x4^+-0");
				GameDBManager.serverDBInfo.uPassword = StringEncrypt.Decrypt(Global.GetSafeAttributeStr(xml, "Database", "upasswd"), "eabcix675u49,/", "3&3i4x4^+-0");
				GameDBManager.serverDBInfo.strIP = Global.GetSafeAttributeStr(xml, "Database", "ip");
				GameDBManager.serverDBInfo.Port = (int)Global.GetSafeAttributeLong(xml, "Database", "port");
				GameDBManager.serverDBInfo.dbName = Global.GetSafeAttributeStr(xml, "Database", "dname");
				GameDBManager.serverDBInfo.maxConns = (int)Global.GetSafeAttributeLong(xml, "Database", "maxConns");
				GameDBManager.serverDBInfo.InternalIP = Global.GetInternalIP();
				GameDBManager.serverDBInfo.ChargeKey = chargeKey;
				GameDBManager.serverDBInfo.ServerKey = serverKey;
			}
			GameDBManager.serverDBInfo.ChargeKey = StringEncrypt.Decrypt(GameDBManager.serverDBInfo.ChargeKey, "eabcix675u49,/", "3&3i4x4^+-0");
			GameDBManager.serverDBInfo.ServerKey = StringEncrypt.Decrypt(GameDBManager.serverDBInfo.ServerKey, "eabcix675u49,/", "3&3i4x4^+-0");
			GameDBManager.serverDBInfo.DbNames = Global.GetSafeAttributeStr(xml, "Database", "names");
			GameDBManager.serverDBInfo.CodePage = (int)Global.GetSafeAttributeLong(xml, "Database", "codePage");
			Console.WriteLine("服务器正在建立数据链接池个数: {0}", GameDBManager.serverDBInfo.maxConns);
			Console.WriteLine("数据库地址: {0}", GameDBManager.serverDBInfo.strIP);
			Console.WriteLine("数据库名称: {0}", GameDBManager.serverDBInfo.dbName);
			Console.WriteLine("数据库字符集: {0}", GameDBManager.serverDBInfo.DbNames);
			Console.WriteLine("数据库代码页: {0}", GameDBManager.serverDBInfo.CodePage);
			DBConnections.dbNames = GameDBManager.serverDBInfo.DbNames;
			Console.WriteLine("正在初始化数据库链接");
			MySQLConnectionString connectStr = new MySQLConnectionString(GameDBManager.serverDBInfo.strIP, GameDBManager.serverDBInfo.dbName, GameDBManager.serverDBInfo.userName, GameDBManager.serverDBInfo.uPassword);
			this._DBManger.LoadDatabase(connectStr, GameDBManager.serverDBInfo.maxConns, GameDBManager.serverDBInfo.CodePage);
			Program.ValidateZoneID();
			GameDBManager.DBName = Global.GetSafeAttributeStr(xml, "Database", "dname");
			DBWriter.ValidateDatabase(this._DBManger, GameDBManager.DBName);
			if (!Global.InitDBAutoIncrementValues(this._DBManger))
			{
				Console.WriteLine("存在致命错误，请输入exit 和 y 退出");
			}
			else
			{
				LineManager.LoadConfig();
				Console.WriteLine("正在初始化网络");
				this._TCPManager = TCPManager.getInstance();
				this._TCPManager.initialize((int)Global.GetSafeAttributeLong(xml, "Socket", "capacity"));
				this._TCPManager.DBMgr = this._DBManger;
				this._TCPManager.RootWindow = this;
				this._TCPManager.Start(Global.GetSafeAttributeStr(xml, "Socket", "ip"), (int)Global.GetSafeAttributeLong(xml, "Socket", "port"));
				PlatTCPManager.getInstance().initialize((int)Global.GetSafeAttributeLong(xml, "Platform", "capacity"));
				PlatTCPManager.getInstance().Start(Global.GetSafeAttributeStr(xml, "Platform", "ip"), (int)Global.GetSafeAttributeLong(xml, "Platform", "port"));
				Console.WriteLine("正在配置后台线程");
				this.eventWorker = new BackgroundWorker();
				this.eventWorker.DoWork += this.eventWorker_DoWork;
				this.updateMoneyWorker = new BackgroundWorker();
				this.updateMoneyWorker.DoWork += this.updateMoneyWorker_DoWork;
				this.releaseMemoryWorker = new BackgroundWorker();
				this.releaseMemoryWorker.DoWork += this.releaseMemoryWorker_DoWork;
				this.updateLiPinMaWorker = new BackgroundWorker();
				this.updateLiPinMaWorker.DoWork += this.updateLiPinMaWorker_DoWork;
				this.updatePreDeleteWorker = new BackgroundWorker();
				this.updatePreDeleteWorker.DoWork += this.updatePreDeleteRoleWorker_DoWork;
				this.updatePreNamesWorker = new BackgroundWorker();
				this.updatePreNamesWorker.DoWork += this.updatePreNamesWorker_DoWork;
				this.updatePaiHangWorker = new BackgroundWorker();
				this.updatePaiHangWorker.DoWork += this.updatePaiHangWorker_DoWork;
				this.dbWriterWorker = new BackgroundWorker();
				this.dbWriterWorker.DoWork += new DoWorkEventHandler(this.dbWriterWorker_DoWork);
				this.dbGoodsBakTableWorker = new BackgroundWorker();
				this.dbGoodsBakTableWorker.DoWork += new DoWorkEventHandler(this.dbGoodsBakTableWorker_DoWork);
				this.updateLastMailWorker = new BackgroundWorker();
				this.updateLastMailWorker.DoWork += this.updateLastMail_DoWork;
				this.updateGroupMailWorker = new BackgroundWorker();
				this.updateGroupMailWorker.DoWork += this.updateGroupMail_DoWork;
				this.updateFacebookWorker = new BackgroundWorker();
				this.updateFacebookWorker.DoWork += this.updateFacebook_DoWork;
				this.MainDispatcherWorker = new BackgroundWorker();
				this.MainDispatcherWorker.DoWork += new DoWorkEventHandler(this.MainDispatcherWorker_DoWork);
				this.userReturnCheckWorker = new BackgroundWorker();
				this.userReturnCheckWorker.DoWork += this.UserReturnCheckWorker_DoWork;
				this.updateTenWorker = new BackgroundWorker();
				this.updateTenWorker.DoWork += this.updateTen_DoWork;
				this.updateGiftCodeWorker = new BackgroundWorker();
				this.updateGiftCodeWorker.DoWork += this.updateGiftCode_DoWork;
				UnhandedException.ShowErrMsgBox = false;
				GlobalServiceManager.initialize();
				GlobalServiceManager.startup();
				if (!this.MainDispatcherWorker.IsBusy)
				{
					this.MainDispatcherWorker.RunWorkerAsync();
				}
				GameDBManager.GameConfigMgr.UpdateGameConfigItem("gamedb_version", Program.GetVersionDateTime());
				DBWriter.UpdateGameConfig(this._DBManger, "gamedb_version", Program.GetVersionDateTime());
				Console.WriteLine("系统启动完毕");
			}
		}

		
		public static void ValidateZoneID()
		{
			Console.WriteLine("本服区号:{0}", GameDBManager.ZoneID);
		}

		
		public void ExitServer()
		{
			if (!Program.NeedExitServer)
			{
				this._TCPManager.Stop();
				GlobalServiceManager.showdown();
				GlobalServiceManager.destroy();
				this.Window_Closing();
				Console.WriteLine("正在尝试关闭服务器,看到服务器关闭完毕提示后回车退出系统");
				if (0 == Program.GetServerPIDFromFile())
				{
					string cmd = Console.ReadLine();
					while (this.MainDispatcherWorker.IsBusy)
					{
						Console.WriteLine("正在尝试关闭服务器");
						cmd = Console.ReadLine();
					}
				}
			}
		}

		
		private void ExecuteBackgroundWorkers(object sender, EventArgs e)
		{
			if (!this.eventWorker.IsBusy)
			{
				this.eventWorker.RunWorkerAsync();
			}
			if (!this.updateMoneyWorker.IsBusy)
			{
				this.updateMoneyWorker.RunWorkerAsync();
			}
			if (!this.updatePaiHangWorker.IsBusy)
			{
				this.updatePaiHangWorker.RunWorkerAsync();
			}
			if (!this.dbWriterWorker.IsBusy)
			{
				this.dbWriterWorker.RunWorkerAsync();
			}
			if (!this.updateLastMailWorker.IsBusy)
			{
				this.updateLastMailWorker.RunWorkerAsync();
			}
			if (!this.updateGroupMailWorker.IsBusy)
			{
				this.updateGroupMailWorker.RunWorkerAsync();
			}
			if (!this.userReturnCheckWorker.IsBusy)
			{
				this.userReturnCheckWorker.RunWorkerAsync();
			}
			if (!this.updateTenWorker.IsBusy)
			{
				this.updateTenWorker.RunWorkerAsync();
			}
			if (!this.updateGiftCodeWorker.IsBusy)
			{
				this.updateGiftCodeWorker.RunWorkerAsync();
			}
			if (!this.updateFacebookWorker.IsBusy)
			{
				this.updateFacebookWorker.RunWorkerAsync();
			}
			long nowTicks = DateTime.Now.Ticks / 10000L;
			if (nowTicks - this.LastReleaseMemoryTicks >= 60000L)
			{
				if (!this.releaseMemoryWorker.IsBusy)
				{
					this.releaseMemoryWorker.RunWorkerAsync();
				}
				if (!this.dbGoodsBakTableWorker.IsBusy)
				{
					this.dbGoodsBakTableWorker.RunWorkerAsync();
				}
				if (!this.updatePreDeleteWorker.IsBusy)
				{
					this.updatePreDeleteWorker.RunWorkerAsync();
				}
			}
		}

		
		private void eventWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				while (GameDBManager.SystemServerSQLEvents.WriteEvent())
				{
				}
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "eventWorker_DoWork", false, false);
			}
		}

		
		private void updateMoneyWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				UserMoneyMgr.UpdateUsersMoney(this._DBManger);
				UserMoneyMgr.ScanInputLogToDBLog(this._DBManger);
				UserMoneyMgr.QueryTotalUserMoney();
				ChatMsgManager.ScanGMMsgToGameServer(this._DBManger);
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "updateMoneyWorker_DoWork", false, false);
			}
		}

		
		private void releaseMemoryWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				long ticks = TimeUtil.NOW();
				if (ticks >= this.NextReleaseIdleDataTicks)
				{
					this.NextReleaseIdleDataTicks = ticks + 30000L;
					int ticksSlot = 1800000;
					this._DBManger.dbUserMgr.ReleaseIdleDBUserInfos(ticksSlot);
					this._DBManger.DBRoleMgr.ReleaseIdleDBRoleInfos(ticksSlot);
					PreNamesManager.ClearUsedPreNames();
				}
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "releaseMemoryWorker_DoWork", false, false);
			}
		}

		
		private void updateLiPinMaWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				LiPinMaManager.LoadLiPinMaFromFile(this._DBManger, this.toAppendLiPinMa);
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "updateLiPinMaWorker_DoWork", false, false);
			}
		}

		
		private void updatePreDeleteRoleWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				long ticks = TimeUtil.NOW();
				if (ticks >= this.NextPreRemoveRoleTicks)
				{
					this.NextPreRemoveRoleTicks = ticks + 10000L;
					GameDBManager.PreDelRoleMgr.UpdatePreDeleteRole();
				}
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "updatePreDeleteRoleWorker_DoWork", false, false);
			}
		}

		
		private void updatePreNamesWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				PreNamesManager.LoadFromFiles(this._DBManger);
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "updatePreNamesWorker_DoWork", false, false);
			}
		}

		
		private void updatePaiHangWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				PaiHangManager.ProcessPaiHang(this._DBManger, false);
				OnlineUserNumMgr.WriteTotalOnlineNumToDB(this._DBManger);
				OnlineUserNumMgr.NotifyTotalOnlineNumToServer(this._DBManger);
				BangHuiNumLevelMgr.RecalcBangHuiNumLevel(this._DBManger);
				BangHuiDestroyMgr.ProcessDestroyBangHui(this._DBManger);
				GameDBManager.BangHuiLingDiMgr.ProcessClearYangZhouTotalTax(this._DBManger);
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "updatePaiHangWorker_DoWork", false, false);
			}
		}

		
		private void dbWriterWorker_DoWork(object sender, EventArgs e)
		{
			try
			{
				DBManager.getInstance().DBConns.SupplyConnections();
				long ticks = DateTime.Now.Ticks / 10000L;
				if (ticks - this.LastWriteDBLogTicks >= 30000L)
				{
					this.LastWriteDBLogTicks = ticks;
					GlobalEventSource.getInstance().fireEvent(new GameRunningEventObject());
				}
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "dbWriterWorker_DoWork", false, false);
			}
		}

		
		private void dbGoodsBakTableWorker_DoWork(object sender, EventArgs e)
		{
			try
			{
				long ticks = TimeUtil.NOW();
				if (ticks - this.LastdbGoodsBakTableWorkerTicks >= 30000L)
				{
					this.LastdbGoodsBakTableWorkerTicks = ticks;
					DBWriter.SwitchGoodsBackupTable(DBManager.getInstance());
				}
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "dbWriterWorker_DoWork", false, false);
			}
		}

		
		private void updateLastMail_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				UserMailManager.ScanLastMails(this._DBManger);
				UserMailManager.ClearOverdueMails(this._DBManger);
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "updateLastMail_DoWork", false, false);
			}
		}

		
		private void updateGroupMail_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				GroupMailManager.ScanLastGroupMails(this._DBManger);
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "updateGroupMail_DoWork", false, false);
			}
		}

		
		private void updateTen_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				TenManager.ScanLastGroup(this._DBManger);
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "updateTen_DoWork", false, false);
			}
		}

		
		private void updateFacebook_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
				FacebookManager.ScanLastGroup(this._DBManger);
			}
			catch (Exception ex)
			{
				DataHelper.WriteFormatExceptionLog(ex, "updateFacebook_DoWork", false, false);
			}
		}

		
		public bool CanExit()
		{
			if (this._TCPManager.MySocketListener.ConnectedSocketsCount > 0)
			{
				if (Global.IsGameServerClientOnline(1) || Global.IsGameServerClientOnline(GameDBManager.ZoneID))
				{
					Console.WriteLine(string.Format("有游戏服务器或者分线服务器连接到{0}，无法断开!", Console.Title));
					return false;
				}
			}
			return true;
		}

		
		private void Window_Closing()
		{
			if (!this.MustCloseNow)
			{
				if (!this.EnterClosingMode)
				{
					this.EnterClosingMode = true;
					this.LastWriteDBLogTicks = 0L;
					Program.NeedExitServer = true;
				}
			}
		}

		
		public static string GetVersionDateTime()
		{
			Program.AssemblyFileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
			int revsion = Assembly.GetExecutingAssembly().GetName().Version.Revision;
			int build = Assembly.GetExecutingAssembly().GetName().Version.Build;
			DateTime dtbase = new DateTime(2000, 1, 1, 0, 0, 0);
			TimeSpan tsbase = new TimeSpan(dtbase.Ticks);
			TimeSpan tsv = new TimeSpan(tsbase.Days + build, 0, 0, revsion * 2);
			DateTime dtv = new DateTime(tsv.Ticks);
			return dtv.ToString("yyyy-MM-dd HH") + string.Format(" {0}", Program.AssemblyFileVersion.FilePrivatePart);
		}

		
		public static FileVersionInfo AssemblyFileVersion;

		
		private static Program.ControlCtrlDelegate newDelegate = new Program.ControlCtrlDelegate(Program.HandlerRoutine);

		
		public static Program ServerConsole = new Program();

		
		private static Dictionary<string, Program.CmdCallback> CmdDict = new Dictionary<string, Program.CmdCallback>();

		
		private static bool NeedExitServer = false;

		
		private static string[] cmdLineARGS = null;

		
		private int _TotalConnections = 0;

		
		private DBManager _DBManger = DBManager.getInstance();

		
		private TCPManager _TCPManager = null;

		
		private bool MustCloseNow = false;

		
		private bool EnterClosingMode = false;

		
		private int ClosingCounter = 6000;

		
		private long LastWriteDBLogTicks = DateTime.Now.Ticks / 10000L;

		
		private long NextReleaseIdleDataTicks = DateTime.Now.Ticks / 10000L;

		
		private long NextPreRemoveRoleTicks = DateTime.Now.Ticks / 10000L;

		
		private long LastdbGoodsBakTableWorkerTicks = DateTime.Now.Ticks / 10000L;

		
		private static string ProgramExtName = "";

		
		private BackgroundWorker eventWorker;

		
		private BackgroundWorker updateMoneyWorker;

		
		private BackgroundWorker releaseMemoryWorker;

		
		private BackgroundWorker updateLiPinMaWorker;

		
		private BackgroundWorker updatePreDeleteWorker;

		
		private BackgroundWorker updatePreNamesWorker;

		
		private BackgroundWorker updatePaiHangWorker;

		
		private BackgroundWorker dbWriterWorker;

		
		private BackgroundWorker updateLastMailWorker;

		
		private BackgroundWorker dbGoodsBakTableWorker;

		
		private BackgroundWorker MainDispatcherWorker;

		
		private BackgroundWorker updateGroupMailWorker;

		
		private BackgroundWorker userReturnCheckWorker;

		
		private BackgroundWorker updateTenWorker;

		
		private BackgroundWorker updateGiftCodeWorker;

		
		private BackgroundWorker updateFacebookWorker;

		
		private long LastReleaseMemoryTicks = DateTime.Now.Ticks / 10000L;

		
		private bool toAppendLiPinMa = false;

		
		
		public delegate bool ControlCtrlDelegate(int CtrlType);

		
		
		public delegate void CmdCallback(string cmd);
	}
}
