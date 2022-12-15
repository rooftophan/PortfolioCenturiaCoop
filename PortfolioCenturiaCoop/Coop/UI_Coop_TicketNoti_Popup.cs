using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_Coop_TicketNoti_Popup : UIBase {
	public enum TICKETNOTI_TYPE {
		TICKET_NOTI,
		TICKET_BUY,
	}

	protected UIResources _object {
		get {
			if( __object == null ) {
				GameObject go = LoadUI( "UI/UIBattle_Challenge/Coop/Coop_TicketNoti_Popup", transform.parent );
				__object = go.GetComponent<UIResources>();
			}
			return __object;
		}
	}

	TICKETNOTI_TYPE _popupType;
	public Action _onBuyCoopTicket = null;

	public override void OnPageAwake( object[] objs ) {
		UIResources parent = _object;
		_popupType = (TICKETNOTI_TYPE)objs[0];
		int priceValue = 0;
		if( objs.Length > 1 ) {
			priceValue = (int)objs[1];
		}
		
		SetPopupType( priceValue );

		UIButton buyBtn = parent.GetData<GameObject>( "buy_btn" ).GetComponent<UIButton>();
		buyBtn.onClick.Add( new EventDelegate(OnClick_Buy) );

		UIButton closeBtn = parent.GetData<GameObject>( "popup_close_btn" ).GetComponent<UIButton>();
		closeBtn.onClick.Add( new EventDelegate( OnClick_Detach ) );

		_PopupCloseEnable = true;
		parent.gameObject.SetActiveX( true );

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

	void OnClick_Buy() {
		OnClick_Detach();
		HttpManager.Request_CoopTicketBuy( OnSuccess_TicketBuy );
	}

	void OnSuccess_TicketBuy(bool isSuccess, object data ) {
		if( isSuccess ) {
			OnClick_Detach();
			_onBuyCoopTicket?.Invoke();
		}
	}

	void SetPopupType( int priceValue ) {
		UIResources parent = _object;
		UILabel titleLabel = parent.GetData<UILabel>( "title_label" );
		UILabel descLabel = parent.GetData<UILabel>( "desc_label" );

		UIResources ticketnoti_style = parent.GetData<GameObject>( "ticketnoti_style" ).GetComponent<UIResources>();
		UIResources ticketbuy_style = parent.GetData<GameObject>( "ticketbuy_style" ).GetComponent<UIResources>();

		switch( _popupType ) {
		case TICKETNOTI_TYPE.TICKET_NOTI:
			//titleLabel.SetTextX_Format( "{0}", "협동전 티켓 부족" );
			titleLabel.SetTextX_Format( "{0}", StringManager.GetStringTable( 12350 ) );
			//descLabel.SetTextX_Format( "{0}", "티켓을 모두 소모하여 보상을 받을 수 없습니다.\n그래도 입장하시겠습니까?" );
			descLabel.SetTextX_Format( "{0}", StringManager.GetStringTable( 12351 ) );
			//ticketnoti_style.GetData<UILabel>( "left_label" ).SetTextX_Format("{0}", "티켓 구매");
			ticketnoti_style.GetData<UILabel>( "left_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12352 ) );
			//ticketnoti_style.GetData<UILabel>( "right_label" ).SetTextX_Format( "{0}", "입장" );
			ticketnoti_style.GetData<UILabel>( "right_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 12353 ) );
			ticketnoti_style.gameObject.SetActiveX( true );
			ticketbuy_style.gameObject.SetActiveX( false );
			break;
		case TICKETNOTI_TYPE.TICKET_BUY:
			//titleLabel.SetTextX_Format( "{0}", "협동전 티켓 구매" );
			titleLabel.SetTextX_Format( "{0}", StringManager.GetStringTable( 12354 ) );
			int ticketCount = GameManager._ConstantTable.GetValueI( CONSTANT_TYPE.COOP_TICKET_REFILL_COUNT );
			//string desc = string.Format( "협동전 티켓 {0}장을 크리스탈 {1}를 사용하여 구매 하시겠습니까?", ticketCount, priceValue );
			string desc = string.Format( StringManager.GetStringTable( 12355 ), ticketCount, priceValue );
			descLabel.SetTextX_Format( "{0}", desc );
			ticketnoti_style.gameObject.SetActiveX( false );
			ticketbuy_style.gameObject.SetActiveX( true );
			ticketbuy_style.GetData<UILabel>( "confirm_label" ).SetTextX_Format( "{0}", StringManager.GetStringTable( 10004 ) );
			ticketbuy_style.GetData<UILabel>( "pricevalue_label" ).SetTextX_Format( "{0}", priceValue );
			break;
		}
	}
}
