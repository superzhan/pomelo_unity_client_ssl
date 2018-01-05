using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pomelo.DotNetClient;
using SimpleJson;
using System;

public class Test : MonoBehaviour {

	public string gateHost="47.100.96.55";
	public int gatePort  = 4000;

	public  PomeloClient pomeloClient=null;

	// Use this for initialization
	void Start () {

		//pomeloClient = new PomeloClient(TransportType.TCP);
		pomeloClient = new PomeloClient(TransportType.SSL);
		pomeloClient.NetWorkStateChangedEvent+=OnNetWorkStateChange;
		pomeloClient.initClient(gateHost,gatePort ,()=>{

			JsonObject user = new JsonObject();
			pomeloClient.connect(user, data =>
				{
					JsonObject msg = new JsonObject();
					msg["uid"] = "hello";
					pomeloClient.request("gate.gateHandler.queryEntry",msg, OnGateQuery);
				});

		});
		
	}

	private void OnGateQuery(JsonObject result)
	{
		Debug.Log (result.ToString());

	}

	private void OnNetWorkStateChange(NetWorkState state)
	{
		Debug.Log (state.ToString());
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
