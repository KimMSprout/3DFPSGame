using System.Text.RegularExpressions;

public class AccountIdSpecification
{
    // ID : 영문, 숫자, -, @, _를 사용한 3~20 글자
    private static readonly Regex IdRegex = new Regex(@"^[A-Za-z0-9\-@_]{3,20}\z");

    public string ErrorMessage => "아이디는 영문, 숫자, -, @, _를 사용해 3~20글자로 입력해주세요.";

    public bool IsSatisfiedBy(string id)
    {
        return IdRegex.IsMatch(id);
    }
}
