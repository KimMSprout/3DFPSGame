using System.Text.RegularExpressions;

public class AccountPasswordSpecification
{
    // 비밀번호 : 대문자, 소문자, 숫자, 기호를 각각 하나 이상 포함한 8~30 글자 (공백 제외 출력 가능한 ASCII만 허용)
    private static readonly Regex PasswordRegex = new Regex(
        @"^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])(?=.*[!-/:-@\[-`{-~])[!-~]{8,30}\z");

    public string ErrorMessage => "비밀번호는 대문자, 소문자, 숫자, 기호를 포함한 8~30글자로 입력해주세요.";

    public bool isSatisfiedBy(string password)
    {
        return PasswordRegex.IsMatch(password);
    }
}