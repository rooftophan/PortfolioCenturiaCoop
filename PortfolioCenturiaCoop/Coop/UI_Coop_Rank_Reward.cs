using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Coop_Rank_Reward : UIBase {
	public enum REWARD_STEP {
		NONE,
		OPEN,
		CONFIRM_STAND,
		GET_REWARD,
		DELAY_TIME,
		COMPLETE,
		CLOSE,
	}

	protected UIResources _object {
		get {
			if( __object == null ) {
				GameObject go = LoadUI( "UI/UIBattle_Challenge/Coop/Coop_Rank_Reward", transform.parent );
				__object = go.GetComponent<UIResources>();
			}
			return __object;
		}
	}

	UnitPlayer _unitPlayer;

	UI_ItemAddEff _itemAddEff = new UI_ItemAddEff();
	int _rewardCount = 0;
	int _GoodsDiffMinReward = 1;

	Vector3 startPosition = Vector2.zero;
	GameObject endObject = null;
	GameObject moveObject = null;
	GameObject targetPointObj = null;
	NVTweenScale nvts;
	NVTweenAlpha nvta;

	int _nRewardCurrency = 0;
	int _plusReward;

	CURRENCY_TYPE _currencyType;

	UserTable _UserTable = null;

	REWARD_STEP _currentStep = REWARD_STEP.NONE;

	float _delayTime;

	public override void OnPageAwake( object[] objs ) {
		UIResources parent = _object;

		_PopupCloseEnable = true;
		parent.gameObject.SetActiveX( true );

		_UserTable = PlayerManager._UserTable;
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

	public override void OnPageAttach() {
		OnFlush();
	}

	protected override void Update() {
		base.Update();

		if( _currentStep == REWARD_STEP.DELAY_TIME ) {
			_delayTime -= Time.deltaTime;
			if(_delayTime <= 0f ) {
				SetRewardStep( REWARD_STEP.CLOSE );
			}
		}
	}

	void SetRewardStep( REWARD_STEP step ) {
		_currentStep = step;
		switch( _currentStep ) {
		case REWARD_STEP.OPEN:
			SetOpenStep();
			break;
		case REWARD_STEP.GET_REWARD:
			SetGetRewardStep();
			break;
		case REWARD_STEP.DELAY_TIME:
			SetDelayTimeStep();
			break;
		case REWARD_STEP.CLOSE:
			OnClick_Detach();
			break;
		}
	}

	void SetOpenStep() {
		_unitPlayer._OnComplete = OnAniComplete;
		_unitPlayer._OnTick = OnAniTick;
		_unitPlayer.SetAnimation( 0 );
	}

	void SetGetRewardStep() {
		if( _nRewardCurrency > 0 ) {
			UIResources parent = _object;
			startPosition = parent.GetData<UISprite>( "icon" ).transform.position;
			moveObject = parent.GetData<UISprite>( "goods_effect" ).gameObject;
			nvta = (targetPointObj = parent.GetData<GameObject>( "legendskillstone_point" )).GetComponent<NVTweenAlpha>();
			nvts = (endObject = parent.GetData<GameObject>( "legendskillstone_piece_icon" )).GetComponent<NVTweenScale>();

			int nUserUpdateAddCount = Mathf.RoundToInt( (float)_nRewardCurrency / _GoodsDiffMinReward );
			nUserUpdateAddCount = Mathf.Max( nUserUpdateAddCount, 1 );
			_itemAddEff.OpenItemAddEff_Create( UI_ItemAddEff.GET_SOUND_TYPE.LEGEND_SKILL_STONE, this, UI_ItemAddEff.MOVE_TYPE.LINE, nUserUpdateAddCount, parent.GetData<GameObject>( "goods_effects" ), moveObject, startPosition, endObject, nvts, null, OnUpdateItemAdd, OnCompleteItemAdd, OnCreateItem );
		} else {
			SetRewardStep( REWARD_STEP.COMPLETE );
		}
	}
	
	void SetDelayTimeStep() {
		_delayTime = 0.2f;
	}

	public override void OnClick( GameObject go, Vector3 pos ) {
		Transform form = go.transform;
		string str = form.name;

		Debug.Log( string.Format( "OnClick str : {0}", str ) );

		switch( str ) {
		case "close_btn":
		case "popup_background_btn":
			if( _currentStep == REWARD_STEP.COMPLETE ) {
				OnClick_Detach();
			} else if( _currentStep == REWARD_STEP.CONFIRM_STAND ) {
				SetRewardStep( REWARD_STEP.GET_REWARD );
			}
			break;
		}
	}

	void OnFlushAwake() {
		UIResources parent = _object;

		//parent.GetData<UILabel>( "tit_label" ).SetTextX_Format( "{0}", "협동전 랭킹 보상" );
		parent.GetData<UILabel>( "tit_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12531 ) );

		//parent.GetData<UILabel>( "myrank1_label" ).SetTextX_Format( "{0}", "나의 랭킹" );
		parent.GetData<UILabel>( "myrank1_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 11983 ) );

		//parent.GetData<UILabel>( "score1_label" ).SetTextX_Format( "{0}", "점수 :" );
		parent.GetData<UILabel>( "score1_label" ).SetTextX_Format( "{0} :", StringManager.GetStringTable( 12453 ) );

		//parent.GetData<UILabel>( "reward_info_label" ).SetTextX_Format( "{0}", "랭킹 보상" );
		parent.GetData<UILabel>( "reward_info_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12532 ) );

		CoopRankRewardData rankRewardData = GameManager._CoopRankRewardTable._DataList[0];
		UIResources goods = parent.GetData<GameObject>( "UIGoods" ).GetComponent<UIResources>();
		_currencyType = (CURRENCY_TYPE)rankRewardData._UserRewardResourceData._ItemValue;

		UICommon_Goods.SetTopGoodsByCurrency( goods, _currencyType );

		parent.GetData<UISprite>( "goods_effect" ).spriteName = rankRewardData._UserRewardResourceData._Icon;

		_nRewardCurrency = PlayerManager._UserRewardTable._UserCurrencyTable.GetCount( _currencyType );
		_plusReward = _UserTable._UserCurrencyTable.GetCount( _currencyType ) - _nRewardCurrency;

		goods.GetData<UILabel>( "legendskillstone_piece_label" ).SetTextX_Format( "{0}", _plusReward );
		parent.GetData<UILabel>( "num_label" ).SetTextX_Format( "{0}", _nRewardCurrency );

		OnFlush_Info( _object );

		_unitPlayer = parent.GetData<GameObject>( "Container" ).GetComponent<UnitPlayer>();

		SetRewardStep( REWARD_STEP.OPEN );
	}

	void OnFlush_Info( UIResources parent ) {
		UserTable ut = PlayerManager._UserTable;
		UserCoopInfo myCoopInfo = ut._UserArenaData._UserChallengeCoop._userCoop;

		int rankPercent = 0;
		int maxRank = GameManager._ConstantTable.GetValueI( CONSTANT_TYPE.COOP_INT_RANK );
		if( myCoopInfo._rank > maxRank ) {
			rankPercent = Mathf.CeilToInt( ((float)myCoopInfo._rank / (float)myCoopInfo._max_user_count) * 100f );
			if( rankPercent > 100 )
				rankPercent = 100;
			parent.GetData<UILabel>( "myrank2_label" ).SetTextX_Format( StringManager.GetStringTable( 12505 ), rankPercent.ToString() );
		} else {
			string rankNum = string.Format( StringManager.GetStringTable( 12426 ), myCoopInfo._rank );
			parent.GetData<UILabel>( "myrank2_label" ).SetTextX_Format( " {0}", rankNum );
		}

		parent.GetData<UILabel>( "score2_label" ).SetTextX_Format( "{0}", myCoopInfo._best_rank_damage.ToString() );

		List<UserRewardResourceData> rewardList = Coop_Helper.GetResultRewardList();
		if( rewardList != null && rewardList.Count > 0 ) {
			UserRewardResourceData rewardData = rewardList[0];

			_rewardCount = rewardData._ItemCount;

			parent.GetData<UISprite>( "icon" ).spriteName = rewardData._Icon;
			parent.GetData<UILabel>( "num_label" ).SetTextX_Format( "{0}", _rewardCount );
		}
	}

	void OnAniTick( UnitPlayer up, int index, int pre, int cur, string command ) {
		UIResources parent = _object;
		if( command.Equals( "Ani_End" ) ) {
			if( _nRewardCurrency > 0 ) {
				SetRewardStep( REWARD_STEP.CONFIRM_STAND );
			} else {
				SetRewardStep( REWARD_STEP.COMPLETE );
			}
		}
	}

	void OnAniComplete( UnitPlayer up, int index ) {
		up.enabled = false;
	}

	void OnUpdateItemAdd( int index ) {
		UIResources res = _object;
		_plusReward += _GoodsDiffMinReward;

		UIResources goods = res.GetData<GameObject>( "UIGoods" ).GetComponent<UIResources>();
		goods.GetData<UILabel>( "legendskillstone_piece_label" ).SetTextX_Format( "{0}", _plusReward );
	}

	void OnCompleteItemAdd() {
		UIResources res = _object;
		UIResources goods = res.GetData<GameObject>( "UIGoods" ).GetComponent<UIResources>();
		goods.GetData<UILabel>( "legendskillstone_piece_label" ).SetTextX_Format( "{0}", _UserTable._UserCurrencyTable.GetCount( _currencyType ) );
		res.GetData<UILabel>( "num_label" ).SetTextX_Format( "{0}", 0 );

		SetRewardStep( REWARD_STEP.DELAY_TIME );
	}

	void OnCreateItem( float fDelay ) {
		StartCoroutine( OnMoveItem( fDelay ) );
	}

	IEnumerator OnMoveItem( float time ) {
		yield return new WaitForSeconds( time );

		_nRewardCurrency -= _GoodsDiffMinReward;
		UIResources res = _object;
		res.GetData<UILabel>( "num_label" ).SetTextX_Format( "{0}", _nRewardCurrency );

		yield break;
	}
}
