using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoopPartyChat {
	public enum COOP_CHAT_EVENT {
		FRIEND_INVITE = 1,

	}

	static CoopPartyChat _instance = null;
	public static CoopPartyChat Instance {
		get {
			if( _instance == null )
				_instance = new CoopPartyChat();

			return _instance;
		}
	}

	public long _coopPartyID = 0;
	public long _otherAccountID = 0;
	//public UIResources _coopBattleResultRes = null;
	

	public static void Init() {
		if( _instance != null ) {
			_instance._coopPartyID = 0;
		}
		_instance = null;
	}

	public void ConnectCoopPartyChat() {
		_coopPartyID = 0;
		long myAccountID = PlayerManager._UserTable._UserInfoTable._account_id;
		if( myAccountID > _otherAccountID ) {
			_coopPartyID = myAccountID;
		} else {
			_coopPartyID = _otherAccountID;
		}
		ChatManager.Request_JoinParty( _coopPartyID, ChatManager.PartyType.COOP_BATTLE, false, OnReceive_CoopJoinPartyInit );
	}

	public void SetOtherAccountID( long accountID ) {
		_otherAccountID = accountID;
	}

	public void DisconnectCoopPartyChat() {
		if( _coopPartyID > 0 ) {
			ChatManager.Request_LeaveParty( _coopPartyID, ChatManager.PartyType.COOP_BATTLE, OnRecive_CoopBattleLeaveChat );
			_coopPartyID = 0;
		}
	}

	/*public void SendCoopPartyExit() {
		if( _coopPartyID > 0 ) {
			SimpleJSON.JSONObject sons = new SimpleJSON.JSONObject();
			sons["account_id"] = PlayerManager._UserTable._UserInfoTable._account_id;
			string msg = jUtil.ToData( sons );
			ChatManager.Request_PartyChat( ChatManager.PartyType.COOP_BATTLE, ChatManager.MESSAGE_KIND.COOP_EXIT, 2, msg );
		}
	}*/

	void OnReceive_CoopJoinPartyInit( int partyType, bool isSuccess ) {
		if( isSuccess ) {
			Debug.Log( string.Format( "!!!!! ===== OnReceive_CoopJoinPartyInit Success _coopPartyID : {0} !!!!!", _coopPartyID ) );
		}
	}

	void OnRecive_CoopBattleLeaveChat( int partyType, bool isSuccess ) {
		Debug.Log( string.Format( "!!!!! ===== OnRecive_CoopBattleLeaveChat Success _coopPartyID : {0} !!!!!", _coopPartyID ) );
	}
}
