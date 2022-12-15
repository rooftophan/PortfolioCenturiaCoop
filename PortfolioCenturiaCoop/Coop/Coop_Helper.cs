using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coop_Helper {
	public static long _coopID { get; set; } = 0;
	public static string _coopCode { get; set; } = "";
	public static bool _isUseTicket { get; set; } = false;
	public static COOP_BATTLE_PLAY_TYPE _CoopPlayType { get; set; } = COOP_BATTLE_PLAY_TYPE.NONE;
	public static ChatManager.CoopChatData _coopChat { get; set; } = null;
	public static bool _isCoopRestart { get; set; } = false;
	public static long _replayScore { get; set; } = 0;
	public static long _opponent_player_id { get; set; } = 0;

	public static List<UnitController> _coopBoss3Summoners = null;

	public static void Init() {
		_coopID = 0;
		_coopCode = "";
		_isUseTicket = false;
		_CoopPlayType = COOP_BATTLE_PLAY_TYPE.NONE;
		_coopChat = null;
		_isCoopRestart = false;
		_replayScore = 0;
		_opponent_player_id = 0;

		if( _coopBoss3Summoners != null ) {
			for( int i = 0;i< _coopBoss3Summoners.Count;i++ ) {
				if( _coopBoss3Summoners[i] == null ) continue;
				GameObject.Destroy( _coopBoss3Summoners[i].gameObject );
			}
			_coopBoss3Summoners.Clear();
			_coopBoss3Summoners = null;
		}

		CoopPartyChat.Init();
	}

	public static void ResetCoopChatData() {
		_coopChat = null;
	}

	public static bool CheckTicketCount( OnOpenPopupResult cb ) {
		int ticketCount = PlayerManager._UserTable._UserCurrencyTable.GetCount( CURRENCY_TYPE.COOP_TICKET );
		if( ticketCount < 1 ) {
			ShowCoopTicketNotiPopup( cb );
			return false;
		}

		return true;
	}

	public static void ShowCoopTicketNotiPopup( OnOpenPopupResult cb ) {
		//string title = "협동전 티켓 부족";
		string title = StringManager.GetStringTable( 12350 );
		//string desc = "티켓을 모두 소모하여 보상을 받을 수 없습니다.\n그래도 입장하시겠습니까?";
		string desc = StringManager.GetStringTable( 12351 );
		//UICommon_Popup.Open( title, desc, "입장", true, cb );
		UICommon_Popup.Open( title, desc,  StringManager.GetStringTable( 12353 ), true, cb );
	}

	public static void ShowBuyCoopTicketPopup( Action onTicketBuy ) {
		//int priceValue = GameManager._ConstantTable.GetValueI( CONSTANT_TYPE.COOP_TICKET_PRICE_1 );
		int priceValue = 0;

		UI_Coop_TicketNoti_Popup notiPopup = UiManager.Attach( typeof( UI_Coop_TicketNoti_Popup ), false, UI_Coop_TicketNoti_Popup.TICKETNOTI_TYPE.TICKET_BUY, priceValue ) as UI_Coop_TicketNoti_Popup;
		notiPopup._onBuyCoopTicket = onTicketBuy;
	}

	public static void RestartCoopBattle() {
		_isCoopRestart = true;
		if( _coopBoss3Summoners != null ) {
			for( int i = 0; i < _coopBoss3Summoners.Count; i++ ) {
				if( _coopBoss3Summoners[i] == null ) continue;
				GameObject.Destroy( _coopBoss3Summoners[i].gameObject );
			}
			_coopBoss3Summoners.Clear();
			_coopBoss3Summoners = null;
		}

		UI_EventNotice.ClosePopup();

		PlayerManager.OnDeckClear();
		PlayerManager._UserTable.OnUpdate();

		SoundManager.Destroy();
		SocketManager.Destroy();

		GlobalManager._ArenaMatchTable._type = ARENA_MATCH_TYPE.USER;
		BattleManager._PVP_Mode = packet.match.Mode.COOP;
		PlayerManager._arena_mode = ARENA_MODE.COOP;
		Coop_Helper._CoopPlayType = COOP_BATTLE_PLAY_TYPE.RETRY;

		GlobalManager._ResourceLoadType = LOAD_TYPE.ARENA;
		PlayerManager._game_mode = GAME_MODE.ARENA;

		Debug.Log( string.Format( "!!!! ====== BattleManager._PVP_Mode : {0}, PlayerManager._arena_mode : {1}, GlobalManager._ResourceLoadTyp : {2}, PlayerManager._game_mode : {3}", BattleManager._PVP_Mode, PlayerManager._arena_mode, GlobalManager._ResourceLoadType, PlayerManager._game_mode ) );

		// 초대를 보낸 유저는 강종시 서버에서 취소 체크를 할수 있도록 바로 매칭서버에 연결해 준다.
		if( ArenaMatchingManager._isInstance ) {
			int parentDepth = -1;
			UIBase curr;
			if( (curr = UiManager.GetCurrentPage()) != null ) {
				parentDepth = curr._depthEnd + 5;
			}

			UiManager.Attach( typeof( UIBattle_ArenaMatchRequest ), true, null );
			UiManager.Change( typeof( UIBattle_ArenaSearch ), (float)0, parentDepth );
		} else {
			UserArenaMatchServerTable matchServerInfo = PlayerManager._UserTable._UserArenaData.GetMatchServerTable( BattleManager._PVP_Mode );
			Debug.LogWarning( "ArenaMatchingManager.Load() - dns: " + matchServerInfo._server_dns + ", port: " + matchServerInfo._server_port );
			ArenaMatchingManager.Load( matchServerInfo._server_dns, matchServerInfo._server_port );
		}
	}

	public static void InitCoopData() {
		if( _coopBoss3Summoners != null ) {
			for( int i = 0; i < _coopBoss3Summoners.Count; i++ ) {
				if( _coopBoss3Summoners[i] == null ) continue;
				GameObject.Destroy( _coopBoss3Summoners[i].gameObject );
			}
			_coopBoss3Summoners.Clear();
			_coopBoss3Summoners = null;
		}
	}
	
	public static List<UserRewardResourceData> GetResultRewardList() {
		List<UserRewardResourceData> retValue = null;

		if( PlayerManager._UserRewardTable != null ) {
			UserCurrencyTable currencyTable = PlayerManager._UserRewardTable._UserCurrencyTable;
			if( currencyTable != null && currencyTable._DataList != null && currencyTable._DataList.Count > 0 ) {
				if( retValue == null )
					retValue = new List<UserRewardResourceData>();

				for( int i = 0; i < currencyTable._DataList.Count; i++ ) {
					UserCurrencyData currencyData = currencyTable._DataList[i];
					if( currencyData != null ) {
						UserRewardResourceData inputRewardData = new UserRewardResourceData();
						inputRewardData.SetData( ITEM_TYPE.CURRENCY, (int)currencyData._ID, currencyData._count );
						retValue.Add( inputRewardData );
					}
				}
			}
		}

		return retValue;
	}

	public static void MakeBoss3Summoners( long monID, SkillAbilityData sad ) {
		if( _coopBoss3Summoners == null ) {
			_coopBoss3Summoners = new List<UnitController>();
		}

		for( int i = 0; i < BattleManager._instance._coopBoss3SummonerPosList.Count; i++ ) {
			int formationIndex = i + 4;
			VectorX monPosition = BattleManager._instance._coopBoss3SummonerPosList[i];

			UnitController cacheUnit = null;
			bool isInputState = true;
			if( _coopBoss3Summoners.Count > i ) {
				cacheUnit = _coopBoss3Summoners[i];
				isInputState = false;
			}

			MakeSummonUnit( monID, sad, formationIndex, monPosition, cacheUnit, isInputState );
		}
	}

	static void MakeSummonUnit( long monID, SkillAbilityData sad, int formationIndex, VectorX monPosition, UnitController cacheUnit, bool isInputState ) {

		UnitController summonUnit = SystemManager.CreateCoopBoss3Summoner( formationIndex, monID, BattleManager._instance._coopDeckTable._BuffStatTable, monPosition, cacheUnit );

		SkillAbilityData findSAD = null;
		if( (findSAD = BattleManager._instance._coopBossUC._UnitStatus.Find_Ability( ABILITY_TYPE.BUFF_ATK_UP )) != null ) {
			SystemManager.SetAbility( summonUnit, summonUnit, findSAD );
		}

		if( (findSAD = BattleManager._instance._coopBossUC._UnitStatus.Find_Ability( ABILITY_TYPE.BUFF_MONKEYKING_FIREFAKE )) != null ) {
			SystemManager.SetAbility( summonUnit, summonUnit, findSAD );

			if( PlayerManager._game_mode == GAME_MODE.ARENA || PlayerManager._game_mode == GAME_MODE.ARENA_DUMMY ) {
				if( SocketManager.BattleInfoSendInterval != 0 ) {
					int targetIndex = summonUnit._FormationIndexOriginal;
					FrameSyncBattleInfo syncBattleInfoMonkey = new FrameSyncBattleInfo();
					syncBattleInfoMonkey._subIndex = 0;
					syncBattleInfoMonkey._teamNum = (sbyte)BattleManager.FindUnitActionGroup( BattleManager._instance._coopBossUC._group_type )._TeamNumber;
					syncBattleInfoMonkey._targetTeamNum = syncBattleInfoMonkey._teamNum;
					syncBattleInfoMonkey._attackIndex = (sbyte)BattleManager._instance._coopBossUC._FormationIndexOriginal;
					syncBattleInfoMonkey._targetIndex = (sbyte)targetIndex;
					syncBattleInfoMonkey._skillType = (sbyte)(GameManager._SkillMonsterTable.Find( sad._skillID )._skillType);
					syncBattleInfoMonkey._attackJudgment = (msg.EnumAttackJudgment)BattleManager._instance._coopBossUC._attackType;
					syncBattleInfoMonkey._frame = BattleManager._instance._FixedUpdateFrame;
					syncBattleInfoMonkey._damage = 0;
					syncBattleInfoMonkey._timestamp = SocketManager._instance._NowBattleTime;
					syncBattleInfoMonkey._atkPercent = 1;
					syncBattleInfoMonkey._chance.Add( 1 );
					syncBattleInfoMonkey.SetBuffList( BattleManager._instance._coopBossUC, summonUnit );
					syncBattleInfoMonkey._abilityList.Add( (ushort)ABILITY_TYPE.BUFF_MONKEYKING_FIREFAKE );
					BattleManager._instance.AddSyncBattleInfo( syncBattleInfoMonkey );
				}
			}
		}

		UIPage_Battle page = UiManager.FindPage<UIPage_Battle>();
		BattleHP cloneHP = page.InitClone_HPInfo( summonUnit );
		summonUnit._cloneParent = BattleManager._instance._coopBossUC;

		cloneHP.HideHPGauge();

		if( isInputState ) {
			_coopBoss3Summoners.Add( summonUnit );
		}

		summonUnit.gameObject.SetActiveX( true );

		SystemManager.ShowUnitEffect( summonUnit, 3182 );
	}

	public static void RemoveBoss3Summoners( SkillAbilityData sad ) {
		UIPage_Battle page = UiManager.FindPage<UIPage_Battle>();

		if( _coopBoss3Summoners != null ) {
			for( int i = 0;i< _coopBoss3Summoners.Count;i++ ) {
				if( _coopBoss3Summoners[i] == null ) {
					_coopBoss3Summoners.RemoveAt( i );
					i--;
					continue;
				}

				SystemManager.ShowUnitEffect( _coopBoss3Summoners[i], 3182 );

				BattleManager._instance.RemoveCoopBoss3Summon( _coopBoss3Summoners[i] );
				if( page != null ) {
					page.RemoveHPUC( _coopBoss3Summoners[i] );
				}
				//GameObject.Destroy( _coopBoss3Summoners[i].gameObject );
				_coopBoss3Summoners[i].OnUnitAlphaNow( 0f );

				if( PlayerManager._game_mode == GAME_MODE.ARENA || PlayerManager._game_mode == GAME_MODE.ARENA_DUMMY ) {
					if( SocketManager.BattleInfoSendInterval != 0 ) {
						UnitController uc = BattleManager._instance._coopBossUC;
						int targetIndex = i + 4;
						FrameSyncBattleInfo syncBattleInfoMonkey = new FrameSyncBattleInfo();
						syncBattleInfoMonkey._subIndex = 0;
						syncBattleInfoMonkey._teamNum = (sbyte)BattleManager.FindUnitActionGroup( uc._group_type )._TeamNumber;
						syncBattleInfoMonkey._targetTeamNum = syncBattleInfoMonkey._teamNum;
						syncBattleInfoMonkey._attackIndex = (sbyte)uc._FormationIndexOriginal;
						syncBattleInfoMonkey._targetIndex = (sbyte)targetIndex;
						syncBattleInfoMonkey._skillType = (sbyte)(SKILL_TYPE.NONE);
						syncBattleInfoMonkey._attackJudgment = (msg.EnumAttackJudgment)uc._attackType;
						syncBattleInfoMonkey._frame = BattleManager._instance._FixedUpdateFrame;
						syncBattleInfoMonkey._damage = 0;
						syncBattleInfoMonkey._timestamp = SocketManager._instance._NowBattleTime;
						syncBattleInfoMonkey._atkPercent = 1;
						syncBattleInfoMonkey._chance.Add( 1 );
						syncBattleInfoMonkey.SetBuffList( uc, _coopBoss3Summoners[i] );
						syncBattleInfoMonkey._abilityList.Add( (ushort)ABILITY_TYPE.DISPELL_BOSS3_FAKE );
						BattleManager._instance.AddSyncBattleInfo( syncBattleInfoMonkey );

						Debug.Log( string.Format( "[Coop_Make] DISPELL_BOSS3_FAKE GameManager._SkillMonsterTable.Find( sad._skillID )._skillType : {0}, _frame : {1}", GameManager._SkillMonsterTable.Find( sad._skillID )._skillType, syncBattleInfoMonkey._frame ) );
					}
				}
			}

			//_coopBoss3Summoners.Clear();
		}
	}

	public static void ActSummonerDmgATKBoss3FakePincer() {
		if( _coopBoss3Summoners != null && _coopBoss3Summoners.Count > 0 ) {
			for(int i = 0;i< _coopBoss3Summoners.Count;i++ ) {
				UnitController curUC = _coopBoss3Summoners[i];
				if( curUC._UnitStatus.Find_Ability( ABILITY_TYPE.BUFF_ATK_UP ) == null )
					continue;

				if( CheckDebuffCCList( curUC._UnitStatus._SkillAbilityList ) )
					continue;

				GameManager.FadeRemove( curUC );
				GameManager.PauseRemove( curUC );
				GameManager.StopRemove( curUC );
				curUC._UnitStatus._IsInputAction = 1;
				curUC._UnitStatus._InputAction = ACTION_KIND.ACTIVE2;
				curUC.SetAnimation( ACTION_KIND.ACTIVE2 );
			}
		}
	}

	public static void RemoveBoss3SummonerFade() {
		if( _coopBoss3Summoners != null && _coopBoss3Summoners.Count > 0 ) {
			for( int i = 0; i < _coopBoss3Summoners.Count; i++ ) {
				UnitController curUC = _coopBoss3Summoners[i];
				GameManager.FadeRemove( curUC );
				GameManager.PauseRemove( curUC );
				GameManager.StopRemove( curUC );
			}
		}
	}

	public static bool CheckDebuffCC( SkillAbilityData sad ) {
		switch( sad._type ) {
		case ABILITY_TYPE.DEBUFF_CC_STUN:
		case ABILITY_TYPE.DEBUFF_CC_BLOCK_PASSIVE_IFACTIVEUSED:
		case ABILITY_TYPE.DEBUFF_CC_STUN_IFDOT:
		case ABILITY_TYPE.DEBUFF_CC_FREEZE:
		case ABILITY_TYPE.DEBUFF_CC_FREEZE_IFCRITICAL:
		case ABILITY_TYPE.DEBUFF_CC_FREEZE_IFHAVEDEBUFF_WEAK_MRG_COOLTIME_DOWN:
        case ABILITY_TYPE.DEBUFF_CC_FREEZE_IFHAVEDEBUFF_WEAK_MRG_COOLTIME_DOWN_HARD:
		case ABILITY_TYPE.DEBUFF_CC_HIJACKING_IFSTUN:
		case ABILITY_TYPE.DEBUFF_CC_SLEEP:
		case ABILITY_TYPE.DEBUFF_CC_KNOCKBACK:
		case ABILITY_TYPE.DEBUFF_CC_BURN_HANDCARD:
		case ABILITY_TYPE.DEBUFF_CC_BURN_MANA:
		case ABILITY_TYPE.DEBUFF_CC_STUN_IFCRITICAL:
		case ABILITY_TYPE.DEBUFF_CC_BURN_HANDCARD_IFCRITICAL:
		case ABILITY_TYPE.DEBUFF_CC_STUN_IFHAVEDEBUFF_WEAK_MRG_COOLTIME_DOWN:
		case ABILITY_TYPE.DEBUFF_CC_STOP:
		case ABILITY_TYPE.DEBUFF_CC_STUN_TRANSITION:
		case ABILITY_TYPE.DEBUFF_CC_STUN_IFSHIELD_ME:
		case ABILITY_TYPE.DEBUFF_CC_FREEZE_ADD_TIME_IFDEBUFF:
			return true;
		}

		return false;
	}

	public static bool CheckDebuffCCList( List<SkillAbilityData> skillAbilityList ) {
		if( skillAbilityList != null && skillAbilityList.Count > 0 ) {
			for( int i = 0; i < skillAbilityList.Count; i++ ) {
				SkillAbilityData sad = skillAbilityList[i];
				if( sad == null || sad._isRemove )
					continue;

				if( CheckDebuffCC( sad ) )
					return true;
			}
		}
		
		return false;
	}
}
