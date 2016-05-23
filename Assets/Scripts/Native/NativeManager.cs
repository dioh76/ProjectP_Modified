using UnityEngine;
using System.Collections;

using IgaworksUnityAOS;

public enum E_NATIVE_ACTION
{
    NONE,
    
    NATIVE_PUSH_SET_REGIST,
    REGIST_DONE,
    NATIVE_DOWNLOAD_IMG,
    DOWN_DONE,

    NATIVE_SET_COMPLETE,

    MAX,
}


public class NativeManager : Immortal<NativeManager> {
	
	public bool AssetBundleUpdageFlag = true;
	public string AppDataPath = "";
	public string AppSdDataPath = "";
	
	public int jsonAssetcount = 0;
    public int jsonTexturecount = 0;
	public int jsoncount = 0;
	
	public string AppSdDataPathDetail = "/.GranMonster";
	
	public string and_udid = "";
	public string and_model = "";
	public string and_version = "";
	public string and_macaddress = "";
	public string and_bundleversion = "";
	
	public string ios_bundleversion = "";
	
	public string gcmId = "";	
	
	public GameObject uiCameraCommon, uiCameraBack, uiCameraDefault, uiCameraPopup;



    public delegate void IsDonePurposeCallBack(bool isDone, string errMessage);

    private E_NATIVE_ACTION _currAct;
    private NativePushManager _pushManager;
    private NativeImageDownLoadManager _imgDownloadManager;

    public AndroidJavaObject _currActivity;
    public AndroidJavaObject onestoreIapManager;
    public bool _nativeInitializeComplete;
    public string _pushRegId;
    private string _curActivityMsg;
    private const string appId = "OA00699734";
    private string _productID = string.Empty;
    private string _productName = string.Empty;


	// Use this for initialization
	void _Start () {
        AppSdDataPathDetail = "/.GranMonster";
		/*
#if !UNITY_EDITOR && UNITY_IPHONE
		//ios_bundleversion = EtceteraBinding.GetBundleVersion();
		//Debug.Log("ios_bundleversion : " + ios_bundleversion);
#endif
#if !UNITY_EDITOR && UNITY_ANDROID
		EtceteraManager.GcmIdCheckEvent  += GcmIdCheckEvent;
		EtceteraManager.AppDataPathEvent  += AppDataPathEvent;
		
		EtceteraBindingAndroid.CallPath();
		string devinfo = EtceteraBindingAndroid.RequestDeviceInfo();
		and_bundleversion = devinfo;
#else		
		AppDataPath = Application.persistentDataPath;
		Debug.Log(AppDataPath);
		//SceneManager.instance.Initialize();
#endif
		*/

        Debug.Log("AppDataPath " + Application.persistentDataPath);

		AppDataPath = Application.persistentDataPath;
		AppSdDataPath = Application.persistentDataPath;		

	}
	
    void Start()
    {
        _pushRegId = string.Empty;

        _imgDownloadManager = new NativeImageDownLoadManager(OnCompleteImgDown);
        _pushManager = new NativePushManager(OnCompletePushRegst);

        //InitializeNativeFunctions(E_NATIVE_ACTION.NATIVE_PUSH_SET_REGIST);

        _curActivityMsg = " START ";
    }

	public void nativeSet()
	{
        AppSdDataPathDetail = "/.GranMonster";
        AppDataPath = Application.persistentDataPath;
        AppSdDataPath = Application.persistentDataPath;

        _curActivityMsg = " NATIVE SET ";

        initializeActivity();

    }
	
	void AppDataPathEvent(string path)
	{
		string[] paths = path.Split('_');
		AppDataPath = paths[0];
		AppSdDataPath = paths[1] + AppSdDataPathDetail;
		
		Debug.Log("AppDataPath : " + AppDataPath);
		Debug.Log("AppSdDataPath : " + AppSdDataPath);
	}
	
	void GcmIdCheckEvent(string _gcmid)
	{
		Debug.Log("gcmid : " + _gcmid);
		gcmId = _gcmid;		
	}

    public float  GetProgress()
    {
        return (float)((JsonLoader.instance.GetProgress() + AssetDownLoad.instance.GetProgress() + TextureDownLoad.instance.GetProgress()) / 3);
        //return (float)( ( AssetDownLoad.instance.GetProgress() + TextureDownLoad.instance.GetProgress() ) / 3 );
    }

    public float GetDownloadProgress()
    {
        return (float)( ( AssetDownLoad.instance.GetProgress() + TextureDownLoad.instance.GetProgress() ) / 2 );
    }

    public void InitializeNativeFunctions(E_NATIVE_ACTION newAct)
    {
        if (_currAct == newAct)
            return;

        _currAct = newAct;

        initiateAction(_currAct);
    }

    public E_NATIVE_ACTION GetCurrentAct()
    {
        return _currAct;
    }


    #region 애드브릭스 Functions....

    public void SetVisiblePublicNotification()
    {
        //하이퍼 링크용 공지
        //test PublicNotification_for_test
        IgaworksUnityPluginAOS.LiveOps.showPopUp("a");
    }

    public void SetVisiblePublicNotificationForPurchase()
    {
        //딥 링크용 공지
        IgaworksUnityPluginAOS.LiveOps.showPopUp("PublicNotification_for_purchase");
    }

    public void SetVisiblePublicNotificationForPurchase2()
    {
        //딥 링크용 공지 넘버 2
        IgaworksUnityPluginAOS.LiveOps.showPopUp("PublicNotification_for_purchase_Second");
    }

    public void SetVisiblePublicNotificationForPurchase3()
    {
        //딥 링크용 공지 넘버 3
        IgaworksUnityPluginAOS.LiveOps.showPopUp("PublicNotification_for_purchase_Third");
    }


    public void SetVisiblePublicNotificationForPurchage4()
    {
        IgaworksUnityPluginAOS.LiveOps.showPopUp("PublicNotification_for_test");
    }
    
    public void SetPushEnable(bool isEnable)
    {
        IgaworksUnityPluginAOS.LiveOps.enableService(isEnable);
    }

    public void ReserveNotifyLocalPush(int delaySecond, int type)
    {
        //Debug.Log(" !!!!! ReserveNotifyLocalPush " + delaySecond + " , " + type);

        string message = string.Empty;

        //Debug.Log(" !!!!! " + delaySecond + " , " + type);

        if(type == 1)
        {
            //스테미너 로컬푸시
            if(null != UIManager.instance)
                message = UIManager.instance._localSteminaText.text;

            //message = UtilityDefine.push_local_for_steminaMessage;
            
        }
        else if (type == 0)
        {
            //스킬포인트 로컬푸시
            if (null != UIManager.instance)
                message = UIManager.instance._localSkillText.text;

            //message = UtilityDefine.push_local_for_skillPointMessage;
        }

        //Debug.Log(" !!!!! " + message);

        IgaworksUnityPluginAOS.LiveOps.setNormalClientPushEvent(delaySecond, message, 1, false);

    }

    public void SetNewUserTrackingActivity(string message)
    {
        IgaworksUnityPluginAOS.Adbrix.firstTimeExperience(message);
    }

    public void SetCustomTrackingActivity(string message)
    {
        IgaworksUnityPluginAOS.Adbrix.retention(message);
    }

    public void SetInAppTrackingActivity(string message)
    {
        IgaworksUnityPluginAOS.Adbrix.buy(message);
    }

    #endregion


    #region 원스토어 인앱결제 관련 Functions...

    // 안드로이드 라이브러리의 결제 요청 메소드를 호출한다.
    public void CallPaymentRequest(string productID, string productName, string transactionID)
    {
        if (onestoreIapManager != null)
        {
            //strLabelPayment = "TRY PURCHASE " + productID + " , \n " + productName + " , \n " + transactionID ;
            onestoreIapManager.Call("PaymentRequest", appId, productID, productName, transactionID);
            //appid 필, product_id 필, product_name 필, tid 선
        }
    }
  
    // 안드로이드 라이브러리의 Query 요청 메소드를 호출한다.
    public void CallCommandRequest(string productID )
    {
        if (onestoreIapManager != null)
        {
            onestoreIapManager.Call("CommandRequest", appId, productID, "request_purchase_history", 0);
            //appid 필, product_id 	선, action 	String 	기능 메소드의 서브 액션 선택
        }
    }
 
    
    #endregion


    void setSteminaLocalpush()
    {
        //6분
        int rechargeSteminaDealyTime = 6;
        int maxStemina = 10;

        //Debug.Log(" !!!!! " + DataManager.instance.netdata.player._Token._Stamina);

        if (null != DataManager.instance.netdata.player._Token._Stamina)
        {
            if (DataManager.instance.netdata.player._Token._Stamina < maxStemina)
            {
                int rechargeSeconds = (maxStemina - DataManager.instance.netdata.player._Token._Stamina) * (60 * rechargeSteminaDealyTime);

                //Debug.Log(" !!!!! " + rechargeSeconds);

                ReserveNotifyLocalPush(rechargeSeconds, 1);
            }
        }
    }
    
    void setSkillPointLocalpush()
    {
        int rechargeSkillPointDeltayTime = 10;
        int maxSkillPoint = 10;

        //Debug.Log(" !!!!! " + DataManager.instance.netdata.player._Asset._SkillPoint);

        if (null != DataManager.instance.netdata.player._Asset._SkillPoint)
        {
            if (DataManager.instance.netdata.player._Asset._SkillPoint < maxSkillPoint)
            {
                int rechargeSeconds = (maxSkillPoint - DataManager.instance.netdata.player._Asset._SkillPoint) * (60 * rechargeSkillPointDeltayTime);

                //Debug.Log(" !!!!! " + rechargeSeconds);

                ReserveNotifyLocalPush(rechargeSeconds, 0);
            }
        }
    }

    void initializeActivity()
    {
        _curActivityMsg = " INITAIALIZE ACTIVITY ";

        _nativeInitializeComplete = false;
        _currAct = E_NATIVE_ACTION.NONE;

#if UNITY_ANDROID
        if (Application.platform == RuntimePlatform.Android)
        {
            AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            _currActivity = jc.GetStatic<AndroidJavaObject>("currentActivity");

            if (null != _currActivity)
            {
                this.onestoreIapManager = new AndroidJavaObject("com.skplanet.iap.unity.IapManager", _currActivity, appId);

                if (onestoreIapManager != null)
                {
                    if (!onestoreIapManager.Call<bool>("InitPlugin", false))
                    {
                       // strLabelPayment = "Init Fail";
                        //strLabelQuery = "Init Fail";
                    }
                }
            }

            _curActivityMsg = " ADDBRIX PLUGIN INITIALIZE ";

            IgaworksUnityPluginAOS.InitPlugin();
            IgaworksUnityPluginAOS.Common.startApplication();
            IgaworksUnityPluginAOS.Common.startSession();
            IgaworksUnityPluginAOS.LiveOps.setNotificationOption(IgaworksUnityPluginAOS.AndroidNotificationPriority.PRIORITY_MAX, IgaworksUnityPluginAOS.AndroidNotificationVisibility.VISIBILITY_PUBLIC);
            //IgaworksUnityPluginAOS.LiveOps.setStackingNotificationOption(true, false, "More events are waiting for you", "See detail", "All events", "For Summary Text");

            IgaworksUnityPluginAOS.Common.setUserId( SystemInfo.deviceUniqueIdentifier );
            IgaworksUnityPluginAOS.LiveOps.initialize();
            
            IgaworksUnityPluginAOS.LiveOps.requestPopupResource();              //공지 리소스 불러오기
            IgaworksUnityPluginAOS.OnReceiveDeeplinkData = OnRecieveDeepLinkData;       //딥링크(결재)용 이벤트 틍록


            _curActivityMsg = " ADDBRIX PLUGIN INITIALIZE COMPLETE";

            //Debug.Log(" !!!!! " + _curActivityMsg );

        }
#endif

        //Debug.Log(" !!!!! NATIVEMANAGER INIT PLUGIN !!!!! ");

    }



    void OnRecieveDeepLinkData(string deepLinkData)
    {
        IgaworksUnityPluginAOS.LiveOps.destroyPopup();

        //!!!!!!!!!!!!!딥링크 테스트해봐야됨!!!!!!!!!!!! 딥링크로 구매 되는 애들이 있음
        _curActivityMsg = deepLinkData;

        JSONObject jsonObj = new JSONObject(deepLinkData);

        int resultCode = (int)jsonObj["result"].n;

        _curActivityMsg = resultCode.ToString();

        switch(resultCode)
        {
            case 1:
                {
                    _productID = "0910047059";
                    _productName = LowData._shopLowData.DateInfoDic[int.Parse(_productID)].Memo_c;
                    break;
                }
            case 2:
                {
                    _productID = "0910047061";
                    _productName = LowData._shopLowData.DateInfoDic[int.Parse(_productID)].Memo_c;
                    break;
                }

            case 3:
                {
                    _productID = "0910047062";
                    _productName = LowData._shopLowData.DateInfoDic[int.Parse(_productID)].Memo_c;
                    break;
                }
        }


        if (null != NativeManager.instance)
        {
            UIManager.instance.OpenCheckPopup(E_CHECK_POPUP_TYPE.YorN, E_LOCALIZE_STRING.BUYING_INAPP_RUBY_DESC, E_LOCALIZE_STRING.BUYING_INAPP_RUBY_SUB_DESC, callbackBuyRubby);
            UIManager.SetLocalizeTerm(UIManager.instance.GetPopup(E_UIPOPUP_TYPE.CheckPopup).GetComponent<CheckPopup>()._desc, E_LOCALIZE_STRING.BUYING_INAPP_RUBY_SUB_DESC);
            string str = UIManager.instance.GetPopup(E_UIPOPUP_TYPE.CheckPopup).GetComponent<CheckPopup>()._desc.text;
            UIManager.instance.GetPopup(E_UIPOPUP_TYPE.CheckPopup).GetComponent<CheckPopup>()._desc.text = string.Format(str, LowData._shopLowData.DateInfoDic[int.Parse(_productID)].Memo_c, LowData._shopLowData.DateInfoDic[int.Parse(_productID)].PaymentCost_i);
        }
    }

    void callbackBuyRubby()
    {
        WebSender.instance.P_GET_TRANSACTION_ID(onCompleteTID);
    }

    void onCompleteTID(JSONObject jsonObj)
    {
        common.Response.GetTIDResult getTIDresult = DataManager.instance.LoadData<common.Response.GetTIDResult>(jsonObj);

        if (common.ResultCode.OK == (common.ResultCode)getTIDresult._Code)
        {
            if (null != NativeManager.instance)
                NativeManager.instance.CallPaymentRequest(_productID, _productName, getTIDresult._TID);

        }
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
            UIManager.instance.OpenCheckPopup(E_CHECK_POPUP_TYPE.YorN, E_LOCALIZE_STRING.SYSTEM_MESSAGE_DESC, E_LOCALIZE_STRING.SYSTEM_MESSAGE_QUIT_ALERT, OnCallbackComfirm);

        executeStepFunction();
    }

    /*
    void OnGUI()
    {
        GUI.Box(new Rect(0f, 0f, Screen.width, 100f), " NATIVE_STEP : " + _curActivityMsg);
    }
    */

    public void OnCallbackComfirm()
    {
        ApplicationQuit();

        Application.Quit();
    }

    void ApplicationQuit()
    {
        IgaworksUnityPluginAOS.Common.endSession();

        if (_currActivity != null)
            _currActivity.Dispose();

        if (onestoreIapManager != null)
            onestoreIapManager.Dispose();

        if (null != OptionManager.Instance)
            OptionManager.Instance.SaveOptionData();

        //앱을 죽였을때 셋팅
        setSkillPointLocalpush();
        setSteminaLocalpush();
    }

    void OnApplicationQuit()
    {
        ApplicationQuit();

       /* if (_currActivity != null)
            _currActivity.Dispose();

        if (onestoreIapManager != null)
            onestoreIapManager.Dispose();
        

        if (null != OptionManager.Instance)
        {
            Debug.Log(" !!!!!! ");
            IgaworksUnityPluginAOS.Common.endSession();
            OptionManager.Instance.SaveOptionData();
        }

        //앱을 죽였을때 셋팅
        setSkillPointLocalpush();
        setSteminaLocalpush();*/
   
    }

    
    void OnApplicationFocus(bool focusStatus)
    {

#if UNITY_ANDROID
        
        if(false == focusStatus)
        {
            _curActivityMsg = " ADDBRIX END SEEEION ";
            //수동 동기화
            IgaworksUnityPluginAOS.LiveOps.flushTargetingData();
            IgaworksUnityPluginAOS.Common.endSession();
        }
        
        else
        {
            //Debug.Log("go to Foreground");
            _curActivityMsg = " ADDBRIX START SESSION ";
            IgaworksUnityPluginAOS.Common.startSession();
            IgaworksUnityPluginAOS.LiveOps.resume();
        }
        
#endif

    }

    void OnApplicationPause(bool pauseStatus)
    {

#if UNITY_ANDROID

        if (pauseStatus)
        {
            //Debug.Log("go to Background");
            _curActivityMsg = " ADDBRIX END SEEEION ";

            //수동 동기화
            IgaworksUnityPluginAOS.LiveOps.flushTargetingData();

            //앱을 내렸을때 셋팅
            setSkillPointLocalpush();
            setSteminaLocalpush();

            IgaworksUnityPluginAOS.Common.endSession();
        }
        else
        {
            //Debug.Log("go to Foreground");
            _curActivityMsg = " ADDBRIX START SESSION ";
            IgaworksUnityPluginAOS.Common.startSession();
            IgaworksUnityPluginAOS.LiveOps.resume();
        }

#endif

	}


    void executeStepFunction()
    {
        if (true == _nativeInitializeComplete)
            return;

        switch(_currAct)
        {
            case E_NATIVE_ACTION.NATIVE_DOWNLOAD_IMG:
                {
                    break;
                }

            case E_NATIVE_ACTION.NATIVE_PUSH_SET_REGIST:
                {
                    break;
                }

            case E_NATIVE_ACTION.NATIVE_SET_COMPLETE:
                {
                    if (false == _nativeInitializeComplete)
                        _nativeInitializeComplete = true;

                    break;
                }
        }
    }

    void DrawCurrentStepDataFlow()
    {
        if (null != this._currActivity)
            _curActivityMsg = " NOT NULL";
        else
            _curActivityMsg = " NULL";

        GUI.Box(new Rect(0f, 0f, Screen.width, 50f), " NATIVE_STEP : " + _currAct + " ,  !!!!! " + _curActivityMsg);

        switch (_currAct)
        { 
            case E_NATIVE_ACTION.NATIVE_DOWNLOAD_IMG:
            case E_NATIVE_ACTION.DOWN_DONE:
                {
                    if (null != _imgDownloadManager)
                        _imgDownloadManager.OnGUI();
                    
                    break;
                }

            case E_NATIVE_ACTION.NATIVE_PUSH_SET_REGIST:
                {
                    if (null != _pushManager)
                        _pushManager.OnGUI();

                    break;
                }
        }
    }

    void initiateAction(E_NATIVE_ACTION currentAct)
    {
        switch (currentAct)
        {
            case E_NATIVE_ACTION.NATIVE_DOWNLOAD_IMG:
                {
                    _imgDownloadManager.Initiate();
                    
                    break;
                }

            case E_NATIVE_ACTION.NATIVE_PUSH_SET_REGIST:
                { 
                    _pushManager.Initiate();

                    break;
                }
        }
    }


    #region callBack Native Img DownLoad Functions....

    public void OnCompleteImgDown(bool isDone, string message)
    {
        if (true == isDone)
        {
            InitializeNativeFunctions(E_NATIVE_ACTION.DOWN_DONE);
        }
    }

    public void callBackServerImgDownLoad(string message)
    {
        if (null != _imgDownloadManager)
            _imgDownloadManager.callBackServerImgDownLoad(message);
    }

    #endregion


    #region callBack Native Push Functions....

    public void OnCompletePushRegst(bool isDone, string message)
    {
        if (true == isDone)
        {
            //InitializeNativeFunctions(E_NATIVE_ACTION.REGIST_DONE);
            _pushRegId = message;
        }
        else
        {
            //재시도? 후처리....
        }
    }


    public void callBackPushRegistComplete(string message)
    {
        if (null != _pushManager)
            _pushManager.callBackPushRegistComplete(message);

    }

    public void callBackPushUnRegistComplete(string message)
    {
        if (null != _pushManager)
            _pushManager.callBackPushUnRegistComplete(message);
    }

    public void callBackPushStateMessage(string message)
    {
        if (null != _pushManager)
            _pushManager.callBackPushStateMessage(message);
    }

    public void callBackOnRetisterd(string message)
    {
        if (null != _pushManager)
            _pushManager.callBackOnRetisterd(message);
    }

    #endregion

}
