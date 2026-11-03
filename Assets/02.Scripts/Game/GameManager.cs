using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    Ready,
    Playing,
    Pause,
    GameOver,
}

public class GameManager : SingletonBehavior<GameManager>
{
    [SerializeField] private TextMeshProUGUI _stateText;
    [SerializeField] private GameObject _optionPopup;
    [SerializeField] private GameObject _gameOverPopup;

    private GameState _state = GameState.Ready;
    public GameState State => _state;

    private void Start()
    {
        StartCoroutine(StartSequence_Coroutine());
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (_state == GameState.Playing)
        {
            Pause();
        }

        else if (_state == GameState.Pause)
        {
            Resume();
        }
    }

    public void Pause()
    {
        if (_state != GameState.Playing) return;

        _state = GameState.Pause;
        Time.timeScale = 0f; // 유니티에서 흐르는 게임 시간을 멈춘다.
        _optionPopup.SetActive(true);
        CursorManager.Instance.UnlockCursor(); // 팝업 버튼을 누를 수 있게 커서를 보이게 한다.
    }

    // 옵션 팝업의 "계속하기" 버튼에서도 호출한다.
    public void Resume()
    {
        if (_state != GameState.Pause) return;

        _state = GameState.Playing;
        Time.timeScale = 1f;
        _optionPopup.SetActive(false);
        CursorManager.Instance.LockCursor();
    }

    private IEnumerator StartSequence_Coroutine()
    {
        _stateText.gameObject.SetActive(true);
        _stateText.text = "준비";

        yield return new WaitForSeconds(2f);

        _stateText.text = "시작";

        yield return new WaitForSeconds(0.5f);

        _state = GameState.Playing;
        _stateText.gameObject.SetActive(false);
    }

    public void GameOver()
    {
        if (_state == GameState.GameOver) return;

        _state = GameState.GameOver;

        // 팝업에 "게임 오버" 제목이 있으므로 상태 텍스트 대신 팝업을 띄운다.
        _gameOverPopup.SetActive(true);
        CursorManager.Instance.UnlockCursor(); // 팝업 버튼을 누를 수 있게 커서를 보이게 한다.
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}