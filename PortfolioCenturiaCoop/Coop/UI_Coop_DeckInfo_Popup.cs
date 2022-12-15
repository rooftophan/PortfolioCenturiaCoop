using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Coop_DeckInfo_Popup : UIBase {
	public class CoopDeckUserInfo {
		public CoopUserInfo _coopUser;
		public UserCoopDeckInfo _coopDeckInfo;
	}

	protected UIResources _object {
		get {
			if( __object == null ) {
				GameObject go = LoadUI( "UI/UIBattle_Challenge/Coop/Coop_DeckInfo_Popup", transform.parent );
				__object = go.GetComponent<UIResources>();
			}
			return __object;
		}
	}

	UserChallengeCoop _coopUserData;
	RankCoopInfo _rankData;

	List<long>[] _is_monster_not_copyable;
	List<long>[] _is_spell_not_copyable;

	bool _isInitState = false;

	List<CoopDeckUserInfo> _coopDeckUserList = new List<CoopDeckUserInfo>();

	public override void OnPageAwake( object[] objs ) {
		UIResources parent = _object;

		_coopUserData = objs[0] as UserChallengeCoop;
		_rankData = objs[1] as RankCoopInfo;

		_PopupCloseEnable = true;
		parent.gameObject.SetActiveX( true );

		InitData();

		OnFlushAwake();

		OnAutoPanel( parent.gameObject );
		PlayForward( parent.gameObject, OnForwardComplete );
	}

	void InitData() {
		_is_monster_not_copyable = new List<long>[2];
		for( int i = 0;i< _is_monster_not_copyable.Length;i++ ) {
			_is_monster_not_copyable[i] = new List<long>();
		}

		_is_spell_not_copyable = new List<long>[2];
		for( int i = 0; i < _is_spell_not_copyable.Length; i++ ) {
			_is_spell_not_copyable[i] = new List<long>();
		}

		_coopDeckUserList.Clear();
		for( int i = 0; i < 2; i++ ) {
			CoopDeckUserInfo inputDeckUserInfo = new CoopDeckUserInfo();
			CoopUserInfo coopUser = _rankData._user_infos[i];
			UserCoopDeckInfo coopDeckInfo = null;
			for( int j = 0; j < 2; j++ ) {
				UserCoopDeckInfo deckInfo = _coopUserData._userCoopDeckInfos[j];
				if( coopUser._account_id == deckInfo._account_id ) {
					coopDeckInfo = deckInfo;
					break;
				}
			}

			inputDeckUserInfo._coopUser = coopUser;
			inputDeckUserInfo._coopDeckInfo = coopDeckInfo;

			_coopDeckUserList.Add( inputDeckUserInfo );
		}
	}

	void OnForwardComplete() {

	}

	public override void OnPageDestroy() {
		if( __object != null ) {
			GameObject.Destroy( __object.gameObject );
			__object = null;
		}
	}

	public override void OnClick_Detach() {
		PlayReverse( _object.gameObject, Detach );
	}

	public override void OnClick( GameObject go, Vector3 pos, object[] objs ) {
		Transform form = go.transform;
		string str = form.name;

		int index = 0;

		if( str.StartsWith( "copy_btn_" ) == true ) {
			index = str.ToInt();
			str = "copy_btn_";
		}

		if( str.StartsWith( "copy_disable_btn_" ) == true ) {
			index = str.ToInt();
			str = "copy_disable_btn_";
		}

		if( str.StartsWith( "Army_MonsterItem_" ) == true ) {
			index = str.ToInt();
			str = "Army_MonsterItem_";
		}

		if( str.StartsWith( "Army_SpellItem_" ) == true ) {
			index = str.ToInt();
			str = "Army_SpellItem_";
		}

		switch( str ) {
		case "deckinfo_close_btn":
			OnClick_Detach();
			break;
		case "copy_btn_":
			OnClick_Copy( index );
			break;
		case "copy_disable_btn_":
			OnClick_DisableCopy( index );
			break;
		case "Army_MonsterItem_":
			if( objs.Length > 0 ) {
				UserMonsterData umd = objs[0] as UserMonsterData;
				if( umd != null ) {
					UI_InfoPopup.OpeSelectEnemyMonsterPopup( umd );
				}
			}
			break;
		case "Army_SpellItem_":
			if( objs.Length > 0 ) {
				UserSkillData usd = objs[0] as UserSkillData;
				if( usd != null ) {
					UI_InfoPopup.OpeSelectEnemySpellPopup( usd );
				}
			}
			break;
		}

		OnPopupClose( str );
	}

	void OnClick_Copy( int index ) {
		UiManager.Attach( typeof( UI_Barrier_DeckCopy_Popup ), false, ARENA_MODE.COOP, _coopDeckUserList[index]._coopUser._account_id, _rankData._team_id );
	}

	void OnClick_DisableCopy( int index ) {
		Camera2D.FadeSystemTextX( StringManager.GetStringTable( 11674 ), SYSTEM_MESSAGE_TYPE.SYSTEM_IN );
		OnShow_ShowDeckInactive( index );
	}

	void OnShow_ShowDeckInactive( int index ) {
		UIResources res = _object.GetList<GameObject>( "user_deck_list" )[index].GetComponent<UIResources>();

		UserCoopDeckInfo coopDeckInfo = _coopDeckUserList[index]._coopDeckInfo;

		int[] monids = coopDeckInfo._userPresetCopyData._monster_ids;
		int[] spellids = coopDeckInfo._userPresetCopyData._skill_ids;

		if( monids == null ) {
			monids = new int[0];
		}

		if( spellids == null ) {
			spellids = new int[0];
		}

		List<GameObject> monsters = res.GetList<GameObject>( "MonsterList" );
		for( int i = 0; i < monsters.Count; ++i ) {
			GameObject monsterIconObj = monsters[i];
			int monid = 0;
			if( i < monids.Length ) {
				monid = monids[i];
			}
			UISprite inactivespr = monsterIconObj.GetComponent<UIResources>().GetData<UISprite>( "inactive" );
			if( monid > 0 ) {
				if( _is_monster_not_copyable[index].Contains( monid ) ) {
					inactivespr.gameObject.SetActiveX( true );
					inactivespr.GetComponent<NVTweenAlpha>().PlayForward();
				} else {
					inactivespr.gameObject.SetActiveX( false );
				}
			} else {
				inactivespr.gameObject.SetActiveX( false );
			}
		}

		List<GameObject> spells = res.GetList<GameObject>( "SkillList" );
		for( int i = 0; i < spells.Count; ++i ) {
			GameObject spellIconObj = spells[i];
			int spellid = 0;
			if( i < spellids.Length ) {
				spellid = spellids[i];
			}
			UISprite inactivespr = spellIconObj.GetComponent<UIResources>().GetData<UISprite>( "inactive" );
			if( spellid > 0 ) {
				if( _is_spell_not_copyable[index].Contains( spellid ) ) {
					inactivespr.gameObject.SetActiveX( true );
					inactivespr.GetComponent<NVTweenAlpha>().PlayForward();
				} else {
					inactivespr.gameObject.SetActiveX( false );
				}
			} else {
				inactivespr.gameObject.SetActiveX( false );
			}
		}
	}

	public override void OnPageAttach() {
		OnFlush();
	}

	void OnFlushAwake() {
		UIResources parent = _object;

		UserTable ut = PlayerManager._UserTable;
		UserSkillData buffSkillData = ut._UserArenaData._CoopSeason.GetCurrentSkillData();

		parent.GetData<UILabel>( "title_label" ).SetTextX_Format( "{0}-{1}", StringManager.GetStringTable( 12333 ), buffSkillData._Name );

		List<UIWidget> copyLabels = parent.GetList<UIWidget>( "copy_label_list" );
		for(int i = 0;i< copyLabels.Count;i++ ) {
			(copyLabels[i] as UILabel).SetTextX_Format( "{0}", StringManager.GetStringTable( 11849 ) );
		}

		OnFlush_All( parent );
	}

	public override void OnFlush() {
		UIResources parent = _object;
	}

	bool IsDeckCopyable( UserMonsterTable monster_table, UserSkillTable spell_table, UserPresetData userPresetCopyData, int index ) {
		_is_monster_not_copyable[index].Clear();
		_is_spell_not_copyable[index].Clear();

		int[] monids = userPresetCopyData._monster_ids;
		int[] spelIids = userPresetCopyData._skill_ids;

		bool retValue = true;
		if( monids != null && monids.Length > 0 ) {
			for( int i = 0; i < monids.Length; i++ ) {
				int monid = monids[i];
				if( monid > 0 ) {
					if( monster_table.Find( monid ) == null ) {
						retValue = false;
						_is_monster_not_copyable[index].Add( monid );
					}
				}
			}
		}

		if( spelIids != null && spelIids.Length > 0 ) {
			for( int i = 0; i < spelIids.Length; i++ ) {
				int spellid = spelIids[i];
				if( spellid > 0 ) {
					if( spell_table.Find( spellid ) == null ) {
						retValue = false;
						_is_spell_not_copyable[index].Add( spellid );
					}
				}
			}
		}

		return retValue;
	}

	void OnFlush_All( UIResources parent ) {
		for( int i = 0; i < 2; i++ ) {
			int index = i;
			OnFlush_UserInfo( parent, index );
			OnFlush_DeckRecord( parent, index );
		}
		

		if( _isInitState == false ) {
			_isInitState = true;
		}
	}

	void OnFlush_UserInfo( UIResources parent, int index ) {
		UIResources userInfo = parent.GetList<GameObject>( "user_info_list" )[index].GetComponent<UIResources>();

		CoopUserInfo coopUser = _coopDeckUserList[index]._coopUser;

		userInfo.GetData<UILabel>( "name_label" ).SetTextX_NameWithTitle( coopUser._name, coopUser._before_season_trophy, coopUser._before_season_rank, "FFFFFF" );
		userInfo.GetData<UILabel>( "guild_name_label" ).SetTextX_Format( "{0}", coopUser._guild_name );

		UIIcon.SetSummonerRankIcon( userInfo.GetData<GameObject>( "summoner_icon" ).GetComponent<UIResources>(), coopUser._icon, coopUser._icon_type, coopUser._grade, coopUser._trophy,
			_rankData._rank, false, before_season_rank: coopUser._before_season_rank, before_season_trophy: coopUser._before_season_trophy );

		userInfo.GetData<GameObject>( "N_NationalFlag_base" ).GetComponent<UI_NationalFlag>().ChangeFlag( coopUser._country );
	}

	void OnFlush_DeckRecord( UIResources parent, int index ) {
		UIResources res = parent.GetList<GameObject>( "user_deck_list" )[index].GetComponent<UIResources>();

		UserCoopDeckInfo coopDeckInfo = _coopDeckUserList[index]._coopDeckInfo;

		UserPresetData presetData = coopDeckInfo._userPresetCopyData;

		OnFlush_DeckMonsterIcon( res.GetList<GameObject>( "MonsterList" ), presetData._monster_ids, coopDeckInfo._userMonsSkinId._userMonSkinIds );
		OnFlush_SpellIcon( res.GetList<GameObject>( "SkillList" ), presetData._skill_ids );

		bool isDeckCopable = IsDeckCopyable( PlayerManager._UserTable._UserMonsterTable, PlayerManager._UserTable._UserSkillTable, presetData, index );

		List<GameObject> copyBtnList = parent.GetList<GameObject>( "copy_btn_list" );
		List<GameObject> copyDisableBtnList = parent.GetList<GameObject>( "copy_disable_btn_list" );

		GameObject copyBtnObj = copyBtnList[index];
		GameObject copyDisableBtnObj = copyDisableBtnList[index];

		copyBtnObj.SetActiveX( isDeckCopable );
		copyDisableBtnObj.SetActiveX( !isDeckCopable );
	}

	void OnFlush_DeckMonsterIcon( List<GameObject> monsterIconList, int[] monids, List<int> skinList ) {
		GameObject monsterIconObj;
		NVButton btn;
		UIResources res;

		if( monids == null ) {
			monids = new int[0];
		}

		for( int i = 0, icount = monsterIconList.Count; i < icount; i++ ) {
			monsterIconObj = monsterIconList[i];
			btn = monsterIconObj.GetComponent<NVButton>();
			int monid = 0;
			if( i < monids.Length ) {
				monid = monids[i];
			}

			monsterIconObj.GetComponent<UIResources>().GetData<UISprite>( "inactive" ).gameObject.SetActiveX( false );
			res = monsterIconObj.GetComponent<UIResources>().GetData<GameObject>( "MonsterIcon_Slot" ).GetComponent<UIResources>();
			if( monid > 0 ) {
				UserMonsterData umd = new UserMonsterData( monid, 0 );
				btn.SetData( umd );
				SetMonsterSkin( umd, skinList );
				UIIcon.MONSTER_ICON_TYPE mit = UIIcon.MONSTER_ICON_TYPE.ATTR_LEFT | UIIcon.MONSTER_ICON_TYPE.SKIN;
				UIIcon.SetMonsterIcon_Slot( res, umd, 0, mit );
			} else {
				btn.SetData( null );
				UIIcon.SetMonsterIcon_Slot( res, null, 0 );
			}
		}
	}

	void SetMonsterSkin( UserMonsterData umd, List<int> skinList ) {
		List<MonsterSkinData> msdList = GameManager._MonsterSkinTable.GetList( (int)umd._ID );

		if( msdList != null ) {
			for( int i = 0, icount = skinList.Count; i < icount; i++ ) {
				for( int j = 0, jcount = msdList.Count; j < jcount; j++ ) {
					if( skinList[i] == msdList[j]._ID ) {
						umd._SkinID = skinList[i];
						return;
					}
				}
			}
		}
	}

	void OnFlush_SpellIcon( List<GameObject> spellIconList, int[] spellids ) {
		GameObject spellIconObj;
		NVButton btn;
		UIResources iconRes;

		if( spellids == null ) {
			spellids = new int[0];
		}

		for( int i = 0, icount = spellIconList.Count; i < icount; i++ ) {
			spellIconObj = spellIconList[i];
			btn = spellIconObj.GetComponent<NVButton>();
			int spellid = 0;
			if( i < spellids.Length ) {
				spellid = spellids[i];
			}

			spellIconObj.GetComponent<UIResources>().GetData<UISprite>( "inactive" ).gameObject.SetActiveX( false );
			iconRes = spellIconObj.GetComponent<UIResources>().GetData<GameObject>( "SpellIcon_Common" ).GetComponent<UIResources>();
			if( spellid > 0 ) {
				UserSkillData usd = new UserSkillData();
				usd.SetData( spellid );
				UIIcon.SPELL_ICON_TYPE sit = UIIcon.SPELL_ICON_TYPE.ICON;
				UIIcon.SetSpellIcon( iconRes, usd, usd._BaseSkillData, sit );
				btn.SetData( usd );
			} else {
				UIIcon.SetSpellIcon( iconRes, null );
				btn.SetData( null );
			}
		}
	}
}
