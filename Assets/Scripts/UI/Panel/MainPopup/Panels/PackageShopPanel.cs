using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class PackageShopPanel : MonoBehaviour 
{
    public Text _titleText;                         // 로컬라이즈용
    public tk2dSpriteFromTexture _titleTexture;     // 이미지 용
    public Text _shopDesc_1;                        //price
    public Text _shopDesc_2;                        //price
    //public tk2dSlicedSprite _merchandiseTexture;

    public Text _buyBtnDesc;

    private Texture2D[] _tempTextures;

    private string _productID;
    private string _productName;
    private int _poppupShopKey;

    private int _cnt;

    private int _purchapsePopupItemIndex = -1;
    private string _message;


    void Awake()
    {
        _tempTextures = new Texture2D[8];

        string path = "Source/PopupShop/{0}";
        for (int i = 0; i < _tempTextures.Length; i++)
        {
            string texName = string.Format("image_package_{0}", (i+1));
            _tempTextures[i] = Resources.Load(string.Format(path, texName)) as Texture2D;

            //Debug.Log(" !!!!! " + _tempTextures[i].name);
        }
    }

    public void LoadPanel()
    {
        this.transform.localPosition = new Vector3(0f, 0f, -1550f);
        this.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        //Debug.Log(" !!!!! " + OptionSetting._playerOption._closePackageTime.Day + " , " + System.DateTime.Now.Day);

        if(OptionSetting._playerOption._closePackageTime.Day != System.DateTime.Now.Day)
            Init();
        else
            OnClickExit();

        //testInit();

    }


    public void LoadPanel(int index)
    {
        this.transform.localPosition = new Vector3(0f, 0f, -1550f);
        this.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

        testInit(index);
    }

    void Init()
    {
        //Debug.Log(" !!!!! " + UtilityDefine.standardTime + " , " + OptionSetting._playerOption._packageBuyTime);

        int index = getNetxtPopupItemTextureIndex();

        //Debug.Log(" !!!!! " + OptionSetting._playerOption._purchaseInAppCnt + " , " + index);

        if (index >= _tempTextures.Length || index == 0)
            index = (0 + 1);
        

        _cnt = index;
        //_message = "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!" + index + " , " + _cnt;

        //Debug.Log(" !!!!! " + _tempTextures.Length + " , " + index);

        int popupShopKey = -1;
        int shopKey = -1;

        switch (index)
        {
            case 1:
                {
                    popupShopKey = 910047047;
                    shopKey = 9011;
                    break;
                }

            case 2:
                {
                    popupShopKey = 910047048;
                    shopKey = 9012;
                    break;
                }
            case 3:
                {
                    popupShopKey = 910047049;
                    shopKey = 9013;
                    break;
                }
            case 4:
                {
                    popupShopKey = 910047051;
                    shopKey = 9014;
                    break;
                }
            case 5:
                {
                    popupShopKey = 910047054;
                    shopKey = 9015;
                    break;
                }
            case 6:
                {
                    popupShopKey = 910047055;
                    shopKey = 9016;
                    break;
                }
            case 7:
                {
                    popupShopKey = 910047056;
                    shopKey = 9017;
                    break;
                }
        }


        _buyBtnDesc.text = string.Format("{0:#,0}원", LowData._shopLowData.DateInfoDic[popupShopKey].PaymentCost_i);

        //Debug.Log(" !!!!! " + UtilityDefine.standardTime + " , " + OptionSetting._playerOption._packageBuyTime);
     

        //Debug.Log(" !!!!! " + UtilityDefine.standardTime + " , " + OptionSetting._playerOption._packageBuyTime);

        if(UtilityDefine.standardTime == OptionSetting._playerOption._packageBuyTime)
        {
            //구매 이력 없음
            setTexture(index);
        }
        else
        {
            
            //구매 이력이 있다
            int seconds = LowData._popupShopLowData.DataInfoDic[shopKey].DelayTime_i;      //데이터에 지정된 시간 값 초단위

            //Debug.Log(" !!!!! " + seconds);

            OptionSetting._playerOption._packageBuyTime.AddSeconds(seconds);
            
            System.TimeSpan spanTime = System.DateTime.Now - OptionSetting._playerOption._packageBuyTime;

            //Debug.Log(" !!!!! " + spanTime.Ticks);

            if (spanTime.Ticks <= 0)
            {
                //구매한 시점으로 부터 특정 시간값이 지났다면 이전 단계 상품 보여줌
                int prevIndex = getPreviousPopupItemTextureIndex(shopKey);

                //Debug.Log(" !!!!! " + prevIndex);

                if (prevIndex != -1)
                    setTexture(prevIndex);
                else
                    OnClickClose();
            }
            else
            {
                if (OptionSetting._playerOption._purchaseInAppIndex == 910047056 && index == 7)
                {
                    UIManager.instance.RemovePanel(E_UIPANEL_TYPE.PackageShopPanel);
                    return;
                }
     
                //다음 상품
                setTexture(index);

            }
        }
    }



    void testInit(int index)
    {
        if (index >= _tempTextures.Length || index == 0)
        {
            index = (0 + 1);
            _cnt = index;
        }
        else
            _cnt = (index + 1);


        //_message = "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!" + index + " , " + _cnt;

        //Debug.Log(" !!!!! " + _tempTextures.Length + " , " + index);

        int popupShopKey = -1;
        int shopKey = -1;

        switch (_cnt)
        {
            case 1:
                {
                    popupShopKey = 910047047;
                    shopKey = 9011;
                    break;
                }

            case 2:
                {
                    popupShopKey = 910047048;
                    shopKey = 9012;
                    break;
                }
            case 3:
                {
                    popupShopKey = 910047049;
                    shopKey = 9013;
                    break;
                }
            case 4:
                {
                    popupShopKey = 910047051;
                    shopKey = 9014;
                    break;
                }
            case 5:
                {
                    popupShopKey = 910047054;
                    shopKey = 9015;
                    break;
                }
            case 6:
                {
                    popupShopKey = 910047055;
                    shopKey = 9016;
                    break;
                }
            case 7:
                {
                    popupShopKey = 910047056;
                    shopKey = 9017;
                    break;
                }
        }


        _buyBtnDesc.text = string.Format("{0:#,0}원", LowData._shopLowData.DateInfoDic[popupShopKey].PaymentCost_i);

        setTexture(_cnt);
    }

    int getPreviousPopupItemTextureIndex(int shopKey)
    {
        int previousItemKey = -1;

        int key = LowData._popupShopLowData.DataInfoDic[shopKey].DelayLinkIndex_i;

        switch(key)
        {
            case 9012:
                {
                    previousItemKey = 2;
                    break;
                }
            case 9013:
                {
                    previousItemKey = 3;
                    break;
                }

            case 9014:
                {
                    previousItemKey = 4;
                    break;
                }
            case 9015:
                {
                    previousItemKey = 5;
                    break;
                }
            case 9016:
                {
                    previousItemKey = 6;
                    break;
                }
        }


        return previousItemKey;
    }

    int getNetxtPopupItemTextureIndex()
    {
        //Debug.Log( "!!!" +  OptionSetting._playerOption._purchaseInAppIndex);

        int appIndex = OptionSetting._playerOption._purchaseInAppIndex;

        if (appIndex <= 0)
            return 0;

        //_message = "111111111111111111111111111111111111111111111111111111" + appIndex;

        /*if(false == LowData._popupShopLowData.DataInfoDic.ContainsKey(appIndex))
            Debug.Log(" !!!!! ERROR NOT FIND APPINDEX !!!!!" );*/

        int shopKey = -1;
        switch(appIndex)
        {
            case 910047047:
                {
                    shopKey = 9011;
                    break;
                }
            case 910047048:
                {
                    shopKey = 9012;
                    break;
                }
            case 910047049:
                {
                    shopKey = 9013;
                    break;
                }
            case 910047051:
                {
                    shopKey = 9014;
                    break;
                }
            case 910047054:
                {
                    shopKey = 9015;
                    break;
                }
            case 910047055:
                {
                    shopKey = 9016;
                    break;
                }
            case 910047056:
                {
                    shopKey = 9017;
                    break;
                }
        }

        //_message = "222222222222222222222222222222222222222222222222222222" + appIndex + " , " + shopKey;
        int nextPopupItemIndex = LowData._popupShopLowData.DataInfoDic[shopKey].BuyLinkIndex_i;

        switch (nextPopupItemIndex)
        {
            case 9015:
                {
                    appIndex = 5;   //슈퍼패키지
                    break;
                }

            case 9016:
                {
                    appIndex = 6;   //울트라 패키지
                    break;
                }

            case 9017:
                {
                    appIndex = 7;   //레전드 패키지
                    break;
                }
        }


        return appIndex;
    }

    void setTexture(int index)
    {
        int currentTexIndex = 0;
        if(index >= _tempTextures.Length)
            currentTexIndex = 0;
        else
        {
            if ((index - 1) <= 0)
                index = 0;
            else
                index -= 1;

            currentTexIndex = index;

            if (currentTexIndex <= 7)
            {
                //Debug.Log(" !!!!! " + currentTexIndex + " , " + _tempTextures[currentTexIndex].name);

                tk2dSpriteCollectionSize size = new tk2dSpriteCollectionSize();
                size.pixelsPerMeter = 1;
                _titleTexture.Create(size, _tempTextures[currentTexIndex], tk2dBaseSprite.Anchor.MiddleCenter);
                _titleTexture.gameObject.name = "Texture : " + name;
                _titleTexture.GetComponent<tk2dBaseSprite>().color = new Color(_titleTexture.GetComponent<tk2dBaseSprite>().color.r, _titleTexture.GetComponent<tk2dBaseSprite>().color.g, _titleTexture.GetComponent<tk2dBaseSprite>().color.b, 1f);
                _titleTexture.gameObject.SetActive(true);
            }
        }
    }
  

    public void OnClickChangeTexture()
    {
       /* _cnt++;

        if(_cnt >= _tempTextures.Length)
            _cnt = 0;

        tk2dSpriteCollectionSize size = new tk2dSpriteCollectionSize();
        size.pixelsPerMeter = 1;
        _titleTexture.Create(size, _tempTextures[_cnt], tk2dBaseSprite.Anchor.MiddleCenter);
        _titleTexture.gameObject.name = "Texture : " + name;
        _titleTexture.gameObject.SetActive(true);
        _titleTexture.GetComponent<tk2dBaseSprite>().color = new Color(_titleTexture.GetComponent<tk2dBaseSprite>().color.r, _titleTexture.GetComponent<tk2dBaseSprite>().color.g, _titleTexture.GetComponent<tk2dBaseSprite>().color.b, 1f);
        _titleTexture.gameObject.SetActive(true);*/

    }

    public void OnClickBuyPackageMerchandise()
    {
        //Debug.Log(" !!!!!! OnClickBuyPackageMerchandise !!!!! ");

        string productID = string.Empty;
        string productName = string.Empty;

        int popupShopKey = -1;

        int currentCnt = (_cnt - 1);

        if (currentCnt < 0)
            currentCnt = 0;

        switch (currentCnt)
        {
            case 0:
                {
                    productID = "0910047047";
                    productName = LowData._shopLowData.DateInfoDic[int.Parse(productID)].Memo_c;

                    popupShopKey = 9011;
                    break;
                }
            case 1:
                {
                    productID = "0910047048";
                    productName = LowData._shopLowData.DateInfoDic[int.Parse(productID)].Memo_c;

                    popupShopKey = 9012;
                    break;
                }
            case 2:
                {
                    productID = "0910047049";
                    productName = LowData._shopLowData.DateInfoDic[int.Parse(productID)].Memo_c;

                    popupShopKey = 9013;
                    break;
                }
            case 3:
                {
                    productID = "0910047051";
                    productName = LowData._shopLowData.DateInfoDic[int.Parse(productID)].Memo_c;

                    popupShopKey = 9014;
                    break;
                }
            case 4:
                {
                    productID = "0910047054";
                    productName = LowData._shopLowData.DateInfoDic[int.Parse(productID)].Memo_c;

                    popupShopKey = 9015;
                    break;
                }
            case 5:
                {
                    productID = "0910047055";
                    productName = LowData._shopLowData.DateInfoDic[int.Parse(productID)].Memo_c;

                    popupShopKey = 9016;
                    break;
                }
            case 6:
                {
                    productID = "0910047056";
                    productName = LowData._shopLowData.DateInfoDic[int.Parse(productID)].Memo_c;

                    popupShopKey = 9017;
                    break;
                }
            /*
            case 7:
                {
                    productID = "910047058";
                    productName = "누구나 사는 패키지";

                    popupShopKey = 9021;
                    break;
                }*/

        }


        UIManager.instance.OpenCheckPopup(E_CHECK_POPUP_TYPE.YorN, E_LOCALIZE_STRING.BUYING_INAPP_RUBY_DESC, E_LOCALIZE_STRING.BUYING_INAPP_RUBY_SUB_DESC, callbackBuyRubby);
        UIManager.SetLocalizeTerm(UIManager.instance.GetPopup(E_UIPOPUP_TYPE.CheckPopup).GetComponent<CheckPopup>()._desc, E_LOCALIZE_STRING.BUYING_INAPP_RUBY_SUB_DESC);
        string str = UIManager.instance.GetPopup(E_UIPOPUP_TYPE.CheckPopup).GetComponent<CheckPopup>()._desc.text;
        UIManager.instance.GetPopup(E_UIPOPUP_TYPE.CheckPopup).GetComponent<CheckPopup>()._desc.text = string.Format(str, LowData._shopLowData.DateInfoDic[int.Parse(productID)].Memo_c, LowData._shopLowData.DateInfoDic[int.Parse(productID)].PaymentCost_i);

        _productID = productID;
        _productName = productName;
        _poppupShopKey = popupShopKey;
        
    }

    void callbackBuyRubby()
    {
        //Debug.Log(" !!!!! " + _productID + " , " + _productName + " , " + _poppupShopKey);

        WebSender.instance.P_GET_TRANSACTION_ID(onCompleteTID);

        //test
        //OptionSetting.SetInAppPurchaseCnt((_cnt));
    }

        
    void onCompleteTID(JSONObject jsonObj)
    {
        common.Response.GetTIDResult getTIDresult = DataManager.instance.LoadData<common.Response.GetTIDResult>(jsonObj);
        
        if (common.ResultCode.OK == (common.ResultCode)getTIDresult._Code)
        {
           if (null != NativeManager.instance)
                NativeManager.instance.CallPaymentRequest(_productID, _productName, getTIDresult._TID);
        }


        //OnClickClose();
    }


    int getCurrentPopupItemKey()
    {
        int currentPopupItemKey = -1;
        switch(_cnt)
        {
            case 0:
                {
                    currentPopupItemKey = 0910047047;
                    break;
                }

            case 1:
                {
                    currentPopupItemKey = 0910047048;
                    break;
                }

            case 2:
                {
                    currentPopupItemKey = 0910047049;
                    break;
                }

            case 3:
                {
                    currentPopupItemKey = 0910047051;
                    break;
                }

            case 4:
                {
                    currentPopupItemKey = 0910047054;
                    break;
                }

            case 5:
                {
                    currentPopupItemKey = 0910047055;
                    break;
                }

            case 6:
                {
                    currentPopupItemKey = 0910047056;
                    break;
                }
        }

        return currentPopupItemKey;
    }

    public void OnClickClose()
    {
        OptionSetting.SetPackageCloseTime( System.DateTime.Now);

        OnClickExit();
    }

    public void OnClickExit()
    {
        //Debug.Log(" !!!!!! OnClickExit !!!!! ");
        UIManager.instance.RemovePanel(E_UIPANEL_TYPE.PackageShopPanel);
    }

    /*
    void OnGUI()
    {
        GUI.Box(new Rect(0f, 500f, Screen.width, 300f), _message);
    }*/
}
