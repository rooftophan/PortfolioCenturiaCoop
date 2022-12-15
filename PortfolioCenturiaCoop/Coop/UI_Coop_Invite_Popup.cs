using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UI_Coop_Invite_Popup : UIBase {
	public enum InviteTypeTab {
		NONE = -1,
		FRIEND,
		GUILD,
	}

	public class FriendContentInfo {
		public CoopFriendMemberData _coopFriendMember;
		public UIResources _contentRes;
		public bool _isConnect;
		public bool _isSelected;

		public void SelectContent(bool isEnable ) {
			_isSelected = isEnable;
			_contentRes.GetData<GameObject>( "select_bg" ).SetActiveX( _isSelected );
		}
	}

	class FriendComparer : IComparer<FriendContentInfo> {
		public int Compare( FriendContentInfo first, FriendContentInfo second ) {
			if( (first._isConnect && second._isConnect) || (!first._isConnect && !second._isConnect) ) {
				if( first._coopFriendMember._best_rank_damage > second._coopFriendMember._best_rank_damage ) {
					return -1;
				} else if( first._coopFriendMember._best_rank_damage < second._coopFriendMember._best_rank_damage ) {
					return 1;
				}
			} else {
				if( first._isConnect ) {
					if( !second._isConnect ) {
						return -1;
					}
				} else {
					if( second._isConnect ) {
						return 1;
					}
				}
			}

			return 0;
		}
	}

	protected UIResources _object {
		get {
			if( __object == null ) {
				GameObject go = LoadUI( "UI/UIBattle_Challenge/Coop/Coop_Invite_Popup", transform.parent );
				__object = go.GetComponent<UIResources>();
			}
			return __object;
		}
	}

	private InviteTypeTab _currnetTab = InviteTypeTab.NONE;

	int _friendSelectedIndex = -1;

	UI_EndlessScroll _endlessScroll = null;

	List<FriendContentInfo> _friendContentList = new List<FriendContentInfo>();

	public override void OnPageAwake( object[] objs ) {
		UIResources parent = _object;
		_PopupCloseEnable = true;
		parent.gameObject.SetActiveX( true );

		parent.GetData<GameObject>( "invite_btn" ).SetActiveX( false );
		parent.GetData<GameObject>( "invite_disable_btn" ).SetActiveX( true );

		if( PlayerManager._UserTable._UserGuildTable._UserGuildData.isInGuild() == false ) {
			parent.GetData<GameObject>( "guild_tab2" ).SetActiveX( false );
		}

		SetGuildInfoRes( parent );

		OnAutoPanel( parent.gameObject );
		PlayForward( parent.gameObject, OnForwardComplete );
	}

	void OnForwardComplete() {
		
	}

	protected override void OnPageRequest() {
		_IsDataReceive = false;
		_object.GetData<UILabel>( "friend_empty_label" ).gameObject.SetActiveX( false );
		HttpManager.Request_CoopFriendList( OnReceiveCoopFriendList );
	}

	void OnReceiveCoopFriendList( bool isSuccess, object data ) {
		if( isSuccess ) {
			List<CoopFriendMemberData> friendList = PlayerManager._UserTable._UserArenaData._CoopFriendMemeberTable.GetFriendList();
			_friendContentList.Clear();
			if( friendList != null && friendList.Count > 0 ) {
				for( int i = 0; i < friendList.Count; i++ ) {
					FriendContentInfo inputContentInfo = new FriendContentInfo();
					inputContentInfo._coopFriendMember = friendList[i];
					_friendContentList.Add( inputContentInfo );
				}
			} else {
				_object.GetData<UILabel>( "friend_empty_label" ).gameObject.SetActiveX( true );
			}
			ChatManager.Request_FriendInfoList( GetUserFriendList(), OnReceiveChatFriendList );
		}
	}

	void OnReceiveChatFriendList( List<UserFriendData> friendList ) {
		for( int i = 0; i < friendList.Count; i++ ) {
			for(int j = 0;j< _friendContentList.Count;j++ ) {
				CoopFriendMemberData coopFriendData = _friendContentList[j]._coopFriendMember;
				if( coopFriendData._account_id == friendList[i]._account_id ) {
					coopFriendData._ChannelID = friendList[i]._ChannelID;
					_friendContentList[j]._isConnect = coopFriendData._ChannelNumber > 0 ? true : false;
				}
			}
		}

		_friendContentList.Sort( new FriendComparer() );
		OnPageLoadComplete( true, null );
	}

	public override void OnPageAttach() {
		base.OnPageAttach();

		if( _IsDataReceive ) {
			OnFlush();
		}
	}

	protected override void OnPageLoading() {
		base.OnPageLoading();
		PageInit();
	}

	void PageInit() {
		UIResources res = _object;

		GameObject obj = res.GetData<GameObject>( "Endless" );
		if( obj != null ) {
			_endlessScroll = obj.GetComponent<UI_EndlessScroll>();
		}

		InitText( res );
	}

	void InitText( UIResources ui ) {
		//팝업 UI 고정 문구들 텍스트 연결하기

		//ui.GetData<UILabel>( "friend_invite_on_label" ).SetTextX_Format( "{0}", "친구 초대" );
		ui.GetData<UILabel>( "friend_invite_on_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12191 ) );
		//ui.GetData<UILabel>( "friend_invite_off_label" ).SetTextX_Format( "{0}", "친구 초대" );
		ui.GetData<UILabel>( "friend_invite_off_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12191 ) );

		//ui.GetData<UILabel>( "guild_on_label" ).SetTextX_Format( "{0}", "연맹 초대" );
		ui.GetData<UILabel>( "guild_on_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12341 ) );
		//ui.GetData<UILabel>( "guild_off_label" ).SetTextX_Format( "{0}", "연맹 초대" );
		ui.GetData<UILabel>( "guild_off_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12341 ) );

		//ui.GetData<UILabel>( "invite_label" ).SetTextX_Format( "{0}", "초대" );
		ui.GetData<UILabel>( "invite_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12343 ) );
		//ui.GetData<UILabel>( "invite_disable_label" ).SetTextX_Format( "{0}", "초대" );
		ui.GetData<UILabel>( "invite_disable_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12343 ) );

		string emptyFriend = GameManager._StringTable.GetData( "Coop_Battle_InvitationSlot_Empty", "Coop_Battle_InvitationSlot_Empty" );
		ui.GetData<UILabel>( "friend_empty_label" ).SetTextX_Format( "{0}", emptyFriend );
	}

	public override void OnFlush() {
		if( _IsDataReceive == false ) {
			return;
		}
	}

	protected override void OnPageLoadCompleteUI( object data ) {
		_IsDataReceive = true;
		EndRepeat();
		BeginRepeat( 0.1f );

		SetTabCurrent( InviteTypeTab.FRIEND );
		OnFlush_FriendList( true );
	}

	public override void OnClick( GameObject go, Vector3 pos ) {
		Transform form = go.transform;
		string str = form.name;

		int index = 0;
		if( str.StartsWith( "bg1" ) == true ) {
			str = go.transform.parent.name;
			index = str.ToInt();

			if( str.StartsWith( "endless_scroll_friend_index_" ) ) {
				str = "endless_scroll_friend_index_";
			}
		}

		switch( str ) {
		case "coop_invite_popup_close_btn":
			OnClick_Detach();
			break;
		case "tab1_btn":
			SetTabCurrent( InviteTypeTab.FRIEND );
			break;
		case "tab2_btn":
			SetTabCurrent( InviteTypeTab.GUILD );
			break;
		case "endless_scroll_friend_index_":
			SelectFriend( index );
			break;
		case "invite_btn":
			OnClick_Invite();
			break;
		case "guild_invite_btn":
			OnClick_GuildInvite();
			break;
		}

		OnPopupClose( str );
	}

	void OnClick_Invite() {
		CoopFriendMemberData friendMember = _friendContentList[_friendSelectedIndex]._coopFriendMember;

		int needGrade = GameManager._ConstantTable.GetValueI( CONSTANT_TYPE.COOP_FRIENDS_INVITATION_LIMIT );
		if( needGrade > friendMember._best_grade ) {
			string popupText = StringManager.GetStringTable( 12362 );
			UICommon_Popup.Open( StringManager.GetStringTable( 10001 ), popupText, StringManager.GetStringTable( 10004 ), true, null );
			return;
		}

		CheckOnline();
	}

	void OnClick_GuildInvite() {
		UserTable ut = PlayerManager._UserTable;
		if( ArenaDeck_Helper.IsDoneCustomDeckSetting( ut._UserArenaData._UserChallengeCoop._userPresetData ) == false ) {
			//UICommon_Popup.Open( "안내", "현재 협동전에 사용되는 덱 정보가 없습니다.\n덱 구성 후 진행할 수 있습니다.", "확인", false, UI_UnitOrganizaion으로 이동 );
			UICommon_Popup.Open( StringManager.GetStringTable( 10002 ), StringManager.GetStringTable( 12519 ), StringManager.GetStringTable( 10004 ), false, ( POPUP_RESULT result_popup, object[] parameters ) => {
				Coop_Helper._coopID = PlayerManager._UserTable._UserArenaData._CoopSeason._current._data_id;
				UiManager.Attach( typeof( UIBattle_ArenaDeckSelect ), false, ARENA_MODE.COOP, COOP_BATTLE_PLAY_TYPE.GUILD_PROPOSAL );
				Attach( typeof( UI_UnitOrganization ), false, UI_UnitOrganization.UNIT_ORGANIZATION_TYPE.COOP );
			} );
		} else {
			Coop_Helper._coopID = PlayerManager._UserTable._UserArenaData._CoopSeason._current._data_id;
			UiManager.Attach( typeof( UIBattle_ArenaDeckSelect ), false, ARENA_MODE.COOP, COOP_BATTLE_PLAY_TYPE.GUILD_PROPOSAL );
		}
	}
	
	public override void OnClick_Detach() {
		PlayReverse( _object.gameObject, Detach );
	}

	public override void OnPageDestroy() {
		_friendContentList.Clear();

		if( __object != null ) {
			GameObject.Destroy( __object.gameObject );
			__object = null;
		}
	}

	void SetTabCurrent( InviteTypeTab tab ) {
		if( _currnetTab == tab ) {
			return;
		}
		_currnetTab = tab;

		UIResources res = _object;

		SetTabByIndex( res, tab );

		switch( _currnetTab ) {
		case InviteTypeTab.FRIEND:
			OnFlush_FriendInfo();
			break;
		case InviteTypeTab.GUILD:
			OnFlush_GuildInfo();
			break;
		}
	}

	void OnFlush_FriendInfo() {
		_object.GetData<GameObject>( "friend_list" ).SetActiveX( true );
		_object.GetData<GameObject>( "guild_info" ).SetActiveX( false );
	}

	void OnFlush_GuildInfo() {
		_object.GetData<GameObject>( "friend_list" ).SetActiveX( false );
		_object.GetData<GameObject>( "guild_info" ).SetActiveX( true );

		if( _isGuildRequest == false ) {
			HttpManager.Request_GuildInfoX( OnReceive_GuildInfo );
		}
	}

	void SetTabByIndex( UIResources res, InviteTypeTab tab ) {
		List<GameObject> list = res.GetList<GameObject>( "Tab1" );
		list[0].SetActive( tab == InviteTypeTab.FRIEND );
		list[1].SetActive( tab != InviteTypeTab.FRIEND );

		list = res.GetList<GameObject>( "Tab2" );
		list[0].SetActive( tab == InviteTypeTab.GUILD );
		list[1].SetActive( tab != InviteTypeTab.GUILD );
	}

	void OnFlush_BaseFriendData( UIResources parent, CoopFriendMemberData ufd ) {
		parent.GetData<UILabel>( "name_label" ).SetTextX_NameWithTitle( ufd._name, ufd._before_season_trophy, ufd._before_season_rank, "FFFFFF" );

		parent.GetData<UILabel>( "best_score_label" ).SetTextX_Format( "{0} : {1}", StringManager.GetStringTable( 12538 ), ufd._best_rank_damage );

		parent.GetData<UILabel>( "trophy_label" ).SetTextX_Format( "{0}", ufd._trophy );
		parent.GetData<GameObject>( "N_NationalFlag_base" ).GetComponent<UI_NationalFlag>().ChangeFlag( ufd._country );

		GameObject summoner_icon = parent.GetData<GameObject>( "summoner_icon" );
		UIIcon.SetSummonerRankIcon( summoner_icon.GetComponent<UIResources>(), ufd._icon, ufd._icon_type, ufd._grade, ufd._trophy, 0, false, before_season_rank: ufd._before_season_rank, before_season_trophy: ufd._before_season_trophy );
	}
}
