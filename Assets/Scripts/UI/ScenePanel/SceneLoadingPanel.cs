using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SceneLoadingPanel : MonoBehaviour 
{
    public tk2dSlicedSprite _bgSpr;
    public Text _textMsg;
    public EnergyBar _progressBar;
    public GameObject[] _loadingPage;

    public void SetPage()
    {
        for( int i = 0; i < _loadingPage.Length; i++ )
        {
            _loadingPage[i].SetActive( false );
        }

        int idx = Random.Range( 0, _loadingPage.Length );
        _loadingPage[idx].SetActive( true );
    }

    public void SetInitialLoadGauge(int minVal, int maxVal)
    {
        _progressBar.SetValueMin(minVal);
        _progressBar.SetValueMax(maxVal);
    }

    public void SetLoadGauge(int currVal)
    {
        _progressBar.SetValueCurrent(currVal);
    }

	
}
