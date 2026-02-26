using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public event Action<eStateGame> StateChangedAction = delegate { };

    public enum eLevelMode
    {
        TIMER,
        MOVES,
    }

    public enum eStateGame
    {
        SETUP,
        MAIN_MENU,
        GAME_STARTED,
        PAUSE,
        GAME_OVER,
        GAME_WIN,
    }

    private eStateGame m_state;
    public eStateGame State
    {
        get { return m_state; }
        private set
        {
            m_state = value;

            StateChangedAction(m_state);
        }
    }

    public enum eAutoMode
    {
        NONE,
        WIN,
        LOSE,
    }

    private GameSettings m_gameSettings;

    private BoardController m_boardController;

    private UIMainManager m_uiMenu;

    private LevelCondition m_levelCondition;

    private void Awake()
    {
        State = eStateGame.SETUP;

        m_gameSettings = Resources.Load<GameSettings>(Constants.GAME_SETTINGS_PATH);

        m_uiMenu = FindObjectOfType<UIMainManager>();
        m_uiMenu.Setup(this);
    }

    void Start()
    {
        State = eStateGame.MAIN_MENU;
    }

    // Update is called once per frame
    void Update()
    {
        if (m_boardController != null)
            m_boardController.Update();
    }

    internal void SetState(eStateGame state)
    {
        State = state;

        if (State == eStateGame.PAUSE)
        {
            DOTween.PauseAll();
        }
        else
        {
            DOTween.PlayAll();
        }
    }

    public void LoadLevel(eLevelMode mode, eAutoMode eAutoMode = eAutoMode.NONE)
    {
        m_boardController = new GameObject("BoardController").AddComponent<BoardController>();
        m_boardController.StartGame(this, m_gameSettings);

        if (mode == eLevelMode.MOVES)
        {
            m_levelCondition = this.gameObject.AddComponent<LevelMoves>();
            m_levelCondition.Setup(
                m_gameSettings.LevelMoves,
                m_uiMenu.GetLevelConditionView(),
                m_boardController
            );
        }
        else if (mode == eLevelMode.TIMER)
        {
            m_levelCondition = this.gameObject.AddComponent<LevelTime>();
            m_levelCondition.Setup(
                m_gameSettings.LevelMoves,
                m_uiMenu.GetLevelConditionView(),
                this
            );
        }

        m_levelCondition.ConditionCompleteEvent += GameOver;
        m_levelCondition.ConditionCompleteEvent += GameWin;

        State = eStateGame.GAME_STARTED;
        transform.DOMove(Vector3.zero, 0.5f);

        switch (eAutoMode)
        {
            case eAutoMode.WIN:
                m_boardController.AutoWin();
                break;
            case eAutoMode.LOSE:
                m_boardController.AutoLose();
                break;
            default:
                break;
        }
    }

    public void GameOver()
    {
        StartCoroutine(WaitBoardController());
    }

    public void GameWin()
    {
        StartCoroutine(WaitWinBoardController());
    }

    internal void ClearLevel()
    {
        if (m_boardController)
        {
            m_boardController.Clear();
            Destroy(m_boardController.gameObject);
            m_boardController = null;
        }
    }

    private IEnumerator WaitBoardController()
    {
        while (m_boardController.IsBusy)
        {
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(1f);

        State = eStateGame.GAME_OVER;

        if (m_levelCondition != null)
        {
            m_levelCondition.ConditionCompleteEvent -= GameOver;

            Destroy(m_levelCondition);
            m_levelCondition = null;
        }
    }

    private IEnumerator WaitWinBoardController()
    {
        while (m_boardController.IsBusy)
        {
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForSeconds(1f);

        State = eStateGame.GAME_WIN;

        if (m_levelCondition != null)
        {
            m_levelCondition.ConditionCompleteEvent -= GameWin;

            Destroy(m_levelCondition);
            m_levelCondition = null;
        }
    }
}
