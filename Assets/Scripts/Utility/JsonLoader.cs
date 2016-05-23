using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;


public enum ASSETS
{
    json = 1,
    texture,
    android_asset,
    windows_asset,
    web_asset,
    ios_asset,
}
public class JsonLoader : Singleton<JsonLoader>
{
    static Dictionary<string,string>    LocalJosnDic = new Dictionary<string, string>();
    public Dictionary<string,string>    TextJsonDic = new Dictionary<string, string>();
	List<string> jsonlist = new List<string>();
	public List<string> Assetlist = new List<string>();
    public List<string> Texturelist = new List<string>();
    public bool     isDone = false;
    public bool isLoad = false;
    int             file_count = 0;
    int             localCount = 0;

    string  nativeAppdatapath = "";
    string  nativeAppSddatapath = "";
    int     randcount = 0;

    public bool isDownloadCheck = false;

    #region AboutAuth by Gubw

    string file_list_json = "file_list.json";

    public void GetAuthJson()
    {
        nativeAppdatapath = NativeManager.instance.AppDataPath;

#if !UNITY_EDITOR && UNITY_ANDROID
		nativeAppSddatapath = NativeManager.instance.AppSdDataPath;
#elif !UNITY_EDITOR && UNITY_IPHONE
        nativeAppSddatapath = NativeManager.instance.AppDataPath;
#elif UNITY_EDITOR
		nativeAppSddatapath = NativeManager.instance.AppDataPath;
#endif
        if (!Directory.Exists(nativeAppSddatapath))
        {
            Directory.CreateDirectory(nativeAppdatapath);
        }
        if (!Directory.Exists(nativeAppdatapath + "/json"))
        {
            Directory.CreateDirectory(nativeAppdatapath + "/json");
        }
        if (!Directory.Exists(nativeAppdatapath + "/texture"))
        {
            Directory.CreateDirectory(nativeAppdatapath + "/texture");
        }
        string fullfileName = "auth.json";

        TempUtil.StartCoroutine(GetAuthVersionCo(fullfileName, 0));
    }

    public void LoadResourcesFile()
    {
        TempUtil.StartCoroutine( GetAuthVersionCo( file_list_json, 2 ) );
    }

    IEnumerator SetWWWtext( string _str )
    {
        //randcount = UnityEngine.Random.Range(0,10000);
        //_str = WWW.EscapeURL(_str);// +  "?p=" + randcount;

        WWW www = new WWW( "file:///" + _str );

        yield return www;

        if ( www.isDone )
        {
            if ( www.error != null )
            {
                Debug.Log( www.error );
                OptionSetting._playerOption = new PlayerOption();

                string strPlayer = JsonConvert.SerializeObject( OptionSetting._playerOption );
                JsonLoader.SaveJsonFile( strPlayer, NativeManager.instance.AppDataPath + "/json/PlayerSetting.json" );
                Debug.Log( "New PlayerSetting" );
            }
            else
            {
                string savetext;
                savetext = www.text;

                OptionSetting._playerOption = JsonConvert.DeserializeObject<PlayerOption>( savetext );
                if ( OptionSetting._playerOption == null )
                {
                    OptionSetting._playerOption = new PlayerOption();

                    string strPlayer = JsonConvert.SerializeObject( OptionSetting._playerOption );
                    JsonLoader.SaveJsonFile( strPlayer, NativeManager.instance.AppDataPath + "/json/PlayerSetting.json" );
                }
            }
        }
    }
    // 로컬에 저장된 json 다 읽기
    void Local_LoadJsonFileCo()
    {
        string JsonPath = nativeAppSddatapath + "/json/";

        string[] FileNames = System.IO.Directory.GetFiles(JsonPath);
        for (int i = 0; i < FileNames.Length; ++i)
        {
            string pathName = FileNames[i];
            if (string.IsNullOrEmpty(pathName))
                continue;

            string[] splitName1 = pathName.Split(new char[] { '/', '\\' });
            if (splitName1.Length <= 0)
                continue;

            string[] splitName2 = splitName1[splitName1.Length - 1].Split('^');

            ++file_count;
            TempUtil.StartCoroutine(SetWWWtext(pathName, splitName2[0] + ".json"));
        }
    }

    /// <summary>
    /// 접속가능한 주소 데이터를 받아서 Json파일 다운 및 검사를 수행되도록 한다. // 0 auth , 1 json , 2 asset
    /// </summary>
    IEnumerator GetAuthVersionCo(string fileName , int type)
    {
		//Debug.Log(type + " | " + Auth.GetAssetUrl(ASSETS.json));
        randcount = UnityEngine.Random.Range(0, int.MaxValue);

		string wwwstr = "";
        if( type == 0 )
            wwwstr = Auth.authURL;
        else if( type == 1 )
            wwwstr = Auth.GetAssetUrl( ASSETS.json );
        else if( type == 2 )
        {
#if UNITY_STANDALONE_WIN
            wwwstr = Auth.GetAssetUrl(ASSETS.windows_asset);
#elif UNITY_ANDROID
            wwwstr = Auth.GetAssetUrl( ASSETS.android_asset );
#elif UNITY_IPHONE || UNITY_STANDALONE_OSX
			wwwstr = Auth.GetAssetUrl(ASSETS.ios_asset);
#elif UNITY_WEBPLAYER
            wwwstr = Auth.GetAssetUrl(ASSETS.web_asset);
#endif
        } else if( type == 3 )
            wwwstr = Auth.GetAssetUrl( ASSETS.texture );

		//Debug.Log(wwwstr + fileName + "?p=" + randcount);
        WWW www = new WWW(wwwstr + fileName + "?p=" + randcount);
        yield return www;
        if (www.error != null)
        {
            // TODO : 게임 시작시, 서버오류로 인해 이 곳에 도달하면 다른 처리하도록 해야함.
            Debug.LogWarning( "GetAuthVersionCo Load error : " + www.error + " address : " + wwwstr );
        }

        if (www.isDone && www.error == null)
        {
            if (type == 0)
            {
                LoadAuthFileCo(www.text);
            }
            else if (type == 1)
            {
                LoadJsonFileCo(www.text);
            }
            else if (type == 2)
            {
                LoadAssetFileCo(www.text);
            }
            else if(type == 3)
            {
                LoadTextureFileCo(www.text);
            }
            www.Dispose();
        }
    }
	
	void LoadJsonFileCo(string text)
	{		
		string[] jsonstrs = text.Replace("\"","").Split(',');
		for(int i=0; i < jsonstrs.Length; i++)
		{	
			NativeManager.instance.jsonAssetcount++;
			jsonlist.Add(jsonstrs[i]);
		}
		NativeManager.instance.jsoncount = NativeManager.instance.jsonAssetcount;

        TempUtil.StartCoroutine(GetAuthVersionCo(file_list_json, 2));
	}
	
	void LoadAssetFileCo(string text)
	{
		string[] jsonstrs = text.Replace("\"","").Split(',');
		for(int i=0; i < jsonstrs.Length; i++)
		{	
			NativeManager.instance.jsonAssetcount++;
			Assetlist.Add(jsonstrs[i]);
		}
		
		CompareToLocalVersion();

        TempUtil.StartCoroutine(GetAuthVersionCo(file_list_json, 3));
	}

    void LoadTextureFileCo(string text)
    {
        string[] jsonstrs = text.Replace("\"", "").Split(',');
        for (int i = 0; i < jsonstrs.Length; i++)
        {
            NativeManager.instance.jsonTexturecount++;
            Texturelist.Add(jsonstrs[i]);
        }
        NativeManager.instance.jsoncount = NativeManager.instance.jsonTexturecount;
    }

    string redirectUrl = string.Empty;
    void LoadAuthFileCo(string text)
    {
        JSONObject jsonObject = new JSONObject( text );
        string platformName = "";

        //Debug.Log(jsonObject);
#if UNITY_EDITOR || UNITY_STANDALONE
        platformName = "Unity";
        redirectUrl = "http://www.google.com/";
#elif !UNITY_EDITOR && UNITY_IPHONE
        platformName = "iPhone";
		version = GameDefine.BuildVersion;
        redirectUrl = "http://www.apple.com/";
#elif !UNITY_EDITOR && UNITY_ANDROID
        platformName = "Android";		
        redirectUrl = "http://www.google.com/";

#elif UNITY_STANDALONE
        platformName = "windows";
        version = GameDefine.BuildVersion;
        redirectUrl = "http://www.google.com/";
#endif
        if (jsonObject[platformName][Auth.Version] == null)
        {
            Debug.LogWarning("notfound version platformName " + platformName + " version " + Auth.Version + " file: " + jsonObject.ToString());
            //UIManager.instance.OpenPopup( E_UIPOPUP_TYPE.CheckPopup ).GetComponent<CheckPopup>().SetCheckPopupDesc( E_CHECK_POPUP_TYPE.YorN, "업데이트", "업데이트가 있습니다.\n마켓으로 바로 연결 하시겠습니까?", VersionDownload, VersionDownloadNo );
            UIManager.instance.OpenPopup( E_UIPOPUP_TYPE.CheckPopup ).GetComponent<CheckPopup>().SetCheckPopupDesc( E_CHECK_POPUP_TYPE.YorN, "업데이트", "업데이트가 있습니다.\n스토어에서 업데이트 해주세요", VersionDownload, VersionDownloadNo );
        }
        else
        {
            if (jsonObject[platformName][Auth.Version]["act"] != null)
            {
                Auth.SetActionUrl(jsonObject[platformName][Auth.Version]["act"].str);
            }

            if (jsonObject[platformName][Auth.Version]["asset_d"] != null)
            {
                Auth.SetAssetUrl(jsonObject[platformName][Auth.Version]["asset_d"].str);

#if UNITY_IPHONE
                 //EtceteraBinding.SetUrl(Auth.GetAssetUrl(ASSETS.photo));
#endif
            }

            if (jsonObject[platformName][Auth.Version]["chatting"] != null)
            {
                //ChattingClientSender.instance.SetChattingUrl(jsonObject[platformName][version]["chatting"].str);
            }

            if (jsonObject[platformName][Auth.Version]["use_crypt"] != null)
            {
                //Debug.Log(platformName);
                
            }
            else
            {
                // 암호화 기본값
                //Auth.SetUseCrypt(jsonObject[platformName][version]["use_crypt"].b);
            }

            //TempUtil.StartCoroutine(JsonCount());

            //if (!GameDefine.DownloadJson)
            //{
            //    // 로컬에 저장된 json 다 읽기
            //    Local_LoadJsonFileCo();
            //}
            //else			
            TempUtil.StartCoroutine(GetAuthVersionCo(file_list_json, 1));
        }
    }

    public void VersionDownload()
    {
        //Application.OpenURL( redirectUrl );
        Application.Quit();
    }

    public void VersionDownloadNo()
    {
        Debug.Log( "Quit" );
        Application.Quit();
    }

    #endregion

    public void SetLocaclVersionInfo()
    {
        //var buttons = new string[] { "OK" };
        //EtceteraBinding.showAlertWithTitleMessageAndButtons( "localFileName : " + Application.persistentDataPath, "small filepath :   " , buttons );

        string strFilePath = NativeManager.instance.AppDataPath + "/LocalJsonversion.txt";
        string readstr;
        if (File.Exists(strFilePath))
        {
            FileStream fs = new FileStream(strFilePath, FileMode.Open);
            StreamReader sr = new StreamReader(fs);

            string keyname = "";
            string valuestr = "";

            while ((readstr = sr.ReadLine()) != null)
            {
                string[] temstr = readstr.Split('\t');
                for (int i=0; i < temstr.Length; i++)
                {
                    if (temstr[i].CompareTo("") == 0)
                        break;
                    if (i == 0)
                    {
                        keyname = temstr[i];
                    }
                    else
                    {
                        valuestr = temstr[i];
                    }
                }

                if (keyname.Length > 0)
                {
                    if (LocalJosnDic.ContainsKey(keyname))
                        LocalJosnDic.Remove(keyname);
                    LocalJosnDic.Add(keyname, valuestr);
                }

            }
            fs.Close();
            sr.Close();
            fs.Dispose();
            sr.Dispose();
        }
        //		Debug.Log("CompareToLocalVersion start");
        //TempUtil.StartCoroutine(CompareToLocalVersion());
    }

    public JSONObject LoadJsonFile(string filename)
    {
        //Debug.Log("filename : " + filename);
        string jsontext = TextJsonDic[filename];

        return new JSONObject(jsontext);
    }

    public float GetProgress()
    {
        if (file_count == 0)
            return 0;

        return (float)((float)localCount / (float)file_count);
    }

    private void CompareToLocalVersion()
    {
        foreach (string json in jsonlist)
        {
            if (json.CompareTo("crossdomain.xml") == 0 || json.CompareTo("") == 0)
            {
                continue;
            }
            file_count++;
        }
#if UNITY_IPHONE
		// auth.json add
		//file_count++;
#endif
        
		TempUtil.StartCoroutine(JsonlistDownSet());

        isDone = true;
    }
	
	IEnumerator JsonlistDownSet()
	{
		yield return new WaitForFixedUpdate();
		foreach (string json in jsonlist)
        {

            if (json.CompareTo("crossdomain.xml") == 0 || json.CompareTo("") == 0)
            {
                continue;
            }

            string[] dates = json.Split('^');
			if(dates.Length > 1)
			{
				string realdate = "";
				string date = dates[1];
				realdate = date.Split('.')[0];
				string[] files =  Directory.GetFiles(nativeAppdatapath + "/json" , dates[0]+"^*");
				foreach(string file in files)
				{
					if(!file.Contains(realdate))
					{
						File.Delete(file);	
					}
				}	
			}
            TempUtil.StartCoroutine(DownJsonFileCo(json));
			jsonlist.Remove(json);
			TempUtil.StartCoroutine(JsonlistDownSet());
			break;
        }
	}

    /// <summary>
    /// 서버로부터 다운 받아야할 Json파일 리스트 목록을 얻어와서 검사한다.
    /// </summary>
    public IEnumerator JsonCount()
    {
        WWW www = new WWW(Auth.GetAssetUrl(ASSETS.json));
        yield return www;

        string strHtml = "";
        if (www.isDone)
        {
            strHtml = www.text;
            Dictionary<string,string> serverJosnCountDic = GetHtmlString(strHtml);

            foreach (KeyValuePair<string, string> pair in serverJosnCountDic)
            {
                if (pair.Key.CompareTo("crossdomain.xml") == 0 || pair.Key.CompareTo("") == 0)
                {
                    continue;
                }

                NativeManager.instance.jsonAssetcount++;
            }

            NativeManager.instance.jsoncount = NativeManager.instance.jsonAssetcount;
            TempUtil.StartCoroutine(AssetCount());
            www.Dispose();
        }
    }

    /// <summary>
    /// 서버로부터 다운 받아야할 Asset파일 리스트 목록을 얻어와서 검사&다운로드 한다.
    /// </summary>
    /// <returns></returns>
    public IEnumerator AssetCount()
    {
        string wwwStr = "";

        if ((Application.platform == RuntimePlatform.Android) || (Application.platform == RuntimePlatform.WindowsEditor) || (Application.platform == RuntimePlatform.OSXEditor))
        {
            wwwStr = Auth.GetAssetUrl(ASSETS.android_asset);
        }
#if UNITY_IPHONE
			wwwStr = Auth.GetAssetUrl(ASSETS.ios_asset);
#endif

        WWW www = new WWW(wwwStr);
        yield return www;
        string strHtml = "";
        if (www.isDone)
        {
            strHtml = www.text;
            Dictionary<string,string> serverAssetDic = GetHtmlString(strHtml);

            foreach (KeyValuePair<string, string> pair in serverAssetDic)
            {
                if (pair.Key.CompareTo("crossdomain.xml") == 0 || pair.Key.CompareTo("") == 0)
                {
                    continue;
                }

                NativeManager.instance.jsonAssetcount++;
            }
        }

        www.Dispose();
        SetLocaclVersionInfo();
    }    

    private void DelDiffFile()
    {
        string[] files = System.IO.Directory.GetFiles(nativeAppdatapath + "/json");

        foreach (string fi in files)
        {
            //Debug.Log(Application.persistentDataPath + "/json/" + fi);
            string temstr = fi.Replace("\\", "/");
            string[] strs = temstr.Split(new char[] { '/' });
            foreach (string str in strs)
            {
                if (str.Contains(".json"))
                {
                    //Debug.Log(str);
                    if (LocalJosnDic.ContainsKey(str))
                    {

                    }
                    else
                    {

                        File.Delete(fi);
                    }
                }
            }
        }
    }


    private static Dictionary<string, string> GetHtmlString(string url)
    {
        string strHtml = url;

        string[] str = strHtml.Split(new char[] { '\n' });
        strHtml = null;

        Dictionary<string,string> dictionary = new Dictionary<string, string>();

        foreach (string s in str)
        {
            if (s.Trim() != "")
                strHtml += s + ":";

            string[] ss = s.Split(new char[] { ' ' });
            string filename = "";
            string resultfailname = "";
            string datestr = "";

            foreach (string strSs in ss)
            {

                filename = strSs;
                filename = filename.Replace("<a", "");
                if (filename.Length > 0)
                {
                    if (filename.StartsWith("href"))
                    {
                        filename = filename.Replace(">", ":").Replace("</a:", "");

                        string[] s1 = filename.Split(new char[] { ':' });
                        foreach (string s1Str in s1)
                        {
                            if (s1Str.StartsWith("<") || s1Str.StartsWith("href") || s1Str.StartsWith("../"))
                            {
                            }
                            else
                            {
                                resultfailname = s1Str;
                            }
                        }

                    }
                    else
                    {
                        datestr += strSs;
                    }
                }
            }
            if (resultfailname.Length > 0)
            {

            }

            if (dictionary.ContainsKey(resultfailname))
            {
                dictionary.Remove(resultfailname);
            }


            dictionary.Add(resultfailname, datestr);

        }

        return dictionary;
    }

    private void DownJsonFile(string filename)
    {

        //string url = "http://121.138.174.182:8099/json/";
        //        string url = Auth.GetAssetUrl(ASSETS.json) + "/";

        //url +=  filename;

        //		Debug.Log("DownJsonFile : " + url);
#if UNITY_IPHONE
		string url = Auth.GetAssetUrl(ASSETS.json) + "/";
		if(File.Exists(nativeAppdatapath+ "/json/" + filename))
		{
//			Debug.Log("DownJsonFile : " + filename + "              " + url);
			//EtceteraBinding.IphoneJsonDownLoad(filename, url);
		}
		else
		{
//			Debug.Log("DownJsonFile : " + filename + "              " + url);
			//EtceteraBinding.IphoneNewJsonDownLoad(filename, url);
			//JsonLowDataSet();
		}
#endif
    }

    private IEnumerator DownJsonFileCo(string filename)
    {
        string url = Auth.GetAssetUrl(ASSETS.json);

        url += WWW.EscapeURL(filename);
        string[] dates = filename.Split('^');

        string savefile = dates[0] + ".json";
        if (File.Exists(nativeAppdatapath + "/json/" + filename))
        {
			yield return null;
			localCount++;
            
            TempUtil.StartCoroutine(SetWWWtext(nativeAppdatapath + "/json/" + filename, savefile));
        }
        else
        {         
            WWW www = new WWW(url);
            yield return www;

            if (www.isDone)
            {
                if (www.error != null)
                {
                }

                localCount++;

                string[] savefilenames = filename.Split('^');         
                SaveJsonFile(www.text, nativeAppdatapath + "/json/" + filename);
                TempUtil.StartCoroutine(SetWWWtext(nativeAppdatapath + "/json/" + filename, savefilenames[0] + ".json"));
                www.Dispose();
                JsonLowDataSet();

            }
            else
            {
                //Debug.Log("www not load : " + filename);
            }
        }
    }

    IEnumerator SetWWWtext(string _str, string filename)
    {
        //randcount = UnityEngine.Random.Range(0,10000);
        //_str = WWW.EscapeURL(_str);// +  "?p=" + randcount;

        WWW www = new WWW("file:///" + _str);
        yield return www;

        if (www.isDone)
        {
            if (www.error != null)
            {
                if (www.error.CompareTo("Null") == 0)
                {

                }
                else
                {
                    //Debug.Log("_str : " + _str);
                    //Debug.Log(www.error); 	
                }
            }
            //Debug.Log( "str : " + _str + "       filename : " + filename + "   " + www.text );
            string savetext;
            if (www.text.StartsWith("<html>"))
            {
                //Error <html><h1>Not Found</h1><p>The resource could not be found.</p></html>
                savetext = string.Empty;
            }
            else if (filename != "StageInfoList.json")
            {
                savetext = Decrypt( www.text );
            } else
            {
                savetext = www.text;
            }

            if (!TextJsonDic.ContainsKey(filename))
            {
                TextJsonDic.Add(filename, savetext);
                JsonLowDataSet();
                www.Dispose();
            }
            else
            {
                //Debug.LogError("error : " + filename);
            }
        }
    }

    public static void SaveJsonFile(byte[] byteData, string fileName)
    {

        try
        {

            if (File.Exists(fileName))
            {

                File.Delete(fileName);
            }
            FileStream fs = new FileStream(fileName, FileMode.Create);
            fs.Seek(0, SeekOrigin.Begin);

            //Debug.Log("bytedata : " + byteData);
            fs.Write(byteData, 0, byteData.Length);

            fs.Close();


        }
        catch (IOException e)
        {
            Debug.LogError(string.Format("SaveJsonFile error : {0} fileName {1} ", e.Message, fileName));
        }

    }

    public static void SaveJsonFile(string text, string fileName)
    {
        try
        {
            if (File.Exists(fileName))
            {
                //Debug.Log("del");
                File.Delete(fileName);
            }

            using (StreamWriter sw = new StreamWriter(fileName))
            {
                sw.Write(text);
            }
        }
        catch (IOException e)
        {
            Debug.LogError(string.Format("SaveJsonFile error : {0} fileName {1} ", e.Message, fileName));
        }

    }


    public void JsonLowDataSet()
    {
        //Debug.Log(TextJsonDic.Count + " | " + file_count);
        if ((TextJsonDic.Count == file_count) && isLoad == false)
        {
            AssetDownLoad.instance.SetLocaclVersionInfo();
            TextureDownLoad.instance.SetLocaclVersionInfo();
            LowData.LoadLowDataALL();
            isDownloadCheck = true;
            //TempUtil.StartCoroutine( JsonAllLoader() );
            //if (!GameDefine.DownloadJson)
            //    AssetDownLoad.instance.Local_LoadAssetFileCo();
        }
    }
    /*
    IEnumerator JsonAllLoader()
    {
        
        LowData._BlackmaketLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._ChapterLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._CostumeLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._GachabagLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._GachashopLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._GalaxyLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._HerolevelLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._HeroLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._ItembundleLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._ItemLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._LevelLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._MonsterLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._PostLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._PVPRankLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._PVPRankRewardLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._SkilllevelLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._StageLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._BlackmarketlvupLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._ClublvupLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._ItemstoragelvupLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._HighblackmarketlvupLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._CommandllvupLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._MoneycontainerlvupLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._StargatelvupLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._StargateplunderLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._TrainingcontainerlvupLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._skillLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._shipLowData.LoadLowData();
        yield return new WaitForEndOfFrame();
        LowData._highmoneycontainerlvupLowData.LoadLowData();

        LowData.bLoadDone = true;
        isLoad = true;
        
    }*/

    private byte[] iv = { 16,29,51,112,210,78,98,186 };
    private byte[] key = { 57,129,125,118,233,60,13,94,153,156,188,9,109,20,138,7,31,221,215,91,241,82,254,189 };
    public string Decrypt(string cryptedString)
    {
        if (string.IsNullOrEmpty(cryptedString))
        {
            Debug.Log
               ("The string which needs to be decrypted can not be null.");
            return null;
        }
        DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
        MemoryStream memoryStream = new MemoryStream
                (Convert.FromBase64String(cryptedString));
        CryptoStream cryptoStream = new CryptoStream(memoryStream,
            cryptoProvider.CreateDecryptor(iv, key), CryptoStreamMode.Read);
        StreamReader reader = new StreamReader(cryptoStream);
        return reader.ReadToEnd();
    }
}



