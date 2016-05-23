using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public delegate void LoadingCallbackFunction();

public class SceneManager : Immortal<SceneManager>
{
    public Camera BlackCamera;
    public static Resolution OriginalRes;
    public GameObject _loadPanel;
    private AsyncOperation _operator; //AsyncLoad를 위함.

    public int _currentChapterIndex;
    public int _currentStageIndex;

    private OptionManager _optionManager;

    public float GetScreenRatio()
    {
        return (float)OriginalRes.height / 1280.0f;
    }

    FSM.FSM<E_SCENEACTION_TYPE, E_SCENE_TYPE, SceneManager> FSM = null;
    public void Update()
    {
        /*if(null != _operator)
        {
            Debug.Log( " !!!!! " + _operator.isDone);
        }*/
    }

    protected override void Init()
    {
        base.Init();

        if( base.gameObject == null )
            return;

        Application.targetFrameRate = 60;

        Init_FSM();

        //게임중 옵션에 의해 Resolution이 바뀌기 때문에 처음시작시, 원본 ScreenSize 저장
        if( OriginalRes.Equals( default( Resolution ) ) )
            OriginalRes = Screen.currentResolution;

        //해상도 조절
        StartCoroutine( SetResolution( false ) );

        // Timeout 설정
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        _optionManager = new OptionManager();

        loadLoadingPanel();
    }

    void Init_FSM()
    {
        //< 초기 State 생성
        FSM = new FSM.FSM<E_SCENEACTION_TYPE, E_SCENE_TYPE, SceneManager>( this );
        FSM.AddState( E_SCENE_TYPE.START, gameObject.AddComponent<StartState>() );
        FSM.AddState( E_SCENE_TYPE.PATCH, gameObject.AddComponent<PatchState>() );
        FSM.AddState(E_SCENE_TYPE.MAIN, gameObject.AddComponent<MainState>());

        FSM.AddState( E_SCENE_TYPE.SINGLE, gameObject.AddComponent<SingleState>() );
        FSM.AddState( E_SCENE_TYPE.PVP, gameObject.AddComponent<PvpState>() );
        FSM.AddState( E_SCENE_TYPE.RAID, gameObject.AddComponent<RaidState>() );

        FSM.RegistEvent( E_SCENE_TYPE.START, E_SCENEACTION_TYPE.NEXT, E_SCENE_TYPE.PATCH );
        FSM.RegistEvent( E_SCENE_TYPE.PATCH, E_SCENEACTION_TYPE.NEXT, E_SCENE_TYPE.MAIN );

        FSM.RegistEvent( E_SCENE_TYPE.MAIN, E_SCENEACTION_TYPE.SINGLE, E_SCENE_TYPE.SINGLE );
        FSM.RegistEvent( E_SCENE_TYPE.MAIN, E_SCENEACTION_TYPE.PVP, E_SCENE_TYPE.PVP );
        FSM.RegistEvent( E_SCENE_TYPE.MAIN, E_SCENEACTION_TYPE.RAID, E_SCENE_TYPE.RAID );

        FSM.RegistEvent( E_SCENE_TYPE.SINGLE, E_SCENEACTION_TYPE.PREV, E_SCENE_TYPE.MAIN );
        FSM.RegistEvent( E_SCENE_TYPE.PVP, E_SCENEACTION_TYPE.PREV, E_SCENE_TYPE.MAIN );
        FSM.RegistEvent( E_SCENE_TYPE.RAID, E_SCENEACTION_TYPE.PREV, E_SCENE_TYPE.MAIN );

        //< 초기값 대입
        FSM.Enable( E_SCENE_TYPE.START );
    }

    public static void ChangeState( E_SCENEACTION_TYPE _action )
    {
        SceneManager.instance.FSM.ChangeState( _action );
    }

    /// <summary>
    /// 화면을 비율에 맞게 바꾸거나, 고정비율로 스크린 사이즈 변경R
    /// </summary>
    /// <param name="_fillScreen">Device 스크린을 채운 상태로 16:9를 표현할지 유무</param>
    IEnumerator SetResolution( bool _fillScreen = false )
    {
        if( null != BlackCamera )
            DestroyImmediate( BlackCamera.gameObject );

        if( _fillScreen )
        {
            ResolutionUtil.AdjuestCamRect( Camera.allCameras, OriginalRes.width, OriginalRes.height );

            // 해상도 16:9로 강제로 맞추기
            int calcHeight = Mathf.CeilToInt( ( 9f / 16f ) * (float)OriginalRes.width );
            Screen.SetResolution( OriginalRes.width, calcHeight, true );
        } else
        {
#if UNITY_ANDROID
            Screen.SetResolution( OriginalRes.width, OriginalRes.height, true );
#endif
            yield return null;
            
            // 9:16 비율로 해상도 변경하기.
            float scaleHeight = ResolutionUtil.AdjuestCamRect( Camera.allCameras, 720, 1280 );

            // 현재 해상도가 16:10에서 벗어난다면, Background카메라 생성하기
            if( false == Mathf.Approximately( 1.0f, scaleHeight ) )
                BlackCamera = ResolutionUtil.CreateBlackCamera( -3, true );
        }
    }

    public void ActionEvent( E_SCENEACTION_TYPE NewAction, bool bLoading = false )
    {
        FSM.ChangeState( NewAction );
    }

    void ShowLoading()
    {
        //if (LoadingPan == null)
        //    LoadingPan = UIManager.instance.OpenUI("UI/LoadingPanel", true, -100).GetComponent<LoadingPanel>();
    }

    public float CurrentRatio;
    public void SetLoadingInfo( string DescStr, float LoadingRatio, LoadingCallbackFunction callback = null )
    {
        CurrentRatio = LoadingRatio;

        ShowLoading();

        //LoadingPan.SetLoadingInfo(DescStr, LoadingRatio, callback);
    }

    public void RepairTimeScale( float a_Delay )
    {
        CancelInvoke( "RepTimeScale" );
        Invoke( "RepTimeScale", a_Delay );
    }

    public void RepTimeScale()
    {
        if( Time.timeScale != 1.0f )
        {
            Time.timeScale = 1.0f;
        }
    }

    public void GameEnd()
    {
        StartLoading();
        StopAllCoroutines();

        E_SCENE_TYPE sceneType = E_SCENE_TYPE.START;

        sceneType = findSceneType( E_SCENE_TYPE.MAIN.ToString() );

        if( E_SCENE_TYPE.START == sceneType )
            return;

        StartCoroutine( loadingEndGame( sceneType ) );
    }

    IEnumerator loadingEndGame( E_SCENE_TYPE sceneType )
    {
        yield return StartCoroutine(loadingSequenceInitiate());

        //UIManager.instance.DestroyMainPanel();
        ScenePanelManager.instance.RemovePanel( E_SCENE_TYPE.MAIN );
        UIManager.instance.RemoveAllPanels();
        UIManager.instance.RemoveAllPopups();
        if( GameModeManager.instance != null )
        {
            GameModeManager.instance.DestroyGame();
        } else
        {
            Destroy( InGameTopPlace.instance.gameObject );
        }

        if( PvpManager.instance != null )
        {
            PvpManager.instance.DestroyGame();
        } else
        {
            Destroy( InGameTopPlace.instance.gameObject );
        }

        yield return StartCoroutine( loadingScene( sceneType ) );

        yield return StartCoroutine(loadingSequenceComplete());
    }

    public void StartLoading( string sceneTypeName )
    {
        StopAllCoroutines();

        E_SCENE_TYPE sceneType = E_SCENE_TYPE.START;

        sceneType = findSceneType( sceneTypeName );

        if( E_SCENE_TYPE.START == sceneType )
            return;

        StartCoroutine( loadingSequenceStart( sceneType ) );
    }


    IEnumerator loadingSequenceStart( E_SCENE_TYPE sceneType)
    {
        yield return StartCoroutine(loadingSequenceInitiate());

        yield return StartCoroutine( loadingScene( sceneType ) );

        yield return StartCoroutine( loadingSequenceComplete() );
    }

    IEnumerator loadingSequenceInitiate()
    {
        /*if (true == isVisibleLoadPanel())
            return;*/

        if( null == UIManager.instance )
            yield break;

        UIManager.instance.ClearPopupPanels();
        /*
        _loadPanel.transform.SetParent(UIManager.instance.transform);
        _loadPanel.transform.localPosition = new Vector3( 0f, 0f, -2001f );
        _loadPanel.transform.localScale = Vector3.one;
        _loadPanel.transform.localRotation = Quaternion.identity;

        _loadPanel.gameObject.SetActive(true);
        _loadPanel.GetComponent<SceneLoadingPanel>().SetInitialLoadGauge(0, 100);
        
        */
        yield return new WaitForEndOfFrame();
    }

    IEnumerator loadingScene( E_SCENE_TYPE sceneType )
    {
        yield return new WaitForEndOfFrame();

        string sceneName = findSceneName( sceneType );

        _operator = Application.LoadLevelAsync( sceneName );
        _operator.allowSceneActivation = false;

        //Scene Loading.
        while( !_operator.isDone )
        {
            if( _operator.progress >= 0.9f )
            {
                //_loadPanel.GetComponent<SceneLoadingPanel>().SetLoadGauge(0);
                //_loadPanel.transform.GetComponent<tk2dUIProgressBar>().Value = 1.0f;
                break;
            } 
            else
                //_loadPanel.GetComponent<SceneLoadingPanel>().SetLoadGauge( (int)(_operator.progress * 10));
                //_loadPanel.transform.GetComponent<tk2dUIProgressBar>().Value = _operator.progress;

            yield return null;
        }


        _operator.allowSceneActivation = true;

        System.GC.Collect();
        Resources.UnloadUnusedAssets();

        yield return new WaitForEndOfFrame();

    }

    IEnumerator loadingSequenceComplete()
    {
        yield return new WaitForEndOfFrame();
    }

    public void StartLoading()
    {
        _loadPanel.transform.SetParent( UIManager.instance.transform );
        _loadPanel.transform.localPosition = new Vector3( 0f, 0f, -2001f );
        _loadPanel.transform.localScale = Vector3.one;
        _loadPanel.transform.localRotation = Quaternion.identity;

        _loadPanel.GetComponent<SceneLoadingPanel>().SetPage();
        _loadPanel.gameObject.SetActive( true );
        _loadPanel.GetComponent<SceneLoadingPanel>().SetInitialLoadGauge( 0, 100 );
    }

    public void UpdateLoadingPanelGage( int value )
    {
        _loadPanel.GetComponent<SceneLoadingPanel>().SetLoadGauge( (int)value );
    }

    public void EndLoading()
    {
        UpdateLoadingPanelGage( 100 );
        _loadPanel.transform.SetParent( this.transform );
        _loadPanel.transform.localPosition = new Vector3( 0f, 0f, -2001f );
        _loadPanel.transform.localScale = Vector3.one;
        _loadPanel.transform.localRotation = Quaternion.identity;
        _loadPanel.gameObject.SetActive( false );
    }

    string findSceneName( E_SCENE_TYPE sceneType )
    {
        string sceneName = "";

        switch( sceneType )
        {
            case E_SCENE_TYPE.START:
                {
                    break;
                }
            case E_SCENE_TYPE.PATCH:
                {
                    sceneName = "2.Patch";
                    break;
                }

            case E_SCENE_TYPE.MAIN:
                {
                    sceneName = "3.Main";
                    break;
                }

            case E_SCENE_TYPE.SINGLE:
                {
                    sceneName = "4.Single";
                    break;
                }

            case E_SCENE_TYPE.PVP:
                {
                    sceneName = "5.PvP";
                    break;
                }

            case E_SCENE_TYPE.RAID:
                {
                    sceneName = "6.Raid";
                    break;
                }

            default:
                {
                    break;
                }
        }

        return sceneName;
    }

    E_SCENE_TYPE findSceneType( string sceneTypeName )
    {
        E_SCENE_TYPE sceneType = E_SCENE_TYPE.START;

        if( true == sceneTypeName.Equals( "PATCH" ) )
        {
            sceneType = E_SCENE_TYPE.PATCH;
        } else if( true == sceneTypeName.Equals( "MAIN" ) )
        {
            sceneType = E_SCENE_TYPE.MAIN;
        } else if( true == sceneTypeName.Equals( "SINGLE" ) )
        {
            sceneType = E_SCENE_TYPE.SINGLE;
        } else if( true == sceneTypeName.Equals( "PVP" ) )
        {
            sceneType = E_SCENE_TYPE.PVP;
        } else if( true == sceneTypeName.Equals( "RAID" ) )
        {
            sceneType = E_SCENE_TYPE.RAID;
        }

        return sceneType;
    }

    void loadLoadingPanel()
    {
        string path = "Game/Common/LoadingPanel";
        _loadPanel = (GameObject)Object.Instantiate( (Object)Resources.Load( path ) );

        if( null == _loadPanel )
            return;

        _loadPanel.transform.SetParent( this.transform );
        _loadPanel.transform.localPosition = new Vector3( 0f, 0f, -2001f );
        _loadPanel.transform.localScale = Vector3.one;
        _loadPanel.transform.localRotation = Quaternion.identity;
        _loadPanel.gameObject.SetActive( false );
    }
}