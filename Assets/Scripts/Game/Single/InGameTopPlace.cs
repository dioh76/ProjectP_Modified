using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class InGameTopPlace : MonoBehaviour
{
    public BackgroundController bgController;
    public EffectManager effectManager;

    public GameObject[] MonsterPrefab;
    public Transform StartPos;
    public Transform EndPos;
    public Transform MaxPos;
    public Transform ObjectPos;
    public Transform MonsterPos;

    public GameObject _spaceShipObj;
    public ParticleSystem _gameEndSpaceShip_wait;
    public ParticleSystem _gameEndSpaceShip_go;

    public SingleGameStatePlace _statePlace;

    public float _maxHP = 0f;
    public float _currentHP = 0f;

    private float _fBackgroundSpeed = 700f;
    private float _fBackgroundBackSpeed = 10f;

    private List<MonsterBase> _listMonster = new List<MonsterBase>();
    private E_GAME_CONTROLL_STATE _gameState;
    private E_GAME_RUNMODE _gameRunState = E_GAME_RUNMODE.RUN;
    
    public Leader _leader;

    public GameObject[] settingHeros = new GameObject[5];

    private MonsterBase _currentMonster;
    private List<MonsterBase> _trapList;
    private Transform _currentTombstone;

    private float _LastMonsterPos = 0;
    private GameObject _preMonster;

    private float _monsterMoveDeltaPoint;
    private float _recoverPoint;

    private float monsterSize = 60f;

    private float _currentMonsterAttackDelay = 0f;
    private float _currentMonsterAttackDeltaTime = 0f;
    private int _monsterCount;

    private float _currentStageDistance = 0;

    public float charScale = UtilityDefine.INGAME_CHAR_SCALE_OFFSET;
    private StageLowData.DataInfo _stageInfo;
    private float[] _monsterRanges;
    private int[] _monsterIndexs;
    private int[] _itemIndexs;
    private bool _haveBoss = false;

    private int _chapterIndex = 10100;
    private int _stageIndex = 10101;

    private int _battleDifficult = 0;

    private float _trapCallTimer = 0f;
    private int _trapCount = 0;

    List<bool> loadingList = new List<bool>();
    
    private List<InGameHero> _ingameHeros = new List<InGameHero>();
    private List<System.Guid> _curUuidList = new List<System.Guid>();

    private bool _isRecoverHp = false;
    private float _targetHp = 0f;

    private bool _isPvp = false;
    public bool _isEnd = false;

    private static int BattleHeroCount = 4;

    private static InGameTopPlace m_Instance = null;

    private bool _isTutorialStart = true;
    private bool _isTutorialBox = true;
    private bool _isTutorialBoss = true;

    public static InGameTopPlace instance
    {
        get
        {
            if( m_Instance == null )
            {
                m_Instance = GameObject.FindObjectOfType( typeof( InGameTopPlace ) ) as InGameTopPlace;

                if( m_Instance == null )
                {
                    GameObject panel = GameObject.Instantiate( Resources.Load( LocationDefine.INGAME_PANEL_PATH + "TopPlace" ) as GameObject ) as GameObject;
                    panel.transform.localPosition = new Vector3( 2000f, 0f, -200f );
                    panel.transform.localScale = Vector3.one;
                    panel.transform.SetParent( UIManager.instance.transform );
                    m_Instance = panel.GetComponent<InGameTopPlace>();
                }
            }
            return m_Instance;
        }
    }

    /// <summary>
    /// SIngle 플레이 용 함수
    /// </summary>
    #region Single Play

    public List<GameObject> GetHeros()
    {
        return _leader.GetHeroList();
    }

    public void CreateTopPlace( Transform parent, string BackgroundName, int stageIndex, int chapterIndex, byte stageType, List<System.Guid> uuidList )
    {
        UIManager.instance.StartLoading();
        transform.localPosition = new Vector3( 2000f, transform.localPosition.y, transform.localPosition.z );
        _gameState = E_GAME_CONTROLL_STATE.GAME_INIT;

        ResetGame();

        if( loadingList == null )
            loadingList = new List<bool>();
        loadingList.Clear();

        gameObject.SetActive( true );
        transform.parent = parent;
        _stageIndex = stageIndex;
        _chapterIndex = chapterIndex;
        _stageInfo = LowData._StageLowData.DataInfoDic[stageIndex];

        effectManager.gameObject.SetActive( false );
        _statePlace.gameObject.SetActive( false );
        bgController.SetBackground( BackgroundName, LoadingComplete, AddLoading() );

        _curUuidList.Clear();
        for( int i = 0; i < 4; i++ )
        {
            _curUuidList.Add( uuidList[i] );
        }
    }

    public void OnClickBackButton()
    {
        gameObject.SetActive( false );
    }

    public void OnClickStart( List<System.Guid> uuidList )
    {
        _isEnd = false;
        SetStatus(); //게임 스테이터스
        UIManager.instance.StartLoading();
        DataManager.instance._isContinue = false;
        _isPvp = false;
        common.Data.PlayerData player =  DataManager.instance.netdata.player;
        WebSender.instance.P_GAMEMODE_SINGLE_START_GAME( _chapterIndex, _stageIndex, uuidList[0], uuidList[1], uuidList[2], uuidList[3], StartGame );
    }

    public void StartGame( JSONObject jsonObj )
    {
        _gameState = E_GAME_CONTROLL_STATE.GAME_LOADING;
        bool bStart = false;
        loadingList.Clear();
        loadingList.Add( bStart );
        int code = (int)jsonObj["_Code"].n;

        if( code != (int)common.ResultCode.OK )
        {
            Debug.Log( "StartGame Error : " + jsonObj.ToString() );
            UIManager.instance.EndLoading();
            SceneManager.instance.GameEnd();
            return;
        }

        common.Response.StartGameResult result = DataManager.instance.LoadData<common.Response.StartGameResult>( jsonObj );

        DataManager.instance.ingameRewardItemList.Clear();
        DataManager.instance.ingameRewardItemList = result._IngameGachaitemBundledic;

        GameModeManager._rewardUID = result._Reward_UID;

        UIManager.instance.ClosePopups();
        ScenePanelManager.instance.RemovePanel( E_SCENE_TYPE.MAIN );
        UIManager.instance.RemoveAllPanels();
        UIManager.instance.RemoveAllPopups();
        System.GC.Collect();

        UIManager.instance.OpenPanel( E_UIPANEL_TYPE.BottomPlace );
        GameModeManager.instance.Gold = 0;
        GameModeManager.instance.Exp = 0;
        GameModeManager.instance.Reward = 0;

        GameModeManager.instance.GameStart( E_GAMEMODE_TYPE.SINGLE );

        //제한시간
        bool possibleAuto = true;
        bool isNone = true;
        for( int i = 0; i < DataManager.instance.GetPlayer()._Stagedic.Count; i++ )
        {
            if( DataManager.instance.GetPlayer()._Stagedic[i]._StageID == _stageIndex )
            {
                isNone = false;
                if( DataManager.instance.GetPlayer()._Stagedic[i].IsFirst == false )
                {
                    possibleAuto = false;
                }
            }
        }

        if( isNone )
            possibleAuto = false;

        GameModeManager.instance.inGameUI.SetTime( LowData._StageLowData.DataInfoDic[_stageIndex].Limittime_i, possibleAuto );        
        //Friend스킬 필요
        GameModeManager.instance.inGameUI.SetSkillIcons( _curUuidList, 0, 0, string.Empty, ActiveSkill );

        RefreshHeros( _curUuidList );
        _leader.SetHeroSkills();
        GameModeManager.instance.inGameUI.UpdateActiveSkillManager( (int)_leader._mp );
        _statePlace.UpdatePlayerHealthBar( true, _leader._maxHp, _leader._hp );

        setNativeActivityTracking();

        //사운드 수정
        UIManager.instance.StartAudio( false );
        SoundManager.Instance.PlayBGM( BGMSoundNameList.Adventure_BGM );
        //ChangeMode( E_GAME_RUNMODE.RUN );
        loadingList[0] = true;
    }

    void setNativeActivityTracking()
    {
        if (null != NativeManager.instance)
        {
            switch ((E_CHAPTER_TYPE)LowData._ChapterLowData.DataInfoDic[_chapterIndex].Chaptertype_b)
            {
                case E_CHAPTER_TYPE.E_HERO_CHAPTER:
                    {
                        //커스텀 태그 Tracking. - 영웅던전을 플레이 했을 떄
                        NativeManager.instance.SetCustomTrackingActivity("Hplay");
                        break;
                    }

                case E_CHAPTER_TYPE.E_EPISODE_CHAPTER:
                    {
                        //커스텀 태그 Tracking. - 시나리오던전을 플레이 했을 떄
                        NativeManager.instance.SetCustomTrackingActivity("Splay");
                        break;
                    }

                case E_CHAPTER_TYPE.E_WEEK_CHAPTER:
                    {
                        //커스텀 태그 Tracking. - 요일던전 플레이 했을 떄
                        NativeManager.instance.SetCustomTrackingActivity("Dplay");
                        break;
                    }
            }
        }
    }

    public void SingleInit()
    {
        //Common 수정 
        //몬스터 생성 새로
        _preMonster = null;
        _currentStageDistance = 0;

        string[] monsterIndex = _stageInfo.Monstergroup_c.Split( new string[] { "//" }, System.StringSplitOptions.None );
        string[] objectIndex = _stageInfo.Boxgroup_c.Split( new string[] { "//" }, System.StringSplitOptions.None );
        int objectCount = _stageInfo.Boxcount_b;
        int battleMonsterCount = _stageInfo.Monsterconut_b;
        int trapCount = _stageInfo.Trapcount_b;

        int allCount = battleMonsterCount + objectCount;
        if( _stageInfo.BatchBoss_i != 0 )
        {
            _haveBoss = true;
            allCount++;
        }
        _monsterRanges = new float[allCount];

        GameModeManager.instance.Gold = 0;
        GameModeManager.instance.Exp = 0;
        GameModeManager.instance._ingameAllMonster = allCount;
        GameModeManager.instance.inGameUI.StateUpdate();

        for( int i = 0; i < _monsterRanges.Length; i++ )
        {
            _monsterRanges[i] = Random.Range( _stageInfo.Stagerange_i, _stageInfo.Eventrange_i );
        }

        if( trapCount > 0 )
        {
            if( _trapList == null )
            {
                _trapList = new List<MonsterBase>();
            }

            _trapList.Clear();

            float allRange = 0f;
            for( int i = 0; i < _monsterRanges.Length; i++ )
            {
                allRange += _monsterRanges[i];
            }

            float[] trapRange = new float[trapCount];

            float trapOneRange = allRange / (float)( trapCount + 1 );

            for( int i = 0; i < trapCount; i++ )
            {
                trapRange[i] = ( i + 1 ) * trapOneRange;
                CreateTrap( trapRange[i] );
            }
        }

        _monsterIndexs = new int[allCount];
        _itemIndexs = new int[allCount];
        int rewardItemCount = DataManager.instance.ingameRewardItemList.Count;

        List<int> rewardItemList = new List<int>();
        for( int i = 0; i < DataManager.instance.ingameRewardItemList.Count; i++ )
        {
            rewardItemList.Add( DataManager.instance.ingameRewardItemList[i] );
        }
        int monsterItemCount = rewardItemCount - battleMonsterCount;

        int monC = 0;
        int objC = 0;
        int rate = battleMonsterCount + objectCount;

        for( int i = 0; i < _monsterIndexs.Length - ( ( _haveBoss ) ? 1 : 0 ); i++ )
        {
            int tempcount =  _monsterIndexs.Length - ( ( _haveBoss ) ? 1 : 0 ) - i;

            if( objectCount - objC > 0 )
            {
                if( UnityEngine.Random.Range( 0, ( battleMonsterCount - monC + objectCount - objC ) * 100 ) <= ( objectCount - objC ) * 100 )
                {
                    int rand = UnityEngine.Random.Range( 0, objectIndex.Length );
                    _monsterIndexs[i] = int.Parse( objectIndex[rand] );
                    //_monsterIndexs[i] = 210001;
                    if( rewardItemList.Count > 0 )
                    {
                        _itemIndexs[i] = rewardItemList[0];
                        rewardItemList.RemoveAt( 0 );
                    }
                    objC++;
                } else
                {
                    if( battleMonsterCount > monC )
                    {
                        int rand = UnityEngine.Random.Range( 0, monsterIndex.Length );
                        _monsterIndexs[i] = int.Parse( monsterIndex[rand] );
                        //_monsterIndexs[i] = 210001;

                        if( tempcount == monsterItemCount )
                        {
                            if( rewardItemList.Count > 0 )
                            {
                                _itemIndexs[i] = rewardItemList[0];
                                rewardItemList.RemoveAt( 0 );
                            }
                        } else
                        {
                            if( rewardItemList.Count > 0 )
                            {
                                if( UnityEngine.Random.Range( 0, 100 ) < 50 )
                                {
                                    _itemIndexs[i] = rewardItemList[0];
                                    rewardItemList.RemoveAt( 0 );
                                }
                            }
                        }

                        monC++;
                    } else
                    {
                        int rand = UnityEngine.Random.Range( 0, objectIndex.Length );
                        _monsterIndexs[i] = int.Parse( objectIndex[rand] );
                        //_monsterIndexs[i] = 210001;
                        if( rewardItemList.Count > 0 )
                        {
                            _itemIndexs[i] = rewardItemList[0];
                            rewardItemList.RemoveAt( 0 );
                        }
                        objC++;
                    }
                }
            } else
            {
                int rand = UnityEngine.Random.Range( 0, monsterIndex.Length );
                _monsterIndexs[i] = int.Parse( monsterIndex[rand] );
                //_monsterIndexs[i] = 210001;
                if( tempcount == monsterItemCount )
                {
                    if( rewardItemList.Count > 0 )
                    {
                        _itemIndexs[i] = rewardItemList[0];
                        rewardItemList.RemoveAt( 0 );
                    }
                } else
                {
                    if( rewardItemList.Count > 0 )
                    {
                        if( UnityEngine.Random.Range( 0, 100 ) < 50 )
                        {
                            _itemIndexs[i] = rewardItemList[0];
                            rewardItemList.RemoveAt( 0 );
                        }
                    }
                }
                monC++;
            }
        }

        if( _haveBoss )
        {
            _monsterIndexs[_monsterIndexs.Length - 1] = _stageInfo.BatchBoss_i;
            
            for( int i = _monsterIndexs.Length - 1; i >= 0; i-- )
            {
                if( LowData._MonsterLowData.DataInfoDic[_monsterIndexs[i]].MonsterType_b == 1 )
                {
                    GameModeManager._endMonsterName = LowData._MonsterLowData.DataInfoDic[_monsterIndexs[i]].ResourceName_c;
                }
            }

            if( LowData._MonsterLowData.DataInfoDic[_stageInfo.BatchBoss_i].Costumenum_b > 1 )
            {
                GameModeManager._endMonsterName = string.Format( "{0}{1}", GameModeManager._endMonsterName, LowData._MonsterLowData.DataInfoDic[_stageInfo.BatchBoss_i].Costumenum_b );
            }
        }

        _monsterCount = 0;

        //_statePlace.SetDistance( _monsterRanges );

        SetMonster();

        _statePlace.Init();

        effectManager.gameObject.SetActive( true );
        _statePlace.gameObject.SetActive( true );

        _statePlace.UpdatePlayerHealthBar( true, _leader._maxHp, _leader._hp );
        _statePlace.UpdatePlyaerManaPointBar( true, _leader._maxMp, _leader._mp );
    }

    public void SetMonster()
    {
        //return; 

        _LastMonsterPos = _leader.transform.localPosition.x;
        _LastMonsterPos += _monsterRanges[0];

        if( _monsterCount >= _monsterRanges.Length )
            return;
        if( _monsterCount >= _monsterIndexs.Length )
            return;


        for( int i=0; i < UtilityDefine.MAX_ADVENTURE_MONSTER_LENGTH; i++ )
        {
            if( i >= _monsterIndexs.Length )
                break;
            if( LowData._MonsterLowData.DataInfoDic[_monsterIndexs[i]].MonsterType_b == (int)E_MONSTER_BATTLE_TYPE.Chest )
            {
                CreateChest( _monsterIndexs[i] );
            } else if( LowData._MonsterLowData.DataInfoDic[_monsterIndexs[i]].MonsterType_b == (int)E_MONSTER_BATTLE_TYPE.Box )
            {
                CreateBox( _monsterIndexs[i] );
            } else
            {
                CreateMonster( _monsterIndexs[i] );
            }
        }
    }

    #endregion

    public void InitTeamSetting( string BackgroundName )
    {
        effectManager.gameObject.SetActive( false );
        _statePlace.gameObject.SetActive( false );
        bgController.SetBackground( BackgroundName, LoadingComplete, AddLoading() );

        _curUuidList.Clear();
        System.Guid uuid = new System.Guid();
        for( int i = 0; i < 4; i++ )
        {
            uuid = System.Guid.Empty;
            uuid = DataManager.instance.GetHeroSlotUuid( i );
            _curUuidList.Add( uuid );
        }
    }

    public void RefreshHeros( List<System.Guid> uuidList )
    {
        loadingList.Clear();
        _leader.ResetObjects();

        for( int i = 0; i < 4; i++ )
        {
            _curUuidList[i] = uuidList[i];
        }

        _leader.SetData( _curUuidList );

        SetLeader();
        SetHero();
    }

    int AddLoading()
    {
        bool bComplete = false;
        loadingList.Add( bComplete );
        return loadingList.Count - 1;
    }

    public void LoadingComplete( int idx )
    {
        if( loadingList.Count > idx )
            loadingList[idx] = true;
    }

    void Update()
    {
        executeStateAction();
    }

    void UpdateMonsterAttack()
    {
        if( _currentMonster != null && _currentMonster.GetMonsterType() == MonsterType.Monster && true == _currentMonster._isLive )
        {
            _currentMonsterAttackDeltaTime += Time.deltaTime * ( ( UtilityDefine.fGameSpeed > 1.5f ) ? 1.5f : UtilityDefine.fGameSpeed );
            _currentMonster.GetComponent<Monster>().SetAttackTimer( _currentMonsterAttackDelay ,_currentMonsterAttackDeltaTime );
            if( _currentMonsterAttackDeltaTime > _currentMonsterAttackDelay )
            {
                _currentMonsterAttackDeltaTime = 0f;
                _currentMonster.GetComponent<Monster>().AttackAnimation( _leader.gameObject );

                float delay = ( ( (Monster)_currentMonster ).GetAttackAniDelayTime() - 0.3f );
                StartCoroutine( WaitForAnimDelayPlayer( delay ) );
            }
        }

        if( _currentMonster == null )
        {
            if( GetMonsterList().Count > 0 )
            {
                ChangeMode( E_GAME_RUNMODE.RUN );
            }
        }
    }

    void UpdateJustMoving()
    {
        _leader.UpdateWalk();

        for( int i = 0; i < settingHeros.Length; i++ )
        {
            if( i == 0 )
                continue;
            if(settingHeros[i] != null)
                settingHeros[i].GetComponent<InGameHero>().playMoveAnimation();
        }
    }

    private static float _runHpRecoveryPerSec = 15f;

    void UpdatePlayerPosition()
    {
        if( _currentMonster == null )
        {
            _leader.UpdateWalk();
            for( int i = 0; i < _ingameHeros.Count; i++ )
            {
                if( _ingameHeros[i] != null )
                {
                    _ingameHeros[i].playMoveAnimation();
                }
            }
            if( _maxHP > _currentHP )
            {
                _currentHP += _runHpRecoveryPerSec * Time.deltaTime;
            } else
            {
                _currentHP = _maxHP;
            }
        } else
        {
            _currentHP -= _currentMonster.GetMonsterDotDamage() * UtilityDefine.fGameSpeed * Time.deltaTime;
        }

        float prePosX = _leader.transform.localPosition.x;
        float cPosX = HPtoPositionX( _currentHP );
        _leader.transform.localPosition = new Vector3( cPosX, _leader.transform.localPosition.y, _leader.transform.localPosition.z );
        _fBackgroundBackSpeed = prePosX - _leader.transform.localPosition.x;

        if( _currentHP <= 0f )
        {
            return;
        }
    }

    IEnumerator ChangeMonsterPos( MonsterBase mon )
    {
        if( mon != null )
        {
            mon.transform.parent = MonsterPos;
            iTween.MoveTo( mon.gameObject, iTween.Hash( "islocal", true, "x", 0f, "y", 0f, "delay", 0.0f, "time", 0.5f, "looptype", iTween.LoopType.none, "easetype", iTween.EaseType.easeInOutBack ) );
        }
        yield return new WaitForSeconds( 0.5f );
    }

    void UpdateMonsterPosition()
    {
        if( _leader != null && _currentMonster == null )
        {
            if( _listMonster != null )
            {
                foreach( MonsterBase mon in _listMonster )
                {
                    mon.transform.localPosition = new Vector3( mon.transform.localPosition.x - _fBackgroundSpeed * UtilityDefine.fGameSpeed * Time.deltaTime, mon.transform.localPosition.y, mon.transform.localPosition.z );

                    if( mon.transform.localPosition.x <= _leader.transform.localPosition.x + monsterSize && true == mon._isLive )
                    {
                        _currentMonster = mon;

                        if( UtilityDefine.IsTutorialSkip == false )
                        {
                            if( _stageIndex == 10101 )
                            {
                                if( _isTutorialBox )
                                {
                                    _isTutorialBox = false;
                                    ChangeState( E_GAME_CONTROLL_STATE.GAME_WAIT );
                                    UIManager.instance.OpenPopup( E_UIPOPUP_TYPE.Tutorials ).GetComponent<Tutorials>().Init( (int)E_TUTORIAL_TEXT_NUMBER.GameBox_min, (int)E_TUTORIAL_TEXT_NUMBER.GameBox_max, TutorialCallback );
                                } else
                                {
                                    if( _isTutorialBoss )
                                    {
                                        _isTutorialBoss = false;
                                        ChangeState( E_GAME_CONTROLL_STATE.GAME_WAIT );
                                        UIManager.instance.OpenPopup( E_UIPOPUP_TYPE.Tutorials ).GetComponent<Tutorials>().Init( (int)E_TUTORIAL_TEXT_NUMBER.GameBoss_min, (int)E_TUTORIAL_TEXT_NUMBER.GameBoss_max, TutorialCallback );
                                    }
                                }
                            }

                        }

                        mon.transform.localPosition = new Vector3( _leader.transform.localPosition.x + monsterSize, mon.transform.localPosition.y, mon.transform.localPosition.z );
                        _leader.MeetMonster( _leader.transform.position );
                        for( int i = 0; i < _ingameHeros.Count; i++ )
                        {
                            if( _ingameHeros[i] != null )
                                _ingameHeros[i].MeetMonster();
                        }

                        if( _currentMonster.GetMonsterType() == MonsterType.Monster )
                        {
                            //수정필요 //선빵
                            _currentMonsterAttackDelay = ( (Monster)_currentMonster ).GetAttackDelay();
                            _currentMonsterAttackDeltaTime = _currentMonsterAttackDelay;
                            if( SkillManager.instance._firstAttackRate > Random.Range( 0, 1000 ) )
                                _currentMonsterAttackDeltaTime = 0;

                            StartCoroutine( ChangeMonsterPos( mon ) );
                            ChangeMode( E_GAME_RUNMODE.BATTLE );
                        } else if( _currentMonster.GetMonsterType() == MonsterType.Box )
                        {
                            _currentMonster.GetComponent<Box>().SetBox();
                        }
                    }
                }
            }
        }

        if( _leader != null && _currentMonster != null )
        {
            if( _listMonster != null )
            {
                if( _leader._hp == 1f )
                    return;
                foreach( MonsterBase mon in _listMonster )
                {
                    float hp = PositionXtoHP( mon.transform.localPosition.x );
                    hp -= _currentMonster.GetMonsterDotDamage() * UtilityDefine.fGameSpeed * Time.deltaTime;
                    mon.transform.localPosition = new Vector3( HPtoPositionX( hp ), mon.transform.localPosition.y, mon.transform.localPosition.z );
                }
            }
        }
    }

    void UpdateTrapPosition()
    {
        if( _isPvp == true )
            return;

        if( _currentMonster != null )
        {
            return;
        }

        float trapSpeed = 140f * Time.deltaTime;
        if( _trapList != null && _trapList.Count > 0)
        {
            for( int i = 0; i < _trapList.Count; i++ )
            {
                if( _trapList[i].transform.position.x > EndPos.position.x )
                    _trapList[i].transform.localPosition = new Vector3( _trapList[i].transform.localPosition.x - trapSpeed, _trapList[i].transform.localPosition.y, _trapList[i].transform.localPosition.z );
                else
                {
                    _trapList[i].transform.position = new Vector3( EndPos.position.x, _trapList[i].transform.position.y, _trapList[i].transform.position.z );
                    _trapList[i].GetComponent<Trap>().TrapNotDestroy( _leader );
                    //TrapDie( Vector3.zero, _trapList[i] );
                }
            }
        }
    }

    void UpdateBackground(float moveSpeed = 0f)
    {
        float speed = 0f;
        if( null == _currentMonster )
        {
            speed = _fBackgroundSpeed * Time.deltaTime * UtilityDefine.fGameSpeed;
        } else
        {
            speed = _fBackgroundBackSpeed;
        }

        if( moveSpeed != 0f )
            speed = moveSpeed * Time.deltaTime * UtilityDefine.fGameSpeed;

        if( _currentTombstone != null )
        {
            if( _currentTombstone.localPosition.x > -1024f )
            {
                _currentTombstone.localPosition = new Vector3( _currentTombstone.localPosition.x - speed, _currentTombstone.localPosition.y, _currentTombstone.localPosition.z );
            }
        }

        bgController.UpdateBackground( speed );
    }

    public float HPtoPositionX( float hp )
    {
        float posX = 0f;

        hp = hp / _maxHP;
        posX = MaxPos.localPosition.x - EndPos.localPosition.x;
        posX = posX * hp;

        return EndPos.localPosition.x + posX;
    }

    public float PositionXtoHP( float posX )
    {
        float hp = 0f;

        posX = posX - EndPos.localPosition.x;
        posX = posX / ( MaxPos.localPosition.x - EndPos.localPosition.x );
        hp = _maxHP * posX;

        return hp;
    }

    public void SetLeader()
    {
        //리더 위치
        float startX = 0f;
        startX = MaxPos.localPosition.x - EndPos.localPosition.x;
        //startX = startX * 0.25f;
        StartPos.localPosition = new Vector3( EndPos.localPosition.x + startX, StartPos.localPosition.y, -5 );

        if( _curUuidList[0] != System.Guid.Empty )
        {
            HeroData heroData =DataManager.instance.GetHeroData( _curUuidList[0] );

            string spineName = heroData.lowHero.ResourceName_c;
            int idx = AddLoading();
            AssetbundleLoader.SkeletonLoadFromName( spineName, go => {
                GameObject leaderObject = null;
                leaderObject = Object.Instantiate( (Object)go ) as GameObject;

                if( null == leaderObject )
                    return;

                //leaderObject.transform.SetParent( _leader.transform );
                settingHeros[0] = leaderObject;
                leaderObject.transform.SetParent( _leader.transform );
                leaderObject.transform.localScale = new Vector3( -charScale, charScale, 1f );
                leaderObject.transform.localPosition = new Vector3( 0f, 0f, 0f );

                leaderObject.AddComponent<InGameHero>();
                leaderObject.GetComponent<InGameHero>().SetIdle();
                _leader.SetSkeleton( leaderObject.GetComponent<SkeletonAnimation>() );
                _leader.SetIdle();

                leaderObject.GetComponent<SkeletonAnimation>().initialSkinName = "01";

                int skinNumber = UIManager.instance.GetCostumeName( heroData );
                if( UIManager.instance.IsHaveSkin( leaderObject.GetComponent<SkeletonAnimation>(), skinNumber ) )
                {
                    leaderObject.GetComponent<SkeletonAnimation>().initialSkinName = string.Format( "0{0}", skinNumber );
                }
                leaderObject.GetComponent<SkeletonAnimation>().Reset();
                _leader.SetLeader( leaderObject );
                UIManager.instance.SetSpineCenter( leaderObject, leaderObject.transform.parent, leaderObject.transform.localPosition );

                _leader.transform.parent = ObjectPos;
                _leader.transform.localScale = new Vector3( 1f, 1f, 1f );
                _leader.transform.localPosition = StartPos.localPosition;

                LoadingComplete( idx );
            } );
        }
    }

    public void SetStatus()
    {
        common.Data.PlayerData playerData = DataManager.instance.netdata.player;

        _maxHP = 1000f;
        _currentHP = -100f;
        _monsterMoveDeltaPoint = PositionXtoHP( _fBackgroundSpeed );
        _recoverPoint = 10f;
    }

    public void SetHero()
    {
        _ingameHeros.Clear();
        for( int i = 0; i < BattleHeroCount - 1; i++ )
        {
            _ingameHeros.Add( null );
        }

        HeroData[] heroInfo = null;

        heroInfo = new HeroData[UtilityDefine.MAX_PET_EQUIP_LENGTH];

        for( int i = 1; i < _curUuidList.Count; i++ )
        {
            heroInfo[i - 1] = DataManager.instance.GetHeroData( _curUuidList[i] );
            if( heroInfo[i - 1] != null )
                initializePetObj( heroInfo[i - 1], i - 1 );
        }

    }

    void initializePetObj( HeroData heroData, int index )
    {
        string prefabObjName = string.Format( "Pet_{0}", heroData );

        GameObject petObj = null;
        int idx = AddLoading();
        AssetbundleLoader.SkeletonLoadFromName( heroData.lowHero.ResourceName_c, go => {
            petObj = Object.Instantiate( (Object)go ) as GameObject;

            if( null == petObj )
                return;

            settingHeros[index + 1] = petObj;
            petObj.transform.SetParent( ObjectPos );
            petObj.transform.localScale = new Vector3( charScale, charScale, 1f );
            petObj.transform.localPosition = new Vector3( 0f, 0f, 0f );

            petObj.GetComponent<SkeletonAnimation>().initialSkinName = "01";

            int skinNumber = UIManager.instance.GetCostumeName( heroData );
            if( UIManager.instance.IsHaveSkin( petObj.GetComponent<SkeletonAnimation>(), skinNumber ) )
            {
                petObj.GetComponent<SkeletonAnimation>().initialSkinName = string.Format( "0{0}", skinNumber );
            }

            petObj.AddComponent<InGameHero>();
            petObj.GetComponent<InGameHero>().SetIdle();
            petObj.GetComponent<SkeletonAnimation>().AnimationName = "01_Default";
            petObj.GetComponent<SkeletonAnimation>().loop = true;
            petObj.GetComponent<SkeletonAnimation>().Reset();

            UIManager.instance.SetSpineCenter( petObj, petObj.transform.parent, petObj.transform.localPosition );

            _ingameHeros[index] = petObj.GetComponent<InGameHero>();
            if( null != _leader )
            {
                _leader.SetHero( petObj, index );
            }

            LoadingComplete( idx );
        } );
    }

    public void CreateMonster( int monsterIndex )
    {
        //if( monsterIndex > 0 )
        //    return;
        MonsterBase mon = null;
        mon = ( Instantiate( MonsterPrefab[0] ) as GameObject ).GetComponent<MonsterBase>();
        mon.transform.parent = ObjectPos;
        mon.transform.localScale = new Vector3( charScale, charScale, 1 );
        _LastMonsterPos = 0f;
        for( int i = 0; i < _listMonster.Count; i++ )
        {
            if( _listMonster[i].gameObject.activeSelf == true )
            {
                if( _listMonster[i].transform.localPosition.x > _LastMonsterPos )
                {
                    _LastMonsterPos = _listMonster[i].transform.localPosition.x;
                }
            }
        }
        if( _LastMonsterPos == 0 )
            _LastMonsterPos = _leader.transform.localPosition.x;

        if( _itemIndexs.Length > _monsterCount )
        {
            mon._ingameItem.SetItem( _itemIndexs[_monsterCount] );
        } else
        {
            mon._ingameItem.SetItem( 0 );
        }
        _listMonster.Add( mon );

        mon.transform.localPosition = new Vector3( _LastMonsterPos + _monsterRanges[_monsterCount], 0f, -10f );

        _monsterCount++;
         
        GameObject monsterSpineObj;

        MonsterLowData.DataInfo monData = LowData._MonsterLowData.DataInfoDic[monsterIndex];
        mon.transform.localScale = new Vector3( monData.ResourceSize_s * 0.001f, monData.ResourceSize_s * 0.001f, 1f );
        int idx = AddLoading();
        //AssetbundleLoader.SkeletonLoadFromName( "Fireman", go => {
        AssetbundleLoader.SkeletonLoadFromName( monData.ResourceName_c, go => {
            monsterSpineObj = Object.Instantiate( (Object)go ) as GameObject;
            monsterSpineObj.transform.parent = mon.transform;
            monsterSpineObj.transform.localScale = new Vector3( 0.9f * 0.71f, 0.9f * 0.71f, 1f );
            monsterSpineObj.transform.localPosition = Vector3.zero;
            monsterSpineObj.name = "Spine";
            monsterSpineObj.layer = mon.gameObject.layer;
            monsterSpineObj.GetComponent<SkeletonAnimation>().initialSkinName = "01";
            if( monData.Costumenum_b > 0 )
            {
                monsterSpineObj.GetComponent<SkeletonAnimation>().initialSkinName = string.Format( "0{0}", monData.Costumenum_b );
            }
            monsterSpineObj.GetComponent<SkeletonAnimation>().Reset();

            mon.SetMonster( MonsterType.Monster, monsterIndex, _stageInfo.BatchMobLevel_b, new PuzzleCount(), MonsterDie );

            _preMonster = mon.gameObject;
            LoadingComplete( idx );
        } );
    }

    public bool CreateChest( int monsterIndex )
    {
        MonsterBase mon = null;
        mon = ( Instantiate( MonsterPrefab[1] ) as GameObject ).GetComponent<MonsterBase>();
        mon.transform.parent = ObjectPos;
        mon.transform.localScale = new Vector3( charScale, charScale, 1 );
        _LastMonsterPos = 0f;
        for( int i = 0; i < _listMonster.Count; i++ )
        {
            if( _listMonster[i].gameObject.activeSelf == true )
            {
                if( _listMonster[i].transform.localPosition.x > _LastMonsterPos )
                {
                    _LastMonsterPos = _listMonster[i].transform.localPosition.x;
                }
            }
        }
        if( _LastMonsterPos == 0 )
            _LastMonsterPos = _leader.transform.localPosition.x;

        mon.transform.localPosition = new Vector3( _LastMonsterPos + _monsterRanges[_monsterCount], 0f, -10f );
        
        PuzzleCount count = new PuzzleCount();
        count.Key = LowData._MonsterLowData.DataInfoDic[monsterIndex].MAXHP_i;

        mon.SetMonster( MonsterType.Chest, monsterIndex, count, null );

        if( _itemIndexs.Length > _monsterCount )
        {
            mon._ingameItem.SetItem( _itemIndexs[_monsterCount] );
        } else
        {
            mon._ingameItem.SetItem( 0 );
        }
        _listMonster.Add( mon );
        _monsterCount++;
        _preMonster = mon.gameObject;

        return true;
    }

    public bool CreateBox( int monsterIndex )
    {
        MonsterBase mon = null;
        mon = ( Instantiate( MonsterPrefab[2] ) as GameObject ).GetComponent<MonsterBase>();
        mon.transform.parent = ObjectPos;
        mon.transform.localScale = new Vector3( 1, 1, 1 );
        _LastMonsterPos = 0f;
        for( int i = 0; i < _listMonster.Count; i++ )
        {
            if( _listMonster[i].gameObject.activeSelf == true )
            {
                if( _listMonster[i].transform.localPosition.x > _LastMonsterPos )
                {
                    _LastMonsterPos = _listMonster[i].transform.localPosition.x;
                }
            }
        }
        if( _LastMonsterPos == 0 )
            _LastMonsterPos = _leader.transform.localPosition.x;

        mon.transform.localPosition = new Vector3( _LastMonsterPos + _monsterRanges[_monsterCount], 0f, -10f );

        PuzzleCount count = new PuzzleCount();

        int maxCount = LowData._MonsterLowData.DataInfoDic[monsterIndex].MAXHP_i;//퍼즐 전체 갯수
        int typeCount = LowData._MonsterLowData.DataInfoDic[monsterIndex].Recoveryhp_i;//퍼즐 종류

        int firCount = 0;
        int secCount = 0;

        if( typeCount == 1 )
        {
            firCount = maxCount;
        } else if( typeCount == 2 )
        {
            firCount = Random.Range( 1, maxCount - 2 );
            secCount = maxCount - firCount;
        }

        switch( Random.Range( 0, 5 ) )
        {
            case 0:
                count.BoxCount = firCount;
                break;
            case 1:
                count.KeyCount = firCount;
                break;
            case 2:
                count.GoldCount = firCount;
                break;
            case 3:
                count.ExpCount = firCount;
                break;
            case 4:
                count.HealCount = firCount;
                break;
        }

        if( secCount > 0 )
        {
            switch( Random.Range( 0, 5 ) )
            {
                case 0:
                    count.BoxCount += secCount;
                    break;
                case 1:
                    count.KeyCount += secCount;
                    break;
                case 2:
                    count.GoldCount += secCount;
                    break;
                case 3:
                    count.ExpCount += secCount;
                    break;
                case 4:
                    count.HealCount += secCount;
                    break;
            }
        }

        mon.SetMonster( MonsterType.Box, monsterIndex, count, null );

        if( _itemIndexs.Length > _monsterCount )
        {
            mon._ingameItem.SetItem( _itemIndexs[_monsterCount] );
        } else
        {
            mon._ingameItem.SetItem( 0 );
        }
        _listMonster.Add( mon );
        _monsterCount++;
        _preMonster = mon.gameObject;

        return true;
    }

    public bool CreateTrap(float range)
    {
        MonsterBase mon = null;
        mon = ( Instantiate( MonsterPrefab[4] ) as GameObject ).GetComponent<MonsterBase>();
        mon.transform.parent = ObjectPos;
        mon.transform.localScale = new Vector3( 1, 1, 1 );
        mon.transform.localPosition = new Vector3( range, 70f, 0f );

        PuzzleCount count = new PuzzleCount();
        switch( Random.Range( 0, 5 ) )
        {
            case 0:
                count.GoldCount = 1;
                break;
            case 1:
                count.ExpCount = 1;
                break;
            case 2:
                count.KeyCount = 1;
                break;
            case 4:
                count.BoxCount = 1;
                break;
            case 5:
                count.HealCount = 1;
                break;
        }

        mon.SetMonster( MonsterType.Trap, count, TrapDie );

        //_listMonster.Add( mon );
        _trapList.Add( mon );
        _trapCount++;

        //_preMonster = mon.gameObject;

        return true;
    }

    public void PuzzleDestroy( PuzzleCount count, bool cross, Vector3 textPos )
    {
        _leader.UpdatePuzzle( count );
        
        if( _currentMonster != null && true == _currentMonster._isLive )
        {
            if( _currentMonster._type == MonsterType.Box )
            {
                if( ( (Box)_currentMonster ).Damaged( count, _leader ) == true )
                {
                    StartCoroutine( WaitforBoxDie() );
                }
            } else if( _currentMonster._type == MonsterType.Chest )
            {
                _statePlace.SetLock( count.Key );

                if( true == ( (Chest)_currentMonster ).Damaged( count, _leader, _statePlace.isAllLocksUnLock(), 0 ) )
                    StartCoroutine( WaitforChestDie() );
            } else
            {
                _leader.DestroyPuzzle( count, _currentMonster );
            }
        }

        if( _trapList != null && _trapList.Count > 0 )
        {
            for( int i = 0; i < _trapList.Count; i++ )
            {
                if( _trapList[i].transform.position.x < MaxPos.position.x + 100f )
                {
                    _trapList[i].Damaged( count, _leader );
                }
            }
        }
        
        //수정 필요 //Gold Exp 연출
        if( count.ExpCount > 0 )
        {
            GameModeManager.instance.Exp += count.ExpCount;
            GameModeManager.instance.inGameUI.StateUpdate();
        }

        if( count.GoldCount > 0 )
        {
            GameModeManager.instance.Gold += count.GoldCount;
            GameModeManager.instance.inGameUI.StateUpdate();
        }

        if( count.BoxCount >= 4 || ( count.BoxCount >= 3 && cross ) )
        {
            int randomCount = Random.Range( 0, 3 );
            int mp = 10;

            if(randomCount == 0)
            {
                int healPoint = (int)( _leader._maxHp * 0.01f );
                _leader.TakeHealJustValue( healPoint, 0 );
                EffectManager.instance.SetActivePuzzleEffect( textPos, E_PUZZLETEXT_TYPE.HP );
            }
            else if( randomCount == 1)
            {
                mp = 20;
                _leader.ChargeMP( mp );
                EffectManager.instance.SetActivePuzzleEffect( textPos, E_PUZZLETEXT_TYPE.MP );
            } else if( randomCount == 2 )
            {
                //포지션 회복
                _currentHP += 10;

                foreach( MonsterBase mon in _listMonster )
                {
                    float hp = PositionXtoHP( mon.transform.localPosition.x );
                    hp += 10;
                    mon.transform.localPosition = new Vector3( HPtoPositionX( hp ), mon.transform.localPosition.y, mon.transform.localPosition.z );
                    EffectManager.instance.SetActivePuzzleEffect( textPos, E_PUZZLETEXT_TYPE.TP );
                }
            }
        }
    }

    public void ObjectDie()
    {
        if( _currentMonster != null )
            ResetMonster( _currentMonster );

        if( _currentMonster != null )
        {
            GameModeManager.instance._ingameDeadMonster++;
            GameModeManager.instance.inGameUI.StateUpdate();
        }

        _currentMonster = null;
        if( _listMonster.Count == 0 )
        {
            ChangeState( E_GAME_CONTROLL_STATE.GAME_RESULT_READY );
            return;
        }

        IsBoss();
    }

    public void MonsterDie( Vector3 pos, MonsterBase deadMonster )
    {
        StartCoroutine( WaitforMonsterDie( pos, deadMonster ) );
    }

    public void TrapDie( Vector3 pos, MonsterBase deadMonster )
    {
        _trapList.Remove( deadMonster );

        if( deadMonster != null && deadMonster.gameObject != null )
        {
            Destroy( deadMonster.gameObject );
        }
    }

    IEnumerator WaitforMonsterDie( Vector3 pos, MonsterBase deadMonster )
    {
        if( deadMonster._ingameItem != null )
        {
            deadMonster._ingameItem.transform.parent = bgController.transform;
            deadMonster._ingameItem.SetAnimation();
        }

        yield return new WaitForSeconds( UtilityDefine.MONSTER_DIE_DELAY - 0.2f );
        /*
        //비석 이펙트
        _currentTombstone = effectManager.SetTombstone( pos );
        _currentTombstone.transform.position = new Vector3( _currentTombstone.transform.position.x, _currentTombstone.transform.position.y, _leader.transform.position.z );
        _currentTombstone.transform.localPosition = new Vector3( _currentTombstone.transform.localPosition.x, _currentTombstone.transform.localPosition.y, _leader.transform.localPosition.z + 0.1f );
        */
        yield return new WaitForSeconds( 0.5f );

        if( deadMonster != null && deadMonster.GetComponent<Monster>() != null )
            _leader.ChargeMP( UtilityDefine.MonsterChargeMP );

        if( _currentMonster != null )
        {
            GameModeManager.instance._ingameDeadMonster++;
            GameModeManager.instance.inGameUI.StateUpdate();
        }

        if( _currentMonster == deadMonster )
        {
            _currentMonster = null;
        }

        if( deadMonster != null )
            ResetMonster( deadMonster );


        //_statePlace.SetVisibleMonsterHealthBar( false, 0f, 0f );
        //_statePlace.EndPlace();

        if( _listMonster.Count > 0 )
        {
            ChangeMode( E_GAME_RUNMODE.RUN );
            IsBoss();
        } else
        {
            //게임 끝 연출
            //_isEnd = true;
            ChangeMode( E_GAME_RUNMODE.RUN );

        }

        yield break;
    }

    void IsBoss()
    {
        if( _haveBoss )
        {
            if( _listMonster.Count == 1 )
            {
                StartCoroutine( BossAction() );
            }
        }
    }

    IEnumerator BossAction()
    {
        ChangeState( E_GAME_CONTROLL_STATE.GAME_BOSSACTION );
        effectManager.SetBossEffect( true );
        yield return new WaitForSeconds( 4f );
        effectManager.SetBossEffect( false );
        ChangeState( E_GAME_CONTROLL_STATE.GAME_PLAYING );
    }

    IEnumerator WaitforBoxDie()
    {
        float delay = UtilityDefine.CHEST_DIE_DELAY;

        if( _currentMonster._ingameItem != null )
        {
            _currentMonster._ingameItem.transform.parent = bgController.transform;
            _currentMonster._ingameItem.SetAnimation();
        }

        yield return new WaitForSeconds( delay );

        ObjectDie();

        yield break;
    }

    IEnumerator WaitforChestDie()
    {
        float delay = ( ( (Chest)_currentMonster ).GetAnimationLength() ) + UtilityDefine.CHEST_DIE_DELAY;

        if( _currentMonster._ingameItem != null )
        {
            _currentMonster._ingameItem.transform.parent = bgController.transform;
            _currentMonster._ingameItem.SetAnimation();
        }

        yield return new WaitForSeconds( delay );

        ObjectDie();

        yield break;
    }

    IEnumerator WaitForAnimDelayPlayer( float delay )
    {
        //애니 싱크.
        yield return new WaitForSeconds( delay );

        if( null == _leader || _currentMonster == null )
            yield break;

        //몬스터 공격 피격 여러명 설정
        int attackCount = _currentMonster.GetComponent<Monster>().GetMaxAttackCount();

        List<IngameHeroStatus> tempHeroStatus = new List<IngameHeroStatus>();
        List<IngameHeroStatus> heroStatus = new List<IngameHeroStatus>();
        heroStatus = _leader.GetHeroStatus();

        for( int i = 0; i < heroStatus.Count; i++ )
        {
            if( heroStatus[i] != null && heroStatus[i]._object != null )
            {
                tempHeroStatus.Add( heroStatus[i] );
            }
        }

        while( attackCount < tempHeroStatus.Count )
        {
            int removeIndex = Random.Range( 0, tempHeroStatus.Count );
            tempHeroStatus.RemoveAt( removeIndex );
        }

        float _playerTakeDamage = 0;
        for( int i = 0; i < tempHeroStatus.Count; i++ )
        {
            float takeDamage = _leader.TakeDamage( tempHeroStatus[i]._data.uuid, ( (Monster)_currentMonster ).GetAttackPoint(), ((Monster)_currentMonster).GetMonsterType() ,( (Monster)_currentMonster )._attribute );
            _playerTakeDamage += takeDamage;
            if( takeDamage > 0 )
            {
                int randomX = Random.Range( -60, 60 );
                Vector3 effectPos = new Vector3( tempHeroStatus[i]._object.transform.position.x + randomX, tempHeroStatus[i]._object.transform.position.y, tempHeroStatus[i]._object.transform.position.z );
                effectManager.SetActiveEffect( _currentMonster.GetComponent<Monster>().GetMonsterType(), _currentMonster.GetComponent<Monster>().GetMonsterAtrribute(), effectPos );
            }
        }

        if( _playerTakeDamage > 0 )
        {
            UtilManager.instance.RequestObjectShake( bgController.gameObject, new Vector3( 5f, 5f, 0f ), 0.3f, false );
            UtilManager.instance.RequestObjectShake( ObjectPos.gameObject, new Vector3( 5f, 5f, 0f ), 0.3f, false );
            UtilManager.instance.RequestObjectShake( effectManager.gameObject, new Vector3( 5f, 5f, 0f ), 0.3f, false );

            effectManager.ActiveHitScreenEffect();
        }

        yield break;
    }

    public void ResetMonster()
    {
        if( _listMonster.Count < ( UtilityDefine.MAX_ADVENTURE_MONSTER_LENGTH + 1 ) )
        {
            SetMonster();

            for( int i = 0; i < _listMonster.Count; i++ )
                _listMonster[i].GetComponent<MonsterBase>().Reset();
        }
    }

    public void ResetMonster( MonsterBase mon )
    {
        _listMonster.Remove( mon );
        if( mon != null && mon.gameObject != null )
        {
            Destroy( mon.gameObject );
        }

        if( _monsterCount >= _monsterRanges.Length )
            return;
        if( _monsterCount >= _monsterIndexs.Length )
            return;

        if( LowData._MonsterLowData.DataInfoDic[_monsterIndexs[_monsterCount]].MonsterType_b == (int)E_MONSTER_BATTLE_TYPE.Chest )
        {
            CreateChest( _monsterIndexs[_monsterCount] );
        } else if( LowData._MonsterLowData.DataInfoDic[_monsterIndexs[_monsterCount]].MonsterType_b == (int)E_MONSTER_BATTLE_TYPE.Box )
        {
            CreateBox( _monsterIndexs[_monsterCount] );
        } else
        {
            CreateMonster( _monsterIndexs[_monsterCount] );
        }
    }

    private void ResetGame()
    {
        _leader.ResetObjects();

        if( _listMonster.Count > 0 )
        {
            for( int i = _listMonster.Count - 1; i >= 0; i-- )
            {
                MonsterBase mon = _listMonster[i];
                _listMonster.Remove( mon );
                Destroy( mon.gameObject );
            }
        }

        _listMonster.Clear();
    }

    public List<MonsterBase> GetMonsterList()
    {
        if( null == _listMonster )
            return null;

        return _listMonster;
    }

    void executeStateAction()
    {
        switch( _gameState )
        {
            case E_GAME_CONTROLL_STATE.GAME_WAIT:
                {
                    break;
                }

            case E_GAME_CONTROLL_STATE.GAME_INIT:
                {
                    bool bLoading = true;
                    for( int i = 0; i < loadingList.Count; i++ )
                    {
                        if( loadingList[i] == false )
                        {
                            bLoading = false;
                            break;
                        }
                    }

                    if( loadingList.Count > 0 )
                    {
                        if( bLoading )
                        {
                            loadingList.Clear();
                            UIManager.instance.EndLoading();
                            _gameState = E_GAME_CONTROLL_STATE.GAME_INIT_COMPLETE;
                            OnClickStart( _curUuidList );
                        }
                    }
                    break;
                }

            case E_GAME_CONTROLL_STATE.GAME_INIT_COMPLETE:
                {
                    break;
                }


            case E_GAME_CONTROLL_STATE.GAME_LOADING:
                {
                    bool bLoading = true;
                    for( int i = 0; i < loadingList.Count; i++ )
                    {
                        if( loadingList[i] == false )
                        {
                            bLoading = false;
                            break;
                        }
                    }

                    if( bLoading )
                    {
                        loadingList.Clear();
                        UIManager.instance.EndLoading();

                        for( int i = 0; i < _ingameHeros.Count; i++ )
                        {
                            if( _ingameHeros[i] != null )
                            {
                                _ingameHeros[i].SetPos();
                            }
                        }

                        _gameState = E_GAME_CONTROLL_STATE.GAME_LOADING_COMPLETE;

                        GameModeManager.instance.transform.localPosition = new Vector3( 0f, 0f, -200f );
                        transform.localPosition = new Vector3( 0f, 0f, -200f );
                        effectManager.SetBattleStart();
                        GameModeManager.instance.ChangeGameState( E_GAME_STATE.GAME_READY );

                        SceneManager.instance.EndLoading();
                    }
                    break;
                }
            case E_GAME_CONTROLL_STATE.GAME_LOADING_COMPLETE:
                {
                    //GameModeManager.instance.transform.localPosition = new Vector3( 2000f, 0f, -200f );
                    /*
                    if( _gameReadyPopup != null )
                        _gameReadyPopup.transform.localPosition = new Vector3( 2000f, 0f, -200f );
                    transform.localPosition = new Vector3( 0f, 0f, -200f );
                    MoveTop();
                    */
                    break;
                }

            case E_GAME_CONTROLL_STATE.GAME_READY:
                {
                    //시작 연출
                    //UpdateBackground();

                    MoveTop();

                    StartCoroutine( StartDelay() );
                    break;
                }

            case E_GAME_CONTROLL_STATE.GAME_PLAYING:
                {
                    GameModeManager.instance.inGameUI.TimerUpdate();

                    GameModeManager.instance.inGameUI.UpdateActiveSkillManager( (int)_leader._mp );

                    if( UtilityDefine.IsTutorialSkip == false )
                    {
                        if( _stageIndex == 10101 )
                        {
                            if( _isTutorialStart )
                            {
                                _isTutorialStart = false;
                                ChangeState( E_GAME_CONTROLL_STATE.GAME_WAIT );
                                UIManager.instance.OpenPopup( E_UIPOPUP_TYPE.Tutorials ).GetComponent<Tutorials>().Init( (int)E_TUTORIAL_TEXT_NUMBER.GameStart_min, (int)E_TUTORIAL_TEXT_NUMBER.GameStart_max, TutorialCallback );
                            }
                        }
                    }

                    if( _gameRunState == E_GAME_RUNMODE.RUN )
                    {
                        UpdatePlayerPosition();
                        UpdateMonsterPosition();
                        UpdateBackground();
                        UpdateTrapPosition();
                    } else if( _gameRunState == E_GAME_RUNMODE.BATTLE )
                    {
                        UpdateMonsterAttack();
                    }
                    break;
                }

            case E_GAME_CONTROLL_STATE.GAME_BOSSACTION:
                {
                    UpdateJustMoving();
                    UpdateBackground();
                    //UpdateTrapPosition();
                    break;
                }

            case E_GAME_CONTROLL_STATE.GAME_RESULT_READY:
                {
                    //UpdateBackground();
                    MoveGameEnd();
                    MoveTopSpaceShip();
                    MoveTopEnd();
                    break;
                }
        }
    }

    public void TutorialCallback()
    {
        ChangeState( E_GAME_CONTROLL_STATE.GAME_PLAYING );
    }

    IEnumerator StartDelay()
    {
        //앞으로 나오는 연출

        yield return new WaitForSeconds( 1.5f );

        GameModeManager.instance.ChangeGameState( E_GAME_STATE.GAME_RUN );
    }

    public E_GAME_RUNMODE GetGameMode()
    {
        return _gameRunState;
    }

    public void ChangeMode( E_GAME_RUNMODE mode )
    {
        if( _gameState == E_GAME_CONTROLL_STATE.GAME_WAIT )
            return;
        ChangeState( E_GAME_CONTROLL_STATE.GAME_WAIT );
        GameModeManager.instance._currentGameMode.GetPuzzleSystem().ChangeBlock( mode );

        if( mode == E_GAME_RUNMODE.RUN )
        {
            StartCoroutine( ChangeRun() );

            SoundManager.Instance.PlayBGM( BGMSoundNameList.Adventure_BGM );
        } else if( mode == E_GAME_RUNMODE.BATTLE )
        {

            if( _trapList != null && _trapList.Count > 0 )
            {
                for( int i = 0; i < _trapList.Count; i++ )
                {
                    if( _trapList[i].transform.position.x < MaxPos.position.x + 100f )
                    {
                        _trapList[i].GetComponent<Trap>().TrapDestroy( _leader );
                        //TrapDie( _trapList[i].transform.position, _trapList[i] );
                    }
                }
            }

            SoundManager.Instance.PlayBGM( BGMSoundNameList.Adventure_Battle_BGM );
            StartCoroutine( ChangeBattle() );
        }
    }

    private bool isBattleTuto = false;

    IEnumerator ChangeBattle()
    {
        GameModeManager.instance.inGameUI.ChangeMode( false );
        _leader.SetBattlePosition();
        _gameRunState = E_GAME_RUNMODE.BATTLE;
        _currentMonster.GetComponent<Monster>().SetMode( E_GAME_RUNMODE.BATTLE );
        bgController.SetMode( false );
        yield return new WaitForSeconds( 1.5f );

        _currentMonster.GetComponent<Monster>().AttackTimerActive();

        if( GameModeManager.instance._isPause == false )
        {
            ChangeState( E_GAME_CONTROLL_STATE.GAME_PLAYING );

            if( _stageIndex == 10102 )
            {
                if( isBattleTuto == false )
                {
                    isBattleTuto = true;
                    ChangeState( E_GAME_CONTROLL_STATE.GAME_WAIT );
                    UIManager.instance.OpenPopup( E_UIPOPUP_TYPE.Tutorials ).GetComponent<Tutorials>().Init( (int)E_TUTORIAL_TEXT_NUMBER.GameFight_min, (int)E_TUTORIAL_TEXT_NUMBER.GAmeFight_max, TutorialCallback );
                }
            }
        }

    }
    IEnumerator ChangeRun()
    {
        GameModeManager.instance.inGameUI.ChangeMode( true );
        _leader.SetAdventurePosition();
        _gameRunState = E_GAME_RUNMODE.RUN;
        bgController.SetMode( true );
        yield return new WaitForSeconds( 1.5f );
        if( GameModeManager.instance._isPause == false )
        {
            if( _listMonster.Count > 0 )
            {
                ChangeState( E_GAME_CONTROLL_STATE.GAME_PLAYING );
            } else
            {
                ChangeState( E_GAME_CONTROLL_STATE.GAME_RESULT_READY );
            }
        }
    }

    public void MoveTop()
    {
        UpdateJustMoving();
        _currentHP += 350f * Time.deltaTime;

        float prePosX = _leader.transform.localPosition.x;
        float cPosX = HPtoPositionX( _currentHP );
        _leader.transform.localPosition = new Vector3( cPosX, _leader.transform.localPosition.y, _leader.transform.localPosition.z );
    }

    public void MoveTopSpaceShip()
    {
        //_spaceShipObj

        float prePos = _spaceShipObj.transform.localPosition.x;
        float hp = PositionXtoHP( prePos );

        if( hp > 500f )
        {
            hp -= 500f * Time.deltaTime;

            if( hp < 500f )
            {
                hp = 500f;
                StartCoroutine( WaitSpaceShip() );
            }
        }

        float cPosX = HPtoPositionX( hp );
        _spaceShipObj.transform.localPosition = new Vector3( cPosX, _spaceShipObj.transform.localPosition.y, _spaceShipObj.transform.localPosition.z );
    }

    IEnumerator WaitSpaceShip()
    {
        _leader.transform.localPosition = new Vector3( _leader.transform.localPosition.x, 1000f, _leader.transform.localPosition.z );

        yield return new WaitForSeconds( 0.5f );

        if( false == _gameEndSpaceShip_go.gameObject.activeSelf )
        {
            _gameEndSpaceShip_go.gameObject.SetActive( true );
            _gameEndSpaceShip_go.Emit( 0 );
        }
        if( true == _gameEndSpaceShip_wait.gameObject.activeSelf )
        {
            _gameEndSpaceShip_wait.gameObject.SetActive( false );
        }

        yield return new WaitForSeconds( 2f );
        _isEnd = true;

    }


    public void MoveTopEnd()
    {
        UpdateJustMoving();
        if( _currentHP > 500f )
        {
            _currentHP -= 250f * Time.deltaTime;

            if( _currentHP < 500f )
            {
                _currentHP = 500f;
            }
        } else if( _currentHP < 500f)
        {
            _currentHP += 250f * Time.deltaTime;

            if( _currentHP > 500f )
            {
                _currentHP = 500f;
            }
        }

        float prePosX = _leader.transform.localPosition.x;
        float cPosX = HPtoPositionX( _currentHP );
        _leader.transform.localPosition = new Vector3( cPosX, _leader.transform.localPosition.y, _leader.transform.localPosition.z );
    }

    private bool _bSingle = true;
    public void GameResultReady( bool bSingle = true )
    {
        _bSingle = bSingle;
        GameModeManager.instance._currentGameMode.GetPuzzleSystem().transform.localPosition = new Vector3( 2000f, 0f, 0f );

        for( int i = 0; i < settingHeros.Length; i++ )
        {
            if( settingHeros[i] != null )
            {
                settingHeros[i].transform.parent = ObjectPos;
                UIManager.instance.SetSpineCenter( settingHeros[i], ObjectPos, Vector3.zero );
            }
        }

        _gameState = E_GAME_CONTROLL_STATE.GAME_PAUSE;
        if( _bSingle )
        {

            WebSender.instance.P_GAMEMODE_SINGLE_END_GAME( GameResultCallback );
        }
    }

    public void MoveGameEnd()
    {
        UpdateJustMoving();

        float prePos = _spaceShipObj.transform.localPosition.x;
        float hp = PositionXtoHP( prePos );
        if( hp == 500f )
        {
            return;
        }
        UpdateBackground( 300f );
    }

    IEnumerator EndMoveHeroes()
    {
        /*
        for( int i = 0; i < settingHeros.Length; i++ )
        {
            if( settingHeros[i] != null )
            {
                iTween.MoveTo( settingHeros[i], iTween.Hash( "islocal", true, "x", 550f - ( ( i ) * 180f ), "y", 50f, "easetype", iTween.EaseType.linear, "time", 3f ) );
            }
        }
        */
        yield return new WaitForSeconds( 0.5f );
         
    }
    
    public void GameResultCallback( JSONObject obj )
    {
        //Common 수정
        common.Response.EndGameResult result = DataManager.instance.LoadData<common.Response.EndGameResult>( obj );

        if( 30000 > _stageIndex && _stageIndex > 20000 )
        {
            int id = 7002;
            int achievementIndex = -1;
            if( true == DataManager.instance.isPossibleToCompleteAchievement( id, out achievementIndex ) )
                WebSender.instance.P_REQUEST_ACHIEVEMENT_COMPLETE( DataManager.instance.netdata.player._Achievement[achievementIndex]._UID, 1, null );
        } else if( 20000 > _stageIndex && _stageIndex > 10000 )
        {
            int id = 7001;
            int achievementIndex = -1;
            if( true == DataManager.instance.isPossibleToCompleteAchievement( id, out achievementIndex ) )
                WebSender.instance.P_REQUEST_ACHIEVEMENT_COMPLETE( DataManager.instance.netdata.player._Achievement[achievementIndex]._UID, 1, null );
        }

        if( GameModeManager.instance.inGameUI != null )
            GameModeManager.instance.inGameUI.SetResultVictor( result );

        if(null != NativeManager.instance)
        {
            int chapterNumber =  (_stageIndex - 10000) / 100;
            int stageNumber = (_stageIndex - _chapterIndex);

            //Debug.Log(" !!!!! " + chapterNumber + " , " + stageNumber);

            //에피소드 던젼 일때
            if (E_CHAPTER_TYPE.E_EPISODE_CHAPTER == (E_CHAPTER_TYPE)LowData._ChapterLowData.DataInfoDic[_chapterIndex].Chaptertype_b)
            {
                //에피소드 던젼 챕터 1 스테이지 1을 클리어 했을때
                string str_customTag = string.Format("S{0}_{1}complete", chapterNumber, stageNumber);
                NativeManager.instance.SetCustomTrackingActivity(str_customTag);
            }
        }

        /*
        common.Response.end_game_result gameResult = new common.Response.end_game_result();
        gameResult._Code = (common.ResultCode)obj["_Code"].n;
        gameResult._end_game_info = DataManager.instance.LoadData<common.Response.end_game_info>( obj["_end_game_info"] );

        List<ItemDic> itemList = DataManager.instance.LoadDataList<ItemDic>( obj["_itemdic"] );
        gameResult._itemdic = new Dictionary<System.Guid, common.Data.Item>();
        gameResult._itemdic.Clear();
        if( itemList.Count > 0 && itemList[0] != null )
        {
            if( itemList != null && itemList.Count > 0 )
            {
                for( int i = 0; i < itemList.Count; i++ )
                {
                    gameResult._itemdic.Add( itemList[i].key, itemList[i].Value );
                }
            }
        }

        if( GameModeManager.instance.inGameUI != null )
            GameModeManager.instance.inGameUI.SetResultVictor( gameResult );
         */
    }

    public void MoveHeros()
    {
        float speed = _fBackgroundSpeed;
        for( int i = 2; i < settingHeros.Length; i++ )
        {
            if( settingHeros[i] != null )
            {
                if( settingHeros[2].transform.localPosition.x > -200f )
                    settingHeros[i].transform.localPosition = new Vector3( settingHeros[i].transform.localPosition.x - speed * Time.deltaTime, settingHeros[i].transform.localPosition.y, settingHeros[i].transform.localPosition.z );
                else
                    settingHeros[i].transform.localPosition = new Vector3( -200f, settingHeros[i].transform.localPosition.y, settingHeros[i].transform.localPosition.z );
            }
        }
    }

    public void GameEndAction()
    {
        //서브캐릭터 출현

    }

    public void ActiveSkill( int skillIndex )
    {
        StartCoroutine( SkillAction( skillIndex ) );
    }

    IEnumerator SkillAction( int skillIndex )
    {
        GameModeManager.instance.SetGamePause( true );

        //화면 전환
        yield return new WaitForSeconds( 0.5f );
        //연출
        SkeletonAnimation effect = effectManager.GetSkillEffect( skillIndex );
        if( effect != null )
        {
            effect.transform.parent = ObjectPos;
            effect.AnimationName = "01";
            effect.timeScale = 0.7f;
            effect.loop = false;
            effect.Reset();
            effect.transform.localPosition = new Vector3( 500f, 0f, -50f );
            effect.transform.localScale = new Vector3( 2.0f, 2.0f, 1f );
            effectManager.ActiveHitScreenEffect( true );
        }

        //원위치
        yield return new WaitForSeconds( 2.0f );

        effectManager.ActiveHitScreenEffect( false );
        GameModeManager.instance.SetGamePause( false );
        //SkillManager.instance.ActiveSkill( skillIndex, _leader, GetMonstersInScreen(), ActiveSkillEnd );
    }

    public List<MonsterBase> GetMonstersInScreen()
    {
        List<MonsterBase> monList = new List<MonsterBase>();
        for( int i = 0; i < _listMonster.Count; i++ )
        {
            if( _listMonster[i]._type == MonsterType.Monster ||
                _listMonster[i]._type == MonsterType.Enemy )
            {
                if( _listMonster[i].transform.position.x < MaxPos.position.x + 100f )
                {
                    monList.Add( _listMonster[i] );
                }
            }
        }
        return monList;
    }

    public void ActiveSkillEnd()
    {
    }

    public void ChangeState( E_GAME_CONTROLL_STATE newState, bool isPause = false )
    {
        if( _isEnd )
            return;

        if( _gameState == newState )
            return;

        if( isPause )
        {
            newState = E_GAME_CONTROLL_STATE.GAME_PLAYING;
            _gameState = newState;
        } else
        {
            _gameState = newState;
        }
    }

    public E_GAME_CONTROLL_STATE GetInGameTopState()
    {
        return _gameState;
    }

    public void TakeDamageUpdateHealthBar()
    {
        _statePlace.UpdatePlayerHealthBar( true, _leader._maxHp, _leader._hp );
    }

    public void UpdateManaPoint()
    {
        _statePlace.UpdatePlyaerManaPointBar( true, _leader._maxMp, _leader._mp );
    }

    public void ActiveSkill( ActiveSkillIcon skillIcon )
    {
        if( _gameState != E_GAME_CONTROLL_STATE.GAME_PLAYING )
            return;
        SkillLowData.DataInfo skillData = skillIcon._skillData;

        StartActiveSkill( skillData.SkillEffect_s, skillIcon );
    }

    #region 공격스킬
    public void ActiveAttack( ActiveSkillIcon skillIcon, int activeType )
    {
        if( _currentMonster == null || _currentMonster._type != MonsterType.Monster )
        {
            return;
        }

        StartCoroutine( StartActiveAttackDamageRate( skillIcon, activeType ) );
    }


    IEnumerator StartActiveAttackDamageRate( ActiveSkillIcon skillIcon, int activeType )
    {
        //Time.timeScale = 0.2f;

        _leader.UseMp( skillIcon._mpCost );
        skillIcon.ResetCoolTime();
        float hitTime = 0.1f;
        if( skillIcon._skillEffect != null )
        {
            GameObject particle = Instantiate( skillIcon._skillEffect.gameObject );

            particle.SetActive( true );
            particle.transform.position = _currentMonster.transform.position;
            particle.transform.localPosition = new Vector3( particle.transform.localPosition.x + 80f, particle.transform.localPosition.y - 50f, -300f );
            particle.GetComponent<ParticleSystem>().Emit( 0 );
            hitTime = particle.GetComponent<ParticleDestroy>().GetHitTime();
            particle.GetComponent<ParticleDestroy>().SetTime();
        }

        yield return new WaitForSeconds( 0.2f );

        Time.timeScale = 1f;

        float attack = _leader._listHeroStatus[skillIcon._heroIndex]._attack;
        float damage = 0;
        if( activeType == 0 )
        {
            damage = attack + attack * ( skillIcon._skillValue * 0.001f );
        } else if( activeType == 1)
        {
            damage = skillIcon._skillValue;
        } else if( activeType == 2 )
        {
            float sumDamage = attack * ( skillIcon._skillValue * 0.001f );
            int heroCount = 0;
            for( int i = 0; i < _leader._listHeroStatus.Count; i++ )
            {
                if( _leader._listHeroStatus[i] != null && _leader._listHeroStatus[i]._data != null )
                {
                    if( _leader._listHeroStatus[i]._data.lowHero.BattleType_b == (int)skillIcon._type )
                    {
                        heroCount++;
                    }
                }
            }

            damage = attack + ( heroCount * (int)sumDamage );
        } else if( activeType == 3 )
        {
            float sumDamage =  skillIcon._skillValue;
            int heroCount = 0;
            for( int i = 0; i < _leader._listHeroStatus.Count; i++ )
            {
                if( _leader._listHeroStatus[i] != null && _leader._listHeroStatus[i]._data != null )
                {
                    if( _leader._listHeroStatus[i]._data.lowHero.BattleType_b == (int)skillIcon._type )
                    {
                        heroCount++;
                    }
                }
            }

            damage = attack + ( heroCount * (int)sumDamage );
        } else if( activeType == 4 )
        {
            float sumDamage = attack * ( skillIcon._skillValue * 0.001f );
            int heroCount = 0;
            for( int i = 0; i < _leader._listHeroStatus.Count; i++ )
            {
                if( _leader._listHeroStatus[i] != null && _leader._listHeroStatus[i]._data != null )
                {
                    if( _leader._listHeroStatus[i]._data.lowHero.Attribute_b == (int)skillIcon._attribute )
                    {
                        heroCount++;
                    }
                }
            }

            damage = attack + ( heroCount * (int)sumDamage );
        } else if( activeType == 5 )
        {
            float sumDamage =  skillIcon._skillValue;
            int heroCount = 0;
            for( int i = 0; i < _leader._listHeroStatus.Count; i++ )
            {
                if( _leader._listHeroStatus[i] != null && _leader._listHeroStatus[i]._data != null )
                {
                    if( _leader._listHeroStatus[i]._data.lowHero.Attribute_b == (int)skillIcon._attribute )
                    {
                        heroCount++;
                    }
                }
            }

            damage = attack + ( heroCount * (int)sumDamage );
        }

        StartCoroutine( SkillAttackDelay( hitTime, damage, skillIcon ) );
        GameModeManager.instance.inGameUI.UpdateActiveSkillManager( (int)_leader._mp );
    }
    #endregion
    public void ActiveDotAttackSkill( ActiveSkillIcon skillIcon )
    {
    }
    #region 힐
    public void ActiveHealSkill( ActiveSkillIcon skillIcon, int activeType )
    {
        if( _currentMonster == null || _currentMonster._type != MonsterType.Monster )
        {
            return;
        }

        StartCoroutine( StartActiveHeal( skillIcon, activeType ) );
    }

    IEnumerator StartActiveHeal( ActiveSkillIcon skillIcon, int damageType )
    {
        _leader.UseMp( skillIcon._mpCost );
        skillIcon.ResetCoolTime();
        float hitTime = 0.1f;
        if( skillIcon._skillEffect != null )
        {
            for( int i = 0; i < 4; i++ )
            {
                if( _leader._listHeroStatus[i] != null && _leader._listHeroStatus[i]._object != null )
                {
                    Vector3 objPos = _leader._listHeroStatus[i]._object.transform.position;

                    GameObject particle = Instantiate( skillIcon._skillEffect.gameObject );

                    particle.SetActive( true );
                    particle.transform.position = new Vector3( objPos.x, objPos.y, objPos.z - 1f);
                    particle.GetComponent<ParticleSystem>().Emit( 0 );
                    hitTime = particle.GetComponent<ParticleDestroy>().GetHitTime();
                    particle.GetComponent<ParticleDestroy>().SetTime();
                }
            }
        }

        yield return new WaitForSeconds( 0.01f );

        Time.timeScale = 1f;

        float recovery = _leader._listHeroStatus[skillIcon._heroIndex]._hpRecovery;
        float hpRecoveryPoint = 0;
        if( damageType == 0 )
        {
            hpRecoveryPoint = recovery * ( skillIcon._skillValue * 0.001f );

            effectManager.SetActiveEffect( skillIcon._type, skillIcon._attribute );
            _leader.TakeHealJustValue( (int)hpRecoveryPoint, skillIcon._hitCount );
        } else if( damageType == 1 )
        {
            hpRecoveryPoint = skillIcon._skillValue;

            effectManager.SetActiveEffect( skillIcon._type, skillIcon._attribute );
            _leader.TakeHealJustValue( (int)hpRecoveryPoint, skillIcon._hitCount );
        } else if( damageType == 2 )
        {
            //최대체력비례
            hpRecoveryPoint = skillIcon._skillValue;

            effectManager.SetActiveEffect( skillIcon._type, skillIcon._attribute );
            _leader.TakeHealPercent( (int)hpRecoveryPoint );
        } else if( damageType == 3 )
        {
            //도트
            hpRecoveryPoint = recovery * ( skillIcon._skillValue * 0.001f );
        } else if( damageType == 4 )
        {
            //도트
            hpRecoveryPoint = skillIcon._skillValue;
        }

        GameModeManager.instance.inGameUI.UpdateActiveSkillManager( (int)_leader._mp );
    }
    #endregion

    #region 마나회복
    public void ActiveChargeMP( ActiveSkillIcon skillIcon, int activeType )
    {
        if( _currentMonster == null || _currentMonster._type != MonsterType.Monster )
        {
            return;
        }

        StartCoroutine( StartActiveChargeMP( skillIcon, activeType ) );
    }

    IEnumerator StartActiveChargeMP( ActiveSkillIcon skillIcon, int damageType )
    {
        _leader.UseMp( skillIcon._mpCost );
        skillIcon.ResetCoolTime();
        float hitTime = 0.1f;
        if( skillIcon._skillEffect != null )
        {
            GameObject particle = Instantiate( skillIcon._skillEffect.gameObject );

            particle.SetActive( true );
            particle.transform.position = _currentMonster.transform.position;
            particle.transform.localPosition = new Vector3( particle.transform.localPosition.x + 80f, particle.transform.localPosition.y - 50f, -300f );
            particle.GetComponent<ParticleSystem>().Emit( 0 );
            hitTime = particle.GetComponent<ParticleDestroy>().GetHitTime();
            particle.GetComponent<ParticleDestroy>().SetTime();
        }

        yield return new WaitForSeconds( 0.01f );


        Time.timeScale = 1f;

        float recovery = _leader._listHeroStatus[skillIcon._heroIndex]._hpRecovery;
        float mpPoint = 0;
        if( damageType == 0 )
        {
            mpPoint = skillIcon._skillValue;

            Vector3 effectPos = new Vector3( _leader._listHeroStatus[0]._object.transform.position.x, _leader._listHeroStatus[0]._object.transform.position.y, _leader._listHeroStatus[0]._object.transform.position.z - 1f );
            effectManager.SetActiveEffect( skillIcon._type, skillIcon._attribute, effectPos );
            _leader.ChargeMP( (int)mpPoint );
        }

        GameModeManager.instance.inGameUI.UpdateActiveSkillManager( (int)_leader._mp );
    }
    #endregion

    public void ActiveDotHealSkill( ActiveSkillIcon skillIcon )
    {
    }

    public void ActiveBuffSkill( ActiveSkillIcon skillIcon )
    {
    }

    public void ActiveDotBuffSkill( ActiveSkillIcon skillIcon )
    {
    }

    public void ActiveCurseSkill( ActiveSkillIcon skillIcon )
    {
    }

    public void ActiveDotCurseSkill( ActiveSkillIcon skillIcon )
    {
    }

    IEnumerator SkillAttackDelay( float delayTime, float damage, ActiveSkillIcon skillIcon )
    {
        switch( skillIcon._type )
        {
            case HERO_TYPE.FIGHTER:
                SoundManager.Instance.Play( SoundNameList.ActiveAttack_Fighter );
                break;
            case HERO_TYPE.MAGICIAN:
                SoundManager.Instance.Play( SoundNameList.ActiveAttack_Magician );
                break;
            case HERO_TYPE.MECHANIC:
                SoundManager.Instance.Play( SoundNameList.ActiveAttack_Mechanic );
                break;
            case HERO_TYPE.WARRIOR:
                SoundManager.Instance.Play( SoundNameList.ActiveAttack_Warrior );
                break;
        }

        yield return new WaitForSeconds( delayTime );

        UtilManager.instance.RequestObjectShake( bgController.gameObject, new Vector3( 5f, 5f, 0f ), 0.3f, false );
        UtilManager.instance.RequestObjectShake( ObjectPos.gameObject, new Vector3( 5f, 5f, 0f ), 0.3f, false );
        UtilManager.instance.RequestObjectShake( effectManager.gameObject, new Vector3( 5f, 5f, 0f ), 0.3f, false );

        int xRandom = Random.Range( -60, 60 );
        if( _currentMonster != null )
        {
            Vector3 effectPos = new Vector3( _currentMonster.transform.position.x + xRandom, _currentMonster.transform.position.y, _currentMonster.transform.position.z );
            effectManager.SetActiveEffect( skillIcon._type, skillIcon._attribute, effectPos );
            EffectManager.instance.SetActiveDamage( E_TARGET_ACTOR_TYPE.MONSTER, _currentMonster.transform.position, (int)damage, false );

            if( _currentMonster != null )
            {
                if( _currentMonster._type == MonsterType.Monster )
                {
                    _currentMonster.Damaged( damage );
                }
            }
        }
    }

    private void StartActiveSkill( int skillType, ActiveSkillIcon skillIcon )
    {
        switch( skillType )
        {
            //배율 공격
            case 1001:
            case 1002:
            case 1003:
            case 1004:
            case 1005:
                ActiveAttack( skillIcon, 0 );
                break;

            //스킬값 공격
            case 1006:
            case 1007:
            case 1008:
            case 1009:
            case 1010:
                ActiveAttack( skillIcon, 1 );
                break;

            //파티 직업 갯수 * 공격력
            case 1011:
            case 1012:
            case 1013:
            case 1014:
                ActiveAttack( skillIcon, 2 );
                break;

            case 1015:
            case 1016:
            case 1017:
            case 1018:
                ActiveAttack( skillIcon, 3 );
                break;


            case 1019:
            case 1020:
            case 1021:
            case 1022:
            case 1023:
                ActiveAttack( skillIcon, 4 );
                break;

            case 1024:
            case 1025:
            case 1026:
            case 1027:
            case 1028:
                ActiveAttack( skillIcon, 5 );
                break;


            case 2001:
                break;
            case 2002:
                break;
            case 2003:
                break;
            case 2004:
                break;


            case 3001:
                ActiveHealSkill( skillIcon, 0 );
                break;
            case 3002:
                ActiveHealSkill( skillIcon, 1 );
                break;
            case 3003:
                ActiveHealSkill( skillIcon, 2 );
                break;


            case 4001:
                break;
            case 4002:
                break;

            case 5001:
                break;
            case 5002:
                break;
            case 5003:
                break;

            case 5009:
                ActiveChargeMP( skillIcon, 0 );
                break;

            case 6001:
                break;
            case 6002:
                break;
            case 6003:
                break;
            case 6004:
                break;
            case 6005:
                break;
            case 6006:
                break;
            case 6007:
                break;
            case 6008:
                break;
            case 6009:
                break;
            case 6010:
                break;
            case 6011:
                break;
            case 6012:
                break;
            case 6013:
                break;
            case 6014:
                break;
            case 6015:
                break;
            case 6016:
                break;
            case 6017:
                break;
            case 6018:
                break;
            case 6019:
                break;
            case 6020:
                break;
            case 6021:
                break;
            case 6022:
                break;
            case 6023:
                break;
            case 6024:
                break;
            case 6025:
                break;
            case 6026:
                break;
            case 6027:
                break;
            case 6028:
                break;
            case 6029:
                break;
            case 6030:
                break;
            case 6031:
                break;
            case 6032:
                break;
            case 6033:
                break;

            case 7001:
                break;
            case 7002:
                break;

            case 8001:
                break;
            case 8002:
                break;
            case 8003:
                break;
            case 8004:
                break;
            case 8005:
                break;
            case 8006:
                break;
            case 8007:
                break;
            case 8008:
                break;
            case 8009:
                break;
            case 8010:
                break;
            case 8011:
                break;
            case 8012:
                break;
            case 8013:
                break;
            case 8014:
                break;
            case 8015:
                break;
            case 8016:
                break;
            case 8017:
                break;
            case 8018:
                break;
            case 8019:
                break;
            case 8020:
                break;
            case 8021:
                break;

            default:
                break;

        }
    }

}