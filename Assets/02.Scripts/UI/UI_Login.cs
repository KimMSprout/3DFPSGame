using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class UI_Login : MonoBehaviour
{
    private enum Mode
    {
        Login,
        Register,
    }

    [Header("입력 칸")]
    [SerializeField] private TMP_InputField _idInput;
    [SerializeField] private TMP_InputField _passwordInput;
    [SerializeField] private TMP_InputField _passwordConfirmInput;

    [Header("버튼")]
    [SerializeField] private Button _loginButton;
    [SerializeField] private GameObject _gotoRegisterLayout;
    [SerializeField] private Button _registerButton;
    [SerializeField] private GameObject _gotoLoginLayout;

    [Header("기타")]
    [SerializeField] private TextMeshProUGUI _messageText;

    private Mode _mode = Mode.Login;

    private PlayerPrefsAccountRepository _repository = new PlayerPrefsAccountRepository();

    private void Start()
    {
        Refresh();
    }

    public void GotoLogin()
    {
        _mode = Mode.Login;
        _messageText.text = string.Empty;
        Refresh();
    }

    public void GotoRegister()
    {
        _mode = Mode.Register;
        _messageText.text = string.Empty;
        Refresh();
    }

    public void Login()
    {
        string id = _idInput.text;
        string password = _passwordInput.text;

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password))
        {
            _messageText.text = "아이디와 비밀번호를 입력해주세요.";
            return;
        }

        if (_repository.CheckPassword(id, password))
        {
            SceneManager.LoadScene("GameScene");
        }
        else
        {
            _messageText.text = "아이디와 비밀번호를 확인해주세요.";
            return;
        }

        SceneManager.LoadScene("GameScene");
    }

    public void Register()
    {
        string id = _idInput.text;
        string password = _passwordInput.text;
        string passwordConfirm = _passwordConfirmInput.text;

        // TODO: ID와 비밀번호 규칙

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordConfirm))
        {
            _messageText.text = "아이디와 비밀번호를 입력해주세요.";
            return;
        }

        if (password != passwordConfirm)
        {
            _messageText.text = "비밀번호 확인이 맞지 않습니다.";
            return;
        }

        if (_repository.Exists(id))
        {
            _messageText.text = "이미 존재하는 ID입니다.";
            return;
        }

        _repository.Register(id, password);

        GotoLogin();
        _messageText.text = "회원가입이 완료되었습니다. 로그인 해주세요";
    }

    public void Refresh()
    {
        bool isLoginMode = _mode == Mode.Login;

        _loginButton.gameObject.SetActive(isLoginMode);
        _passwordConfirmInput.gameObject.SetActive(!isLoginMode);
        _gotoRegisterLayout.gameObject.SetActive(isLoginMode);
        _registerButton.gameObject.SetActive(!isLoginMode);
        _gotoLoginLayout.gameObject.SetActive(!isLoginMode);
    }
}