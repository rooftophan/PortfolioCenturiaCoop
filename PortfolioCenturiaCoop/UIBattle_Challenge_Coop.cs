using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIBattle_Challenge_Coop : UIBase {

	class RankComparer : IComparer<RankCoopInfo> {
		public int Compare( RankCoopInfo first, RankCoopInfo second ) {
			if( first == null && second == null ) return 0;
			else if( first == null ) return -1;
			else if( second == null ) return 1;
			else {
				if( first._rank > second._rank ) {
					return 1;
				} else if( first._rank < second._rank ) {
					return -1;
				}
			}

			return 0;
		}
	}

	public enum COOP_BOSS_SKILL_TYPE {
		PASSIVE = 0,
		ACTIVE_1 = 1,
		ACTIVE_2 = 2,
		ACTIVE_3 = 3,
		ACTIVE_4 = 4,
	}

	protected UIResources _object {
		get {
			if( __object == null ) {

				GameObject go = LoadUI( "UI/UIBattle_Challenge/ChallengeMode_coop", transform.parent );
				__object = go.GetComponent<UIResources>();
			}
			return __object;
		}
	}

	private List<UIResources> _leftMenuList = new List<UIResources>();
	private List<int> _bossSkillIDList = new List<int>();
	private List<BaseSkillData> _bossSkillInfos = new List<BaseSkillData>();
	private List<BaseSkillData> _servantSkillInfos = new List<BaseSkillData>();
	private UIResources _skillToolTip = null;
	private UIResources _seasonSkillToolTip = null;
	bool _isResizeTooltip = false;
	UISprite _toolTipBGSprite = null;
	UILabel _toolTipTitleLabel = null;
	string _titleText;
	int _delayFrameCount = 0;
	bool _isToolTipLeftBG = false;
	bool _isResizeSeasonTooltip = false;
	int _delaySeasonFrameCount = 0;
	UISprite _toolTipSeasonBGSprite = null;
	GameObject _bossCharObj = null;

	private CHALLENGE_COOP_STATE _curCoopState = CHALLENGE_COOP_STATE.NONE;

	UILabel _seasonRemainTime;
	string _leftTimeStr = "";
	Dictionary<UILabel, string> _TooltipColorDic = new Dictionary<UILabel, string>();

	bool _IsRankRefresh = true; /// 랭킹 리스트 최초 1회만 갱신 되도록 처리
	List<RankCoopInfo> _rankList = new List<RankCoopInfo>();
	Dictionary<string /* accountID + accoundID */, RankCoopInfo> _dicRankData = new Dictionary<string, RankCoopInfo>();

	private bool _isRequestCoopInfo = false;
	bool _isAttachState = false;

	UI_EndlessScroll _rankEndless = null;

	long _userRank = 0;
	bool _isTimeUpdate = false;

	float _toolTipPosX = 0f;
	float _toolTipGapX = 0f;

	int _curMonsterID = -1;

	public override void OnPageAwake( object[] objs ) {
		PlayerManager._last_game_mode = GAME_MODE.LOBBY;

		UIResources parent = _object;
		parent.gameObject.SetActiveX( true );

		_userRank = 0;
		_isRequestCoopInfo = false;

		_skillToolTip = parent.GetData<GameObject>( "skill_tooltip" ).GetComponent<UIResources>();
		_skillToolTip.gameObject.SetActiveX( false );
		_seasonSkillToolTip = parent.GetData<GameObject>( "current_spell_tooltip" ).GetComponent<UIResources>();
		_seasonSkillToolTip.gameObject.SetActiveX( false );

		OnAutoPanel( parent.gameObject, true, -1, false );
		PlayForward( parent.gameObject, OnForwardComplete );
		BeginRepeat( 0.25f );
		OnFlush_Awake();

		_isTimeUpdate = true;
	}

	public override void OnPageAttach() {
		OnFlush();
		GlobalManager._UserSetupTable._UserContentsNoticeTable.SaveData( MOD_TYPE.COOP_BATTLE );

		if( _IsRankRefresh )
			_IsRankRefresh = false;

		_isAttachState = true;
	}

	public override void OnPageDetach() {
		base.OnPageDetach();

		_isAttachState = false;
	}

	protected override void OnPageRequest() {
		_IsDataReceive = false;

		HttpManager.Request_CoopRankList( OnCoopRankComplete );
	}

	void OnCoopRankComplete( bool isSuccess, object data ) {
		if( isSuccess ) {
			OnPageLoadComplete( true, null );
			OnFlush_Rank();
			OnFlush_MyRank();
		}
	}

	void OnForwardComplete() {
        parentPage.OnPageActive( false );
	}

	public override void OnPageActive( bool isActive ) {
		base.OnPageActive( isActive );
		_object.gameObject.SetActiveX( isActive );
	}

	protected override void Update() {
#if NOVA_SIMULATOR
		return;
#endif
		base.Update();

		if( _skillToolTip != null && _skillToolTip.gameObject.activeSelf ) {
			if(!_isResizeTooltip) {
				_delayFrameCount++;
				if( _delayFrameCount >= 3 ) {
					_toolTipTitleLabel.SetTextX_Format( "{0}", _titleText );
					BoxCollider toolTipBGCollier = _toolTipBGSprite.gameObject.GetComponent<BoxCollider>();
					if( _isToolTipLeftBG ) {
						toolTipBGCollier.center = new Vector3( (float)_toolTipBGSprite.width * 0.5f, -(float)_toolTipBGSprite.height * 0.5f, 0f );
					} else {
						toolTipBGCollier.center = new Vector3( -(float)_toolTipBGSprite.width * 0.5f, (float)_toolTipBGSprite.height * 0.5f, 0f );
					}

					toolTipBGCollier.size = new Vector3( _toolTipBGSprite.width - 20, _toolTipBGSprite.height - 20, 1f );
					_isResizeTooltip = true;
				}
			} else {
				if( Input.GetMouseButtonDown( 0 ) ) {
					Ray ray = GameManager._ui_camera.cachedCamera.ScreenPointToRay( Input.mousePosition );
					RaycastHit hit;
					if( Physics.Raycast( ray, out hit ) ) {
						if( hit.collider.gameObject.name != "skill_tooltip_bg" ) {
							CloseBossSkillToolTip();
						}
					} else {
						CloseBossSkillToolTip();
					}
				}
			}
		}

		if( _seasonSkillToolTip != null && _seasonSkillToolTip.gameObject.activeSelf ) {
			if( !_isResizeSeasonTooltip ) {
				_delaySeasonFrameCount++;
				if( _delaySeasonFrameCount >= 3 ) {
					BoxCollider toolTipSeasonBGCollier = _toolTipSeasonBGSprite.gameObject.GetComponent<BoxCollider>();
					toolTipSeasonBGCollier.center = new Vector3( (float)_toolTipSeasonBGSprite.width * 0.5f, (float)_toolTipSeasonBGSprite.height * 0.5f, 0f );

					toolTipSeasonBGCollier.size = new Vector3( _toolTipSeasonBGSprite.width - 20, _toolTipSeasonBGSprite.height - 20, 1f );
					_isResizeSeasonTooltip = true;
				}
			} else {
				if( Input.GetMouseButtonDown( 0 ) ) {
					Ray ray = GameManager._ui_camera.cachedCamera.ScreenPointToRay( Input.mousePosition );
					RaycastHit hit;
					if( Physics.Raycast( ray, out hit ) ) {
						if( hit.collider.gameObject.name != "current_spell_bg" ) {
							CloseSeasonSkillToolTip();
						}
					} else {
						CloseSeasonSkillToolTip();
					}
				}
			}
		}
	}

	void SetCoopText( UIResources parent ) {
		//parent.GetData<UILabel>( "title_label" ).SetTextX_Format( "{0}", "협동전" );
		parent.GetData<UILabel>( "title_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12333 ) );

		//parent.GetData<UILabel>( "friend_matching_btn_label" ).SetTextX_Format( "{0}", "친구 초대" ); // StringManager.GetStringTable( 12191 )
		parent.GetData<UILabel>( "friend_matching_btn_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12191 ) );
		//parent.GetData<UILabel>( "random_matching_btn_label" ).SetTextX_Format( "{0}", "랜덤 매칭" );
		parent.GetData<UILabel>( "random_matching_btn_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12337 ) );

		//parent.GetData<UILabel>( "boss_damage_info_label" ).SetTextX_Format( "{0}", "보스에게 입힌 피해량이 높아질수록\n더 많은 보상을 획득할 수 있습니다." );
		parent.GetData<UILabel>( "boss_damage_info_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12503 ) );

		//parent.GetData<UILabel>( "rank_empty_label" ).SetTextX_Format( "{0}", "아직 협동전에서 승리한 소환사가 없습니다." );
		parent.GetData<UILabel>( "rank_empty_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12515 ) );

		//parent.GetData<UILabel>( "coop_end_label" ).SetTextX_Format( "{0}", "협동전 종료" );
		parent.GetData<UILabel>( "coop_end_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12523 ) );
	}

	void OnFlush_Coop( UIResources parent ) {
		ChallengeCoopSeason season = PlayerManager._UserTable._UserArenaData._CoopSeason.GetCurrentSeason();
		if( season == null ) {
			season = PlayerManager._UserTable._UserArenaData._CoopSeason.GetNextSeason();
		}

		if( season != null ) {
			CHALLENGE_COOP_STATE coopState = PlayerManager._UserTable._UserArenaData._CoopSeason.GetState();
			bool isRefreshCoopInfo = false;
			if( _curCoopState != coopState ) {
				if( _curCoopState == CHALLENGE_COOP_STATE.NEXT_START_READY || _curCoopState == CHALLENGE_COOP_STATE.CALCULATE_COOP || _curCoopState == CHALLENGE_COOP_STATE.NEXT_CALCULATE_COOP ) {
					isRefreshCoopInfo = true;
					OnRefreshCoopInfo();
				}
			}

			if( isRefreshCoopInfo == false ) {
				SetCoopState( coopState );
			}
		}
	}

	void OnRefreshCoopInfo() {
		if( _isRequestCoopInfo )
			return;

		_isRequestCoopInfo = true;
		HttpManager.Request_CoopInfo( OnComplete_CoopInfo );
	}

	protected void OnComplete_CoopInfo( bool isSuccess, object data ) {
		if( isSuccess ) {
			_isRequestCoopInfo = false;
			CoopSeasonInfo seasonInfo = PlayerManager._UserTable._UserArenaData._CoopSeason;
			CHALLENGE_COOP_STATE state = seasonInfo.GetState();
			SetCoopState( state );
			OnFlush();
			_isTimeUpdate = true;
		}
	}

	void SetCoopState( CHALLENGE_COOP_STATE state ) {
		if( _curCoopState == state )
			return;

		_curCoopState = state;
		switch( _curCoopState ) {
		case CHALLENGE_COOP_STATE.NONE:
			SetCoopNoneState();
			break;
		case CHALLENGE_COOP_STATE.NOT_OPEN:
			SetCoopNotOpenState();
			break;
		case CHALLENGE_COOP_STATE.START_READY:
			SetCoopStartReadyState();
			break;
		case CHALLENGE_COOP_STATE.PROGRESS_COOP:
			SetCoopProgressCoopState();
			break;
		case CHALLENGE_COOP_STATE.CALCULATE_COOP:
			SetCoopCalculateState();
			break;
		}
	}

	void SetCoopNoneState() {
		UIResources parent = _object;
		parent.GetData<UILabel>( "coop_end_label" ).gameObject.SetActiveX( false );
		parent.GetData<GameObject>( "btn_obj" ).SetActiveX( false );

		_seasonRemainTime.SetTextX_Format( _leftTimeStr, "--:--" );

		GameObject spellObj = parent.GetData<GameObject>( "SpellIcon_Small" );
		spellObj.gameObject.SetActiveX( false );
	}

	void SetCoopNotOpenState() {
		UIResources parent = _object;
		parent.GetData<UILabel>( "coop_end_label" ).gameObject.SetActiveX( false );
		parent.GetData<GameObject>( "btn_obj" ).SetActiveX( false );

		_seasonRemainTime.SetTextX_Format( _leftTimeStr, "--:--" );

		GameObject spellObj = parent.GetData<GameObject>( "SpellIcon_Small" );
		spellObj.gameObject.SetActiveX( false );
	}

	void SetCoopStartReadyState() {
		UIResources parent = _object;
		parent.GetData<UILabel>( "coop_end_label" ).gameObject.SetActiveX( false );
		parent.GetData<GameObject>( "btn_obj" ).SetActiveX( false );

		ChallengeCoopSeason season = PlayerManager._UserTable._UserArenaData._CoopSeason.GetCurrentSeason();
		if( season != null ) {
			_seasonRemainTime.SetTextX_TimeSpan( _leftTimeStr, season._CoopBattleTimeData.GetStartRemainTime() );
		}

		GameObject spellObj = parent.GetData<GameObject>( "SpellIcon_Small" );
		spellObj.gameObject.SetActiveX( false );
	}

	void SetCoopProgressCoopState() {
		UIResources parent = _object;
		parent.GetData<UILabel>( "coop_end_label" ).gameObject.SetActiveX( false );
		parent.GetData<GameObject>( "btn_obj" ).SetActiveX( true );

		OnFlush_SeasonSpell( parent );
	}

	void OnFlush_SeasonSpell( UIResources parent ) {
		ChallengeCoopSeason season = PlayerManager._UserTable._UserArenaData._CoopSeason.GetCurrentSeason();
		if( season != null ) {
			_seasonRemainTime.SetTextX_TimeSpan( _leftTimeStr, season._CoopBattleTimeData.GetEndRemainTime() );

			UserSkillData skillData = PlayerManager._UserTable._UserArenaData._CoopSeason.GetCurrentSkillData();
			if( skillData != null ) {
				GameObject spellObj = parent.GetData<GameObject>( "SpellIcon_Small" );
				UIIcon.SetSpellIcon_Small( spellObj.GetComponent<UIResources>(), skillData );
			}
		}
	}

	void SetCoopCalculateState() {
		UIResources parent = _object;
		parent.GetData<UILabel>( "coop_end_label" ).gameObject.SetActiveX( true );
		parent.GetData<GameObject>( "btn_obj" ).SetActiveX( false );

		ChallengeCoopSeason season = PlayerManager._UserTable._UserArenaData._CoopSeason.GetCurrentSeason();
		if( season != null ) {
			_seasonRemainTime.SetTextX_TimeSpan( _leftTimeStr, season._CoopCalculateTimeData.GetEndRemainTime() );

			UserSkillData skillData = PlayerManager._UserTable._UserArenaData._CoopSeason.GetCurrentSkillData();
			if( skillData != null ) {
				GameObject spellObj = parent.GetData<GameObject>( "SpellIcon_Small" );
				UIIcon.SetSpellIcon_Small( spellObj.GetComponent<UIResources>(), skillData );
			}

			UserTable ut = PlayerManager._UserTable;
			UserCoopInfo myCoopInfo = ut._UserArenaData._UserChallengeCoop._userCoop;
			if( myCoopInfo._season_rewarded_at == 0 && myCoopInfo._rank > 0 ) {
				HttpManager.Request_CoopSeasonRankReward( OnReceive_CoopSeasonRankReward );
			}
		}
	}

	void OnReceive_CoopSeasonRankReward( bool isSuccess, object obj ) {
		if( isSuccess == true ) {
			PlayerManager._UserTable._UserRedDotTable.SetData( CONTENT_TYPE.COOP_SEASON, false );
			UiManager.Attach( typeof( UI_Coop_Rank_Reward ), false );
		}
	}

	void OnFlush_RankTextInfo() {
		UIResources parent = _object;

		_seasonRemainTime = parent.GetData<UILabel>( "left_time_label" );
		_leftTimeStr = StringManager.GetStringTable( 11394 ) + " : {0}";

		//parent.GetData<UILabel>( "rank_title_label" ).SetTextX_Format( "{0}", "협동전 랭킹" );
		parent.GetData<UILabel>( "rank_title_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12504 ) );

		//parent.GetData<UILabel>( "myrank_info_label" ).SetTextX_Format( "{0}", "나의 랭킹" );
		parent.GetData<UILabel>( "myrank_info_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 11983 ) );

		//parent.GetData<UILabel>( "damage_label" ).SetTextX_Format( "{0}", "입힌 데미지" );
		parent.GetData<UILabel>( "damage_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12506 ) );

		//parent.GetData<UILabel>( "rank_reward_label" ).SetTextX_Format( "{0}", "예상 랭킹 보상" );
		parent.GetData<UILabel>( "rank_reward_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12507 ) );
	}

	void OnFlush_Rank() {
		UIResources parent = _object;

		UserTable ut = PlayerManager._UserTable;
		List<RankCoopInfo> rankCoops = ut._UserArenaData._UserChallengeCoop._ranks;

		_rankList.Clear();
		_dicRankData.Clear();
		for( int i = 0; i < rankCoops.Count; i++ ) {
			RankCoopInfo rankData = rankCoops[i];
			_rankList.Add( rankData );
			string dicKey = "";
			if( rankData._user_infos[0]._account_id > rankData._user_infos[1]._account_id ) {
				dicKey = string.Format( "{0}_{1}", rankData._user_infos[0]._account_id, rankData._user_infos[1]._account_id );
			} else {
				dicKey = string.Format( "{0}_{1}", rankData._user_infos[0]._account_id, rankData._user_infos[1]._account_id );
			}
		}

		if( _rankList.Count > 0 ) {
			_rankList.Sort( new RankComparer() );
			_rankEndless.OnFlush( this, OnFlush_RankItem, _rankList.Count, _IsRankRefresh );
		}

		parent.GetData<GameObject>( "rank_scroll_obj" ).SetActiveX( _rankList.Count > 0 );
		parent.GetData<GameObject>( "empty_obj" ).SetActiveX( !(_rankList.Count > 0) );
	}

	void OnFlush_RankItem( UIResources parent, int index ) {
		RankCoopInfo rankData = _rankList[index];
		if( rankData._rank >= 1 && rankData._rank <= 3 ) {
			parent.GetData<UILabel>( "rank_label" ).gameObject.SetActiveX( false );
			parent.GetData<UISprite>( "rank_icon" ).gameObject.SetActiveX( true );
			parent.GetData<UISprite>( "rank_icon" ).spriteName = GetRankSpriteName( rankData._rank );
		} else {
			parent.GetData<UILabel>( "rank_label" ).gameObject.SetActiveX( true );
			parent.GetData<UISprite>( "rank_icon" ).gameObject.SetActiveX( false );
			parent.GetData<UILabel>( "rank_label" ).SetTextX_Format( "{0}", rankData._rank.ToString() );
		}

		List<GameObject> playerObjList = parent.GetList<GameObject>( "player_obj_list" );
		for( int i = 0;i< playerObjList.Count;i++ ) {
			CoopUserInfo rankUserInfo = rankData._user_infos[i];
			UIResources playerRes = playerObjList[i].GetComponent<UIResources>();
			playerRes.GetData<UILabel>( "name_label" ).SetTextX_NameWithTitle( rankUserInfo._name, rankUserInfo._before_season_trophy, rankUserInfo._before_season_rank, "FFFFFF" );
			playerRes.GetData<GameObject>( "NationalFlag_base" ).GetComponent<UI_NationalFlag>().ChangeFlag( rankUserInfo._country );
		}

		parent.GetData<UILabel>( "damage_label" ).SetTextX_Format( "{0}", rankData._damage.ToString() );
	}

	void OnFlush_MyRank() {
		UIResources parent = _object;

		UserTable ut = PlayerManager._UserTable;
		UserCoopInfo myCoopInfo = ut._UserArenaData._UserChallengeCoop._userCoop;

		if( myCoopInfo._rank == 0 ) {
			parent.GetData<UILabel>( "myrank_rank_label" ).SetTextX_Format( "{0}", " -" );
			parent.GetData<UILabel>( "damage_num_label" ).SetTextX_Format( "{0}", "0" );
			parent.GetData<UILabel>( "rank_reward_num_label" ).SetTextX_Format( "{0}", "0" );
			return;
		}

		if( _userRank == myCoopInfo._rank )
			return;

		_userRank = myCoopInfo._rank;

		int rewardType = 0;
		int rankPercent = 0;
		int maxRank = GameManager._ConstantTable.GetValueI( CONSTANT_TYPE.COOP_INT_RANK );
		if( myCoopInfo._rank > maxRank ) {
			rewardType = 1;
			rankPercent = Mathf.CeilToInt( ((float)myCoopInfo._rank / (float)myCoopInfo._max_user_count) * 100f );
			if( rankPercent > 100 )
				rankPercent = 100;
			parent.GetData<UILabel>( "myrank_rank_label" ).SetTextX_Format( StringManager.GetStringTable( 12505 ), rankPercent.ToString() );
		} else {
			rewardType = 0;
			string rankNum = string.Format( StringManager.GetStringTable( 12426 ), myCoopInfo._rank );
			parent.GetData<UILabel>( "myrank_rank_label" ).SetTextX_Format( " {0}", rankNum );
		}

		parent.GetData<UILabel>( "damage_num_label" ).SetTextX_Format( "{0}", myCoopInfo._best_rank_damage.ToString() );

		CoopRankRewardData rankRewardData = null;
		for( int i = 0; i < GameManager._CoopRankRewardTable._DataList.Count; i++ ) {
			CoopRankRewardData curData = GameManager._CoopRankRewardTable._DataList[i];
			if( rewardType != curData._rankRangeMinType )
				continue;

			if(rewardType == 0 ) {
				if( curData._rankRangeMin <= myCoopInfo._rank && curData._rankRangeMax >= myCoopInfo._rank ) {
					rankRewardData = curData;
					break;
				}
			} else {
				if( curData._rankRangeMin <= rankPercent && curData._rankRangeMax >= rankPercent ) {
					rankRewardData = curData;
					break;
				}
			}
		}

		if( rankRewardData != null ) {
			UserRewardResourceData rewardData = rankRewardData._UserRewardResourceData;

			parent.GetData<UILabel>( "rank_reward_num_label" ).SetTextX_Format( "{0}", rewardData._ItemCount.ToString() );
		} else {
			parent.GetData<UILabel>( "rank_reward_num_label" ).SetTextX_Format( "{0}", "0" );
		}
	}

	string GetRankSpriteName( long rank ) {
		return stringx.Format( "{0}{1}", "rank_icon_", rank.ToString() );
	}

	void OnFlush_RightInfo( CoopBattleConfigData coopBattleData ) {
		UIResources parent = _object;
		
		parent.GetData<UILabel>( "friend_matching_cost_label" ).SetTextX_Format( "{0}", "1" );
		parent.GetData<UILabel>( "random_matching_cost_label" ).SetTextX_Format( "{0}", "1" );
	}

	public override void OnClick( GameObject go, Vector3 pos, object[] objs ) {
		string str = go.name;

		int index = 0;

		if( str.StartsWith( "boss_skill_base_" ) == true ) {
			index = str.ToInt();
			str = "boss_skill_base_";
		}

		if( str.StartsWith( "endless_scroll_rank_index_" ) == true ) {
			index = str.ToInt();
			str = "endless_scroll_rank_index_";
		}

		if( str.StartsWith( "servant_skill_base_" ) == true ) {
			index = str.ToInt();
			str = "servant_skill_base_";
		}

		switch( str ) {
		case "coop_list_back_btn":
			OnClick_Detach();
			break;
		case "friend_matching_btn":
			OnClick_InviteFriend();
			break;
		case "random_matching_btn":
			OnClick_RandomMatching();
			break;
		case "boss_skill_base_":
			OnClick_BossSkillIcon( index - 1 );
			break;
		case "servant_skill_base_":
			OnClick_ServantSkillIcon( index - 1 );
			break;
		case "reward_info_btn":
			OnClick_RewardInfo();
			break;
		case "rank_reward_info_btn":
			OnClick_RankRewardInfo();
			break;
		case "endless_scroll_rank_index_": {
				RankCoopInfo rankData = _rankList[index];
				HttpManager.Request_CoopDeck( rankData._team_id, index, OnReceive_CoopDeck );
			}
			break;
		case "season_spell_icon":
			OnClick_SeasonSpell();
			break;
		case "cowork_ticket_icon":
			Attach( typeof( UI_ItemTooltip ), false, go, (object)CURRENCY_TYPE.COOP_TICKET );
			break;
		}
	}

	void OnClick_SeasonSpell() {
		UserTable ut = PlayerManager._UserTable;
		UserSkillData buffSkillData = ut._UserArenaData._CoopSeason.GetCurrentSkillData();
		ShowSeasonSkillToolTip( buffSkillData );
	}

	void OnReceive_CoopDeck( bool isSuccess, object obj ) {
		if( isSuccess == true ) {
			int index = (int)obj;
			RankCoopInfo rankData = _rankList[index];

			UserTable ut = PlayerManager._UserTable;

			UiManager.Attach( typeof( UI_Coop_DeckInfo_Popup ), false, ut._UserArenaData._UserChallengeCoop, rankData );
		}
	}

	void OnClick_InviteFriend() {
		if( Coop_Helper.CheckTicketCount( OnTicketBuyNoti_InviteFrined ) == false )
			return;

		Attach( typeof( UI_Coop_Invite_Popup ), false );
	}

	void OnClick_RandomMatching() {
		if( Coop_Helper.CheckTicketCount( OnTicketBuyNoti_RandomMatching ) == false )
			return;

		SetRandomMatchingData();
	}

	void SetRandomMatchingData() {
		UserTable ut = PlayerManager._UserTable;
		if( ArenaDeck_Helper.IsDoneCustomDeckSetting( ut._UserArenaData._UserChallengeCoop._userPresetData ) == false ) {
			//UICommon_Popup.Open( "안내", "현재 협동전에 사용되는 덱 정보가 없습니다.\n덱 구성 후 진행할 수 있습니다.", "확인", false, UI_UnitOrganizaion으로 이동 );
			UICommon_Popup.Open( StringManager.GetStringTable( 10002 ), StringManager.GetStringTable( 12519 ), StringManager.GetStringTable( 10004 ), false, ( POPUP_RESULT result_popup, object[] parameters ) => {
				Coop_Helper._coopID = PlayerManager._UserTable._UserArenaData._CoopSeason._current._data_id;
				UiManager.Attach( typeof( UIBattle_ArenaDeckSelect ), false, ARENA_MODE.COOP, COOP_BATTLE_PLAY_TYPE.RANDOM_MATCHING );
				Attach( typeof( UI_UnitOrganization ), false, UI_UnitOrganization.UNIT_ORGANIZATION_TYPE.COOP );
			} );
		} else {
			Coop_Helper._coopID = PlayerManager._UserTable._UserArenaData._CoopSeason._current._data_id;
			UiManager.Attach( typeof( UIBattle_ArenaDeckSelect ), false, ARENA_MODE.COOP, COOP_BATTLE_PLAY_TYPE.RANDOM_MATCHING );
		}
	}

	void OnTicketBuyNoti_InviteFrined( POPUP_RESULT result, object[] parameters ) {
		if( result == POPUP_RESULT.RESULT_LEFT ) {
			Coop_Helper.ShowBuyCoopTicketPopup( OnSuccess_TicketBuy );
		} else if( result == POPUP_RESULT.RESULT_CENTER ) {
			Attach( typeof( UI_Coop_Invite_Popup ), false );
		}
	}

	void OnTicketBuyNoti_RandomMatching( POPUP_RESULT result, object[] parameters ) {
		if( result == POPUP_RESULT.RESULT_LEFT ) {
			Coop_Helper.ShowBuyCoopTicketPopup( OnSuccess_TicketBuy );
		} else if( result == POPUP_RESULT.RESULT_CENTER ) {
			SetRandomMatchingData();
		}
	}

	void OnSuccess_TicketBuy() {
		OnFlush_TicketCount();
	}

	void OnClick_BossSkillIcon( int index ) {
		if( _bossSkillInfos[index] != null ) {
			ShowSkillToolTip( index, _bossSkillInfos[index] );
		}
	}

	void OnClick_ServantSkillIcon( int index ) {
		if( _servantSkillInfos[index] != null ) {
			ShowSkillToolTip( index, _servantSkillInfos[index], false );
		}
	}

	void OnClick_RewardInfo() {
		Attach( typeof( UI_Coop_Reward_Info ), false, _curMonsterID );
	}

	void OnClick_RankRewardInfo() {
		Attach( typeof( UI_Coop_Rank_Reward_Popup ), false );
	}

	public override void OnClick_Detach() {
        PlayReverse(_object.gameObject, Detach);
    }

    public override void OnPageDestroy() {
        if (__object != null) {
            GameObject.Destroy(__object.gameObject);
            __object = null;
        }
    }

    void OnFlush_Awake() {
		UIResources parent = _object;
		_rankEndless = parent.GetData<GameObject>( "Endless" ).GetComponent<UI_EndlessScroll>();

		parent.GetData<GameObject>( "rank_scroll_obj" ).SetActiveX( false );
		parent.GetData<GameObject>( "empty_obj" ).SetActiveX( false );

		parent.GetData<GameObject>( "btn_obj" ).SetActiveX( false );

		OnFlush_CoopInfo();

		OnFlush_TicketCount();

		SetCoopText( _object );
		OnFlush_RankTextInfo();

		OnFlush_MyRank();

		OnFlush_SeasonSpell( parent );
	}

	void OnFlush_TicketCount() {
		UIResources parent = _object;
		int ticketCount = PlayerManager._UserTable._UserCurrencyTable.GetCount( CURRENCY_TYPE.COOP_TICKET );
		UILabel coopTicketLabel = _object.GetData<UILabel>( "goods_cowork_token_label" );
		coopTicketLabel.SetTextX_Format( "{0}", ticketCount );
		if( ticketCount > 0 ) {
			parent.GetData<GameObject>( "Fx_Random_btn_obj" ).SetActiveX( true );
		} else {
			parent.GetData<GameObject>( "Fx_Random_btn_obj" ).SetActiveX( false );
		}
	}

	void ShowSkillToolTip( int index, BaseSkillData bsd, bool isBossState = true ) {
		_isResizeTooltip = false;
		_delayFrameCount = 0;

		GameObject anchor_br = _skillToolTip.GetData<GameObject>( "anchor_br" );
		GameObject anchor_bl = _skillToolTip.GetData<GameObject>( "anchor_bl" );

		UIResources toolTipInfoRes = null;
		if( isBossState ) {
			if( index < 2 ) {
				anchor_br.SetActiveX( false );
				anchor_bl.SetActiveX( true );
				toolTipInfoRes = anchor_bl.GetComponent<UIResources>();
				_isToolTipLeftBG = true;
			} else {
				anchor_br.SetActiveX( true );
				anchor_bl.SetActiveX( false );
				toolTipInfoRes = anchor_br.GetComponent<UIResources>();
				_isToolTipLeftBG = false;
			}
		} else {
			if( index == 0 ) {
				anchor_br.SetActiveX( false );
				anchor_bl.SetActiveX( true );
				toolTipInfoRes = anchor_bl.GetComponent<UIResources>();
				_isToolTipLeftBG = true;
			} else {
				anchor_br.SetActiveX( true );
				anchor_bl.SetActiveX( false );
				toolTipInfoRes = anchor_br.GetComponent<UIResources>();
				_isToolTipLeftBG = false;
			}
		}

		int skillLevel = 1;
		GameObject tooltipBGObj = toolTipInfoRes.GetData<GameObject>( "skill_tooltip_bg" );
		_toolTipBGSprite = tooltipBGObj.GetComponent<UISprite>();

		_toolTipTitleLabel = toolTipInfoRes.GetData<UILabel>( "tit_label" );
		_titleText = string.Format( "{0}", bsd.GetSkillName( skillLevel ) );
		_toolTipTitleLabel.SetTextX_Format( "{0} ", _titleText );

		UILabel toolTipLabel = toolTipInfoRes.GetData<UILabel>( "info_label" );
		string colorText = "";
		if( _TooltipColorDic.TryGetValue( toolTipLabel, out colorText ) == false ) {
			colorText = NGUIText.EncodeColor24( toolTipLabel.color );
			_TooltipColorDic.Add( toolTipLabel, colorText );
			toolTipLabel.color = Color.white;
		}
		if( colorText.Length > 0 ) {
			toolTipLabel.SetTextX_Format( "[{1}]{0}[-]", bsd.GetSkillDesc( skillLevel ), colorText );
		} else {
			toolTipLabel.SetTextX_Format( "{0}", bsd.GetSkillDesc( skillLevel ) );
		}

		_skillToolTip.gameObject.SetActiveX( true );

		float toolTipPosX = 0f;
		if( isBossState ) {
			toolTipPosX = _toolTipPosX; // -198f;
			float gapX = _toolTipGapX; // 92f;
			if( index < 2 ) {
				toolTipPosX += index * gapX;
			} else {
				toolTipPosX += (index - 2) * gapX;
			}
		} else {
			if( index == 0 ) {
				toolTipPosX = -273f;
			} else if( index == 1 ) {
				toolTipPosX = 54;
			}
		}

		_skillToolTip.GetData<GameObject>( "Position" ).transform.localPosition = new Vector3( toolTipPosX, 28f, 0f );
	}

	void ShowSeasonSkillToolTip( UserSkillData buffSkillData ) {
		_isResizeSeasonTooltip = false;
		_delaySeasonFrameCount = 0;

		GameObject tooltipBGObj = _seasonSkillToolTip.GetData<GameObject>( "skill_tooltip_bg" );
		_toolTipSeasonBGSprite = tooltipBGObj.GetComponent<UISprite>();

		_seasonSkillToolTip.GetData<UILabel>( "tit_label" ).SetTextX_Format( "{0}", buffSkillData._Name );
		UILabel toolTipLabel = _seasonSkillToolTip.GetData<UILabel>( "info_label" );
		string colorText = "";
		if( _TooltipColorDic.TryGetValue( toolTipLabel, out colorText ) == false ) {
			colorText = NGUIText.EncodeColor24( toolTipLabel.color );
			_TooltipColorDic.Add( toolTipLabel, colorText );
			toolTipLabel.color = Color.white;
		}
		if( colorText.Length > 0 ) {
			toolTipLabel.SetTextX_Format( "[{1}]{0}[-]", buffSkillData._Desc, colorText );
		} else {
			toolTipLabel.SetTextX_Format( "{0}", buffSkillData._Desc );
		}

		_seasonSkillToolTip.gameObject.SetActiveX( true );
	}

	void CloseBossSkillToolTip() {
		_skillToolTip.gameObject.SetActiveX( false );
	}

	void CloseSeasonSkillToolTip() {
		_seasonSkillToolTip.gameObject.SetActiveX( false );
	}

	void OnFlush_CoopInfo() {
		UIResources parent = _object;
		ChallengeCoopSeason curSeason = PlayerManager._UserTable._UserArenaData._CoopSeason._current;

		CoopBattleConfigData coopBattleData = GameManager._CoopBattleConfigTable.GetCoopBattleConfigData( curSeason._data_id );
		//GameObject main_summonObj = parent.GetData<GameObject>( "main_summon" );
		//main_summonObj.SetActiveX( false );
		OnFlush_BossChar( parent, coopBattleData );

		OnFlush_RightInfo( coopBattleData );
		OnFlush_BossSkill_Info( coopBattleData );

		GameObject rewardIcon1 = parent.GetData<GameObject>( "boss_da_reward_icon1" );
		GameObject rewardIcon2 = parent.GetData<GameObject>( "boss_da_reward_icon2" );

		_curMonsterID = coopBattleData._monster_1_ID;
		CoopBattleRewardData coopRewardData = GameManager._CoopBattleRewardTable.GetBattleRewardList( coopBattleData._monster_1_ID )[0];

		UserRewardResourceData rewardData1 = coopRewardData._UserRewardResourceData_1;
		UserRewardResourceData rewardData2 = coopRewardData._UserRewardResourceData_2;

		UIIcon.SetRewardIcon_Common( rewardIcon1.GetComponent<UIResources>(), rewardData1, true );
		UIIcon.SetRewardIcon_Common( rewardIcon2.GetComponent<UIResources>(), rewardData2, true );

		CoopRankRewardData rankRewardData = GameManager._CoopRankRewardTable._DataList[0];

		UserRewardResourceData rankRewardResData = rankRewardData._UserRewardResourceData;

		GameObject rewardIcon = parent.GetData<GameObject>( "myrank_reward_icon" );
		UIIcon.SetRewardIcon_Common( rewardIcon.GetComponent<UIResources>(), rankRewardResData, true );
	}

	void OnFlush_BossChar( UIResources parent, CoopBattleConfigData coopBattleData ) {
		GameObject bossCharParent = parent.GetData<GameObject>( "boss_char_parent" );
		string charPath = string.Format( "UI/UIBattle_Challenge/Coop/Coop_Boss_Char_{0}", coopBattleData._monster_1_ID );
		_bossCharObj = LoadUI( charPath, transform.parent );
		if( _bossCharObj != null ) {
			_bossCharObj.SetActiveX( true );
			_bossCharObj.transform.SetParentEx( bossCharParent.transform );
		}
	}

	void OnFlush_BossSkill_Info( CoopBattleConfigData coopBattleData ) {
#if NOVA_SIMULATOR
		return;
#endif
		List<GameObject> _skillGroupList = _object.GetList<GameObject>( "skill_group_list" );

		GameObject skillParentObj = _skillGroupList[0];
		for(int i = 0;i< _skillGroupList.Count;i++ ) {
			string skillGroupName = string.Format( "grid_skill_{0}", coopBattleData._monster_1_ID );
			if( skillGroupName == _skillGroupList[i].name ) {
				skillParentObj = _skillGroupList[i];
				_skillGroupList[i].SetActiveX( true );
			} else {
				_skillGroupList[i].SetActiveX( false );
			}
		}

		//List<GameObject> _skillBossList = _object.GetList<GameObject>( "Skill_Icon_List" );
		if( skillParentObj != null )
			skillParentObj.SetActiveX( true );
		UIResources skillParentRes = skillParentObj.GetComponent<UIResources>();
		List<GameObject> skillBossList = skillParentRes.GetList<GameObject>( "Skill_Icon_List" );
		int bossID = coopBattleData._monster_1_ID;

		_bossSkillInfos.Clear();
		MonsterData bossMonData = GameManager._MonsterTable.Find( bossID );
		for( int i = 0; i < skillBossList.Count; i++ ) {
			UIResources skillIconRes = skillBossList[i].GetComponent<UIResources>();
			List<BaseSkillData> bsd_list = null;
			if( i == (int)COOP_BOSS_SKILL_TYPE.PASSIVE ) {
				bsd_list = GameManager._BaseSkillTable.GetList( bossMonData._skillGroupID, SKILL_TYPE.BASIC_PASSIVE );
			} else if( i == (int)COOP_BOSS_SKILL_TYPE.ACTIVE_1 ) {
				bsd_list = GameManager._BaseSkillTable.GetList( bossMonData._skillGroupID, SKILL_TYPE.ACTIVE1 );
			} else if( i == (int)COOP_BOSS_SKILL_TYPE.ACTIVE_2 ) {
				bsd_list = GameManager._BaseSkillTable.GetList( bossMonData._skillGroupID, SKILL_TYPE.ACTIVE2 );
			} else if( i == (int)COOP_BOSS_SKILL_TYPE.ACTIVE_3 ) {
				bsd_list = GameManager._BaseSkillTable.GetList( bossMonData._skillGroupID, SKILL_TYPE.ACTIVE3 );
			} else if( i == (int)COOP_BOSS_SKILL_TYPE.ACTIVE_4 ) {
				bsd_list = GameManager._BaseSkillTable.GetList( bossMonData._skillGroupID, SKILL_TYPE.ACTIVE4 );
			}

			if( bsd_list != null && bsd_list.Count > 0 ) {
				BaseSkillData bsd = bsd_list[0];
				_bossSkillInfos.Add( bsd );

				skillIconRes.gameObject.SetActiveX( true );
				skillIconRes.GetData<UISprite>( "skill_icon" ).spriteName = bsd._icon;
			} else {
				_bossSkillInfos.Add( null );
				skillIconRes.gameObject.SetActiveX( false );
			}
		}

		GameObject anchor_br = _skillToolTip.GetData<GameObject>( "anchor_br" );

		_servantSkillInfos.Clear();
		if( coopBattleData._monster_2_ID > 0 ) {
			List<GameObject> skillServantList = skillParentRes.GetList<GameObject>( "skill_icon_servant_list" );
			for(int i = 0;i< skillServantList.Count;i++ ) {
				int servantID = 0;
				if( i == 0 ) {
					servantID = coopBattleData._monster_2_ID;
				} else if( i == 1 ) {
					servantID = coopBattleData._monster_3_ID;
				}
				MonsterData serVantMonData = GameManager._MonsterTable.Find( servantID );
				UIResources servantIconRes = skillServantList[i].GetComponent<UIResources>();
				List<BaseSkillData> bsd_list = GameManager._BaseSkillTable.GetList( serVantMonData._skillGroupID, SKILL_TYPE.BASIC_PASSIVE );
				
				if( bsd_list != null && bsd_list.Count > 0 ) {
					BaseSkillData bsd = bsd_list[0];
					_servantSkillInfos.Add( bsd );

					servantIconRes.gameObject.SetActiveX( true );
					servantIconRes.GetData<UISprite>( "skill_icon" ).spriteName = bsd._icon;
				} else {
					_servantSkillInfos.Add( null );
					servantIconRes.gameObject.SetActiveX( false );
				}
			}

			_toolTipPosX = -179f;
			_toolTipGapX = 83f;
			anchor_br.transform.localPosition = new Vector3( 28f, 0f, 0f );
		} else {
			_toolTipPosX = -198f;
			_toolTipGapX = 92f;
			anchor_br.transform.localPosition = new Vector3( 47f, 0f, 0f );
		}
	}

    public override void OnFlush() {
		if( _IsDataReceive == false ) {
			return;
		}

		UIResources parent = _object;

		OnFlush_Coop( parent );
	}

	UserMonsterData GetUserMonsterData( long monID ) {
		UserMonsterData userMonster = new UserMonsterData();
		userMonster.SetData( monID, 1, 1, 1, 0 );

		return userMonster;
	}

	protected override void OnUpdateRepeat( float deltaTime ) {
		base.OnUpdateRepeat( deltaTime );

		if( !_isTimeUpdate )
			return;

		UserTable ut = PlayerManager._UserTable;
		CHALLENGE_COOP_STATE state = ut._UserArenaData._CoopSeason.GetState();

		if( _isAttachState ) {
			if( state == CHALLENGE_COOP_STATE.START_READY || state == CHALLENGE_COOP_STATE.NEXT_START_READY || state == CHALLENGE_COOP_STATE.NONE ) {
				_curCoopState = state;
				_seasonRemainTime.SetTextX_Format( _leftTimeStr, "--:--" );
				_isTimeUpdate = false;
				OnClick_Detach();
				return;
			}
		}

		if( state != CHALLENGE_COOP_STATE.NONE && state != _curCoopState ) {

			if( state == CHALLENGE_COOP_STATE.NEXT_PROGRESS_COOP || state == CHALLENGE_COOP_STATE.NEXT_CALCULATE_COOP ) {
				_isTimeUpdate = false;
				OnRefreshCoopInfo();
				return;
			}

			if( _curCoopState == CHALLENGE_COOP_STATE.START_READY || _curCoopState == CHALLENGE_COOP_STATE.NEXT_START_READY ) {
				_isTimeUpdate = false;
				OnRefreshCoopInfo();
				return;
			}

			if( state == CHALLENGE_COOP_STATE.START_READY ) {
				_curCoopState = state;
				_seasonRemainTime.SetTextX_Format( _leftTimeStr, "--:--" );
			} else {
				SetCoopState( state );
			}
		}

		ChallengeCoopSeason seasonInfo = ut._UserArenaData._CoopSeason.GetCurrentSeason();

		if( seasonInfo != null ) {
			switch( state ) {
			case CHALLENGE_COOP_STATE.PROGRESS_COOP:
				if( _seasonRemainTime != null )
					_seasonRemainTime.SetTextX_TimeSpan( _leftTimeStr, seasonInfo._CoopBattleTimeData.GetEndRemainTime() );
				break;
			case CHALLENGE_COOP_STATE.CALCULATE_COOP:
				if( _seasonRemainTime != null )
					_seasonRemainTime.SetTextX_TimeSpan( _leftTimeStr, seasonInfo._CoopCalculateTimeData.GetEndRemainTime() );
				break;
			}
		}
	}
}
