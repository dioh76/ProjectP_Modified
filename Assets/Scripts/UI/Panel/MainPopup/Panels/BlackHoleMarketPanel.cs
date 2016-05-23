using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using I2.Loc;
using System.Text;


public class BlackHoleMarketPanel : MonoBehaviour 
{
    public GameObject buySuccessRoot;
    public GameObject contentsRoot;
    public GameObject buyButtons;
    public Text _buyMoreBtnDesc;
    public Text _buyMorecost;
    public tk2dSprite _buyMorecostType;

    public GameObject newItemRoot;
    public GameObject itemPrefab;

    public BlackHoleMarketContent normalContent;
    public BlackHoleMarketContent rareContent;
    public BlackHoleMarketContent eventContent;

    public tk2dSpriteFromTexture _bgTex;
    public tk2dSpriteFromTexture _normalShopIcon;
    public tk2dSpriteFromTexture _normalShopBand;
    public tk2dSpriteFromTexture _rareShopIcon;
    public tk2dSpriteFromTexture _rareShopBand;

    public SkeletonAnimation _chestBoxSpine;
    public ParticleSystem _explosionParticle;

    public ParticleSystem _starParticle;
    public ParticleSystem _summonItemParticle;

    public Text _bonusItemDesc;
    public Text _boxTouchDesc;


    private const int _normalShop_key_1   = 2101;
    private const int _rareShop_key_1     = 2201;
    private const int _eventShop_key_1    = 2301;

    private const int _normalShop_key_10    = 2102;
    private const int _rareShop_key_10      = 2202;
    private const int _eventShop_key_10     = 2302;

    
    private int _currentPriceType;
    private int _currentPrice;

    //private List<common.Data.Item> _itemLists;
    private List<ItembundleLowData.DataInfo> _itemLists;

    private ItembundleLowData.DataInfo _bonusItem;

    private List<ParticleSystem> _particleLists;
    private bool _reBuy;
    public event System.Action OnBack;



    private bool _isBuyDelay;

    public void LoadPanel()
    {
        UIManager.instance.StartLoading();

        if (null != _bonusItemDesc)
            UIManager.SetLocalizeTerm(_bonusItemDesc, E_LOCALIZE_STRING.GachaBonusItem_Desc);

        if (null != _boxTouchDesc)
            UIManager.SetLocalizeTerm(_boxTouchDesc, E_LOCALIZE_STRING.BLACKHOLEMARKET_TOUCH_TREASURE_BOX_DESC);

        _itemLists = new List<ItembundleLowData.DataInfo>();

        normalContent.SetContent(E_SHOP_TYPE.NORMAL_SHOP, _normalShop_key_1, _normalShop_key_10, OnClickBUY_ONE_Normal, OnClickBUY_TEN_Normal);
        rareContent.SetContent(E_SHOP_TYPE.RARE_SHOP, _rareShop_key_1, _rareShop_key_10, OnClickBUY_ONE_Rare, OnClickBUY_TEN_Rare);
        eventContent.SetContent(E_SHOP_TYPE.TIME_LIMIE_SHOP, _eventShop_key_1, _eventShop_key_10, OnClickBUY_ONE_LIMIT, OnClickBUY_TEN_LIMIT);

        setInitalizeContents();
        setInitializeParticle();

        UIManager.instance.EndLoading();
    }

    public void Init()
    {
        _isBuyDelay = false;

        transform.localPosition = new Vector3(0f, 0f, -50f);

        UIManager.instance.GetPanel( E_UIPANEL_TYPE.UIMoveTab ).GetComponent<UIMoveTab>().OnBack += SelectTab;

        normalContent.UpdateContent(E_SHOP_TYPE.NORMAL_SHOP);
        rareContent.UpdateContent(E_SHOP_TYPE.RARE_SHOP);
    }


    /// 일반 가챠 네트워크 함수
    
    #region 일반 가챠
    
    //1개
    public void OnClickBUY_ONE_Normal(int priceType, int price)
    {
        if (true == DataManager.instance.IsItemInvenFull())
        {
            UIManager.instance.OpenCheckPopup(E_CHECK_POPUP_TYPE.Confirm, E_LOCALIZE_STRING.SpaceShipItemInven_Desc, E_LOCALIZE_STRING.INVENTORY_STORAGE_IS_FULL_DESC, callBackOnClick);
            return;
        }

        if (true == _isBuyDelay)
            return;

        _isBuyDelay = true;

        //일반 1개 뽑기
        _currentPriceType = priceType;
                
        //Debug.Log(" !!!!!! " + DataManager.instance.netdata.player._GachaBoard._LatestUseFreeNormalDate + " , " + isPossibleToFreeBuy(E_SHOP_TYPE.NORMAL_SHOP));


        if (true == isPossibleToFreeBuy(E_SHOP_TYPE.NORMAL_SHOP))
        {
            _currentPrice = -1;

            //Debug.Log(" !!!!!! 무료 구매 !!!!!! ");

            UIManager.instance.StartLoading();

            WebSender.instance.P_BUY_GACHASHOPITEM_ONETIME(common.BUY_TYPE.FREE, common.GACHA_TYPE.NORMAL, buyNormalGachaShopItemOneTimeCallBack);
        }
        else
        {
            //Debug.Log(" !!!!!! 유료 구매 !!!!!! ");
            
            _currentPrice = LowData._GachashopLowData.DataInfoDic[_normalShop_key_1].PriceValue_i;

            if (DataManager.instance.netdata.player._Asset._Gold >= _currentPrice)
            {
                UIManager.instance.StartLoading();

                WebSender.instance.P_BUY_GACHASHOPITEM_ONETIME(common.BUY_TYPE.BUY, common.GACHA_TYPE.NORMAL, buyNormalGachaShopItemOneTimeCallBack);
            }
            else
                setOpenErrorPopup(common.ResultCode.NOT_ENOUGH);
        }
      
    }
    
    void buyNormalGachaShopItemOneTimeCallBack(JSONObject jsonObj)
    {
        common.Response.BuyGachaShopItemOneTimeResult buyGachaShopItemOneTimeResult = DataManager.instance.LoadData<common.Response.BuyGachaShopItemOneTimeResult>(jsonObj);

        if (common.ResultCode.OK != (common.ResultCode)buyGachaShopItemOneTimeResult._Code)
        {
            setOpenErrorPopup((common.ResultCode)buyGachaShopItemOneTimeResult._Code);
            return;
        }

        //Debug.Log(" !!!!!! " + buyGachaShopItemOneTimeResult._GachaShopBonusItemBundle + " , " + buyGachaShopItemOneTimeResult._GachaShopItemBundle + " , " + buyGachaShopItemOneTimeResult._GachaShopCountBonusItemBundle);

        if (true == LowData._ItembundleLowData.DataInfoDic.ContainsKey(buyGachaShopItemOneTimeResult._GachaShopBonusItemBundle))
        {
            ItembundleLowData.DataInfo bonusItem = LowData._ItembundleLowData.DataInfoDic[buyGachaShopItemOneTimeResult._GachaShopBonusItemBundle];

            if (null != bonusItem)
            {
                //Debug.Log(" !!!!! BONUS ITEM : " + buyGachaShopItemOneTimeResult._GachaShopBonusItemBundle + " , " + bonusItem.ItemIndex_i + " , " + bonusItem.BubdleValue_i);
                _bonusItem = bonusItem;
            }
        }
        /*else
            Debug.Log(" !!!!! 없는 아이템 번들 키 " + buyGachaShopItemOneTimeResult._GachaShopBonusItemBundle);*/

        //Debug.Log(" !!!!! " + buyGachaShopItemOneTimeResult._GachaShopItemBundle + " , " + buyGachaShopItemOneTimeResult._GachaShopCountBonusItemBundle);

        if (true == LowData._ItembundleLowData.DataInfoDic.ContainsKey(buyGachaShopItemOneTimeResult._GachaShopItemBundle))
        {
            ItembundleLowData.DataInfo newItem = LowData._ItembundleLowData.DataInfoDic[buyGachaShopItemOneTimeResult._GachaShopItemBundle];

            if (null != newItem)
            {
                //Debug.Log(" !!!!! " + newItem.ItemIndex_i + " , " + newItem.BubdleValue_i);
                _itemLists.Add(newItem);
            }

        }
        /*else
        {
            Debug.Log(" !!!!! 없는 아이템 번들 키 " + buyGachaShopItemOneTimeResult._GachaShopItemBundle);
        }*/

        if (true == LowData._ItembundleLowData.DataInfoDic.ContainsKey(buyGachaShopItemOneTimeResult._GachaShopCountBonusItemBundle))
        {
            ItembundleLowData.DataInfo newItem = LowData._ItembundleLowData.DataInfoDic[buyGachaShopItemOneTimeResult._GachaShopCountBonusItemBundle];

            if (null != newItem)
            {
                //Debug.Log(" !!!!! " + newItem.ItemIndex_i + " , " + newItem.BubdleValue_i);
                _itemLists.Add(newItem);
            }
        }
        /*else
        {
            Debug.Log(" !!!!! 없는 아이템 번들 키 " + buyGachaShopItemOneTimeResult._GachaShopCountBonusItemBundle);
        }
        */
        //Debug.Log(" !!!!!!!!!! " + buyGachaShopItemOneTimeResult._GachaShopBonusItemBundle + " , " + buyGachaShopItemOneTimeResult._GachaShopCountBonusItemBundle + " , " + buyGachaShopItemOneTimeResult._GachaShopItemBundle);

        //재화 차감
        if (_currentPrice != -1)
        {
            DataManager.instance.UseMoney(_currentPriceType, _currentPrice);

            setCustomTagActivityTracking(E_SHOP_TYPE.NORMAL_SHOP);
                
        }
        else
        {
            checkAchievementSuccess();

            DataManager.instance.UseFreeGachaCount(0);
        }

        setItemPrefab();

        setVisibleContents(3);

        UIManager.instance.EndLoading();
    }


    //10개
    public void OnClickBUY_TEN_Normal(int priceType, int price)
    {
        if (true == DataManager.instance.IsItemInvenFull())
        {
            UIManager.instance.OpenCheckPopup(E_CHECK_POPUP_TYPE.Confirm, E_LOCALIZE_STRING.SpaceShipItemInven_Desc, E_LOCALIZE_STRING.INVENTORY_STORAGE_IS_FULL_DESC, callBackOnClick);
            return;
        }

        if (true == _isBuyDelay)
            return;

        _isBuyDelay = true;

        //일반 10개뽑기
        _currentPriceType = priceType;
        _currentPrice = LowData._GachashopLowData.DataInfoDic[_normalShop_key_10].PriceValue_i;

        if (DataManager.instance.netdata.player._Asset._Gold >= _currentPrice)
        {
            UIManager.instance.StartLoading();

            WebSender.instance.P_BUY_GACHASHOPITEM_TENTIME(common.GACHA_TYPE.NORMAL, buyNormalGachaShopItemTenTimeCallBack);
        }
        else
            setOpenErrorPopup(common.ResultCode.NOT_ENOUGH);

    }

    void buyNormalGachaShopItemTenTimeCallBack(JSONObject jsonObj)
    {
        common.Response.BuyGachaShopItemTenTimesResult buyGachaShopOtemTenTimesResult = DataManager.instance.LoadData<common.Response.BuyGachaShopItemTenTimesResult>(jsonObj);

        if (common.ResultCode.OK != (common.ResultCode)buyGachaShopOtemTenTimesResult._Code)
        {
            setOpenErrorPopup((common.ResultCode)buyGachaShopOtemTenTimesResult._Code);
            return;
        }

        //Debug.Log(" !!!!! " + buyGachaShopOtemTenTimesResult._GachaShopBonusItemBundle + " , " + buyGachaShopOtemTenTimesResult._GachaShopCountBonusItemBundle + " , " + buyGachaShopOtemTenTimesResult._GachaShopItemBundledic.Count);

        if (true == LowData._ItembundleLowData.DataInfoDic.ContainsKey(buyGachaShopOtemTenTimesResult._GachaShopBonusItemBundle))
        {
            //Debug.Log(" !!!! 보너스 아이템 Exist !!!!! ");

            ItembundleLowData.DataInfo bonusItem = LowData._ItembundleLowData.DataInfoDic[buyGachaShopOtemTenTimesResult._GachaShopBonusItemBundle];

            if (null != bonusItem)
            {
                //Debug.Log(" !!!!! BONUS ITEM : " + buyGachaShopOtemTenTimesResult._GachaShopBonusItemBundle + " , " + bonusItem.Index_i + " , " + bonusItem.ItemIndex_i + " , " + bonusItem.BubdleValue_i);
                _bonusItem = bonusItem;
            }

        }

        if(true == LowData._ItembundleLowData.DataInfoDic.ContainsKey(buyGachaShopOtemTenTimesResult._GachaShopCountBonusItemBundle))
        {
            ItembundleLowData.DataInfo newItem = LowData._ItembundleLowData.DataInfoDic[buyGachaShopOtemTenTimesResult._GachaShopCountBonusItemBundle];

            if(null != newItem)
                _itemLists.Add(newItem);
            
        }

        for (int i = 0; i < buyGachaShopOtemTenTimesResult._GachaShopItemBundledic.Count; i++)
        {
            if (true == LowData._ItembundleLowData.DataInfoDic.ContainsKey(buyGachaShopOtemTenTimesResult._GachaShopItemBundledic[i]))
            {
                //Debug.Log(" !!!!! " + i + " , " + buyGachaShopOtemTenTimesResult._GachaShopItemBundledic[i]);

                ItembundleLowData.DataInfo newItem = LowData._ItembundleLowData.DataInfoDic[buyGachaShopOtemTenTimesResult._GachaShopItemBundledic[i]];

                if (null != newItem)
                {
                    //Debug.Log(" !!!!! NEW ITEM : " + buyGachaShopOtemTenTimesResult._GachaShopBonusItemBundle + " , " + newItem.Index_i + " , " + newItem.ItemIndex_i + " , " + newItem.BubdleValue_i);
                    _itemLists.Add(newItem);
                }
            }
            /*else
            {
                Debug.Log(" !!!!! 없는 번들 아이템 인덱스: " + buyGachaShopOtemTenTimesResult._GachaShopItemBundledic[i]);
            }*/

        }

        //재화 차감
        DataManager.instance.UseMoney(_currentPriceType, _currentPrice);
        setCustomTagActivityTracking(E_SHOP_TYPE.NORMAL_SHOP);

        setItemPrefab();

        setVisibleContents(3);

        UIManager.instance.EndLoading();

    }

   
    #endregion

    /// 유니크 가챠 네트워크 함수

    #region 유니크 가챠

    //1개
    public void OnClickBUY_ONE_Rare( int priceType, int price )
    {
        if (true == DataManager.instance.IsItemInvenFull())
        {
            UIManager.instance.OpenCheckPopup(E_CHECK_POPUP_TYPE.Confirm, E_LOCALIZE_STRING.SpaceShipItemInven_Desc, E_LOCALIZE_STRING.INVENTORY_STORAGE_IS_FULL_DESC, callBackOnClick);
            return;
        }

        if (true == _isBuyDelay)
            return;

        _isBuyDelay = true;

        //희귀 1개뽑기
        _currentPriceType = priceType;

        //long ticks = TimeManager.Instance.GetTime(E_TIMER_TYPE.FREE_RUBY_GACHA_TIME);

        //Debug.Log(" !!!!! " + ticks);

        //Debug.Log(" !!!!!! " + DataManager.instance.netdata.player._GachaBoard._LatestUseFreePremiumGachaDate + " , " + DataManager.instance.netdata.player._GachaBoard._LeftBonusPremiumGachaCount);


        if(true == isPossibleToFreeBuy(E_SHOP_TYPE.RARE_SHOP))
        {
            //Debug.Log(" !!!!!! 무료 구매 !!!!!! ");

            _currentPrice = -1;

            UIManager.instance.StartLoading();

            WebSender.instance.P_BUY_GACHASHOPITEM_ONETIME(common.BUY_TYPE.FREE, common.GACHA_TYPE.PREMIUM, buyRareGachaShopItemOneTimeCallBack);
        }
        else
        {
            //Debug.Log(" !!!!!! 유료 구매 !!!!!! ");

            _currentPrice = LowData._GachashopLowData.DataInfoDic[_rareShop_key_1].PriceValue_i;
            
            //예외처리
            if (DataManager.instance.netdata.player._Asset._Ruby >= _currentPrice)
            {
                UIManager.instance.StartLoading();

                WebSender.instance.P_BUY_GACHASHOPITEM_ONETIME(common.BUY_TYPE.BUY, common.GACHA_TYPE.PREMIUM, buyRareGachaShopItemOneTimeCallBack);
            }
            else
                setOpenErrorPopup(common.ResultCode.NOT_ENOUGH);
        }

    }

    void buyRareGachaShopItemOneTimeCallBack(JSONObject jsonObj)
    {
        common.Response.BuyGachaShopItemOneTimeResult buyGachaShopItemOneTimeResult = DataManager.instance.LoadData<common.Response.BuyGachaShopItemOneTimeResult>(jsonObj);

        if (common.ResultCode.OK != (common.ResultCode)buyGachaShopItemOneTimeResult._Code)
        {
            setOpenErrorPopup((common.ResultCode)buyGachaShopItemOneTimeResult._Code);
            return;
        }

        //Debug.Log(" !!!!! " + buyGachaShopItemOneTimeResult._GachaShopBonusItemBundle + " , " + buyGachaShopItemOneTimeResult._GachaShopItemBundle + " , " + buyGachaShopItemOneTimeResult._GachaShopCountBonusItemBundle);

        if (true == LowData._ItembundleLowData.DataInfoDic.ContainsKey(buyGachaShopItemOneTimeResult._GachaShopBonusItemBundle))
        {
            //Debug.Log(" !!!! 보너스 아이템 Exist !!!!! ");

            ItembundleLowData.DataInfo bonusItem = LowData._ItembundleLowData.DataInfoDic[buyGachaShopItemOneTimeResult._GachaShopBonusItemBundle];

            if (null != bonusItem)
            {
                //Debug.Log(" !!!!! BONUS ITEM : " + buyGachaShopItemOneTimeResult._GachaShopBonusItemBundle + " , " + bonusItem.Index_i + " , " + bonusItem.ItemIndex_i + " , " + bonusItem.BubdleValue_i);
                _bonusItem = bonusItem;
            }

        }

        //Debug.Log(" !!!!! " + buyGachaShopItemOneTimeResult._GachaShopItemBundle + " , " + buyGachaShopItemOneTimeResult._GachaShopCountBonusItemBundle);

        if (true == LowData._ItembundleLowData.DataInfoDic.ContainsKey(buyGachaShopItemOneTimeResult._GachaShopItemBundle))
        {
            //Debug.Log(" !!!!! " + buyGachaShopItemOneTimeResult._GachaShopItemBundle);

            ItembundleLowData.DataInfo newItem = LowData._ItembundleLowData.DataInfoDic[buyGachaShopItemOneTimeResult._GachaShopItemBundle];
            if (null != newItem)
                _itemLists.Add(newItem);
        }

        if (true == LowData._ItembundleLowData.DataInfoDic.ContainsKey(buyGachaShopItemOneTimeResult._GachaShopCountBonusItemBundle))
        {
            ItembundleLowData.DataInfo newItem = LowData._ItembundleLowData.DataInfoDic[buyGachaShopItemOneTimeResult._GachaShopCountBonusItemBundle];

            if (null != newItem)
                _itemLists.Add(newItem);
        }

        //재화 차감
        if (_currentPrice != -1)
        {
            DataManager.instance.UseMoney(_currentPriceType, _currentPrice);

            setCustomTagActivityTracking(E_SHOP_TYPE.RARE_SHOP);
        }
        else
        {
            DataManager.instance.UseFreeGachaCount(1);

            checkAchievementSuccess();
        }

        setItemPrefab();

        setVisibleContents(3);

        UIManager.instance.EndLoading();
    }

    void checkAchievementSuccess()
    {
        int spaceshipAcievementID = 7009;
        int achievementIndex = -1;

        //Debug.Log(" !!!!! " + DataManager.instance.isPossibleToCompleteAchievement(spaceshipAcievementID, out achievementIndex));

        //완료가 가능 하면....
        if (true == DataManager.instance.isPossibleToCompleteAchievement(spaceshipAcievementID, out achievementIndex))
            WebSender.instance.P_REQUEST_ACHIEVEMENT_COMPLETE(DataManager.instance.netdata.player._Achievement[achievementIndex]._UID, 1, onAchievementComplete);

    }

    void onAchievementComplete(JSONObject jsonObj)
    {
        int code = (int)jsonObj["_Code"].n;

        if (common.ResultCode.OK != (common.ResultCode)code)
        {
            //UIManager.instance.OpenCheckPopup(E_CHECK_POPUP_TYPE.Confirm, E_LOCALIZE_STRING.ACHIEVEMENT_COMPLETE_DESC, E_LOCALIZE_STRING.ACHIEVEMENT_COMPLETE_FAIL);
        }
        else
        {
            //UIManager.instance.OpenCheckPopup(E_CHECK_POPUP_TYPE.Confirm, " 업적 체크", " 업적 완료 성공");

            //WebSender.instance.P_GET_PLAYERDATA(GetPlayerCallBack);
        }

    }

    //10개
    public void OnClickBUY_TEN_Rare( int priceType, int price )
    {
        if (true == DataManager.instance.IsItemInvenFull())
        {
            UIManager.instance.OpenCheckPopup(E_CHECK_POPUP_TYPE.Confirm, E_LOCALIZE_STRING.SpaceShipItemInven_Desc, E_LOCALIZE_STRING.INVENTORY_STORAGE_IS_FULL_DESC, callBackOnClick);
            return;
        }


        if (true == _isBuyDelay)
            return;

        _isBuyDelay = true;

        //희귀 10개뽑기
        _currentPriceType = priceType;
        _currentPrice = LowData._GachashopLowData.DataInfoDic[_rareShop_key_10].PriceValue_i;
        
        //예외처리
        if (DataManager.instance.netdata.player._Asset._Ruby >= _currentPrice)
        {
            UIManager.instance.StartLoading();

            WebSender.instance.P_BUY_GACHASHOPITEM_TENTIME(common.GACHA_TYPE.PREMIUM, buyRareGachaShopItemTenTimeCallBack);
        }
        else
            setOpenErrorPopup(common.ResultCode.NOT_ENOUGH);
        
    }

    void buyRareGachaShopItemTenTimeCallBack(JSONObject jsonObj)
    {
        common.Response.BuyGachaShopItemTenTimesResult buyGachaShopOtemTenTimesResult = DataManager.instance.LoadData<common.Response.BuyGachaShopItemTenTimesResult>(jsonObj);

        if (common.ResultCode.OK != (common.ResultCode)buyGachaShopOtemTenTimesResult._Code)
        {
            setOpenErrorPopup((common.ResultCode)buyGachaShopOtemTenTimesResult._Code);
            return;
        }

        //Debug.Log(" !!!!! " +  buyGachaShopOtemTenTimesResult._GachaShopBonusItemBundle + " , " + buyGachaShopOtemTenTimesResult._GachaShopCountBonusItemBundle + " , " + buyGachaShopOtemTenTimesResult._GachaShopItemBundledic.Count);

        if (true == LowData._ItembundleLowData.DataInfoDic.ContainsKey(buyGachaShopOtemTenTimesResult._GachaShopBonusItemBundle))
        {
            //Debug.Log(" !!!! 보너스 아이템 Exist !!!!! ");

            ItembundleLowData.DataInfo bonusItem = LowData._ItembundleLowData.DataInfoDic[buyGachaShopOtemTenTimesResult._GachaShopBonusItemBundle];

            //Debug.Log(" !!!!! " + bonusItem.Index_i + " , " + bonusItem.ItemIndex_i + " , " + bonusItem.BubdleValue_i);

            if (null != bonusItem)
            {
                //Debug.Log(" !!!!! BONUS ITEM : " + buyGachaShopOtemTenTimesResult._GachaShopBonusItemBundle + " , " + bonusItem.Index_i + " , " + bonusItem.ItemIndex_i + " , " + bonusItem.BubdleValue_i);
                _bonusItem = bonusItem;
            }
        }

        if (true == LowData._ItembundleLowData.DataInfoDic.ContainsKey(buyGachaShopOtemTenTimesResult._GachaShopCountBonusItemBundle))
        {
            ItembundleLowData.DataInfo newItem = LowData._ItembundleLowData.DataInfoDic[buyGachaShopOtemTenTimesResult._GachaShopCountBonusItemBundle];

            //Debug.Log(" !!!!! " + newItem.ItemIndex_i);

            if (null != newItem)
                _itemLists.Add(newItem);

        }

        for (int i = 0; i < buyGachaShopOtemTenTimesResult._GachaShopItemBundledic.Count; i++)
        {
            if (true == LowData._ItembundleLowData.DataInfoDic.ContainsKey(buyGachaShopOtemTenTimesResult._GachaShopItemBundledic[i]))
            {
                //Debug.Log(" !!!!! " + i + " , " +  buyGachaShopOtemTenTimesResult._GachaShopItemBundledic[i]);

                ItembundleLowData.DataInfo newItem = LowData._ItembundleLowData.DataInfoDic[buyGachaShopOtemTenTimesResult._GachaShopItemBundledic[i]];

                //Debug.Log(" !!!!! " + i + " , " + newItem.ItemIndex_i);

                if (null != newItem)
                {
                    //Debug.Log(" !!!!! " + buyGachaShopOtemTenTimesResult._GachaShopItemBundledic[i] + " , " + newItem.Index_i + " , " + newItem.ItemIndex_i + " , " + newItem.BubdleValue_i);
                    _itemLists.Add(newItem);
                }
            }
            else
            {
                Debug.Log(" !!!!! 없는 번들 아이템 인덱스: " + buyGachaShopOtemTenTimesResult._GachaShopItemBundledic[i]);
            }
        }

        //재화 차감
        DataManager.instance.UseMoney(_currentPriceType, _currentPrice);

        setCustomTagActivityTracking(E_SHOP_TYPE.RARE_SHOP);

        setItemPrefab();

        setVisibleContents(3);

        UIManager.instance.EndLoading();

    }

    #endregion


    /// 기간 한정 가챠 네트워크 함수

    #region 기간 한정 가챠

    //1개
    public void OnClickBUY_ONE_LIMIT( int priceType, int price )
    {
        //기간한정 1개 뽑기
        _currentPriceType = priceType;
        _currentPrice = price;
    }

    public void buy_event_shop_one_time_callback(JSONObject jsonObj)
    {
        int code = (int)jsonObj["_Code"].n;

        if (common.ResultCode.OK != (common.ResultCode)code)
        {
            setOpenErrorPopup((common.ResultCode)code);
            return;
        }
    }

    //10개
    public void OnClickBUY_TEN_LIMIT( int priceType, int price )
    {
        //기간 한정 10개 뽑기
        _currentPriceType = priceType;
        _currentPrice = price;
    }

    public void buy_event_shop_ten_time_callback(JSONObject jsonObj)
    {
        int code = (int)jsonObj["_Code"].n;

        if (common.ResultCode.OK != (common.ResultCode)code)
        {
            setOpenErrorPopup((common.ResultCode)code);
            return;
        }
    }

    #endregion


    public void OnClickBuyMore()
    {
        //Issue 9 - Check Asset for one more click: show error popup (2016-05-23)
        if (_currentPriceType == 1)
        {
            if (DataManager.instance.netdata.player._Asset._Gold < _currentPrice)
            {
                setOpenErrorPopup(common.ResultCode.NOT_ENOUGH);
                return;
            }
        }
        else if (_currentPriceType == 2)
        {
            if (DataManager.instance.netdata.player._Asset._Ruby < _currentPrice)
            {
                setOpenErrorPopup(common.ResultCode.NOT_ENOUGH);
                return;
            }
        }        

        _reBuy = true;
        clearNewItems();
        resetEffect();

        _chestBoxSpine.state.ClearTracks();
        _chestBoxSpine.Reset();
        _chestBoxSpine.state.SetAnimation(0, "01_2_StateLoop", false).Event += OnEndEvent;

        int resultIndex = getCurrentGachaType();

        sendBuyMoreGacha(resultIndex, _currentPriceType, _currentPrice);

    }

    public void OnClickBuyComplete()
    {
        clearNewItems();

        normalContent.UpdateContent (E_SHOP_TYPE.NORMAL_SHOP);
        rareContent.UpdateContent(E_SHOP_TYPE.RARE_SHOP);
        //eventContent.UpdateContent(E_SHOP_TYPE.TIME_LIMIE_SHOP);

        _reBuy = false;
        setInitalizeContents();

        //켜줌
        UIManager.instance.GetPanel(E_UIPANEL_TYPE.UIHostMenu).GetComponent<UIHostMenu>().gameObject.SetActive(true);
        UIManager.instance.GetPanel(E_UIPANEL_TYPE.UIMoveTab).GetComponent<UIMoveTab>().gameObject.SetActive(true);
        
    }
    
    public void OnClickBackButton()
    {
        if ( gameObject.activeSelf == true )
        {
            OnClickBuyComplete();
            this.transform.localPosition = new Vector3( 3000f, 0f, this.transform.localPosition.z );
        }
        UIManager.instance.GetPanel( E_UIPANEL_TYPE.UIMoveTab ).GetComponent<UIMoveTab>().HomeMoveTab();
       // UIManager.instance.GetPanel( E_UIPANEL_TYPE.UIMoveTab ).GetComponent<UIMoveTab>().OnBack -= OnClickBackButton;
        
    }
    public void SelectTab()
    {
        if ( OnBack != null ) OnBack();

        this.gameObject.SetActive( false );

        UIManager.instance.GetPanel( E_UIPANEL_TYPE.UIMoveTab ).GetComponent<UIMoveTab>().OnBack -= SelectTab;

    }
    
    public void OnClickChestBox()
    {
        _chestBoxSpine.state.SetAnimation(0, "01_3_Open", false).Event += OnEndEvent;
        _boxTouchDesc.gameObject.SetActive(false);
        _chestBoxSpine.GetComponent<BoxCollider>().enabled = false;
        //사운드 수정
        SoundManager.Instance.Play( SoundNameList.GachaOpen );
    }


    // 구매 성공 후 아이템 셋팅 함수

    #region 구매 성공 후 아이템 셋팅 함수

    void setItemPrefab()
    {
        float x = 0f;
        float y = 0f;
        float w = itemPrefab.GetComponent<BoxCollider>().size.x + 30f;
        float h = itemPrefab.GetComponent<BoxCollider>().size.y + 50f;

        for (int i = 0; i < _itemLists.Count; i++)
        {
            if ((i % 4) == 0)
            {
                y -= h;
                x = w;
            }
            else
                x += w;

            GameObject newItem = Object.Instantiate(itemPrefab) as GameObject;
            newItem.name = string.Format("newItem_{0}", i);
            newItem.transform.parent = newItemRoot.transform;
            newItem.transform.localScale = Vector3.one;
            newItem.transform.localPosition = new Vector3(x, y, 0);
            if(_particleLists[i] != null)
                _particleLists[i].transform.localPosition = newItem.transform.localPosition;

            setNewItem(newItem, i);

        }

        //위치
        float reversX = 0f;
        int itemIndex = -1;
        if (_itemLists.Count > 4 )
        {
            //Debug.Log(" !!!!! " + _itemLists.Count);
            /*
            if(_itemLists.Count > 10)
                itemIndex = (_particleLists.Count + (_itemLists.Count - 4));
            else
                itemIndex = (_particleLists.Count + (_itemLists.Count - 3));*/

            itemIndex = (_particleLists.Count + (_itemLists.Count - ((_itemLists.Count % 4) + 1)));

            reversX = (newItemRoot.transform.GetChild(itemIndex).transform.localPosition.x / 2f) + (w / 2f);
        }
        else
        {
            itemIndex = (_particleLists.Count + (_itemLists.Count - 1));
            reversX = (newItemRoot.transform.GetChild(itemIndex).transform.localPosition.x / 2f) + (w / 2f);
        }

        //Debug.Log(" !!!!! " + newItemRoot.transform.localPosition + " , " + new Vector3(reversX, (-(newItemRoot.transform.localPosition.y) - 90f), 0));

        newItemRoot.transform.localPosition = new Vector3(-reversX, (-y) + 100f , newItemRoot.transform.localPosition.z);

        if(null != _bonusItem)
        {
            GameObject newItem = Object.Instantiate(itemPrefab) as GameObject;
            newItem.name = "bonusItem";
            newItem.transform.parent = newItemRoot.transform;
            newItem.transform.localScale = Vector3.one;

            if (_itemLists.Count > 4)
                newItem.transform.localPosition = new Vector3(reversX, (-(newItemRoot.transform.localPosition.y) - 90f), -2.2f);
            else
                newItem.transform.localPosition = new Vector3(reversX, (-(newItemRoot.transform.localPosition.y) - 100f), -2.2f);
            

            _particleLists[(_itemLists.Count)].transform.localPosition = newItem.transform.localPosition;

            setBonusItem(newItem);
        }
        
        UIManager.instance.GetPanel(E_UIPANEL_TYPE.UIHostMenu).GetComponent<UIHostMenu>().SetHostMenu();

        //일단 갱신
        WebSender.instance.P_GET_PLAYERDATA(GetPlayerCallBack);

    }

    void GetPlayerCallBack(JSONObject jsonObj)
    {
        int code = (int)jsonObj["_Code"].n;

        if (code == (int)common.ResultCode.OK)
            DataManager.instance.SetPlayer(jsonObj);

        //UIManager.instance.GetPanel(E_UIPANEL_TYPE.UIHostMenu).GetComponent<UIHostMenu>().gameObject.SetActive(true);
        UIManager.instance.GetPanel( E_UIPANEL_TYPE.UIMoveTab ).GetComponent<UIMoveTab>().RefreshCheck();

        //업적 완료 되면 팝업 없이 메인에 느낌표 띄워줌
        UIManager.instance.GetMainpanel().ResetAchevementData();
    }

    void setNewItem(GameObject newItem, int index)
    {
        if(false == LowData._ItemLowData.DataInfoDic.ContainsKey(_itemLists[index].ItemIndex_i))
        {
            Debug.Log(" !!!!!!!!!!! 없음 !!!!!!!!!!!! BundleItemIndex : " + _itemLists[index].Index_i + " , ItemIndex : " + _itemLists[index].ItemIndex_i);
            newItem.SetActive(false);
            return;
        }


        UIManager.instance.StartLoading();

        ItemLowData.DataInfo item = LowData._ItemLowData.DataInfoDic[_itemLists[index].ItemIndex_i];

        setItemBG(newItem, item.Itemtype_b);
        setItemFrame(newItem, item.Itemtype_b, item.Categroy_b);
        setItemTexture(newItem, item.ResourceName_c);
        setItemAmount(newItem, _itemLists[index].BubdleValue_i);
        setItemName(newItem, item.ItemName_i);

        UIManager.instance.EndLoading();

        newItem.SetActive(false);
    }

    void setBonusItem(GameObject bonusItem)
    {
        if(false == LowData._ItemLowData.DataInfoDic.ContainsKey(_bonusItem.ItemIndex_i))
        {
            Debug.Log(" !!!!!!!!!!! 없음 !!!!!!!!!!!! bonusItem : " + _bonusItem.ItemIndex_i );
            bonusItem.SetActive(false);
            return;
        }

        UIManager.instance.StartLoading();

        ItemLowData.DataInfo item = LowData._ItemLowData.DataInfoDic[_bonusItem.ItemIndex_i];

        setItemBG(bonusItem, item.Itemtype_b);
        setItemFrame(bonusItem, item.Itemtype_b, item.Categroy_b);
        setItemTexture(bonusItem, item.ResourceName_c);
        setItemAmount(bonusItem, _bonusItem.BubdleValue_i);
        setItemName(bonusItem, item.ItemName_i);

        UIManager.instance.EndLoading();

        bonusItem.SetActive(false);
    }

    void setItemBG(GameObject item, byte itemType)
    {
        string bgName = string.Empty;

        switch (itemType)
        {
            case 1:
                {
                    //영웅조각
                    bgName = "character_bg";
                    break;
                }

            case 2:
                {
                    //물약
                    bgName = "potion_bg";
                    break;
                }

            case 3:
                {
                    //장비
                    //장비 레벨에 따라 바껴야댐
                    bgName = "item_6-9_bg";
                    break;
                }

            case 4:
                {
                    //장비 부품
                    bgName = "item_piece_bg";
                    break;
                }
        }

        item.transform.GetChild(0).GetComponent<tk2dSlicedSprite>().SetSprite(bgName);
    }

    void setItemFrame(GameObject item, byte itemType, byte itemCategory)
    {
        string frameName = string.Empty;

        switch (itemType)
        {
            case 1:
                {
                    //영웅조각
                    //frameName = "character_piece_bg";
                    frameName = "icon_class";
                    break;
                }

            case 2:
                {
                    //물약
                    //frameName = "potion_bg";
                    break;
                }

            case 3:
                {
                    //장비
                    //frameName = "item_6-9_bg";
                    break;
                }

            case 4:
                {
                    //장비 부품
                    frameName = "item_piece_line";
                    break;
                }
        }

        GameObject frame = item.transform.GetChild(1).gameObject;
        if (true == string.IsNullOrEmpty(frameName))
            frame.SetActive(false);
        else
        {
            frame.GetComponent<tk2dSlicedSprite>().SetSprite(frameName);
            frame.GetComponent<tk2dSlicedSprite>().color = new Color(112f / 256f, 112f / 256f, 112f / 256f, 1f);
            frame.transform.GetChild(1).GetComponent<tk2dSprite>().color = frame.GetComponent<tk2dSlicedSprite>().color;

            frame.SetActive(true);

            GameObject jobIcon = frame.transform.GetChild(0).gameObject;
            /*
            if (itemType == 1)
            {
                jobIcon.GetComponent<tk2dSlicedSprite>().SetSprite( "icon_type_piece" );
                jobIcon.SetActive(true);
            }
            else
                jobIcon.SetActive(false);*/

            string iconName = string.Empty;
          
            switch(itemCategory)
            {
                case 1:
                    {
                        iconName = "icon_type_piece";
                        break;
                    }

                case 2:
                    {
                        iconName = "icon_type_potin";
                        break;
                    }
                case 3:
                case 4:
                    {
                        iconName = "icon_type_part";
                        
                        break;
                    }
            }

            jobIcon.GetComponent<tk2dSlicedSprite>().SetSprite(iconName);
            jobIcon.SetActive(true);
        }

    }

    void setItemTexture(GameObject item, string texName)
    {
        UIManager.instance.SetItemTexture(texName, item.transform.GetChild(2).GetComponent<tk2dSpriteFromTexture>(), onTextureLoadComplete);
    }

    void onTextureLoadComplete(bool bComplete)
    {
        /*if (true == bComplete)
            UIManager.instance.EndLoading();*/
    }

    void setItemAmount(GameObject item, int itemAmount)
    {
        UIManager.SetLocaliceAndStringFormat(item.transform.GetChild(3).GetComponent<Text>(), E_LOCALIZE_STRING.Count_Desc, itemAmount.ToString());
    }

    void setItemName(GameObject item, int itemName)
    {
        UIManager.SetLocalizeTerm(item.transform.GetChild(4).GetComponent<Text>(), itemName.ToString());
    }

    #endregion



    void setInitalizeContents()
    {
        buySuccessRoot.SetActive(false);
        contentsRoot.SetActive( true );

        resetEffect();
        reset();
    }

    void setInitializeParticle()
    {
        _particleLists = new List<ParticleSystem>();

        //max 파티클 미리 생성
        int maxItemCnt = 12;
        for (int i = 0; i < maxItemCnt; i++)
        {
            ParticleSystem pcs = Object.Instantiate(_summonItemParticle) as ParticleSystem;
            pcs.transform.SetParent(newItemRoot.transform);
            pcs.transform.localPosition = Vector3.zero;
            _particleLists.Add(pcs);
        }
    }

    void setVisibleContents(int id)
    {
        switch(id)
        {
            case 0:
                {
                    normalContent.OnClicOutsideBtn();
                    buySuccessRoot.SetActive(false);
                    itemPrefab.SetActive(false);
                    break;
                }

            case 1:
                {
                    rareContent.OnClicOutsideBtn();
                    buySuccessRoot.SetActive(false);
                    itemPrefab.SetActive(false);
                    break;
                }

            case 2:
                {
                    eventContent.OnClicOutsideBtn();
                    buySuccessRoot.SetActive(false);
                    itemPrefab.SetActive(false);
                    break;
                }

            case 3:
                {
                    contentsRoot.SetActive( false );
                    buySuccessRoot.SetActive(true);
                    setBuyButton(false);
                    itemPrefab.SetActive(false);

                    if (null != _bonusItemDesc)
                        _bonusItemDesc.gameObject.SetActive(false);

                    StartCoroutine(startEffect());
                    
                    break;
                }
        }
        
    }

    void setBuyButton(bool isActive)
    {
        //추가 구매 버튼
        if (null != _buyMoreBtnDesc)
        {
            if (_itemLists.Count >= 10)
                UIManager.SetLocaliceAndStringFormat(_buyMoreBtnDesc, E_LOCALIZE_STRING.BuyMoreTime_Desc, 10);
            else
                UIManager.SetLocaliceAndStringFormat(_buyMoreBtnDesc, E_LOCALIZE_STRING.BuyMoreTime_Desc, 1);
        }

        if (null != _buyMorecost)
        {
            string freeCnt = string.Empty;

            //Debug.Log(" !!!!! " + _currentPriceType + " , " + _currentPrice);

            if (_currentPriceType == 1)
            {
                //Debug.Log(" !!!!! " + isPossibleToFreeBuy(E_SHOP_TYPE.NORMAL_SHOP));
               
                if (_currentPrice == -1 && true == isPossibleToFreeBuy(E_SHOP_TYPE.NORMAL_SHOP))
                {
                    //무료
                    freeCnt = string.Format("({0}/{1})", DataManager.instance.netdata.player._GachaBoard._FreeNormalGachaCount, LowData._GachashopLowData.DataInfoDic[_normalShop_key_1].DayFreeGachaValue_i);
                    UIManager.SetLocaliceAndStringFormat(_buyMorecost, E_LOCALIZE_STRING.Gacha_FreeCount, freeCnt);
                    _buyMorecost.text = _buyMorecost.text;
                }
                else
                {
                    int price = -1;
                    if (_itemLists.Count >= 10)
                    {
                        price = LowData._GachashopLowData.DataInfoDic[_normalShop_key_10].PriceValue_i;
                        _buyMorecost.text = string.Format("{0:#,0}", price);                            // 10개
                    }
                    else
                    {
                        price = LowData._GachashopLowData.DataInfoDic[_normalShop_key_1].PriceValue_i;
                        _buyMorecost.text = string.Format("{0:#,0}", price);                            // 1개
                    }
                }
            }
            else if (_currentPriceType == 2)
            {
                //Debug.Log(" !!!!! " + isPossibleToFreeBuy(E_SHOP_TYPE.RARE_SHOP));

                if (_currentPrice == -1 && true == isPossibleToFreeBuy(E_SHOP_TYPE.RARE_SHOP))
                {
                    freeCnt = string.Format("({0}/{1})", DataManager.instance.netdata.player._GachaBoard._FreePremiumGachaCount, LowData._GachashopLowData.DataInfoDic[_rareShop_key_1].DayFreeGachaValue_i);
                    UIManager.SetLocaliceAndStringFormat(_buyMorecost, E_LOCALIZE_STRING.Gacha_FreeCount, freeCnt);
                    _buyMorecost.text = _buyMorecost.text;
                }
                else
                {
                    int price = -1;
                    if (_itemLists.Count >= 10)
                    {
                        price = LowData._GachashopLowData.DataInfoDic[_rareShop_key_10].PriceValue_i;
                        _buyMorecost.text = string.Format("{0:#,0}", price);                            // 10개
                    }
                    else
                    {
                        price = LowData._GachashopLowData.DataInfoDic[_rareShop_key_1].PriceValue_i;
                        _buyMorecost.text = string.Format("{0:#,0}", price);                            // 1개
                    }
                }
            }
        }
        if (null != _buyMorecostType)
        {
             if (_currentPriceType == 1)
             {
                 //무료가 연속으로 돌리는 시스템이 아니므로 필요없을듯
                 /*
                 if (_currentPrice == -1 &&  DataManager.instance.netdata.player._GachaBoard._FreeNormalGachaCount > 0)
                {
                    //icon off
                    _buyMorecostType.gameObject.SetActive(false);
                }
                else
                {
                    //icon on
                    _buyMorecostType.gameObject.SetActive(true);
                    _buyMorecostType.SetSprite("icon_gold");
                }*/


                 _buyMorecostType.gameObject.SetActive(true);
                 _buyMorecostType.SetSprite("icon_gold");

             }
             else if(_currentPriceType == 2)
             {
                 /*if (_currentPrice == -1 && DataManager.instance.netdata.player._GachaBoard._FreePremiumGachaCount > 0)
                 {
                     //icon off
                     _buyMorecostType.gameObject.SetActive(false);
                 }
                 else
                 {
                     //icon on
                     _buyMorecostType.gameObject.SetActive(true);
                     _buyMorecostType.SetSprite("icon_ruby");
                 }*/

                 _buyMorecostType.gameObject.SetActive(true);
                 _buyMorecostType.SetSprite("icon_ruby");
             }
        }


        buyButtons.gameObject.SetActive(isActive);
    }

    int getCurrentGachaType()
    {
        int resultIndex = -1;

        if(_currentPriceType == 1)
        {
            //골드(노멀) 가챠
            if(_currentPrice == LowData._GachashopLowData.DataInfoDic[_normalShop_key_1].PriceValue_i)
            {
                //1번
                resultIndex = 1;
            }
            else if(_currentPrice == LowData._GachashopLowData.DataInfoDic[_normalShop_key_10].PriceValue_i)
            {
                //10번
                resultIndex = 2;
            }
            else
            {
                //무료 가챠
                resultIndex = 0;
            }
           
        }
        else if(_currentPriceType == 2)
        {
            //캐쉬(유니크) 가챠
            if(_currentPrice == LowData._GachashopLowData.DataInfoDic[_rareShop_key_1].PriceValue_i)
            {
                //1번
                resultIndex = 4;
            }
            else if(_currentPrice == LowData._GachashopLowData.DataInfoDic[_rareShop_key_10].PriceValue_i)
            {
                //10번
                resultIndex = 5;
            }
            else
            {
                //무료 가챠
                resultIndex = 3;
            }
        }


        return resultIndex;
    }

    void sendBuyMoreGacha(int resultIndex, int shopType, int price)
    {
        switch(resultIndex)
        {
            case 0:
            case 1:
                {
                    //골드
                    //무료 & 유료 1회
                    OnClickBUY_ONE_Normal(shopType, price);
                    
                    break;
                }

            case 2:
                {
                    //유료 10회
                    OnClickBUY_TEN_Normal(shopType, price);
                    break;
                }

            case 3:
            case 4:
                {
                    //캐쉬
                    //무료 & 유료 1회
                    OnClickBUY_ONE_Rare(shopType, price);
                    break;
                }
            case 5:
                {
                    //유료 10회
                    OnClickBUY_TEN_Rare(shopType, price);
                    break;
                }
        }
    }

    void setVisibleNewItems()
    {
        if (null == newItemRoot)
            return;

        StartCoroutine(setVisiblenNewItemEffect());

        StartCoroutine(setVisibleNewItem());

        /*
        for (int i = 0; i < _itemLists.Count; i++)
            _particleLists[i].gameObject.SetActive(false);*/

    }

    void clearNewItems()
    {
        int cnt = _particleLists.Count;
        while (true)
        {
            int maxItemCnt = _particleLists.Count + _itemLists.Count;

            if(null != _bonusItem)
                maxItemCnt += 1;

            if (cnt >= maxItemCnt)
                break;

            GameObject childObj = newItemRoot.transform.GetChild(cnt).gameObject;
            if (null != childObj)
            {
                Object.Destroy(childObj);
                cnt++;
            }
        }

        _bonusItem = null;
        _itemLists.Clear();
    }

    void setOpenErrorPopup(common.ResultCode code)
    {
        //Debug.Log(" !!!!!! " + code);

        //UIManager.instance.OpenCheckPopup(E_CHECK_POPUP_TYPE.Confirm, E_LOCALIZE_STRING.BLACKHOLE_GACHA_DESC, E_LOCALIZE_STRING.BLACKHOLE_GACHA_NOTENOUGH_MONEY, OnBuyFailCallback);
        
        if ((common.ResultCode)code == common.ResultCode.NOT_ENOUGH)
            UIManager.instance.OpenCheckPopup(E_CHECK_POPUP_TYPE.Confirm, E_LOCALIZE_STRING.BLACKHOLE_GACHA_DESC, E_LOCALIZE_STRING.BLACKHOLE_GACHA_NOTENOUGH_MONEY, OnBuyFailCallback);
        else
            UIManager.instance.OpenCheckPopup(E_CHECK_POPUP_TYPE.Confirm, E_LOCALIZE_STRING.BLACKHOLE_GACHA_DESC, E_LOCALIZE_STRING.BLACKHOLE_GACHA_BUY_FAIL, OnBuyFailCallback);


        _isBuyDelay = false;
    }

    void OnBuyFailCallback()
    {
        OnClickBuyComplete();
    }

    void OnEndEvent(Spine.AnimationState state, int trackIndex, Spine.Event e)
    {
        //Debug.Log(" !!!!! " + state + " , " + e.Data.name);

        if (true == e.Data.name.Contains("into_End"))
        {
            _chestBoxSpine.state.ClearTracks();
            _chestBoxSpine.GetComponent<BoxCollider>().enabled = true;

            _chestBoxSpine.state.SetAnimation(0, "01_2_StateLoop", true);

            //사운드 수정
            SoundManager.Instance.Play( SoundNameList.Gacha );

        }
        else if(e.Data.name.Contains("Item_Start_1"))
        {
            _starParticle.gameObject.SetActive(true);
            
            if (null != _explosionParticle)
                _explosionParticle.gameObject.SetActive(true);

            setVisibleNewItems();
            //setBuyButton(true);
        }
        else
        {
            _chestBoxSpine.state.ClearTracks();
            _chestBoxSpine.state.SetAnimation(0, "01_4_Loop", true);
        }
    }

    IEnumerator startEffect()
    {
        UIManager.instance.GetPanel(E_UIPANEL_TYPE.UIHostMenu).GetComponent<UIHostMenu>().gameObject.SetActive(false);
        UIManager.instance.GetPanel(E_UIPANEL_TYPE.UIMoveTab).GetComponent<UIMoveTab>().gameObject.SetActive(false);

        yield return new WaitForSeconds(1f);

        _boxTouchDesc.gameObject.SetActive(true);

        if (false == _reBuy)
        {
            _chestBoxSpine.state.ClearTracks();
            _chestBoxSpine.Reset();

            if (null != _chestBoxSpine)
            {
                _chestBoxSpine.gameObject.SetActive(true);
                _chestBoxSpine.state.SetAnimation(0, "01_1_Into", false).Event += OnEndEvent;
            }

        }
        else
            OnClickChestBox();
        

        yield break;
    }
    
    IEnumerator setVisiblenNewItemEffect()
    {
        int maxLength = _itemLists.Count;

        if (null != _bonusItem)
            maxLength += 1;

        for (int i = 0; i < maxLength; i++)
        {
            _particleLists[i].gameObject.SetActive(true);
            //사운드 수정
            SoundManager.Instance.Play( SoundNameList.GetItem );
            yield return new WaitForSeconds(0.2f);
        }

        if (null != _bonusItem)
        {
            if (null != _bonusItemDesc)
                _bonusItemDesc.gameObject.SetActive(true);
        }
    }
    
    IEnumerator setVisibleNewItem()
    {
        yield return new WaitForSeconds(0.5f);

        int maxLength = _itemLists.Count;

        if (null != _bonusItem)
            maxLength += 1;

        for (int i = 0; i < maxLength; i++)
        {
            GameObject obj = newItemRoot.transform.GetChild((i + _particleLists.Count)).gameObject;

            if (null == obj)
                continue;

            obj.SetActive(true);
            yield return new WaitForSeconds(0.2f);
        }

        UIManager.instance.GetPanel(E_UIPANEL_TYPE.UIHostMenu).GetComponent<UIHostMenu>().gameObject.SetActive(true);
        setBuyButton(true);

        _isBuyDelay = false;

        yield break;
    }

    void resetEffect()
    {
        setBuyButton(false);

        if (false == _reBuy)
            _chestBoxSpine.gameObject.SetActive(false);

        if (null != _explosionParticle)
            _explosionParticle.gameObject.SetActive(false);

        _chestBoxSpine.state.ClearTracks();
        _chestBoxSpine.Reset();
        
        _chestBoxSpine.GetComponent<BoxCollider>().enabled = false;

        if (null != _particleLists)
        {
            for (int i = 0; i < _particleLists.Count; i++)
                _particleLists[i].gameObject.SetActive(false);
        }

        _starParticle.gameObject.SetActive(false);

        newItemRoot.transform.localPosition = new Vector3(0f, 0f, newItemRoot.transform.localPosition.z);

        if (null != _bonusItemDesc)
            _bonusItemDesc.gameObject.SetActive(false);

        _isBuyDelay = false;

    }

    void setCustomTagActivityTracking(E_SHOP_TYPE type)
    {
        if(null != NativeManager.instance)
        {
            string str_customTag = string.Empty;

            if(type == E_SHOP_TYPE.NORMAL_SHOP)
                str_customTag = "b_bu";
            else
                str_customTag = "Pb_bu";

            NativeManager.instance.SetCustomTrackingActivity(str_customTag);
        }
    }

    bool isPossibleToFreeBuy(E_SHOP_TYPE type)
    {
        if(type == E_SHOP_TYPE.NORMAL_SHOP)
        {
            //Debug.Log(" !!!!! " + DataManager.instance.netdata.player._GachaBoard._LatestUseFreeNormalDate);

            if (UtilityDefine.standardTime == DataManager.instance.netdata.player._GachaBoard._LatestUseFreeNormalDate)
            {
                //Debug.Log(" !!!!! " + DataManager.instance.netdata.player._GachaBoard._FreeNormalGachaCount);

                if (DataManager.instance.netdata.player._GachaBoard._FreeNormalGachaCount > 0)
                    return true;

                return false;
            }
            else
            {
                System.DateTime resetTime = DataManager.instance.netdata.player._GachaBoard._LatestUseFreeNormalDate.AddHours(1);
                System.TimeSpan span = (resetTime - System.DateTime.Now);

                //Debug.Log(" !!!!! " + span.Ticks);

                if (span.Ticks < 0)
                {
                    //Debug.Log(" !!!!! " + DataManager.instance.netdata.player._GachaBoard._FreeNormalGachaCount);

                    if (DataManager.instance.netdata.player._GachaBoard._FreeNormalGachaCount > 0)
                        return true;

                    return false;
                }
                    
                return false;

            }
        }
        else
        {
            //Debug.Log(" !!!!! " + DataManager.instance.netdata.player._GachaBoard._LatestUseFreePremiumGachaDate);

            //무료 구매를 할수 있는 시간임?
            if (UtilityDefine.standardTime == DataManager.instance.netdata.player._GachaBoard._LatestUseFreePremiumGachaDate)
            {
                //Debug.Log(" !!!!! " + DataManager.instance.netdata.player._GachaBoard._FreePremiumGachaCount);

                //무료 구매가 가능한 카운트가 있음?
                if (DataManager.instance.netdata.player._GachaBoard._FreePremiumGachaCount > 0)
                    return true;

                return false;
            }
            else
            {
                System.DateTime resetTime = DataManager.instance.netdata.player._GachaBoard._LatestUseFreeNormalDate.AddHours(1);
                System.TimeSpan span = (resetTime - System.DateTime.Now);

                //Debug.Log(" !!!!! " + span.Ticks);
                //무료 구매 가능한 시간임?
                if (span.Ticks < 0)
                {
                    //Debug.Log(" !!!!! " + DataManager.instance.netdata.player._GachaBoard._FreePremiumGachaCount);

                    //무료 구매가 가능한 카운트가 있음?
                    if (DataManager.instance.netdata.player._GachaBoard._FreePremiumGachaCount > 0)
                        return true;

                    return false;
                }
                
                return false;
            }
        }
    }

    
    void callBackOnClick()
    {
        OnClickBuyComplete();

        //우주선 이동
        UIManager.instance.GetPanel(E_UIPANEL_TYPE.UIMoveTab).GetComponent<UIMoveTab>().OnClickTab1();
    }

    void reset()
    {
        _currentPrice = -1;
        _currentPriceType = -1;
    }

}
