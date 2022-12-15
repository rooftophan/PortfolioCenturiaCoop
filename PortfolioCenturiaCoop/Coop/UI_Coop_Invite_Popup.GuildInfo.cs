using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class UI_Coop_Invite_Popup : UIBase {
	public class CoopGuildMemInfo {
		public UserGuildMemberData _guildMember;
		public UIResources _contentRes;
	}

	UIResources _guildInfoRes;

	List<CoopGuildMemInfo> _guildMemContentList = new List<CoopGuildMemInfo>();

	UI_EndlessScroll _guildEndlessScroll = null;

	bool _isGuildRequest = false;

	void SetGuildInfoRes( UIResources parent ) {
		_isGuildRequest = false;

		_guildInfoRes = parent.GetData<GameObject>( "guild_info" ).GetComponent<UIResources>();

		GameObject guildEndlessObj = _guildInfoRes.GetData<GameObject>( "guild_endless" );
		if( guildEndlessObj != null ) {
			_guildEndlessScroll = guildEndlessObj.GetComponent<UI_EndlessScroll>();
		}

		if( PlayerManager._UserTable._UserGuildTable._UserGuildData.isInGuild() ) {
			UserGuildBaseData ugbd = PlayerManager._UserTable._UserGuildTable._UserGuildData;

			//_guildInfoRes.GetData<UILabel>( "guild_info_label" ).SetTextX_Format( "{0}", "연맹 채팅으로 협동전 초대 메시지를 보냅니다." );
			_guildInfoRes.GetData<UILabel>( "guild_info_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12342 ) );
			//_guildInfoRes.GetData<UILabel>( "guild_invite_label" ).SetTextX_Format( "{0}", "연맹 초대" );
			_guildInfoRes.GetData<UILabel>( "guild_invite_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12341 ) );
			//_guildInfoRes.GetData<UILabel>( "guild_disable_invite_label" ).SetTextX_Format( "{0}", "연맹 초대" );
			_guildInfoRes.GetData<UILabel>( "guild_disable_invite_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12341 ) );

			//_guildInfoRes.GetData<UILabel>( "guild_connect_label" ).SetTextX_Format( "{0}", "접속 중인 연맹원" );
			_guildInfoRes.GetData<UILabel>( "guild_connect_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12508 ) );

			//_guildInfoRes.GetData<UILabel>( "guild_empty_label" ).SetTextX_Format( "{0}", "현재 접속중인 연맹원이 없습니다." );
			_guildInfoRes.GetData<UILabel>( "guild_empty_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12539 ) );

			_guildInfoRes.GetData<UILabel>( "guild_name_label" ).SetTextX_Format( "{0}", ugbd._guild_name );
			_guildInfoRes.GetData<UILabel>( "trophy_label" ).SetTextX_Format( "{0}", ugbd._guild_trophy );

			_guildInfoRes.GetData<UILabel>( "guild_empty_label" ).gameObject.SetActiveX( false );

			UIIcon.SetGuildMarkIcon( _guildInfoRes.GetData<GameObject>( "GuildMarkIcon" ), ugbd._guild_id, ugbd._guild_mark, ugbd._guild_color, ugbd._guild_level );
		}
	}

	void OnReceive_GuildInfo( bool isSuccess, object obj ) {
		if( isSuccess == true ) {
			_guildMemContentList.Clear();
			List<UserGuildMemberData> guildMemList = PlayerManager._UserTable._UserGuildTable._UserGuildMemberTable._DataList;
			if( guildMemList != null && guildMemList.Count > 0 ) {
				for( int i = 0; i < guildMemList.Count; i++ ) {
					if( PlayerManager._UserTable._UserInfoTable._account_id == guildMemList[i]._account_id )
						continue;

					CoopGuildMemInfo inputGuildMemInfo = new CoopGuildMemInfo();
					inputGuildMemInfo._guildMember = guildMemList[i];
					_guildMemContentList.Add( inputGuildMemInfo );
				}
			}
			ChatManager.Request_FriendInfoList( GetUserFriendListByGuild(), OnReceiveChatGuildList );
		}
	}

	void OnReceiveChatGuildList( List<UserFriendData> connectList ) {
		for( int i = 0; i < connectList.Count; i++ ) {
			for( int j = 0; j < _guildMemContentList.Count; j++ ) {
				UserGuildMemberData guildMember = _guildMemContentList[j]._guildMember;
				if( guildMember._account_id == connectList[i]._account_id ) {
					guildMember._ChannelID = connectList[i]._ChannelID;
					bool isConnect = guildMember._ChannelNumber > 0 ? true : false;
					if( isConnect == false ) {
						_guildMemContentList.RemoveAt( j );
						j--;
					}
				}
			}
		}

		_isGuildRequest = true;

		if( _guildMemContentList.Count > 0 ) {
			OnFlush_GuildList( true );
		} else {
			_guildInfoRes.GetData<UILabel>( "guild_empty_label" ).gameObject.SetActiveX( true );
			_guildInfoRes.GetData<GameObject>( "guild_invite_btn" ).SetActiveX( false );
			_guildInfoRes.GetData<GameObject>( "guild_disable_invite_btn" ).SetActiveX( true );
		}
	}

	void OnFlush_GuildList( bool isReset ) {
		if( _guildMemContentList.Count > 0 && _guildEndlessScroll != null ) {
			_guildEndlessScroll.OnFlush( this, OnFlush_GuildData, _guildMemContentList.Count, isReset );
			_guildEndlessScroll.gameObject.SetActiveX( true );
		}
	}

	void OnFlush_GuildData( UIResources parent, int index ) {
		UserGuildMemberData guildMember = _guildMemContentList[index]._guildMember;
		parent.gameObject.SetActiveX( true );
		_guildMemContentList[index]._contentRes = parent;

		if( guildMember != null ) {
			OnFlush_BaseGuildData( parent, guildMember );
			UIIcon.SetArenaRankIcon( parent.GetData<GameObject>( "ArenaRankIcon" ), guildMember._trophy, 0 );
		}
	}

	void OnFlush_BaseGuildData( UIResources parent, UserGuildMemberData guildMember ) {
		parent.GetData<UILabel>( "name_label" ).SetTextX_NameWithTitle( guildMember._name, guildMember._before_season_trophy, guildMember._before_season_rank, "FFFFFF" );
		parent.GetData<UILabel>( "trophy_label" ).SetTextX_Format( "{0}", guildMember._trophy );
		parent.GetData<GameObject>( "N_NationalFlag_base" ).GetComponent<UI_NationalFlag>().ChangeFlag( guildMember._country );

		GameObject summoner_icon = parent.GetData<GameObject>( "summoner_icon" );
		UIIcon.SetSummonerRankIcon( summoner_icon.GetComponent<UIResources>(), guildMember._icon, guildMember._icon_type, guildMember._grade, guildMember._trophy, 0, false, 
			before_season_rank: guildMember._before_season_rank, before_season_trophy: guildMember._before_season_trophy );
	}

	List<UserFriendData> GetUserFriendListByGuild() {
		List<UserFriendData> retValue = new List<UserFriendData>();

		for( int i = 0; i < _guildMemContentList.Count; i++ ) {
			retValue.Add( GetUserFriendDataByGuild( _guildMemContentList[i]._guildMember ) );
		}

		return retValue;
	}

	UserFriendData GetUserFriendDataByGuild( UserGuildMemberData guildMem ) {
		UserFriendData userFriend = new UserFriendData();
		userFriend._account_id = guildMem._account_id;
		userFriend._player_id = guildMem._player_id;
		userFriend._name = guildMem._name;
		userFriend._country = guildMem._country;
		userFriend._level = guildMem._level;
		userFriend._icon = guildMem._icon;
		userFriend._trophy = guildMem._trophy;
		userFriend._grade = guildMem._grade;
		userFriend._before_season_rank = guildMem._before_season_rank;
		userFriend._before_season_trophy = guildMem._before_season_trophy;

		return userFriend;
	}
}
