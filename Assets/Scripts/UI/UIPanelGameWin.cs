using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPanelGameWin : MonoBehaviour, IMenu
{
    [SerializeField]
    private Button btnClose;

    private UIMainManager m_mngr;

    private void Awake()
    {
        btnClose.onClick.AddListener(OnClickWin);
    }

    private void OnDestroy()
    {
        if (btnClose)
            btnClose.onClick.RemoveAllListeners();
    }

    private void OnClickWin()
    {
        m_mngr.ShowMainMenu();
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }

    public void Setup(UIMainManager mngr)
    {
        m_mngr = mngr;
    }

    public void Show()
    {
        this.gameObject.SetActive(true);
    }
}
