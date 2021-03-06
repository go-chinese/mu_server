﻿using System;

namespace KF.Remoting
{
	
	public class KuaFuLueDuoStateMachine
	{
		
		public KuaFuLueDuoStateMachine.StateType GetCurrState()
		{
			return this._CurrState;
		}

		
		public void SetCurrState(KuaFuLueDuoStateMachine.StateType state, DateTime now, int param)
		{
			KuaFuLueDuoStateMachine.StateHandler oldHandler = this.Handlers[(int)this._CurrState];
			if (oldHandler != null)
			{
				oldHandler.Leave(now, param);
			}
			this._CurrState = state;
			KuaFuLueDuoStateMachine.StateHandler newHandler = this.Handlers[(int)this._CurrState];
			this._CurrStateEnterTicks = now.Ticks;
			if (newHandler != null)
			{
				newHandler.Enter(now, param);
			}
		}

		
		public long ContinueTicks(DateTime now)
		{
			return now.Ticks - this._CurrStateEnterTicks;
		}

		
		public void Install(KuaFuLueDuoStateMachine.StateHandler handler)
		{
			this.Handlers[(int)handler.State] = handler;
		}

		
		public void Tick(DateTime now, int param)
		{
			KuaFuLueDuoStateMachine.StateHandler handler = this.Handlers[(int)this._CurrState];
			if (handler != null)
			{
				handler.Update(now, param);
			}
		}

		
		private KuaFuLueDuoStateMachine.StateHandler[] Handlers = new KuaFuLueDuoStateMachine.StateHandler[7];

		
		private KuaFuLueDuoStateMachine.StateType _CurrState = KuaFuLueDuoStateMachine.StateType.None;

		
		private long _CurrStateEnterTicks;

		
		public long Tag1;

		
		public TimeSpan Tag2;

		
		public enum StateType
		{
			
			None,
			
			Init,
			
			SignUp,
			
			PrepareGame,
			
			NotifyEnter,
			
			GameStart,
			
			RankAnalyse,
			
			Max
		}

		
		public class StateHandler
		{
			
			
			
			public KuaFuLueDuoStateMachine.StateType State { get; private set; }

			
			public void Enter(DateTime now, int param)
			{
				if (this.enterAction != null)
				{
					this.enterAction(now, param);
				}
			}

			
			public void Update(DateTime now, int param)
			{
				if (this.updateAction != null)
				{
					this.updateAction(now, param);
				}
			}

			
			public void Leave(DateTime now, int param)
			{
				if (this.leaveAction != null)
				{
					this.leaveAction(now, param);
				}
			}

			
			public StateHandler(KuaFuLueDuoStateMachine.StateType state, Action<DateTime, int> enter, Action<DateTime, int> updater, Action<DateTime, int> leaver)
			{
				this.State = state;
				this.enterAction = enter;
				this.updateAction = updater;
				this.leaveAction = leaver;
			}

			
			private Action<DateTime, int> enterAction = null;

			
			private Action<DateTime, int> updateAction = null;

			
			private Action<DateTime, int> leaveAction = null;
		}
	}
}
