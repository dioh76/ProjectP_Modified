using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class AttendPanel : MonoBehaviour 
{
    public Text _titleTex;
    public Text _desc_big;
    public Text _desc_receive;
    public Text _desc_reset;
    public Text _desc_accure;
    public Text _desc_currentAttend;
    public Text _desc_attentCnt;
    public GameObject _packagePopup;
    public Text _popupDesc;

    public AttendScrollManager _scrollManager;

    private int _monthType = -1;

    private List<AttendanceOddLowData.DataInfo> _oddAccureLists;                //짝수달
    private List<AttendanceEvenLowData.DataInfo> _evenAccureLists;              //홀수달

    public AttendPackageReward[] _packageRewardItem;

    public AttendItemInfoSlot[] _packageCompositionItem;                        //패키지 구성 아이템

    private List<int> _packageCompositionIndex = new List<int>();               //패키지 구성 품 위치 인덱스

    private List<RewardItemData> _rewardItems;

    private bool _isLoad = false;

    public void LoadPanel()
    {
        if ((System.DateTime.Now.Month % 2) == 0)
            _monthType = 0;
        else
            _monthType = 1;

        if (null != _titleTex)
            UIManager.SetLocaliceAndStringFormat(_titleTex, E_LOCALIZE_STRING.MonthReward_Desc, System.DateTime.Now.Month);

        if (null != _desc_big)
            UIManager.SetLocalizeTerm(_desc_big, E_LOCALIZE_STRING.EveryAttend_Desc);

        if (null != _desc_receive)
            UIManager.SetLocalizeTerm(_desc_receive, E_LOCALIZE_STRING.AttendReceive_Desc);

        if (null != _desc_reset)
            UIManager.SetLocalizeTerm(_desc_reset, E_LOCALIZE_STRING.AttendReset_Desc);

        if (null != _desc_accure)
            UIManager.SetLocalizeTerm(_desc_accure, E_LOCALIZE_STRING.AttendAccure_Desc);

        if (null != _desc_currentAttend)
            UIManager.SetLocalizeTerm(_desc_currentAttend, E_LOCALIZE_STRING.AttendCurrentMonth_Desc);

        setAttendAccureCount(DataManager.instance.netdata.player._Attend._Reward);

        _packagePopup.gameObject.SetActive(false);

        initAccrueAttendReward();

        if (null != _scrollManager)
        {
            _scrollManager.InitializeScrollManager();

            //일일 출석 셋팅
            _scrollManager.SetAllItems(_monthType);
        }



        _rewardItems = new List<RewardItemData>();

        _isLoad = true;
    }

    public void Init()
    {
        if( !_isLoad )
            LoadPanel();

        transform.localPosition = new Vector3(0f, 0f, -550f);
        UIManager.instance.GetPanel(E_UIPANEL_TYPE.UIMoveTab).GetComponent<UIMoveTab>().OnBack += OnClickBackButton;

        if(null != _rewardItems)
            _rewardItems.Clear();
    }


    public void ResetAttend()
    {
        if ((System.DateTime.Now.Month % 2) == 0)
            _monthType = 0;
        else
            _monthType = 1;

        setAttendAccureCount(DataManager.instance.netdata.player._Attend._Count);

        if (null != _scrollManager)
        {
            //일일 출석 셋팅
            _scrollManager.SetAllItems(_monthType);
        }
    }
 

    void OnClickBackButton()
    {
        UIManager.instance.ClosePanel(E_UIPANEL_TYPE.AttendPanel);
        UIManager.instance.GetPanel(E_UIPANEL_TYPE.UIMoveTab).GetComponent<UIMoveTab>().OnBack -= OnClickBackButton;
    }

    public void OnClickExit()
    {
        OnClickBackButton();
    }

    public void OnPackagePopupReward(int selectItemIndex)
    {
        OnPackagePopup(selectItemIndex, findPackageAttendItem(selectItemIndex));
    }


    public void OnPackagePopup(int accureItemIndex, PackageLowData.DataInfo packageItem)
    {
        addPackageItemLists(packageItem);
        setPackageItem(accureItemIndex, packageItem);

        _packagePopup.gameObject.SetActive(true);
    }


    public void OnClosePackagePopup()
    {
        _packagePopup.gameObject.SetActive(false);
    }
    


    public void OnClickReceiveTodayReward()
    {
        //보상 받기
        WebSender.instance.P_GET_ATTEND_REWARD(onCompleteGetAttendReward);
    }

    void onCompleteGetAttendReward(JSONObject jsonObj)
    {
        common.Response.AttendDataResult attendDataResult = DataManager.instance.LoadData<common.Response.AttendDataResult>(jsonObj);

        if(common.ResultCode.OK != (common.ResultCode)attendDataResult._Code)
        {
            //팝업 에러 처리
            if (common.ResultCode.ALREADY_EXISTS == (common.ResultCode)attendDataResult._Code)
                UIManager.instance.OpenCheckPopup(E_CHECK_POPUP_TYPE.Confirm, E_LOCALIZE_STRING.AttendRewardPopup_Desc, E_LOCALIZE_STRING.AlReadyAttendReward_Desc);
            else
                UIManager.instance.OpenCheckPopup(E_CHECK_POPUP_TYPE.Confirm, E_LOCALIZE_STRING.AttendRewardPopup_Desc, E_LOCALIZE_STRING.ATTEND_REWARD_RECIEVE_FAIL);

        }
        else
        {
            //서버에서 해당 리워드 아이템, 보너스 아이템 전부 자동 지급. 갱신 해야댐
            
            //Debug.Log(" !!!!! " + DataManager.instance.netdata.player._Attend._Count + " , " + DataManager.instance.netdata.player._Attend._Reward);

            int accureRewardKey = getAccureAttendReward(DataManager.instance.netdata.player._Attend._Count);
            int normalRewardKey = getNormalAttendReward(DataManager.instance.netdata.player._Attend._Count);

            //Debug.Log(" !!!!! " + accureRewardKey + " , " + normalRewardKey);

            resetAccureAttendReward();          //누적 출석 보상 아이템수령 체크
            _scrollManager.OnCheckCurrentDay(DataManager.instance.netdata.player._Attend._Count);       //일반 보상 수령 체크

            if(accureRewardKey > 0)
                setAccureAttendRewardItem(accureRewardKey);
            
            if(normalRewardKey > 0)
                setAttendRewardItem(normalRewardKey);

            if (null != NativeManager.instance)
            {
                //유저가 출석체크를 했을 때
                NativeManager.instance.SetCustomTrackingActivity("Dcheck");
            }

            OptionSetting.SetAttendCloseTime(System.DateTime.Now);

            setRewardPopupOpen();

            //일단 갱신 ㅋㅋㅋㅋㅋㅋㅋㅋㅋㅋㅋㅋㅋㅋ
            WebSender.instance.P_GET_PLAYERDATA(GetPlayerCallBack);
        }
    }

    void GetPlayerCallBack(JSONObject jsonObj)
    {
        int code = (int)jsonObj["_Code"].n;

        if (code == (int)common.ResultCode.OK)
            DataManager.instance.SetPlayer(jsonObj);


        UIManager.instance.GetPanel(E_UIPANEL_TYPE.UIHostMenu).GetComponent<UIHostMenu>().SetHostMenu(UIHOST_TYPE.BASIC);

        ResetAttend();
    }

    void setAccureAttendRewardItem(int accureRewardKey)
    {
        //누적 출석 보상 패키지 아이템
        if(_monthType == 0)
        {
            //Debug.Log(" !!!!! " + LowData._attendanceOddLowData.DATAInfoDic[accureRewardKey].AttendanceRewardValue_i);

            int packageItemIndex = LowData._attendanceOddLowData.DATAInfoDic[accureRewardKey].AttendanceRewardValue_i;
            
            if (false == LowData._packageLowData.DATAInfoDic.ContainsKey(packageItemIndex))
            {
                Debug.LogError(" !!!!!! 없는 패키지 아이템 키 :" + packageItemIndex);
                return;
            }

            addAccureRewardLists(LowData._packageLowData.DATAInfoDic[packageItemIndex]);
            
        }
        else
        {
            //Debug.Log(" !!!!! " + LowData._attendanceEvenLowData.DATAInfoDic[accureRewardKey].AttendanceRewardValue_i);

            int packageItemIndex = LowData._attendanceEvenLowData.DATAInfoDic[accureRewardKey].AttendanceRewardValue_i;

            if (false == LowData._packageLowData.DATAInfoDic.ContainsKey(packageItemIndex))
            {
                Debug.LogError(" !!!!!! 없는 패키지 아이템 키 :" + packageItemIndex);
                return;
            }

            addAccureRewardLists(LowData._packageLowData.DATAInfoDic[packageItemIndex]);
        }

    }

    void setAttendRewardItem(int normalRewardKey)
    {
        //일반 출석 보상 번들 아이템
        if (_monthType == 0)
        {
            int bundleItemIndex = LowData._attendanceOddLowData.DATAInfoDic[normalRewardKey].AttendanceRewardValue_i;

            if (false == LowData._ItembundleLowData.DataInfoDic.ContainsKey(bundleItemIndex))
            {
                Debug.LogError(" !!!!!! 없는 번들 아이템 키 :" + bundleItemIndex);
                return;
            }

            addRewardLists(LowData._ItembundleLowData.DataInfoDic[bundleItemIndex]);
        }
        else
        {
            int bundleItemIndex = LowData._attendanceEvenLowData.DATAInfoDic[normalRewardKey].AttendanceRewardValue_i;

            if( false == LowData._ItembundleLowData.DataInfoDic.ContainsKey( bundleItemIndex ) )
            {
                Debug.LogError(" !!!!! 없는 번들 아이템 키 :" + normalRewardKey);
                return;
            }

            addRewardLists(LowData._ItembundleLowData.DataInfoDic[bundleItemIndex]);
        }

    }

    void addAccureRewardLists(PackageLowData.DataInfo packageItemData)
    {
        if(packageItemData.Itembundle01_i > 0)
        {
            if(false == LowData._ItembundleLowData.DataInfoDic.ContainsKey(packageItemData.Itembundle01_i))
            {
                Debug.LogError(" !!!!! 없는 번들 아이템 키 :" + packageItemData.Itembundle01_i);
                return;
            }

            addRewardLists(LowData._ItembundleLowData.DataInfoDic[packageItemData.Itembundle01_i]);
        }

        if (packageItemData.Itembundle02_i > 0)
        {
            if (false == LowData._ItembundleLowData.DataInfoDic.ContainsKey(packageItemData.Itembundle02_i))
            {
                Debug.LogError(" !!!!! 없는 번들 아이템 키 :" + packageItemData.Itembundle02_i);
                return;
            }

            addRewardLists(LowData._ItembundleLowData.DataInfoDic[packageItemData.Itembundle02_i]);
        }

        if (packageItemData.Itembundle03_i > 0)
        {
            if (false == LowData._ItembundleLowData.DataInfoDic.ContainsKey(packageItemData.Itembundle03_i))
            {
                Debug.LogError(" !!!!! 없는 번들 아이템 키 :" + packageItemData.Itembundle03_i);
                return;
            }

            addRewardLists(LowData._ItembundleLowData.DataInfoDic[packageItemData.Itembundle03_i]);
        }

        if (packageItemData.Itembundle04_i > 0)
        {
            if (false == LowData._ItembundleLowData.DataInfoDic.ContainsKey(packageItemData.Itembundle04_i))
            {
                Debug.LogError(" !!!!! 없는 번들 아이템 키 :" + packageItemData.Itembundle04_i);
                return;
            }

            addRewardLists(LowData._ItembundleLowData.DataInfoDic[packageItemData.Itembundle04_i]);
        }

        if (packageItemData.Itembundle05_i > 0)
        {
            if (false == LowData._ItembundleLowData.DataInfoDic.ContainsKey(packageItemData.Itembundle05_i))
            {
                Debug.LogError(" !!!!! 없는 번들 아이템 키 :" + packageItemData.Itembundle05_i);
                return;
            }

            addRewardLists(LowData._ItembundleLowData.DataInfoDic[packageItemData.Itembundle05_i]);
        }

        if (packageItemData.Itembundle06_i > 0)
        {
            if (false == LowData._ItembundleLowData.DataInfoDic.ContainsKey(packageItemData.Itembundle06_i))
            {
                Debug.LogError(" !!!!! 없는 번들 아이템 키 :" + packageItemData.Itembundle06_i);
                return;
            }

            addRewardLists(LowData._ItembundleLowData.DataInfoDic[packageItemData.Itembundle06_i]);
        }

        if (packageItemData.Itembundle07_i > 0)
        {
            if (false == LowData._ItembundleLowData.DataInfoDic.ContainsKey(packageItemData.Itembundle07_i))
            {
                Debug.LogError(" !!!!! 없는 번들 아이템 키 :" + packageItemData.Itembundle07_i);
                return;
            }

            addRewardLists(LowData._ItembundleLowData.DataInfoDic[packageItemData.Itembundle07_i]);
        }

        if (packageItemData.Itembundle08_i > 0)
        {
            if (false == LowData._ItembundleLowData.DataInfoDic.ContainsKey(packageItemData.Itembundle08_i))
            {
                Debug.LogError(" !!!!! 없는 번들 아이템 키 :" + packageItemData.Itembundle08_i);
                return;
            }

            addRewardLists(LowData._ItembundleLowData.DataInfoDic[packageItemData.Itembundle08_i]);
        }

    }


    void addRewardLists(ItembundleLowData.DataInfo bundleItem)
    {
        if(false == LowData._ItemLowData.DataInfoDic.ContainsKey(bundleItem.ItemIndex_i))
        {
            Debug.Log(" !!!!! 없는 아이템 인덱스 : " + bundleItem.ItemIndex_i);
            return;
        }


        RewardItemData newRewardItemData = new RewardItemData();
        newRewardItemData._count = bundleItem.BubdleValue_i;
        newRewardItemData._index = bundleItem.ItemIndex_i;

        REWARD_ITEM_TYPE type = REWARD_ITEM_TYPE.NONE;

        switch (LowData._ItemLowData.DataInfoDic[bundleItem.ItemIndex_i].Itemtype_b)
        {
            case 1:
                {
                    //아이템
                    type = REWARD_ITEM_TYPE.Item;
                    break;
                }

            case 2:
                {
                    //골드
                    type = REWARD_ITEM_TYPE.Gold;
                    break;
                }

            case 3:
                {
                    //루비
                    type = REWARD_ITEM_TYPE.Ruby;
                    break;
                }

            case 4:
                {
                    
                    //트로피
                    type = REWARD_ITEM_TYPE.Trophy;
                    break;
                }

            case 5:
                {
                    //우정포인트
                    type = REWARD_ITEM_TYPE.FriendShipPoint;
                    break;
                }

            case 6:
                {
                    //스테미너
                    type = REWARD_ITEM_TYPE.Stemina;
                    break;
                }

            case 7:
                {
                    //pvp토큰
                    type = REWARD_ITEM_TYPE.PvPTicket;
                    break;
                }

            case 8:
                {
                    // 팀 경험치...?
                    type = REWARD_ITEM_TYPE.Exp;
                    break;
                }
        }

        newRewardItemData._type = type;

        //Debug.Log(" !!!!! " + newRewardItemData._count + " , " + newRewardItemData._index + " , " + newRewardItemData._type);

        _rewardItems.Add(newRewardItemData);

    }


    void setRewardPopupOpen()
    {
        //Debug.Log(" !!!!! " + _rewardItems.Count);

        /*for (int i = 0; i < _rewardItems.Count; i++)
            Debug.Log(" !!!!! " + _rewardItems[i]._index + " , " + _rewardItems[i]._type + " , " + _rewardItems[i]._count);*/

        UIManager.instance.OpenPopup(E_UIPOPUP_TYPE.RewardPopup).GetComponent<RewardPopup>().Init(_rewardItems);

        //갱신
        UIManager.instance.GetPanel(E_UIPANEL_TYPE.UIHostMenu).GetComponent<UIHostMenu>().SetHostMenu(UIHOST_TYPE.BASIC);

        _rewardItems.Clear();
    }


    REWARD_ITEM_CATEGORY getItemCategoryType( int subCategoryType )
    {
        REWARD_ITEM_CATEGORY categoryType = REWARD_ITEM_CATEGORY.None;
 
        switch(subCategoryType)
        {
            case 1:
                {
                    //영웅조각
                    categoryType = REWARD_ITEM_CATEGORY.HeroPiece;
                    break;
                }

            case 2:
                {
                    categoryType = REWARD_ITEM_CATEGORY.Potion;
                    break;
                }

            case 3:
                {
                    categoryType = REWARD_ITEM_CATEGORY.EquipmentPiece;
                    break;
                }

            case 4:
                {
                    categoryType = REWARD_ITEM_CATEGORY.SpaceComposition;
                    break;
                }
        }

        return categoryType;
    }

    int getAccureAttendReward(int attendCount)
    {
        //누적 출석 보상
        if(_monthType == 0)
        {
            for (int i = 0; i < _oddAccureLists.Count; i++)
            {
                if (_oddAccureLists[i].AttendanceType_b != 2)
                    continue;

                if (_oddAccureLists[i].AttendanceDay_b == attendCount)
                    return _oddAccureLists[i].Index_i;
            }
        }
        else
        {
            for (int i = 0; i < _evenAccureLists.Count; i++)
            {
                if (_evenAccureLists[i].AttendanceType_b != 2)
                    continue;

                if (_evenAccureLists[i].AttendanceDay_b == attendCount)
                    return _evenAccureLists[i].Index_i;
            }
        }

        return -1;
    }

    int getNormalAttendReward(int attendCount)
    {
        //일반 출석 보상
        if (_monthType == 0)
        {
            foreach (KeyValuePair<int, AttendanceOddLowData.DataInfo> oddAttend in LowData._attendanceOddLowData.DATAInfoDic)
            {
                if (oddAttend.Value.AttendanceRewardType_b == 2)
                    continue;

                if (oddAttend.Value.AttendanceDay_b == attendCount)
                    return oddAttend.Key;
            }
        }
        else
        {
            foreach (KeyValuePair<int, AttendanceEvenLowData.DataInfo> evenAttend in LowData._attendanceEvenLowData.DATAInfoDic)
            {
                if (evenAttend.Value.AttendanceRewardType_b == 2)
                    continue;

                if (evenAttend.Value.AttendanceDay_b == attendCount)
                    return evenAttend.Key;
            }
        }


        return -1;
    }
    
    void setReceiveDailyReward(int cnt, common.Data.Attend dailyReward)
    {

        //Common 수정
        //아이템 정보 안에 타입으로 나눔
        /*
        //Debug.Log(" !!!!! setReceiveDailyReward !!!!! ");
        //Debug.Log(" !!!!! " + dailyReward._reward_type + " , " + dailyReward._reward_value);

        switch (dailyReward._reward_type)
        {
            case 1:
                {
                    //골드
                    DataManager.instance.InCreaseGold(dailyReward._reward_value);
                    break;
                }
            case 2:
                {
                    //캐시
                    DataManager.instance.InCreaseCash(dailyReward._reward_value);
                    break;
                }
            case 3:
                {
                    //트로피
                    //???????
                    break;
                }
            case 4:
                {
                    //아이템
                    if (null != dailyReward._reward_item)
                    {
                        //Debug.Log(" !!!!! " + dailyReward._reward_item._count + " , " + dailyReward._reward_item._grade + " , " + dailyReward._reward_item._itemID + " , " + dailyReward._reward_item._mount + " , " + dailyReward._reward_item._uuid);
                        DataManager.instance.AddItemDic(dailyReward._reward_item);
                    }

                    break;
                }
            case 5:
                {
                    //스테미너
                    DataManager.instance.InCreaseStemina(dailyReward._reward_value);
                    break;
                }
            case 6:
                {
                    //pvp토큰
                    DataManager.instance.InCreatePvpStemina(dailyReward._reward_value);
                    break;
                }
            case 7:
                {
                    //우정포인트
                    break;
                }
            case 8:
                {
                    //패키지는 넘어 올리 없음
                    break;
                }
        }
        */
        if(null != _scrollManager)
            _scrollManager.SetReceiveCheck(cnt);
    }

    void setAttendAccureCount(int cnt)
    {
        if (null != _desc_attentCnt)
            UIManager.SetLocaliceAndStringFormat(_desc_attentCnt, E_LOCALIZE_STRING.AttendCnt_Desc, cnt);

    }

    void initAccrueAttendReward()
    {
        if(_monthType == 0)
        {
            _oddAccureLists = new List<AttendanceOddLowData.DataInfo>();

            //짝수
            foreach (KeyValuePair<int, AttendanceOddLowData.DataInfo> attendLowData in LowData._attendanceOddLowData.DATAInfoDic)
            {
                if (null == attendLowData.Value)
                    continue;

                if(attendLowData.Value.AttendanceType_b == 2)
                    _oddAccureLists.Add(attendLowData.Value);
            }
        }
        else
        {
            _evenAccureLists = new List<AttendanceEvenLowData.DataInfo>();

            if(_evenAccureLists.Count > 0)
            {
                //홀수
                foreach (KeyValuePair<int, AttendanceEvenLowData.DataInfo> attendLowData in LowData._attendanceEvenLowData.DATAInfoDic)
                {
                    if (null == attendLowData.Value)
                        continue;

                    if (attendLowData.Value.AttendanceType_b == 2)
                        _evenAccureLists.Add(attendLowData.Value);

                }

            }
        }
                
        for(int i=0; i< _packageRewardItem.Length; i++)
        {
            bool result = false;

            if(_monthType == 0)
            {
                result = DataManager.instance.netdata.player._Attend._Reward >= _oddAccureLists[i].AttendanceDay_b ? true : false;
                _packageRewardItem[i].Init(i, _oddAccureLists[i].AttendanceDay_b, result, this.transform);
            }
            else
            {
                if (_evenAccureLists.Count > 0)
                {
                    result = DataManager.instance.netdata.player._Attend._Reward >= _evenAccureLists[i].AttendanceDay_b ? true : false;
                    _packageRewardItem[i].Init(i, _evenAccureLists[i].AttendanceDay_b, result, this.transform);
                }
            }
        }
    }


    void resetAccureAttendReward()
    {
        for (int i = 0; i < _packageRewardItem.Length; i++)
        {
            bool result = false;

            if (_monthType == 0)
            {
                //Debug.Log(" !!!!! " + DataManager.instance.netdata.player._Attend._Count + " , " + _oddAccureLists[i].AttendanceDay_b);
                
                result = DataManager.instance.netdata.player._Attend._Count >= _oddAccureLists[i].AttendanceDay_b ? true : false;
                _packageRewardItem[i].Init(i, _oddAccureLists[i].AttendanceDay_b, result, this.transform);
            }
            else
            {
                //Debug.Log(" !!!!! " + DataManager.instance.netdata.player._Attend._Count + " , " + _evenAccureLists[i].AttendanceDay_b);

                result = DataManager.instance.netdata.player._Attend._Count >= _evenAccureLists[i].AttendanceDay_b ? true : false;
                _packageRewardItem[i].Init(i, _evenAccureLists[i].AttendanceDay_b, result, this.transform);
            }
        }
    }

    PackageLowData.DataInfo findPackageAttendItem(int packageItemindex)
    {
        int pIndex = -1;
        if (_monthType == 0)
        {
            pIndex = _oddAccureLists[packageItemindex].AttendanceRewardValue_i;

            if (false == LowData._packageLowData.DATAInfoDic.ContainsKey(pIndex))
            {
                Debug.LogError(" !!!!!! 없는 패키지 인덱스 " + pIndex);
                return null;
            }
        }
        else
        {
            pIndex = _evenAccureLists[packageItemindex].AttendanceRewardValue_i;

            if (false == LowData._packageLowData.DATAInfoDic.ContainsKey(pIndex))
            {
                Debug.LogError(" !!!!!! 없는 패키지 인덱스 " + pIndex);
                return null;
            }
        }

        return LowData._packageLowData.DATAInfoDic[pIndex];
    }

    void setPackageItem(int accureItemIndex, PackageLowData.DataInfo packageItem)
    {
        //~일 출석 보상
        if (null != _popupDesc)
        {
            int day = -1;
 
            if (_monthType == 0)
                day = _oddAccureLists[accureItemIndex].AttendanceDay_b;
            else
                day = _evenAccureLists[accureItemIndex].AttendanceDay_b;

            UIManager.SetLocaliceAndStringFormat(_popupDesc, E_LOCALIZE_STRING.AttendAccurePopup_Desc, day);
        }

        //패키지 구성 품
        for(int i=0; i< _packageCompositionItem.Length; i++)
        {
            if (i >= _packageCompositionIndex.Count)
            {
                _packageCompositionItem[i].DisableItem();
            }
            else
                setPackageCompositionItem(i, _packageCompositionIndex[i], packageItem);
        }
    }

    void addPackageItemLists(PackageLowData.DataInfo packageItem)
    {
        _packageCompositionIndex.Clear();

        //자리찾기
        //아이템 번들로 통일

        if (packageItem.Itembundle01_i > 0)
            _packageCompositionIndex.Add(0);

        if (packageItem.Itembundle02_i > 0)
            _packageCompositionIndex.Add(1);

        if (packageItem.Itembundle03_i > 0)
            _packageCompositionIndex.Add(2);

        if (packageItem.Itembundle04_i > 0)
            _packageCompositionIndex.Add(3);

        if (packageItem.Itembundle05_i > 0)
            _packageCompositionIndex.Add(4);

        if (packageItem.Itembundle06_i > 0)
            _packageCompositionIndex.Add(5);

        if (packageItem.Itembundle07_i > 0)
            _packageCompositionIndex.Add(6);

        if (packageItem.Itembundle08_i > 0)
            _packageCompositionIndex.Add(7);

    }


    void setPackageCompositionItem(int index, int type, PackageLowData.DataInfo packageItem)
    {
        if(false == LowData._ItembundleLowData.DataInfoDic.ContainsKey(packageItem.Itembundle01_i))
            return;

        ItembundleLowData.DataInfo bundleItem = LowData._ItembundleLowData.DataInfoDic[packageItem.Itembundle01_i]; 

        //무조건 번들 아이템
        //번들 아이템으로 통합

        switch(type)
        {
        
            case 0:
                {
                    _packageCompositionItem[index].SetPackageCompositionItemData(packageItem.Itembundle01_i);
                    break;
                }

            case 1:
                {
                    _packageCompositionItem[index].SetPackageCompositionItemData(packageItem.Itembundle02_i);
                    break;
                }

            case 2:
                {
                    _packageCompositionItem[index].SetPackageCompositionItemData(packageItem.Itembundle03_i);
                    break;
                }
            case 3:
                {
                    _packageCompositionItem[index].SetPackageCompositionItemData(packageItem.Itembundle04_i);
                    break;
                }
            case 4:
                {
                    _packageCompositionItem[index].SetPackageCompositionItemData(packageItem.Itembundle05_i);
                    break;
                }
            case 5:
                {
                    _packageCompositionItem[index].SetPackageCompositionItemData(packageItem.Itembundle06_i);
                    break;
                }

            case 6:
                {
                    _packageCompositionItem[index].SetPackageCompositionItemData(packageItem.Itembundle07_i);
                    break;
                }

            case 7:
                {
                    _packageCompositionItem[index].SetPackageCompositionItemData(packageItem.Itembundle08_i);
                    break;
                }
                 
        }
    }

}
