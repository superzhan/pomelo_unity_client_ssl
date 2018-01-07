# pomelo Unity 客户端 

-----

### ------增加SSL/TSL 协议扩展

-----

### Introduce

pomelo的Unity客户端，项目基于 <https://github.com/NetEase/pomelo-unityclient-socket> pomelo-unityclient-socket。

原项目只能使用TCP协议，在此基础上添加了SSL/TSL 协议扩展。

### 使用示例

新建客户端连接的时候可以根据需求选择 TCP连接或者 SSL 连接。

```C#
pomeloClient = new PomeloClient(TransportType.SSL);  //使用SSL 加密连接
```

```C#
pomeloClient = new PomeloClient(TransportType.TCP);  //使用TCP 连接
```


-----


项目测试示例。位于目录 Assets/Test/test.cs

```C#
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pomelo.DotNetClient;
using SimpleJson;
using System;

public class Test : MonoBehaviour {

	public string gateHost="127.0.0.1";
	public int gatePort  = 3200;

	public  PomeloClient pomeloClient=null;

	// Use this for initialization
	void Start () {

		//pomeloClient = new PomeloClient(TransportType.TCP); // 可以自由选择TCP 协议或者 SSL协议。
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
}

```

## 其他用法

目前只是修改了插件的内部实现，对外的API没有变化。其他用法可以参考<https://github.com/NetEase/pomelo-unityclient-socket>

Use request and response

```c#
pclient.request(route, message, (data)=>{
    //process the data
});
```

Notify server without response

```c#
pclient.notify(route, messge);
```

Add event listener, process broadcast message

```c#
pclient.on(route, (data)=>{
    //process the data
});
```
Disconnect the client.

```c#
pclient.disconnect();
```

## 服务器配置

1 需要使用openssl 生成一套证书。参考 <https://www.cnblogs.com/littleatp/p/5878763.html>

2 配置服务器的 app.js

```javascript
app.configure('production|development', 'gate', function(){
    app.set('connectorConfig',
        {
            connector : pomelo.connectors.hybridconnector,
            ssl: {
                key: fs.readFileSync(__dirname+'/keys/openssl.key'),
                cert: fs.readFileSync(__dirname+'/keys/openssl.crt'),
            }
        });
});
```

## 没有实现的功能

缺少证书验证功能。





