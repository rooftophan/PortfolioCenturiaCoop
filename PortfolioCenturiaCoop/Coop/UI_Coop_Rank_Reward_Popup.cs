using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Coop_Rank_Reward_Popup : UIBase {
	protected UIResources _object {
		get {
			if( __object == null ) {
				GameObject go = LoadUI( "UI/UIBattle_Challenge/Coop/Coop_Rank_Reward_Popup", transform.parent );
				__object = go.GetComponent<UIResources>();
			}
			return __object;
		}
	}

	UI_EndlessScroll _rewardScroll = null;

	public override void OnPageAwake( object[] objs ) {
		UIResources parent = _object;

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

		switch( str ) {
		case "Coop_rank_reward_close_btn":
			OnClick_Detach();
			break;
		}

		OnPopupClose( str );
	}

	public override void OnPageAttach() {
		OnFlush();
	}

	void OnFlushAwake() {
		UIResources parent = _object;

		//parent.GetData<UILabel>( "title_label" ).SetTextX_Format( "{0}", "랭킹 보상 정보" );
		parent.GetData<UILabel>( "title_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12511 ) );

		//parent.GetData<UILabel>( "info_label" ).SetTextX_Format( "{0}", "시즌이 종료되고 랭킹에 따라 보상이 우편으로 지급됩니다." );
		parent.GetData<UILabel>( "info_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12512 ) );

		//parent.GetData<UILabel>( "info1_label" ).SetTextX_Format( "{0}", "피해량" );
		parent.GetData<UILabel>( "info1_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12510 ) );

		//parent.GetData<UILabel>( "info2_label" ).SetTextX_Format( "{0}", "보상" );
		parent.GetData<UILabel>( "info2_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 11373 ) );
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
		_rewardScroll.OnFlush( this, OnFlushItemRewardEndless, GameManager._CoopRankRewardTable._DataList.Count, resetPosition );
	}

	void OnFlushItemRewardEndless( UIResources res, int i ) {
		CoopRankRewardData rankRewardData = GameManager._CoopRankRewardTable._DataList[i];
		if( rankRewardData != null ) {
			if( rankRewardData._rankRangeMinType == 0 ) { // Num
				
				if( rankRewardData._rankRangeMin == rankRewardData._rankRangeMax ) {
					string rankStr = string.Format( StringManager.GetStringTable( 12426 ), rankRewardData._rankRangeMin );
					res.GetData<UILabel>( "rank_label" ).SetTextX_Format( "{0}", rankStr );
				} else {
					string rankMinStr = string.Format( StringManager.GetStringTable( 12426 ), rankRewardData._rankRangeMin );
					string rankMaxStr = string.Format( StringManager.GetStringTable( 12426 ), rankRewardData._rankRangeMax );
					res.GetData<UILabel>( "rank_label" ).SetTextX_Format( "{0}~{1}", rankMinStr, rankMaxStr );
				}
			} else if( rankRewardData._rankRangeMinType == 1 ) { // Percent
				string percentStr = string.Format( StringManager.GetStringTable( 12513 ), rankRewardData._rankRangeMax );
				res.GetData<UILabel>( "rank_label" ).SetTextX_Format( "{0}", percentStr );
			}

			UserRewardResourceData rewardData = rankRewardData._UserRewardResourceData;

			res.GetData<UISprite>( "reward_icon" ).spriteName = rewardData._Icon;
			res.GetData<UILabel>( "num_label" ).SetTextX_Format( "{0}", rewardData._ItemCount );
		}
	}
}
