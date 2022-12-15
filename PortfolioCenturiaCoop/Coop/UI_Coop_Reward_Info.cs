using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Coop_Reward_Info : UIBase {
	protected UIResources _object {
		get {
			if( __object == null ) {
				GameObject go = LoadUI( "UI/UIBattle_Challenge/Coop/Coop_Reward_Info", transform.parent );
				__object = go.GetComponent<UIResources>();
			}
			return __object;
		}
	}

	UI_EndlessScroll _rewardScroll = null;

	List<CoopBattleRewardData> _battleRewardList = null;

	int _curMonsterID;

	public override void OnPageAwake( object[] objs ) {
		UIResources parent = _object;
		_curMonsterID = (int)objs[0];
		UIButton closeBtn = parent.GetData<GameObject>( "Coop_reward_info_close_btn" ).GetComponent<UIButton>();
		closeBtn.onClick.Add( new EventDelegate( OnClick_Detach ) );

		_PopupCloseEnable = true;
		parent.gameObject.SetActiveX( true );

		OnFlushAwake();

		OnAutoPanel( parent.gameObject );
		PlayForward( parent.gameObject, OnForwardComplete );
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

	public override void OnClick( GameObject go, Vector3 pos ) {
		Transform form = go.transform;
		string str = form.name;

		OnPopupClose( str );
	}

	public override void OnPageAttach() {
		OnFlush();
	}

	void OnFlushAwake() {
		UIResources parent = _object;

		//parent.GetData<UILabel>( "title_label" ).SetTextX_Format( "{0}", "보상 정보" );
		parent.GetData<UILabel>( "title_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12339 ) );

		//parent.GetData<UILabel>( "info_label" ).SetTextX_Format( "{0}", "보스에게 입힌 데미지에 따라 보상을 획득할 수 있습니다." );
		parent.GetData<UILabel>( "info_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12509 ) );

		//parent.GetData<UILabel>( "info1_label" ).SetTextX_Format( "{0}", "피해량" );
		parent.GetData<UILabel>( "info1_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12510 ) );

		//parent.GetData<UILabel>( "info2_label" ).SetTextX_Format( "{0}", "보상" );
		parent.GetData<UILabel>( "info2_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 11373 ) );

		_battleRewardList = GameManager._CoopBattleRewardTable.GetBattleRewardList( _curMonsterID );
	}

	public override void OnFlush() {
		UIResources parent = _object;

		OnFlushList( parent );		
	}

	void OnFlushList( UIResources res, bool resetPosition = false ) {
		if( _rewardScroll == null ) {
			_rewardScroll = res.GetData<GameObject>( "Endless" ).GetComponent<UI_EndlessScroll>();
		}

		_rewardScroll.gameObject.SetActiveX( true );
		if( _battleRewardList != null ) {
			_rewardScroll.OnFlush( this, OnFlushItemRewardEndless, _battleRewardList.Count, resetPosition );
		}
	}

	void OnFlushItemRewardEndless( UIResources res, int i ) {
		CoopBattleRewardData coopRewardData = _battleRewardList[i];
		if( coopRewardData != null ) {
			res.GetData<UILabel>( "damage_label" ).SetTextX_Format( "{0}~{1}", coopRewardData._bossDamageRangeMin, coopRewardData._bossDamageRangeMax );

			UserRewardResourceData rewardData1 = coopRewardData._UserRewardResourceData_1;
			UserRewardResourceData rewardData2 = coopRewardData._UserRewardResourceData_2;

			res.GetData<UISprite>( "reward1_icon" ).spriteName = rewardData1._Icon;
			res.GetData<UILabel>( "num1_label" ).SetTextX_Format( "{0}", rewardData1._ItemCount );

			res.GetData<UISprite>( "reward2_icon" ).spriteName = rewardData2._Icon;
			res.GetData<UILabel>( "num2_label" ).SetTextX_Format( "{0}", rewardData2._ItemCount );
		}
	}
}
