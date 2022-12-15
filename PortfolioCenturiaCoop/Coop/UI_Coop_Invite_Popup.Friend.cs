using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UI_Coop_Invite_Popup : UIBase {

	void OnFlush_FriendList( bool isReset ) {
		UIResources parent = _object;
		if( _friendContentList.Count > 0 && _endlessScroll != null ) {
			_endlessScroll.OnFlush( this, OnFlush_FriendData, _friendContentList.Count, isReset );
			_endlessScroll.gameObject.SetActiveX( true );
		}
	}

	void OnFlush_FriendData( UIResources parent, int index ) {
		CoopFriendMemberData memberData = _friendContentList[index]._coopFriendMember;
		_friendContentList[index]._contentRes = parent;
		_friendContentList[index].SelectContent( false );

		if( memberData != null ) {
			OnFlush_BaseFriendData( parent, memberData );

			jUtil.ShowRanking( parent.GetData<UISprite>( "HighRank_icon" ), parent.GetData<UILabel>( "LowRank_label" ), index );
			UIIcon.SetArenaRankIcon( parent.GetData<GameObject>( "ArenaRankIcon" ), memberData._trophy, 0 );

			SetLineInfo( _friendContentList[index] );
		}
	}

	void SetLineInfo( FriendContentInfo friendContent ) {
		friendContent._contentRes.GetData<UILabel>( "Id_label" ).gameObject.SetActiveX( true );
		friendContent._contentRes.GetData<UILabel>( "Id_label" ).SetTextX_Format( StringManager.GetStringTable( 11703 ), friendContent._coopFriendMember._user_uuid );

		if( friendContent._coopFriendMember._ChannelNumber <= 0 ) {
			SetLineFriend( friendContent._contentRes, false );
			friendContent._contentRes.GetData<UILabel>( "offline_label" ).SetTextX_Format( StringManager.GetStringTable( 11689 ),
				GameManager.GetHistoryTimeSpan( PlayerManager._DateTime - new System.DateTime( friendContent._coopFriendMember._last_login ) ) );
		} else {
			SetLineFriend( friendContent._contentRes, true );
			friendContent._contentRes.GetData<UILabel>( "online_label" ).SetTextX_Format( StringManager.GetStringTable( 11698 ),
				friendContent._coopFriendMember._ChannelNumber, GameManager._LanguageTable.GetData( friendContent._coopFriendMember._ChannelLanguage ) );
		}
	}

	void SetLineFriend( UIResources parent, bool isConnect ) {
		parent.GetData<GameObject>( "online_label" ).SetActiveX( isConnect );
		parent.GetData<GameObject>( "offline_label" ).SetActiveX( !isConnect );
		parent.GetData<GameObject>( "inactive_bg" ).SetActiveX( !isConnect );
	}

	void SelectFriend( int currentIndex ) {
		if( !_friendContentList[currentIndex]._isConnect )
			return;

		if( _friendSelectedIndex == currentIndex )
			return;

		if( _friendSelectedIndex != -1 ) {
			_friendContentList[_friendSelectedIndex].SelectContent( false );
		} else {
			_object.GetData<GameObject>( "invite_btn" ).SetActiveX( true );
			_object.GetData<GameObject>( "invite_disable_btn" ).SetActiveX( false );
		}

		_friendSelectedIndex = currentIndex;
		_friendContentList[_friendSelectedIndex].SelectContent( true );
	}

	void ResetSelectFriend() {
		if( _friendSelectedIndex != -1 ) {
			UIResources before = _endlessScroll.GetUIResourceByIndex( _friendSelectedIndex );
			if( before != null ) {
				before.GetData<GameObject>( "tooltip" ).SetActiveX( false );
			}
		}
		_friendSelectedIndex = -1;
	}

	void OpenFriendProfile() {
		HttpManager.Request_PlayerProfile( _friendContentList[_friendSelectedIndex]._coopFriendMember._account_id, OnReceive_FriendPlayerProfile );
	}

	void OnReceive_FriendPlayerProfile( bool isSuccess, object obj ) {
		if( isSuccess == true ) {
			UiManager.Attach( typeof( UI_PlayerProfile ), false, obj as UserPlayerProfileData );
		}
	}

	void CheckOnline() {
		List<UserFriendData> pidlist = new List<UserFriendData>();
		pidlist.Add( GetUserFriendData( _friendContentList[_friendSelectedIndex]._coopFriendMember ) );

		ChatManager.Request_FriendInfoList( pidlist, list => {
			UserFriendData ufd = list[0];
			if( ufd._ChannelNumber > 0 ) {
				UserTable ut = PlayerManager._UserTable;
				if( ArenaDeck_Helper.IsDoneCustomDeckSetting( ut._UserArenaData._UserChallengeCoop._userPresetData ) == false ) {
					//UICommon_Popup.Open( "안내", "현재 협동전에 사용되는 덱 정보가 없습니다.\n덱 구성 후 진행할 수 있습니다.", "확인", false, UI_UnitOrganizaion으로 이동 );
					UICommon_Popup.Open( StringManager.GetStringTable( 10002 ), StringManager.GetStringTable( 12519 ), StringManager.GetStringTable( 10004 ), false, ( POPUP_RESULT result_popup, object[] parameters ) => {
						Coop_Helper._coopID = PlayerManager._UserTable._UserArenaData._CoopSeason._current._data_id;
						UiManager.Attach( typeof( UIBattle_ArenaDeckSelect ), false, ARENA_MODE.COOP, COOP_BATTLE_PLAY_TYPE.INVITE_FRIEND, ufd._account_id, ufd._name );
						Attach( typeof( UI_UnitOrganization ), false, UI_UnitOrganization.UNIT_ORGANIZATION_TYPE.COOP );
					} );
				} else {
					Coop_Helper._coopID = PlayerManager._UserTable._UserArenaData._CoopSeason._current._data_id;
					UiManager.Attach( typeof( UIBattle_ArenaDeckSelect ), false, ARENA_MODE.COOP, COOP_BATTLE_PLAY_TYPE.INVITE_FRIEND, ufd._account_id, ufd._name );
				}
			} else {
				//온라인이라 표기되어 선택했지만 현재 오프라인인 경우
				//"해당 영주님은 게임 접속을 종료하셨습니다."
				Camera2D.FadeSystemTextX( StringManager.GetStringTable( 11744 ), SYSTEM_MESSAGE_TYPE.SYSTEM_IN );
				SetDeconnectFriend( ufd, _friendSelectedIndex );
				_friendSelectedIndex = -1;
			}
		} );
	}

	void SetDeconnectFriend( UserFriendData ufd, int friendIndex ) {
		_object.GetData<GameObject>( "invite_btn" ).SetActiveX( false );
		_object.GetData<GameObject>( "invite_disable_btn" ).SetActiveX( true );

		_friendContentList[friendIndex]._coopFriendMember._last_login = ufd._last_login;
		_friendContentList[friendIndex]._coopFriendMember._ChannelID = ufd._ChannelID;
		_friendContentList[friendIndex]._isConnect = false;

		_friendContentList[friendIndex].SelectContent( false );
		SetLineFriend( _friendContentList[friendIndex]._contentRes, false );
		_friendContentList[friendIndex]._contentRes.GetData<UILabel>( "offline_label" ).SetTextX_Format( StringManager.GetStringTable( 11689 ),
			GameManager.GetHistoryTimeSpan( PlayerManager._DateTime - new System.DateTime( _friendContentList[friendIndex]._coopFriendMember._last_login ) ) );
	}

	List<UserFriendData> GetUserFriendList() {
		List<UserFriendData> retValue = new List<UserFriendData>();

		for( int i = 0; i < _friendContentList.Count; i++ ) {
			retValue.Add( GetUserFriendData( _friendContentList[i]._coopFriendMember ) );
		}

		return retValue;
	}

	UserFriendData GetUserFriendData( CoopFriendMemberData coopFriend) {
		UserFriendData userFriend = new UserFriendData();
		userFriend._account_id = coopFriend._account_id;
		userFriend._last_login = coopFriend._last_login;
		userFriend._player_id = coopFriend._player_id;
		userFriend._name = coopFriend._name;
		userFriend._country = coopFriend._country;
		userFriend._level = coopFriend._level;
		userFriend._icon = coopFriend._icon;
		userFriend._trophy = coopFriend._trophy;
		userFriend._grade = coopFriend._grade;
		userFriend._before_season_rank = coopFriend._before_season_rank;
		userFriend._before_season_trophy = coopFriend._before_season_trophy;
		userFriend._user_uuid = coopFriend._user_uuid;

		return userFriend;
	}
}
